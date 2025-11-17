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

        // Basit bir "Manhattan" çizgisi (Önce aşağı, sonra yana, sonra aşağı)
        // Köşeleri yuvarlatmak için SKPath kullanıyoruz.

        SKPath path = new SKPath();
        path.MoveTo(x1, y1); // Başlangıç (Üst kutunun altı)

        float midY = (y1 + y2) / 2; // İki kutunun tam ortası

        // Eğer köşe yuvarlatma istiyorsan burası biraz matematik gerektirir.
        // Şimdilik düz çizgilerle iskeleti kuralım, sonra "ArcTo" ile yuvarlatırız.

        path.LineTo(x1, midY); // Aşağı in
        path.LineTo(x2, midY); // Yana git
        path.LineTo(x2, y2);   // Hedefe in

        canvas.DrawPath(path, linePaint);
    }
}