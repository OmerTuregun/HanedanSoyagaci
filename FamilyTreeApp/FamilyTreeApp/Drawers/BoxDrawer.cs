using SkiaSharp;
using FamilyTreeApp.Models;

namespace FamilyTreeApp.Drawers;

public static class BoxDrawer
{
    // STANDART ÖLÇÜLER (Senin kuralların)
    public const float X = 60;              // Yükseklik (Bunu değiştirirsen hepsi büyür/küçülür)
    public const float Width = 2 * X;       // Genişlik (2X)
    public const float Height = X;          // Yükseklik (X)

    public static void Draw(SKCanvas canvas, Person person, float x, float y)
    {
        SKRect rect = new SKRect(x, y, x + Width, y + Height);

        // 1. KUTU DOLGUSU (İç Renk)
        SKPaint fillPaint = new SKPaint
        {
            Color = person.FillColor,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        // 2. KUTU ÇERÇEVESİ (Dış Çizgi)
        SKPaint borderPaint = new SKPaint
        {
            Color = person.BorderColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            IsAntialias = true
        };

        // ŞEKİL MANTIĞI (Erkek: Hafif Köşe, Kadın: Tam Oval)
        float cornerRadius = (person.Gender == Gender.Female) ? (Height / 2) : 5;

        // Önce içini boya, sonra çerçeveyi çiz
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, fillPaint);
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, borderPaint);

        // 3. METİN YAZMA (Hizalama düzeltildi)
        DrawCenteredText(canvas, person.Name, rect);
    }

    private static void DrawCenteredText(SKCanvas canvas, string text, SKRect rect)
    {
        SKPaint textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 16, // Font boyutu
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), // Kalın font
            TextAlign = SKTextAlign.Center
        };

        // Metnin dikey merkezini hesaplamak için ölçüm yapıyoruz
        SKRect textBounds = new SKRect();
        textPaint.MeasureText(text, ref textBounds);

        float textX = rect.MidX;
        // Metnin yüksekliğinin yarısı kadar aşağı kaydırarak tam ortalıyoruz
        float textY = rect.MidY - textBounds.MidY;

        canvas.DrawText(text, textX, textY, textPaint);
    }
}