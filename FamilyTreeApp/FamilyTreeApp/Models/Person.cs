using SkiaSharp; // Renkler için gerekli

namespace FamilyTreeApp.Models;

public class Person
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public List<string> Reigns { get; set; } = new List<string>();

    // Görsel Ayarlar (Varsayılanlar)
    // Erkekse Mavi tonu, Kadınsa Pembe tonu (Değiştirilebilir)
    public SKColor FillColor { get; set; } = SKColors.LightGray;
    public SKColor BorderColor { get; set; } = SKColors.Black;
}