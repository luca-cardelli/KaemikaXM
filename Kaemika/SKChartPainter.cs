using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Kaemika
{
    public class SKChartPainter : SKPainter, ChartPainter {
        public SKChartPainter(SKCanvas canvas) : base(canvas) {
        }

        public /*interface ChartPainter*/ void DrawLine(List<KChartEntry> list,  int seriesIndex, float pointSize, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                using (var paint = LinePaint(pointSize, color)) {
                    var path = new SKPath();
                    path.MoveTo(pinchPan % list[0].Ypoint[seriesIndex]);
                    for (int i = 0; i < list.Count; i++) path.LineTo(pinchPan % list[i].Ypoint[seriesIndex]);
                    canvas.DrawPath(path, paint);
                }
            }
        }

        public /*interface ChartPainter*/ void DrawLineRange(List<KChartEntry> list, int seriesIndex, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                using (var paint = FillPaint(color)) {
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

        public /*interface ChartPainter*/ void DrawLineFill(List<KChartEntry> list, int seriesIndex, float bottom, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                using (var paint = FillPaint(color)) {
                    var path = new SKPath();
                    path.MoveTo(pinchPan % new SKPoint(list[0].Ypoint[seriesIndex].X, bottom));
                    path.LineTo(pinchPan % list[0].Ypoint[seriesIndex]);
                    for (int i = 0; i < list.Count; i++) path.LineTo(pinchPan % list[i].Ypoint[seriesIndex]);
                    path.LineTo(pinchPan % new SKPoint(list[list.Count-1].Ypoint[seriesIndex].X, bottom));
                    path.Close();
                    canvas.DrawPath(path, paint);
                }
            }
        }

    }
}
