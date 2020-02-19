using System;
using System.Collections.Generic;
using AppKit;
using CoreGraphics;
using SkiaSharp;
using Kaemika;

namespace KaemikaMAC {

    public class CGChartPainter : CGPainter, ChartPainter {

        public CGChartPainter(CGContext canvas) : base(canvas) {
        }

        public /*interface ChartPainter*/ void DrawCourse(List<KChartEntry> list,  int seriesIndex, float pointSize, SKColor color, Swipe pinchPan) {
            if (list.Count == 1) DrawCircle(list[0].Ypoint[seriesIndex], pinchPan % 8, FillPaint(color));
            if (list.Count > 1) {
                canvas.SetStrokeColor(CG.Color(color));
                canvas.SetLineWidth(pointSize);
                var path = new CGPath();
                path.MoveToPoint(CG.Point(pinchPan % list[0].Ypoint[seriesIndex]));
                for (int i = 0; i < list.Count; i++) path.AddLineToPoint(CG.Point(pinchPan % list[i].Ypoint[seriesIndex]));
                canvas.AddPath(path);
                canvas.StrokePath();
            }
        }

        public /*interface ChartPainter*/ void DrawCourseRange(List<KChartEntry> list, int seriesIndex, SKColor color, Swipe pinchPan) {
            if (list.Count == 1) DrawCircle(list[0].Ypoint[seriesIndex], pinchPan % 8, FillPaint(color));
            if (list.Count > 1) {
                canvas.SetFillColor(CG.Color(color));
                var path = new CGPath();
                KChartEntry entry0 = list[0];
                SKPoint meanPoint0 = entry0.Ypoint[seriesIndex];
                float range0 = entry0.YpointRange[seriesIndex];
                path.MoveToPoint(CG.Point(pinchPan % new SKPoint(meanPoint0.X, meanPoint0.Y + range0)));
                path.AddLineToPoint(CG.Point(pinchPan % new SKPoint(meanPoint0.X, meanPoint0.Y - range0)));
                for (int i = 0; i < list.Count; i++) {
                    KChartEntry entry = list[i];
                    SKPoint meanPoint = entry.Ypoint[seriesIndex];
                    float range = entry.YpointRange[seriesIndex];
                    path.AddLineToPoint(CG.Point(pinchPan % new SKPoint(meanPoint.X, meanPoint.Y - range)));
                }
                for (int i = list.Count - 1; i >= 0; i--) {
                    KChartEntry entry = list[i];
                    SKPoint meanPoint = entry.Ypoint[seriesIndex];
                    float range = entry.YpointRange[seriesIndex];
                    path.AddLineToPoint(CG.Point(pinchPan % new SKPoint(meanPoint.X, meanPoint.Y + range)));
                }
                path.CloseSubpath();
                canvas.AddPath(path);
                canvas.FillPath();
            }
        }

        public /*interface ChartPainter*/ void DrawCourseFill(List<KChartEntry> list, int seriesIndex, float bottom, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                canvas.SetFillColor(CG.Color(color));
                var path = new CGPath();
                path.MoveToPoint(CG.Point(pinchPan % new SKPoint(list[0].Ypoint[seriesIndex].X, bottom)));
                path.AddLineToPoint(CG.Point(pinchPan % list[0].Ypoint[seriesIndex]));
                for (int i = 0; i < list.Count; i++) path.AddLineToPoint(CG.Point(pinchPan % list[i].Ypoint[seriesIndex]));
                path.AddLineToPoint(CG.Point(pinchPan % new SKPoint(list[list.Count-1].Ypoint[seriesIndex].X, bottom)));
                path.CloseSubpath();
                canvas.AddPath(path);
                canvas.FillPath();
            }
        }

    }
}
