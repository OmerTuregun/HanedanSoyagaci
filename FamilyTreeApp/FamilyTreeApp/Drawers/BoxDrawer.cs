using SkiaSharp;
using FamilyTreeApp.Models;

namespace FamilyTreeApp.Drawers;

public static class BoxDrawer
{
    public const float X = 60;
    public const float Width = 2 * X;
    public const float Height = X;

    public static void Draw(SKCanvas canvas, Person person, float x, float y)
    {
        SKRect rect = new SKRect(x, y, x + Width, y + Height);

        // 1. Renkler
        SKPaint fillPaint = new SKPaint { Color = person.FillColor, Style = SKPaintStyle.Fill, IsAntialias = true };
        SKPaint borderPaint = new SKPaint { Color = person.BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = 3, IsAntialias = true };

        // 2. Şekil
        float cornerRadius = (person.Gender == Gender.Female) ? (Height / 2) : 5;
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, fillPaint);
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, borderPaint);

        // 3. Metin (Word Wrap / Satır Kaydırma Özellikli)
        DrawWrappedText(canvas, person.Name, rect);
    }

    private static void DrawWrappedText(SKCanvas canvas, string text, SKRect rect)
    {
        SKPaint textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 14, // Biraz küçülttük
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            TextAlign = SKTextAlign.Center
        };

        // Metni kelimelere böl
        var words = text.Split(' ');
        var lines = new List<string>();
        string currentLine = "";

        // Satırlara sığdırma mantığı
        foreach (var word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            float testWidth = textPaint.MeasureText(testLine);

            if (testWidth < rect.Width - 10) // 10px padding
            {
                currentLine = testLine;
            }
            else
            {
                lines.Add(currentLine);
                currentLine = word;
            }
        }
        lines.Add(currentLine);

        // Dikey ortalama hesabı
        float totalHeight = lines.Count * textPaint.TextSize;
        float startY = rect.MidY - (totalHeight / 2) + (textPaint.TextSize / 2); // Biraz matematik

        for (int i = 0; i < lines.Count; i++)
        {
            canvas.DrawText(lines[i], rect.MidX, startY + (i * textPaint.TextSize * 1.2f), textPaint);
        }
    }
}