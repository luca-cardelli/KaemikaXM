using SkiaSharp;
using CoreGraphics;
using Kaemika;

namespace KaemikaMAC {

    public class CGColorer : SKColorer {
        public override /*interface Colorer*/ SKRect MeasureText(string text, SKPaint paint) {
            if (string.IsNullOrEmpty(text)) return new SKRect(0, 0, 0, 0);
            CGSize size = CG.MeasureText(text, paint);
            return new SKRect(0,0,(float)size.Width, (float)size.Height);
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

        //public /*interface Painter*/ void DrawRect(SKRect rect, SKColor color) {
        //    using (var paint = FillPaint(color)) { DrawRect(rect, paint); }
        //}

        public /*interface Painter*/ void DrawRect(SKRect rect, SKPaint paint) {
            CG.DrawRect(canvas, CG.Rect(rect), CG.Color(paint.Color));
        }
        public /*interface Painter*/ void DrawRoundRect(SKRect rect, float padding, SKPaint paint) {
            CG.DrawRoundRect(canvas, rect, padding, paint);
        }
        public /*interface Painter*/ void DrawCircle(SKPoint p, float radius, SKPaint paint) {
            CG.DrawCircle(canvas, p, radius, paint);
        }

        public /*interface Painter*/ void DrawText(string text, SKPoint point, SKPaint paint) {
            CG.DrawText(canvas, text, point, paint);
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

        public static void DrawRect(CGContext canvas, CGRect rect, CGColor color) {
            var path = new CGPath();
            path.AddRect(rect);
            canvas.SetFillColor(color);
            canvas.AddPath(path);
            canvas.FillPath();
        }

        public static void DrawRoundRect(CGContext canvas, SKRect rect, float padding, SKPaint paint) {
            //###
        }
        public static void DrawCircle(CGContext canvas, SKPoint p, float radius, SKPaint paint) {
            //###
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
        public static CGSize DrawText(CGContext canvas, string text, SKPoint point, SKPaint paint) {
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

        public static CGSize MeasureText(string text, SKPaint paint) {
            // https://docs.microsoft.com/en-us/dotnet/api/foundation.nsattributedstring?view=xamarin-ios-sdk-12
            var typeface = (paint.Typeface == null) ? "Helvetica" : paint.Typeface.FamilyName;
            var attributedString = new Foundation.NSAttributedString (text,
                   new CoreText.CTStringAttributes () {
                       ForegroundColor = Color(paint.Color), // ForegroundColor is used (only?) if paint.Style = Fill, and overrides paint.Color
                       Font = new CoreText.CTFont(typeface, paint.TextSize)
                   });
            var textLine = new CoreText.CTLine(attributedString);
            return attributedString.Size;  // maybe we can extract the true baseline origin and return a Rect instead of a Size?
        }


    }
}
