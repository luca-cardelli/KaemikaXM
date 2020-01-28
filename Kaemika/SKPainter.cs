using SkiaSharp;

namespace Kaemika
{

    // ====  SKIASHARP (SHARED) GRAPHICS =====

    // To convert SkiaSharp color to Xamarin.Forms color :
    // SkiaSharp.Views.Forms.Extensions.ToFormsColor(legend[i].color),

    public class SKColorer : PlatformTexter, Colorer {
        public /*interface Colorer*/ SKTypeface font {
            get {
                return SKTypeface.FromFamilyName(this.fontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            }
        }
        public /*interface Colorer*/ SKTypeface fixedFont { 
            get {
                return SKTypeface.FromFamilyName(this.fixedFontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            }
        }
        public /*interface Colorer*/ SKPaint TextPaint(SKTypeface typeface, float textSize, SKColor color) {
            return new SKPaint { Typeface = typeface, IsStroke = false, Style = SKPaintStyle.Fill, TextSize = textSize, Color = color, IsAntialias = true };
        }
        public /*interface Colorer*/ SKPaint FillPaint(SKColor color) { 
            return new SKPaint { IsStroke = false, Style = SKPaintStyle.Fill, Color = color, IsAntialias = true };
        }
        public /*interface Colorer*/ SKPaint LinePaint(float strokeWidth, SKColor color) {
            return new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth, Color = color, IsAntialias = true };
        }
        public virtual /*interface Colorer*/ SKRect MeasureText(string text, SKPaint paint) {
            if (string.IsNullOrEmpty(text)) return new SKRect(0, 0, 0, 0); // or MeasureText will crash
            var bounds = new SKRect();
            float length = paint.MeasureText(text, ref bounds);
            return bounds;
        }
    }

    public class SKPainter : SKColorer, Painter {
        //### more painting routines are in GraphLayout.cs e.g. for Splines
        protected SKCanvas canvas;
        public SKPainter(SKCanvas canvas) : base() {
            this.canvas = canvas;
        }
        public /*interface Painter*/ void Clear(SKColor background) {
            canvas.Clear(background);
        }
        public /*interface Painter*/ void DrawRect(SKRect rect, SKPaint paint) {
            canvas.DrawRect(rect, paint);
        }
        public /*interface Painter*/ void DrawRoundRect(SKRect rect, float padding, SKPaint paint) {
            canvas.DrawRoundRect(rect, padding, padding, paint);
        }
        public /*interface Painter*/ void DrawCircle(SKPoint p, float radius, SKPaint paint) {
            canvas.DrawCircle(p.X, p.Y, radius, paint);
        }
        public /*interface Painter*/ void DrawText(string text, SKPoint point, SKPaint paint) {
            canvas.DrawText(text, point, paint);
        }
        public /*interface Painter*/ object GetCanvas() {
            return this.canvas;
        }
    }
}
