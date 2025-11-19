using SkiaSharp;
using FamilyTreeApp.Models;
using FamilyTreeApp.Drawers;

namespace FamilyTreeApp.Helpers;

public static class LayoutHelper
{
    private const float Gap = BoxDrawer.Height / 2; // X/2 boşluk kuralı
    private const float Padding = 20; // Kutular arası yatay boşluk
    private const float CollisionCheckMargin = 5; // Çarpışma kontrolü için margin

    public static SKRect CalculatePosition(
        Person sourcePerson,
        RelationshipType relation,
        List<Person> allPeople,
        List<Relationship> allRelationships)
    {
        float newX = 0;
        float newY = 0;

        // Kaynak kişinin mevcut konumu
        SKRect refRect = sourcePerson.ScreenRect;

        switch (relation)
        {
            case RelationshipType.ParentChild:
                newX = CalculateChildPosition(sourcePerson, allPeople, allRelationships, refRect, out newY);
                break;

            case RelationshipType.Spouse:
                newX = CalculateSpousePosition(sourcePerson, allPeople, allRelationships, refRect, out newY);
                break;
        }

        SKRect newRect = new SKRect(newX, newY, newX + BoxDrawer.Width, newY + BoxDrawer.Height);
        
        // Çarpışma kontrolü ve düzeltme
        newRect = AvoidCollisions(newRect, allPeople, sourcePerson);
        
        return newRect;
    }

    // Çocuk pozisyonu hesaplama (kardeşler için çarpışma önleme)
    private static float CalculateChildPosition(Person sourcePerson, List<Person> allPeople, 
        List<Relationship> allRelationships, SKRect refRect, out float newY)
    {
        // Bu kişinin ZATEN eklenmiş çocuklarını bulalım
        var existingChildrenIds = allRelationships
            .Where(r => r.FromPersonId == sourcePerson.Id && r.Type == RelationshipType.ParentChild)
            .Select(r => r.ToPersonId)
            .ToList();

        var existingChildren = allPeople
            .Where(p => existingChildrenIds.Contains(p.Id))
            .ToList();

        if (existingChildren.Any())
        {
            // Zaten çocuğu var: En sağdaki çocuğun da sağına koy (SIBLING LOGIC)
            float rightMostX = existingChildren.Max(c => c.ScreenRect.Right);
            newY = existingChildren.First().ScreenRect.Top;
            
            // Çarpışma önleme: En sağdaki boşluğu bul
            return FindRightmostAvailablePosition(rightMostX + Padding, newY, allPeople, sourcePerson);
        }
        else
        {
            // İlk çocuk: Parent'ın altına ortalanmış
            float parentMidX = refRect.MidX;
            newY = refRect.Bottom + Gap + Padding;
            
            // Çocuğun kutusunun başlangıç noktası (Left) = Parent Ortası - Kutu Genişliğinin Yarısı
            float centeredX = parentMidX - (BoxDrawer.Width / 2);
            
            // Çarpışma kontrolü yap
            return FindRightmostAvailablePosition(centeredX, newY, allPeople, sourcePerson);
        }
    }

    // Eş pozisyonu hesaplama (çoklu eşler için çarpışma önleme)
    private static float CalculateSpousePosition(Person sourcePerson, List<Person> allPeople,
        List<Relationship> allRelationships, SKRect refRect, out float newY)
    {
        // Mevcut eşleri bul
        var existingSpouseIds = allRelationships
            .Where(r => (r.FromPersonId == sourcePerson.Id || r.ToPersonId == sourcePerson.Id) 
                && r.Type == RelationshipType.Spouse)
            .Select(r => r.FromPersonId == sourcePerson.Id ? r.ToPersonId : r.FromPersonId)
            .ToList();

        var existingSpouses = allPeople
            .Where(p => existingSpouseIds.Contains(p.Id))
            .ToList();

        newY = refRect.Top; // Eşler aynı Y seviyesinde

        if (existingSpouseIds.Count == 0)
        {
            // İlk eş -> Sağ
            float rightX = refRect.Right + Gap;
            return FindRightmostAvailablePosition(rightX, newY, allPeople, sourcePerson);
        }
        else
        {
            // İkinci (veya daha fazla) -> Sol
            // Sol tarafta yer var mı kontrol et
            float leftX = refRect.Left - BoxDrawer.Width - Gap;
            
            // Sol tarafta çarpışma var mı?
            SKRect testRect = new SKRect(leftX, newY, leftX + BoxDrawer.Width, newY + BoxDrawer.Height);
            if (HasCollision(testRect, allPeople, sourcePerson))
            {
                // Sol tarafta yer yok, sağa kaydır (mevcut eşlerin en sağına)
                float rightMostX = existingSpouses.Max(s => s.ScreenRect.Right);
                return FindRightmostAvailablePosition(rightMostX + Padding, newY, allPeople, sourcePerson);
            }
            
            return leftX;
        }
    }

    // Çarpışma önleme: Verilen pozisyondan başlayarak en sağdaki boş pozisyonu bul
    private static float FindRightmostAvailablePosition(float startX, float y, List<Person> allPeople, Person excludePerson)
    {
        float currentX = startX;
        SKRect testRect = new SKRect(currentX, y, currentX + BoxDrawer.Width, y + BoxDrawer.Height);
        
        // Çarpışma yoksa direkt dön
        if (!HasCollision(testRect, allPeople, excludePerson))
        {
            return currentX;
        }

        // Çarpışma varsa, sağa doğru kaydırarak boş yer bul
        float step = BoxDrawer.Width + Padding;
        int maxIterations = 50; // Sonsuz döngüyü önle
        int iteration = 0;

        while (HasCollision(testRect, allPeople, excludePerson) && iteration < maxIterations)
        {
            currentX += step;
            testRect = new SKRect(currentX, y, currentX + BoxDrawer.Width, y + BoxDrawer.Height);
            iteration++;
        }

        return currentX;
    }

    // Çarpışma kontrolü: Verilen dikdörtgen başka bir kutuyla çarpışıyor mu?
    private static bool HasCollision(SKRect rect, List<Person> allPeople, Person excludePerson)
    {
        // Margin eklenmiş çarpışma kontrolü
        SKRect expandedRect = new SKRect(
            rect.Left - CollisionCheckMargin,
            rect.Top - CollisionCheckMargin,
            rect.Right + CollisionCheckMargin,
            rect.Bottom + CollisionCheckMargin
        );

        foreach (var person in allPeople)
        {
            if (person == excludePerson)
                continue;

            if (expandedRect.IntersectsWith(person.ScreenRect))
            {
                return true;
            }
        }

        return false;
    }

    // Ana çarpışma önleme metodu
    private static SKRect AvoidCollisions(SKRect rect, List<Person> allPeople, Person excludePerson)
    {
        if (!HasCollision(rect, allPeople, excludePerson))
        {
            return rect;
        }

        // Çarpışma varsa, sağa kaydır
        float newX = FindRightmostAvailablePosition(rect.Left, rect.Top, allPeople, excludePerson);
        return new SKRect(newX, rect.Top, newX + BoxDrawer.Width, rect.Bottom);
    }

    // Ekran ortası hesaplayıcı (Değişmedi)
    public static SKRect CalculateCenterPosition(float screenWidth, float screenHeight)
    {
        float x = (screenWidth - BoxDrawer.Width) / 2;
        float y = (screenHeight - BoxDrawer.Height) / 2;
        return new SKRect(x, y, x + BoxDrawer.Width, y + BoxDrawer.Height);
    }
}
