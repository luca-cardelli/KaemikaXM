using System.Collections.Generic;
using SkiaSharp;
using CoreGraphics;
using Kaemika;

namespace KaemikaMAC {

    public class CGColorer : SKColorer {
        public override /*interface Colorer*/ SKRect MeasureText(string text, SKPaint paint) {
            if (string.IsNullOrEmpty(text)) return new SKRect(0, 0, 0, 0);
            CGRect rect = CG.MeasureText(text, paint);
            return new SKRect((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom);
        }
    }

    public class CGPainter : CGColorer, Painter {
        protected CGContext canvas;
        public CGPainter(CGContext canvas) : base() {
            this.canvas = canvas;
        }
        public /*interface Painter*/ void Clear(SKColor background) {
            canvas.SetFillColor(CG.Color(background));
            canvas.FillRect(canvas.GetClipBoundingBox());
        }
        public /*interface Painter*/ void DrawLine(List<SKPoint> points, SKPaint paint) {
            CG.DrawLine(canvas, points, paint);
        }
        public /*interface Painter*/ void DrawPolygon(List<SKPoint> points, SKPaint paint) {
            CG.DrawPolygon(canvas, points, paint);
        }
        public /*interface Painter*/ void DrawSpline(List<SKPoint> points, SKPaint paint) {
            CG.DrawSpline(canvas, points, paint);
        }
        public /*interface Painter*/ void DrawRect(SKRect rect, SKPaint paint) {
            CG.DrawRect(canvas, CG.Rect(rect), paint);
        }
        public /*interface Painter*/ void DrawRoundRect(SKRect rect, float padding, SKPaint paint) {
            CG.DrawRoundRect(canvas, rect, padding, paint);
        }
        public /*interface Painter*/ void DrawCircle(SKPoint p, float radius, SKPaint paint) {
            CG.DrawCircle(canvas, p, radius, paint);
        }
        public /*interface Painter*/ void DrawText(string text, SKPoint point, SKPaint paint) {
            CG.DrawTextS(canvas, text, point, paint);
        }

        public /*interface Painter*/ object GetCanvas() { // platform dependent
            return this.canvas;
        }
    }

    public static class CG {

        public static CGPoint Point(SKPoint point) { return new CGPoint(point.X, point.Y); }
        public static CGSize Size(SKSize size) { return new CGSize(size.Width, size.Height); }
        public static CGRect Rect(SKRect rect) { return new CGRect(rect.Left, rect.Top, rect.Width, rect.Height); }
        public static CGRect RectFromCircle(SKPoint center, float radius) { return new CGRect(center.X-radius, center.Y-radius, radius+radius, radius+radius); } 

        public static CGColor Color(SKColor color) { return new CGColor(((float)color.Red)/255.0F, ((float)color.Green)/255.0F, ((float)color.Blue)/255.0F, ((float)color.Alpha)/255.0F); }
        public static CGLineJoin LineJoin(SKStrokeJoin join) { return (join == SKStrokeJoin.Round) ? CGLineJoin.Round : (join == SKStrokeJoin.Bevel) ? CGLineJoin.Bevel : (join == SKStrokeJoin.Miter) ? CGLineJoin.Miter : 0; }
        public static CGLineCap LineCap(SKStrokeCap cap) { return (cap == SKStrokeCap.Round) ? CGLineCap.Round : (cap == SKStrokeCap.Butt) ? CGLineCap.Butt : (cap == SKStrokeCap.Square) ? CGLineCap.Square : 0; }

        public static CGTextDrawingMode TextDrawingMode(SKPaintStyle style) { return (style == SKPaintStyle.Fill) ? CGTextDrawingMode.Fill : (style == SKPaintStyle.Stroke) ? CGTextDrawingMode.Stroke : (style == SKPaintStyle.StrokeAndFill) ? CGTextDrawingMode.FillStroke : CGTextDrawingMode.Invisible; }

        public static void FlipCoordinateSystem(CGContext context) {
            // flip the coordinate system once for all
            var flipVertical = new CGAffineTransform(xx: 1, yx: 0, xy: 0, yy: -1, x0: 0, y0: context.GetClipBoundingBox().Height);
            context.ConcatCTM(flipVertical);
        }

        private static void PaintPath(CGContext canvas, CGPath path, SKPaint paint) {
            if (paint.IsStroke) {
                canvas.SetStrokeColor(CG.Color(paint.Color));
                canvas.SetLineWidth(paint.StrokeWidth);
                canvas.SetLineCap(LineCap(paint.StrokeCap));
                canvas.SetLineJoin(LineJoin(paint.StrokeJoin));
                canvas.AddPath(path);
                canvas.StrokePath();
            } else {
                canvas.SetFillColor(CG.Color(paint.Color));
                canvas.AddPath(path);
                canvas.ClosePath();
                canvas.FillPath();
            }
        }

        public static void DrawLine(CGContext canvas, List<SKPoint> points, SKPaint paint) {
            if (points.Count > 1) {
                var path = new CGPath();
                path.MoveToPoint(CG.Point(points[0]));
                for (int i = 0; i < points.Count; i++) path.AddLineToPoint(CG.Point(points[i]));
                PaintPath(canvas, path, paint);
            }
        }

        public static void DrawPolygon(CGContext canvas, List<SKPoint> points, SKPaint paint) {
            if (points.Count > 1) {
                var path = new CGPath();
                path.MoveToPoint(CG.Point(points[0]));
                for (int i = 0; i < points.Count; i++) path.AddLineToPoint(CG.Point(points[i]));
                PaintPath(canvas, path, paint);
            }
        }

        public static void DrawSpline(CGContext canvas, List<SKPoint> points, SKPaint paint) {
            if (points.Count > 1) {
                CGPath path = DrawSplinePath(points);
                PaintPath(canvas, path, paint);
            }
        }
        private static CGPath DrawSplinePath(List<SKPoint> points) { // points.Count > 1
            points.Insert(0, points[0]); // duplicate first point for spline
            SKPoint ultimate = points[points.Count - 1];
            points.Insert(points.Count, ultimate); // duplicate last point for spline
            List<SKPoint> controlPoints = SKPainter.ControlPoints(points);
            return AddBeziers(new CGPath(), controlPoints.ToArray());
        }
        private static CGPath AddBeziers(CGPath path, SKPoint[] controlPoints) {
            path.MoveToPoint(CG.Point(controlPoints[0]));
            for (int i = 0; i < controlPoints.Length - 2; i += 4) {
                if (i+3 > controlPoints.Length - 1) {
                    var cp = CG.Point(controlPoints[i + 1]); var p = CG.Point(controlPoints[i + 2]);
                    path.AddQuadCurveToPoint(cp.X, cp.Y, p.X, p.Y);
                } else {
                    path.AddCurveToPoint(CG.Point(controlPoints[i + 1]), CG.Point(controlPoints[i + 2]), CG.Point(controlPoints[i + 3]));
                }
            }
            return path;
        }  

        public static void DrawRect(CGContext canvas, CGRect rect, CGColor color) {
            var path = new CGPath();
            path.AddRect(rect);
            canvas.SetFillColor(color);
            canvas.AddPath(path);
            canvas.FillPath();
        }

        public static void DrawRect(CGContext canvas, CGRect rect, SKPaint paint) {
            var path = new CGPath();
            path.AddRect(rect);
            PaintPath(canvas, path, paint);
        }

        public static void DrawRoundRect(CGContext canvas, SKRect rect, float padding, SKPaint paint) {
            var path = new CGPath();
            path.MoveToPoint(rect.Left + padding, rect.Top);
            path.AddLineToPoint(rect.Right - padding, rect.Top);
            path.AddQuadCurveToPoint(rect.Right, rect.Top, rect.Right, rect.Top + padding);
            path.AddLineToPoint(rect.Right, rect.Bottom - padding);
            path.AddQuadCurveToPoint(rect.Right, rect.Bottom, rect.Right - padding, rect.Bottom);
            path.AddLineToPoint(rect.Left + padding, rect.Bottom);
            path.AddQuadCurveToPoint(rect.Left, rect.Bottom, rect.Left, rect.Bottom - padding);
            path.AddLineToPoint(rect.Left, rect.Top + padding);
            path.AddQuadCurveToPoint(rect.Left, rect.Top, rect.Left + padding, rect.Top);
            PaintPath(canvas, path, paint);
        }

        public static void DrawCircle(CGContext canvas, SKPoint p, float radius, SKPaint paint) {
            var path = new CGPath();
            CGRect rect = new CGRect(p.X - radius, p.Y - radius, 2*radius, 2*radius);
            path.AddEllipseInRect(rect);
            PaintPath(canvas, path, paint);
        }

      //https://csharp.hotexamples.com/examples/-/CGBitmapContext/-/php-cgbitmapcontext-class-examples.html
        public static CGBitmapContext Bitmap(int width, int height) {
            try {
                return new CGBitmapContext(null, width, height, 8, 4*width, CGColorSpace.CreateDeviceRGB(), CGBitmapFlags.PremultipliedFirst);
            } catch {
                throw new Error("new CGBitmapContext failed");
            }
        }

        // using the attributes of SKPaint paint parameter:
            //Typeface,
            //TextSize,
            //TextAlign(Center|Left),
            //Style(Fill|Stroke),
            //Color,
            //StrokeWidth(if Style=Stroke)
        public static CGSize DrawTextS(CGContext canvas, string text, SKPoint point, SKPaint paint) {
            // BASIC TEXT DRAWING IN CGContext:
            //canvas.SetTextDrawingMode(toCGTextDrawingMode(paint.Style));
            //canvas.SetFillColor(CGUtil.toCGColor(paint.Color));
            //canvas.SetStrokeColor(CGUtil.toCGColor(paint.Color));
            //canvas.SetLineWidth(paint.StrokeWidth);
            //canvas.SelectFont("Helvetica", paint.TextSize, CGTextEncoding.MacRoman);
            //// upsidedown text: https://stackoverflow.com/questions/44122778/ios-swift-core-graphics-string-is-upside-down
            //canvas.SaveState();
            //canvas.TranslateCTM(center.X, center.Y); // text should be centered, but isn't
            //canvas.ScaleCTM(1, -1);
            //canvas.ShowTextAtPoint(0, 0, text);
            //canvas.RestoreState();

            // TEXT DRAWING IN CoreText to compute text size:
            // https://docs.microsoft.com/en-us/dotnet/api/foundation.nsattributedstring?view=xamarin-ios-sdk-12
            var typeface = (paint.Typeface == null) ? "Helvetica" : paint.Typeface.FamilyName;
            var attributedString = new Foundation.NSAttributedString (text,
                   new CoreText.CTStringAttributes () {
                       ForegroundColor = Color(paint.Color), // ForegroundColor is used (only?) if paint.Style = Fill, and overrides paint.Color
                       Font = new CoreText.CTFont(typeface, paint.TextSize)
                   });
            var textLine = new CoreText.CTLine(attributedString);

            CGSize textSize = attributedString.Size;  // maybe we can extract the true baseline origin and return a Rect instead of a Size?
            bool centered = paint.TextAlign == SKTextAlign.Center;
            CGPoint textStart = (centered) ? new CGPoint(point.X - textSize.Width/2.0f, point.Y) : Point(point);

            canvas.SetTextDrawingMode(CG.TextDrawingMode(paint.Style));
            canvas.SetStrokeColor(CG.Color(paint.Color)); // paint.Color is used (only?) if paint.Style = Stroke
            canvas.SetLineWidth(paint.StrokeWidth);
            // upsidedown text: https://stackoverflow.com/questions/44122778/ios-swift-core-graphics-string-is-upside-down
            canvas.SaveState();
            canvas.TranslateCTM(textStart.X, textStart.Y);
            canvas.ScaleCTM(1, -1);

            canvas.TextPosition = new CGPoint(0,0); // because of the translation
            textLine.Draw(canvas);

            canvas.RestoreState();

            return textSize;
        }

        public static CGRect MeasureText(string text, SKPaint paint) {
            var typeface = (paint.Typeface == null) ? "Helvetica" : paint.Typeface.FamilyName;
            CoreText.CTFont font = new CoreText.CTFont(typeface, paint.TextSize);
            var size = MeasureTextSize2(text, font);
            return new CGRect(0, -font.AscentMetric, size.Width, font.AscentMetric + font.DescentMetric);
        }

        private static CGSize MeasureTextSize1(string text, CoreText.CTFont font) { // this method seems to give the wrong width
            // https://stackoverflow.com/questions/11245526/measuring-the-text-width
            var dict = new Foundation.NSDictionary<Foundation.NSString,CoreText.CTFont> ( new Foundation.NSString("font"), font );
            return new Foundation.NSString(text).StringSize(dict);
        }

        private static CGSize MeasureTextSize2(string text, CoreText.CTFont font) {
            // https://docs.microsoft.com/en-us/dotnet/api/foundation.nsattributedstring?view=xamarin-ios-sdk-12
            var attributedString = new Foundation.NSAttributedString (text,
                   new CoreText.CTStringAttributes () { Font = font });
            return attributedString.Size;
        }

    }
}
