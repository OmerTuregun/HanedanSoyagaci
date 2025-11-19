using SkiaSharp;
using FamilyTreeApp.Models;

namespace FamilyTreeApp.Drawers;

public static class BoxDrawer
{
    public const float X = 60;
    public const float Width = 2 * X;
    public const float Height = X;
    private const float Padding = 5f;
    private const float MinFontSize = 8f;
    private const float MaxFontSize = 16f;

    public static void Draw(SKCanvas canvas, Person person, float x, float y)
    {
        SKRect rect = new SKRect(x, y, x + Width, y + Height);

        // 1. ÇOKLU RENK DESTEĞİ
        DrawMultiColorFill(canvas, rect, person);

        // 2. Şekil (Border)
        float cornerRadius = (person.Gender == Gender.Female) ? (Height / 2) : 5;
        SKPaint borderPaint = new SKPaint { Color = person.BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = 3, IsAntialias = true };
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, borderPaint);

        // 3. Metin ve Tarih (Dinamik Font Boyutlandırma ile)
        DrawTextWithReigns(canvas, person, rect);
    }

    // Çoklu renk doldurma: Dikey veya yatay bölme
    private static void DrawMultiColorFill(SKCanvas canvas, SKRect rect, Person person)
    {
        var colors = person.GetMultipleFillColors();
        
        if (colors.Count == 1)
        {
            // Tek renk: Normal doldurma
            SKPaint fillPaint = new SKPaint { Color = colors[0], Style = SKPaintStyle.Fill, IsAntialias = true };
            float cornerRadius = (person.Gender == Gender.Female) ? (Height / 2) : 5;
            canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, fillPaint);
        }
        else if (colors.Count > 1)
        {
            // Çoklu renk: Dikey bölme (her renk eşit alan kaplar)
            float segmentHeight = rect.Height / colors.Count;
            float cornerRadius = (person.Gender == Gender.Female) ? (Height / 2) : 5;
            
            for (int i = 0; i < colors.Count; i++)
            {
                float segmentY = rect.Top + (i * segmentHeight);
                SKRect segmentRect = new SKRect(rect.Left, segmentY, rect.Right, segmentY + segmentHeight);
                
                SKPaint fillPaint = new SKPaint { Color = colors[i], Style = SKPaintStyle.Fill, IsAntialias = true };
                
                // Basitleştirilmiş: Tüm segmentler düz dikdörtgen
                // (Yuvarlatma sadece dış border'da uygulanıyor)
                canvas.DrawRect(segmentRect, fillPaint);
            }
        }
    }

    // İsim ve tarihleri dinamik font boyutu ile çiz
    private static void DrawTextWithReigns(SKCanvas canvas, Person person, SKRect rect)
    {
        // Tüm metin içeriğini hazırla
        var textLines = new List<string>();
        textLines.Add(person.Name);
        
        // Tarihleri ekle (örn: "1451-52 / 1454-55")
        if (person.Reigns != null && person.Reigns.Count > 0)
        {
            string reignsText = string.Join(" / ", person.Reigns);
            if (!string.IsNullOrWhiteSpace(reignsText))
            {
                textLines.Add(reignsText);
            }
        }

        // Dinamik font boyutu hesapla
        float fontSize = CalculateOptimalFontSize(canvas, textLines, rect);
        
        SKPaint textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = fontSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            TextAlign = SKTextAlign.Center
        };

        // Metinleri çiz
        float lineHeight = textPaint.FontSpacing;
        float totalHeight = textLines.Count * lineHeight;
        float startY = rect.MidY - (totalHeight / 2) + (lineHeight / 2);

        for (int i = 0; i < textLines.Count; i++)
        {
            string line = textLines[i];
            // Her satırı kendi içinde wrap et
            DrawWrappedLine(canvas, line, rect.MidX, startY + (i * lineHeight), textPaint, rect.Width - (Padding * 2));
        }
    }

    // Dinamik font boyutu hesaplama: Kutu boyutunu sabit tut, font'u küçült
    private static float CalculateOptimalFontSize(SKCanvas canvas, List<string> textLines, SKRect rect)
    {
        float testSize = MaxFontSize;
        float minSize = MinFontSize;
        float optimalSize = testSize;
        
        // Binary search benzeri yaklaşım
        for (int iteration = 0; iteration < 10; iteration++)
        {
            SKPaint testPaint = new SKPaint
            {
                TextSize = testSize,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
                TextAlign = SKTextAlign.Center
            };

            bool fits = true;
            float availableWidth = rect.Width - (Padding * 2);
            float availableHeight = rect.Height - (Padding * 2);
            float lineHeight = testPaint.FontSpacing;
            float totalHeight = textLines.Count * lineHeight;

            // Yükseklik kontrolü
            if (totalHeight > availableHeight)
            {
                fits = false;
            }
            else
            {
                // Genişlik kontrolü (her satır için)
                foreach (var line in textLines)
                {
                    float lineWidth = testPaint.MeasureText(line);
                    if (lineWidth > availableWidth)
                    {
                        fits = false;
                        break;
                    }
                }
            }

            if (fits)
            {
                optimalSize = testSize;
                minSize = testSize;
                testSize = (testSize + MaxFontSize) / 2;
            }
            else
            {
                testSize = (minSize + testSize) / 2;
            }

            // Çok küçükse dur
            if (testSize < MinFontSize + 0.5f)
                break;
        }

        return Math.Max(MinFontSize, Math.Min(MaxFontSize, optimalSize));
    }

    // Tek satırı wrap et (çok uzunsa)
    private static void DrawWrappedLine(SKCanvas canvas, string text, float centerX, float y, SKPaint paint, float maxWidth)
    {
        float textWidth = paint.MeasureText(text);
        
        if (textWidth <= maxWidth)
        {
            // Tek satırda sığıyor
            canvas.DrawText(text, centerX, y, paint);
        }
        else
        {
            // Kelimelere böl ve wrap et
            var words = text.Split(' ');
            var lines = new List<string>();
            string currentLine = "";

            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                float testWidth = paint.MeasureText(testLine);

                if (testWidth < maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                        lines.Add(currentLine);
                    currentLine = word;
                }
            }
            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine);

            // Wrap edilmiş satırları çiz
            float lineHeight = paint.FontSpacing;
            float startY = y - ((lines.Count - 1) * lineHeight / 2);
            
            for (int i = 0; i < lines.Count; i++)
            {
                canvas.DrawText(lines[i], centerX, startY + (i * lineHeight), paint);
            }
        }
    }
}
