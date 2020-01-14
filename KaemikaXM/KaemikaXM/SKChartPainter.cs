using System;
using System.Collections.Generic;
using SkiaSharp;
using Kaemika;

namespace KaemikaXM
{
    public class SKChartPainter : ChartPainter {
        private SKCanvas canvas;

        public SKChartPainter(SKCanvas canvas) {
            this.canvas = canvas;
        }

        public override void Clear(SKColor background) {
            canvas.Clear(background);
        }

        public override SKRect DrawLabel_Hor(string text, float X, float Y, bool hor, float textSize, SKColor textColor, Swipe pinchPan) {
            using (var paint = new SKPaint()) {
                paint.TextSize = pinchPan % textSize;
                paint.IsAntialias = true;
                paint.Color = textColor;
                paint.IsStroke = false;
                var bounds = new SKRect();
                paint.MeasureText(text, ref bounds);
                var r = pinchPan % new SKRect(X - 1, Y - 30, X + 1, Y + 1);
                canvas.DrawRect(new SKRect(r.Left, (Y + 1) - (r.Bottom - r.Top), r.Right, Y + 1), paint); // Clamped to bottom of hor axis
                canvas.DrawText(text, (pinchPan % new SKPoint(X + 6, 0)).X, Y - 6, paint); // Clamped to hor axis
                return bounds;
            }
        }

        public override SKRect DrawLabel_Ver(string text, float X, float Y, bool hor, float textSize, SKColor textColor, Swipe pinchPan) {
            using (var paint = new SKPaint()) {
                paint.TextSize = pinchPan % textSize;
                paint.IsAntialias = true;
                paint.Color = textColor;
                paint.IsStroke = false;
                var bounds = new SKRect();
                paint.MeasureText(text, ref bounds);
                var r = pinchPan % new SKRect(X - 1, Y - 1, X + 30, Y + 1);
                canvas.DrawRect(new SKRect(X - 1, r.Top, (X - 1) + (r.Right - r.Left), r.Bottom), paint); // Clamped to left of ver axis
                canvas.DrawText(text, X + 6, (pinchPan % new SKPoint(0, Y - 6)).Y, paint); // Clamped to ver axis
                return bounds;
            }
        }

        public override void DrawLine(List<KChartEntry> list,  int seriesIndex, KLineStyle lineStyle, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                using (var paint = new SKPaint {
                    Style = SKPaintStyle.Stroke,
                    Color = color,
                    StrokeWidth = lineStyle == KLineStyle.Thick ? 6 : 2,
                    IsAntialias = true,
                }) {
                    var path = new SKPath();
                    path.MoveTo(pinchPan % list[0].Ypoint[seriesIndex]);
                    for (int i = 0; i < list.Count; i++) path.LineTo(pinchPan % list[i].Ypoint[seriesIndex]);
                    canvas.DrawPath(path, paint);
                }
            }
        }

        public override void DrawLineRange(List<KChartEntry> list, int seriesIndex, KLineStyle lineStyle, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                using (var paint = new SKPaint {
                    Style = SKPaintStyle.Fill,
                    Color = color,
                    IsAntialias = true,
                }) {
                    var path = new SKPath();
                    KChartEntry entry0 = list[0];
                    SKPoint meanPoint0 = entry0.Ypoint[seriesIndex];
                    float range0 = entry0.YpointRange[seriesIndex];
                    path.MoveTo(pinchPan % new SKPoint(meanPoint0.X, meanPoint0.Y + range0));
                    path.LineTo(pinchPan % new SKPoint(meanPoint0.X, meanPoint0.Y - range0));
                    for (int i = 0; i < list.Count; i++) {
                        KChartEntry entry = list[i];
                        SKPoint meanPoint = entry.Ypoint[seriesIndex];
                        float range = entry.YpointRange[seriesIndex];
                        path.LineTo(pinchPan % new SKPoint(meanPoint.X, meanPoint.Y - range));
                    }
                    for (int i = list.Count - 1; i >= 0; i--) {
                        KChartEntry entry = list[i];
                        SKPoint meanPoint = entry.Ypoint[seriesIndex];
                        float range = entry.YpointRange[seriesIndex];
                        path.LineTo(pinchPan % new SKPoint(meanPoint.X, meanPoint.Y + range));
                    }
                    path.Close();
                    canvas.DrawPath(path, paint);
                }
            }
        }

        public override void DisplayPinchOrigin(SKPoint pinchOrigin) {
            // ### GraphLayout.CanvasDrawCircle(canvas, pinchOrigin, 20, false, SKColors.LightGray);
        }

    }
}
