using SkiaSharp;
using FamilyTreeApp.Models;
using FamilyTreeApp.Drawers;

namespace FamilyTreeApp.Helpers;

public static class LayoutHelper
{
    // NOT: Artık parametre olarak tüm insanları ve ilişkileri de alıyoruz ki
    // "Bu adamın başka çocuğu var mı?" diye bakabilelim.
    public static SKRect CalculatePosition(
        Person sourcePerson,
        RelationshipType relation,
        List<Person> allPeople,
        List<Relationship> allRelationships)
    {
        float gap = BoxDrawer.Height / 2; // X/2 boşluk kuralı
        float padding = 20; // Kutular arası yatay boşluk

        float newX = 0;
        float newY = 0;

        // Kaynak kişinin mevcut konumu
        SKRect refRect = sourcePerson.ScreenRect;

        switch (relation)
        {
            case RelationshipType.ParentChild:
                // 1. Önce bu kişinin ZATEN eklenmiş çocuklarını bulalım
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

                    newX = rightMostX + padding; // Yanına koy
                    newY = existingChildren.First().ScreenRect.Top;
                }
                else
                {
                    float parentMidX = refRect.MidX;

                    // Çocuğun kutusunun başlangıç noktası (Left) = Parent Ortası - Kutu Genişliğinin Yarısı
                    newX = parentMidX - (BoxDrawer.Width / 2);
                    newY = refRect.Bottom + gap + padding;
                }
                break;

            case RelationshipType.Spouse:
                // Mevcut eşleri bul
                var existingSpouseIds = allRelationships
                    .Where(r => r.FromPersonId == sourcePerson.Id && r.Type == RelationshipType.Spouse)
                    .Select(r => r.ToPersonId)
                    .ToList();

                // BASİT MANTIK: İlk eş SAĞA, İkinci eş SOLA
                if (existingSpouseIds.Count == 0)
                {
                    // İlk eş -> Sağ
                    newX = refRect.Right + gap;
                    newY = refRect.Top;
                }
                else
                {
                    // İkinci (veya daha fazla) -> Sol
                    // Not: Sol tarafta yer var mı diye kontrol etmiyoruz şimdilik, direkt sola koyuyoruz.
                    newX = refRect.Left - BoxDrawer.Width - gap;
                    newY = refRect.Top;
                }
                break;
        }

        return new SKRect(newX, newY, newX + BoxDrawer.Width, newY + BoxDrawer.Height);
    }

    // Ekran ortası hesaplayıcı (Değişmedi)
    public static SKRect CalculateCenterPosition(float screenWidth, float screenHeight)
    {
        float x = (screenWidth - BoxDrawer.Width) / 2;
        float y = (screenHeight - BoxDrawer.Height) / 2;
        return new SKRect(x, y, x + BoxDrawer.Width, y + BoxDrawer.Height);
    }
}