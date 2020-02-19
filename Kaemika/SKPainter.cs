using System.Collections.Generic;
using SkiaSharp;

namespace Kaemika {

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
        public /*interface Colorer*/ SKPaint LinePaint(float strokeWidth, SKColor color, SKStrokeCap cap = SKStrokeCap.Butt) {
            return new SKPaint { IsStroke = true, Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth, Color = color, IsAntialias = true,
            StrokeCap = cap, StrokeJoin = StrokeJoin(cap)};
            //, StrokeCap = SKStrokeCap.Butt/Round/Square, StrokeJoin = SKStrokeJoin.Bevel/Miter/Round, StrokeMiter = float
        }
        public virtual /*interface Colorer*/ SKRect MeasureText(string text, SKPaint paint) {
            if (string.IsNullOrEmpty(text)) return new SKRect(0, 0, 0, 0); // or MeasureText will crash
            var bounds = new SKRect();
            float length = paint.MeasureText(text, ref bounds);
            return bounds;
        }
        public static SKStrokeJoin StrokeJoin(SKStrokeCap cap) {
            return
                (cap == SKStrokeCap.Butt) ? SKStrokeJoin.Bevel :
                (cap == SKStrokeCap.Round) ? SKStrokeJoin.Round :
                (cap == SKStrokeCap.Square) ? SKStrokeJoin.Miter :
                SKStrokeJoin.Bevel;
        }

        // Removes transparency but does the equivalent compositing over white
        public static System.Drawing.Color ColorOverWhite(SKColor color) {
            return ColorOverWhite(System.Drawing.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));
        }
        public static System.Drawing.Color ColorOverWhite(System.Drawing.Color color) { 
            float R = ((float)color.R) / 255.0f;
            float G = ((float)color.G) / 255.0f;
            float B = ((float)color.B) / 255.0f;
            float A = ((float)color.A) / 255.0f;
            return System.Drawing.Color.FromArgb(255,
                (byte)((R * A + 1.0f - A) * 255.0f),
                (byte)((G * A + 1.0f - A) * 255.0f),
                (byte)((B * A + 1.0f - A) * 255.0f));
        }
    }

    public class SKPainter : SKColorer, Painter {
        protected SKCanvas canvas;
        public SKPainter(SKCanvas canvas) : base() {
            this.canvas = canvas;
        }
        public /*interface Painter*/ void Clear(SKColor background) {
            canvas.Clear(background);
        }
        private static void PaintPath(SKCanvas canvas, SKPath path, SKPaint paint) {
            if (paint.IsStroke) {
                canvas.DrawPath(path, paint);
            } else {
                path.Close();
                canvas.DrawPath(path, paint);
            }
        }
        public /*interface Painter*/ void DrawLine(List<SKPoint> points, SKPaint paint) {
            if (points.Count > 1) {
                var path = new SKPath();
                path.MoveTo(points[0]);
                for (int i = 0; i < points.Count; i++) path.LineTo(points[i]);
                PaintPath(canvas,path, paint);
            }
        }
        public /*interface Painter*/ void DrawPolygon(List<SKPoint> points, SKPaint paint) {
            if (points.Count > 1) {
                var path = new SKPath();
                path.MoveTo(points[0]);
                for (int i = 0; i < points.Count; i++) path.LineTo(points[i]);
                PaintPath(canvas,path, paint);
            }
        }
        public /*interface Painter*/ void DrawSpline(List<SKPoint> points, SKPaint paint) {
            if (points.Count > 1) {
                SKPath path = DrawSplinePath(points);
                PaintPath(canvas,path, paint);
            }
        }
        private SKPath DrawSplinePath(List<SKPoint> points) { // points.Count > 1
            points.Insert(0, points[0]); // duplicate first point for spline
            SKPoint ultimate = points[points.Count - 1];
            points.Insert(points.Count, ultimate); // duplicate last point for spline
            List<SKPoint> controlPoints = ControlPoints(points);
            return AddBeziers(new SKPath(), controlPoints.ToArray());
        }
        private SKPath AddBeziers(SKPath path, SKPoint[] controlPoints) {
            path.MoveTo(controlPoints[0]);
            for (int i = 0; i < controlPoints.Length - 2; i += 4) {
                if (i+3 > controlPoints.Length - 1) {
                    path.QuadTo(controlPoints[i + 1], controlPoints[i + 2]);
                } else {
                    path.CubicTo(controlPoints[i + 1], controlPoints[i + 2], controlPoints[i + 3]);
                }
            }
            return path;
        }  
        public static List<SKPoint> ControlPoints(List<SKPoint> path) {
	        List<SKPoint> controlPoints = new List<SKPoint>();
	        for ( int i = 1; i < path.Count - 1; i += 2 ) {
		        controlPoints.Add(new SKPoint((path[i - 1].X + path[i].X) / 2, (path[i - 1].Y + path[i].Y) / 2));
		        controlPoints.Add(path[i]);
		        controlPoints.Add(path[i+1]);
                if (i + 2 < path.Count - 1) {
                    controlPoints.Add(new SKPoint((path[i + 1].X + path[i + 2].X)/2, (path[i + 1].Y + path[i + 2].Y) / 2));
		        }
	        }
            return controlPoints;
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
