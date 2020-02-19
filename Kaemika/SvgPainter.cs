using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Kaemika {

    public class SvgCanvas {
        public SKSize docSize { get; }
        public SKSize size { get; }
        private string svg;
        private bool closed;
        public SvgCanvas(SKSize size /*pixels*/, SKSize docSize /*cm*/) {
            this.docSize = docSize;
            this.size = size;
            this.closed = false;
            this.svg =
                "<?xml version=\"1.0\" standalone=\"no\"?>" + Environment.NewLine +
                "<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\"" + Environment.NewLine +
                "   \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">" + Environment.NewLine +
                "<svg width=\""+docSize.Width+"cm\" height=\""+docSize.Height+"cm\"" + this.ViewBox(size) + Environment.NewLine +
                "   xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" >" + Environment.NewLine;
        }
        public void Add(string more) {
            if (this.closed) throw new Error("SvgCanvas.Add: canvas closed");
            svg += more;
        }
        public string Close() {
            if (this.closed) throw new Error("SvgCanvas.Add: canvas closed");
            this.svg += "</svg>" + Environment.NewLine;
            this.closed = true;
            return svg;
        }
        private string ViewBox(SKSize viewSize) {
            return
                " viewBox=\"0 0 " + 
                " " + viewSize.Width + 
                " " + viewSize.Height + 
                "\"";
        }
    }

    public class SvgTexter : Texter {
        public /*interface Texter*/ string fontFamily {
            // do not use alternaltive fonts like "Arial, Helvetica, sans-serif" because then FromFamilyName will pick platforms-specific fonts
            // and if we force "sans-serif" in SvgTextPaint, it may not match the computations done by MeasureText 
            get { return "Arial"; }
        }
        public /*interface Texter*/ string fixedFontFamily {
            // do not use alternaltive fonts like "Courier, monospace" because then FromFamilyName will pick platforms-specific fonts
            // and if we force "sans-serif" in SvgTextPaint it may not match the computations done by MeasureText 
            get { return "Courier"; }
        }
    }

    public class SvgColorer : SvgTexter, Colorer {
        public SvgColorer() : base() {
        }
        public /*interface Colorer*/ SKTypeface font {
            get { return SKTypeface.FromFamilyName(this.fontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright); }
        }
        public /*interface Colorer*/ SKTypeface fixedFont { 
            get { return SKTypeface.FromFamilyName(this.fixedFontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright); }
        }
        public /*interface Colorer*/ SKPaint TextPaint(SKTypeface typeface, float textSize, SKColor color) {
            return new SKPaint { Typeface = typeface, IsStroke = false, Style = SKPaintStyle.Fill, TextSize = textSize, Color = color, IsAntialias = true };
        }
        public /*interface Colorer*/ SKPaint FillPaint(SKColor color) { 
            return new SKPaint { IsStroke = false, Style = SKPaintStyle.Fill, Color = color, IsAntialias = true };
        }
        public /*interface Colorer*/ SKPaint LinePaint(float strokeWidth, SKColor color, SKStrokeCap cap = SKStrokeCap.Butt) {
            return new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth, Color = color, IsAntialias = true, StrokeCap = cap, StrokeJoin = SKColorer.StrokeJoin(cap) };
        }
        public virtual /*interface Colorer*/ SKRect MeasureText(string text, SKPaint paint) {
            if (string.IsNullOrEmpty(text)) return new SKRect(0, 0, 0, 0); // or MeasureText will crash
            var bounds = new SKRect();
            float length = paint.MeasureText(text, ref bounds);
            return bounds;
        }
        public string SvgPaintPath(SKPaint paint) {
            if (paint.IsStroke) {
                return
                    " fill=\"none\"" +
                    SvgColorStroke(paint.Color) +
                    " stroke-width=\"" + paint.StrokeWidth + "\"" +
                    " stroke-linecap=\"" + 
                        (paint.StrokeCap==SKStrokeCap.Butt ? "butt" :
                        paint.StrokeCap==SKStrokeCap.Round ? "round" :
                        paint.StrokeCap==SKStrokeCap.Square ? "square" : "")
                        +"\"";
            } else {
                return
                    " stroke=\"none\"" +
                    SvgColorFill(paint.Color);
            }
        }
        public string SvgTextPaint(SKPaint paint) {
            return 
                " font-family=\"" + paint.Typeface.FamilyName + "\"" +
                " font-size=\"" + paint.TextSize + "\"" +
                SvgColorFill(paint.Color);
        }
        public string SvgColorFill(SKColor color) {
            return
                " fill=" + SvgRGB(color) +
                " fill-opacity=" + SvgA(color);
        }
        public string SvgColorStroke(SKColor color) {
            return
                " stroke=" + SvgRGB(color) +
                " stroke-opacity=" + SvgA(color);
        }
        private string SvgRGB(SKColor color) {
            return
                "\"rgb(" + color.Red + ", " + color.Green + ", " + color.Blue + ")\"";
        }
        private string SvgA(SKColor color) {
            return
                "\"" + color.Alpha/255.0f + "\"";
        }
    }

    public class SvgPainter : SvgColorer, Painter {
        protected SvgCanvas canvas;

        public SvgPainter(SvgCanvas canvas) : base() {
            this.canvas = canvas;
        }

        public object GetCanvas() { // platform dependent
            return this.canvas;
        }

        public /*interface Painter*/ void Clear(SKColor background) {
            using (var paint = FillPaint(background)) { DrawRect(new SKRect(0.0f, 0.0f, canvas.size.Width, canvas.size.Height), paint); }
        }

        public /*interface Painter*/ void DrawLine(List<SKPoint> points, SKPaint paint) {
            if (points.Count > 1) {
                NewPolyLine(paint);
                MoveTo(points[0]);
                for (int i = 0; i < points.Count; i++) LineTo(points[i]);
                EndPolyLine();
            }
        }

        public /*interface Painter*/ void DrawPolygon(List<SKPoint> points, SKPaint paint) {
            if (points.Count > 1) {
                NewPolygon(paint);
                MoveTo(points[0]);
                for (int i = 0; i < points.Count; i++) LineTo(points[i]);
                EndPolygon();
            }
        }

        public /*interface Painter*/ void DrawSpline(List<SKPoint> points, SKPaint paint) {
            if (points.Count > 1) {
                points.Insert(0, points[0]); // duplicate first point for spline
                SKPoint ultimate = points[points.Count - 1];
                points.Insert(points.Count, ultimate); // duplicate last point for spline
                List<SKPoint> controlPoints = SKPainter.ControlPoints(points);
                NewPath(paint);
                AddBeziers(controlPoints.ToArray());
                EndPath();
            }
        }
        private void AddBeziers(SKPoint[] controlPoints) {
            PathMoveTo(controlPoints[0]);
            for (int i = 0; i < controlPoints.Length - 2; i += 4) {
                if (i+3 > controlPoints.Length - 1) {
                    PathQuadTo(controlPoints[i + 1], controlPoints[i + 2]);
                } else {
                    PathCubicTo(controlPoints[i + 1], controlPoints[i + 2], controlPoints[i + 3]);
                }
            }
        }

        public /*interface Painter*/ void DrawRect(SKRect rect, SKPaint paint) {
            canvas.Add( 
                "<rect" +
                SvgRect(rect) +
                SvgPaintPath(paint)  + 
                "/>" +
                Environment.NewLine
                );
        }

        public /*interface Painter*/ void DrawRoundRect(SKRect rect, float padding, SKPaint paint) {
            canvas.Add(
                "<rect" +
                SvgRect(rect) +
                " rx=\"" + padding + "\"" +
                " ry=\"" + padding + "\"" +
                SvgPaintPath(paint) +
                "/>" +
                Environment.NewLine
                );
        }

        public /*interface Painter*/ void DrawCircle(SKPoint p, float radius, SKPaint paint) {
            canvas.Add(
                "<circle" +
                " cx=\"" + p.X + "\"" +
                " cy=\"" + p.Y + "\"" +
                " r=\"" + radius + "\"" +
                SvgPaintPath(paint) +
                "/>" +
                Environment.NewLine
                );
        }

        public /*interface Painter*/ void DrawText(string text, SKPoint point, SKPaint paint) {
            canvas.Add(
                "<text" +
                SvgPoint(point) +
                SvgTextPaint(paint) + 
                ">" + Environment.NewLine +
                text.Replace("<","&lt;").Replace(">","&gt;") + Environment.NewLine +
                "</text>" + 
                Environment.NewLine
                );
        }

        private string SvgPoint(SKPoint point) {
            return
                " x=\"" + point.X + "\"" +
                " y=\"" + point.Y + "\"";
        }

        private string SvgRect(SKRect rect) {
            return
                " x=\"" + rect.Left + "\"" +
                " y=\"" + rect.Top + "\"" +
                " width=\"" + rect.Width + "\"" +
                " height=\"" + rect.Height + "\"" +
                Environment.NewLine;
        }

        protected void NewPath(SKPaint paint) {
            canvas.Add(
                "<path" +
                SvgPaintPath(paint) +
                " d=\""
                );
        }
        protected void EndPath() {
            canvas.Add(
                "\" />" + 
                Environment.NewLine
                );
        }

        protected void NewPolyLine(SKPaint paint) {
            canvas.Add(
                "<polyline" +
                SvgPaintPath(paint) +
                " points=\""
                );
        }
        protected void EndPolyLine() {
            canvas.Add(
                "\" />" + 
                Environment.NewLine
                );
        }

        protected void NewPolygon(SKPaint paint) {
            canvas.Add(
                "<polygon" +
                SvgPaintPath(paint) +
                " points=\""
                );
        }
        protected void EndPolygon() {
            canvas.Add(
                "\" />" + 
                Environment.NewLine
                );
        }

        protected void MoveTo(SKPoint point) {
            canvas.Add(
                " " + point.X + "," + point.Y
                );
        }
        protected void LineTo(SKPoint point) {
            canvas.Add(
                " " + point.X + "," + point.Y
                );
        }

        protected void PathMoveTo(SKPoint point) {
            canvas.Add(
                " M " + point.X + " " + point.Y
                );
        }
        protected void PathQuadTo(SKPoint point1, SKPoint point2) {
            canvas.Add(
                " Q " + point1.X + " " + point1.Y + ", " + point2.X + " " + point2.Y
                );
        }
        protected void PathCubicTo(SKPoint point1, SKPoint point2, SKPoint point3) {
            canvas.Add(
                " C " + point1.X + " " + point1.Y + ", " + point2.X + " " + point2.Y + ", " + point3.X + " " + point3.Y
                );
        }
    }

    public class SvgChartPainter : SvgPainter, ChartPainter { 

        public SvgChartPainter(SvgCanvas canvas) : base(canvas) {
        }
        public /*interface ChartPainter*/ void DrawCourse(List<KChartEntry> list, int seriesIndex, float pointSize, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                using (var paint = LinePaint(pointSize, color)) {
                    NewPolyLine(paint);
                    MoveTo(pinchPan % list[0].Ypoint[seriesIndex]);
                    for (int i = 0; i < list.Count; i++) LineTo(pinchPan % list[i].Ypoint[seriesIndex]);
                    EndPolyLine();
                }
            }
        }
        public /*interface ChartPainter*/ void DrawCourseRange(List<KChartEntry> list, int seriesIndex, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                using (var paint = FillPaint(color)) {
                    NewPolygon(paint);
                    KChartEntry entry0 = list[0];
                    SKPoint meanPoint0 = entry0.Ypoint[seriesIndex];
                    float range0 = entry0.YpointRange[seriesIndex];
                    MoveTo(pinchPan % new SKPoint(meanPoint0.X, meanPoint0.Y + range0));
                    LineTo(pinchPan % new SKPoint(meanPoint0.X, meanPoint0.Y - range0));
                    for (int i = 0; i < list.Count; i++) {
                        KChartEntry entry = list[i];
                        SKPoint meanPoint = entry.Ypoint[seriesIndex];
                        float range = entry.YpointRange[seriesIndex];
                        LineTo(pinchPan % new SKPoint(meanPoint.X, meanPoint.Y - range));
                    }
                    for (int i = list.Count - 1; i >= 0; i--) {
                        KChartEntry entry = list[i];
                        SKPoint meanPoint = entry.Ypoint[seriesIndex];
                        float range = entry.YpointRange[seriesIndex];
                        LineTo(pinchPan % new SKPoint(meanPoint.X, meanPoint.Y + range));
                    }
                    EndPolygon();
                }
            }
        }
        public /*interface ChartPainter*/ void DrawCourseFill(List<KChartEntry> list, int seriesIndex, float bottom, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                using (var paint = FillPaint(color)) {
                    NewPolygon(paint);
                    MoveTo(pinchPan % new SKPoint(list[0].Ypoint[seriesIndex].X, bottom));
                    LineTo(pinchPan % list[0].Ypoint[seriesIndex]);
                    for (int i = 0; i < list.Count; i++) LineTo(pinchPan % list[i].Ypoint[seriesIndex]);
                    LineTo(pinchPan % new SKPoint(list[list.Count-1].Ypoint[seriesIndex].X, bottom));
                    EndPolygon();
                }
            }
        }

    }

}
