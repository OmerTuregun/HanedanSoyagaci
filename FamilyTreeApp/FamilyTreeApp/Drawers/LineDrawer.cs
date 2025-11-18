using SkiaSharp;

namespace FamilyTreeApp.Drawers;

public static class LineDrawer
{
    public static void DrawConnection(SKCanvas canvas, float x1, float y1, float x2, float y2)
    {
        SKPaint linePaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            IsAntialias = true
        };

        SKPath path = new SKPath();
        path.MoveTo(x1, y1);

        // Eğer X koordinatları eşitse (Dikey hat), sadece LineTo kullan
        if (Math.Abs(x1 - x2) < 1) // 1 pikselden az fark varsa düz kabul et
        {
            // Sadece dikey in
            path.LineTo(x2, y2);
        }
        else if (Math.Abs(y1 - y2) < 1) // Eğer Y koordinatları eşitse (Yatay hat)
        {
            // Sadece yatay git
            path.LineTo(x2, y2);
        }
        else
        {
            // Eğer hem X hem Y farklıysa (Manhattan/Köşeli bağlantı)
            float midY = (y1 + y2) / 2;
            path.LineTo(x1, midY); // Aşağı in
            path.LineTo(x2, midY); // Yana git
            path.LineTo(x2, y2);   // Hedefe in
        }

        canvas.DrawPath(path, linePaint);
    }
}