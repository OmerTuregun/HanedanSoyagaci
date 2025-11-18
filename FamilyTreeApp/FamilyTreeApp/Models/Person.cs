using SkiaSharp;

namespace FamilyTreeApp.Models;

public class Person
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public List<string> Reigns { get; set; } = new List<string>();

    // Görsel Ayarlar
    public SKColor FillColor { get; set; } = SKColors.LightGray;
    public SKColor BorderColor { get; set; } = SKColors.Black;

    // --- YENİ: Tıklama Kontrolü İçin ---
    // Bu özellik veritabanına kaydedilmez, sadece o anki ekran konumu içindir.
    public SKRect ScreenRect { get; set; }
}