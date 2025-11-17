using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using FamilyTreeApp.Models;
using FamilyTreeApp.Drawers;

namespace FamilyTreeApp;

public partial class MainPage : ContentPage
{
    // Verilerimizi burada tutalım
    List<Person> people = new List<Person>();
    List<Relationship> relationships = new List<Relationship>();

    public MainPage()
    {
        InitializeComponent();
        InitializeData(); // Verileri hazırla
    }

    private void InitializeData()
    {
        // --- 1. KİŞİLERİ OLUŞTUR ---
        var ahmed = new Person
        {
            Name = "I. Ahmed",
            Gender = Gender.Male,
            FillColor = SKColors.LightBlue // Erkek Rengi
        };

        var kosem = new Person
        {
            Name = "Kösem Sultan",
            Gender = Gender.Female,
            FillColor = SKColors.Pink // Kadın Rengi
        };

        // Kösem'i biraz uzağa ve aşağı koyacağız (Simülasyon)

        people.Add(ahmed);
        people.Add(kosem);

        // --- 2. İLİŞKİ OLUŞTUR (Örn: Baba -> Kız gibi çizelim şimdilik test için) ---
        relationships.Add(new Relationship
        {
            FromPersonId = ahmed.Id,
            ToPersonId = kosem.Id,
            Type = RelationshipType.ParentChild
        });
    }

    private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        SKCanvas canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        // --- KOORDİNAT HESABI (BASİT) ---
        // Gerçek uygulamada burası otomatik hesaplanacak (Layout Algoritması).
        // Şimdilik "Kutular arası X/2 mesafe" kuralını elle yapıyoruz.

        float startX = 100;
        float startY = 50;

        float gap = BoxDrawer.Height / 2; // Kural: X/2 boşluk

        // 1. Kişinin Koordinatları
        float p1_X = startX;
        float p1_Y = startY;

        // 2. Kişinin Koordinatları (Bir alt nesil)
        // Yeri: Üstteki Y + Kutu Boyu + Boşluk
        float p2_X = startX + 150; // Biraz sağa kayık olsun ki çizgi belli olsun
        float p2_Y = p1_Y + BoxDrawer.Height + gap;

        // --- ÇİZİM ---

        // 1. Önce Çizgileri Çiz (Kutuların altında kalsın diye)
        // Ahmed'in Alt Ortasından -> Kösem'in Üst Ortasına
        float startLineX = p1_X + (BoxDrawer.Width / 2);
        float startLineY = p1_Y + BoxDrawer.Height;

        float endLineX = p2_X + (BoxDrawer.Width / 2);
        float endLineY = p2_Y;

        LineDrawer.DrawConnection(canvas, startLineX, startLineY, endLineX, endLineY);

        // 2. Sonra Kutuları Çiz
        BoxDrawer.Draw(canvas, people[0], p1_X, p1_Y); // Ahmed
        BoxDrawer.Draw(canvas, people[1], p2_X, p2_Y); // Kösem
    }
}