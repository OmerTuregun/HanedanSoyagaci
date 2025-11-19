using SkiaSharp;

namespace FamilyTreeApp.Models;

public class Person
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public List<string> Reigns { get; set; } = new List<string>();

    // Görsel Ayarlar
    // Tek renk veya çoklu renk desteği için object kullanıyoruz
    // SKColor veya List<SKColor> olabilir
    private object _fillColor = SKColors.LightGray;
    public object FillColor 
    { 
        get => _fillColor; 
        set => _fillColor = value; 
    }
    
    // Tek renk için helper property
    public SKColor GetSingleFillColor()
    {
        if (_fillColor is SKColor color)
            return color;
        if (_fillColor is List<SKColor> colors && colors.Count > 0)
            return colors[0];
        return SKColors.LightGray;
    }
    
    // Çoklu renk için helper property
    public List<SKColor> GetMultipleFillColors()
    {
        if (_fillColor is List<SKColor> colors)
            return colors;
        if (_fillColor is SKColor color)
            return new List<SKColor> { color };
        return new List<SKColor> { SKColors.LightGray };
    }
    
    public SKColor BorderColor { get; set; } = SKColors.Black;

    // --- YENİ: Tıklama Kontrolü İçin ---
    // Bu özellik veritabanına kaydedilmez, sadece o anki ekran konumu içindir.
    public SKRect ScreenRect { get; set; }
}