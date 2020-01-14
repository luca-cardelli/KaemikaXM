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

    public class SvgColorer : SKColorer {
        public SvgColorer() : base() {
        }
        public string SvgFillPaint(SKPaint paint) {
            return
                " stroke=\"none\"" +
                SvgColorFill(paint.Color);
        }
        public string SvgStrokePaint(SKPaint paint) {
            return
                " fill=\"none\"" +
                SvgColorStroke(paint.Color) +
                " stroke-width=\"" + paint.StrokeWidth + "\"";
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

        public /*interface Painter*/ void DrawRect(SKRect rect, SKPaint paint) {
            canvas.Add( 
                "<rect" +
                SvgRect(rect) +
                SvgFillPaint(paint)  + 
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
                SvgFillPaint(paint) +
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
                SvgFillPaint(paint) +
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
                text + Environment.NewLine +
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
    }

    public class SvgChartPainter : SvgPainter, ChartPainter { 

        public SvgChartPainter(SvgCanvas canvas) : base(canvas) {
        }
        public /*interface ChartPainter*/ void DrawLine(List<KChartEntry> list, int seriesIndex, float pointSize, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                using (var paint = LinePaint(pointSize, color)) {
                    NewPolyLine(paint);
                    MoveTo(pinchPan % list[0].Ypoint[seriesIndex]);
                    for (int i = 0; i < list.Count; i++) LineTo(pinchPan % list[i].Ypoint[seriesIndex]);
                    EndPolyLine();
                }
            }
        }
        public /*interface ChartPainter*/ void DrawLineRange(List<KChartEntry> list, int seriesIndex, SKColor color, Swipe pinchPan) {
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

        private void NewPolyLine(SKPaint paint) {
            canvas.Add(
                "<polyline" +
                SvgStrokePaint(paint) +
                " points=\""
                );
        }
        private void NewPolygon(SKPaint paint) {
            canvas.Add(
                "<polygon" +
                SvgFillPaint(paint) +
                " points=\""
                );
        }
        private void MoveTo(SKPoint point) {
            canvas.Add(
                " " + point.X + "," + point.Y
                );
        }
        private void LineTo(SKPoint point) {
            canvas.Add(
                " " + point.X + "," + point.Y
                );
        }
        private void EndPolyLine() {
            canvas.Add(
                "\" />" + 
                Environment.NewLine
                );
        }
        private void EndPolygon() {
            canvas.Add(
                "\" />" + 
                Environment.NewLine
                );
        }
    }

}
