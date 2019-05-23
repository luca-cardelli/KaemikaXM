// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using SkiaSharp;
using GraphSharp;

namespace Microcharts {

    public enum LineMode { Line, Range }
    public enum LineStyle { Thin, Thick }

    public class Series {
        public string name;
        public SKColor color;
        public LineMode lineMode;
        public LineStyle lineStyle;
        public bool visible;
        public Series(string name, Color color, LineMode lineMode, LineStyle lineStyle) {
            this.name = name;
            this.color = new SKColor(color.R, color.G, color.B, color.A);
            this.lineMode = lineMode;
            this.lineStyle = lineStyle;
            this.visible = true;
        }
    }

    public class ChartEntry {
        public float X;                                // This is value on the X axis, e.g. time point
        public float[] Y;                              // This are the values on the Y axis.
        public float[] Yrange;                         // This are the ranges + or - over the Y value.
        public SKPoint[] Ypoint;                       // This is where the Y points are plotted
        public float[] YpointRange;                    // This is + or - the ranges of where the points are plotted
        public string[] YLabel;                        // This are the labels for the Y points, e.g. the series they belongs to

        public ChartEntry(float X, float[] Y, float[] Yrange) {
            this.X = X;
			this.Y = Y;
            this.Ypoint = new SKPoint[Y.Length];
            this.Yrange = Yrange;
            this.YpointRange = new float[Yrange.Length];
        }

        public float MinY(List<Series> seriesList) {
            float min = float.MaxValue;
            for (int i = 0; i < Y.Length; i++) if (seriesList[i].visible) min = Math.Min(Y[i]-Yrange[i], min);
            return min;
        }

        public float MaxY(List<Series> seriesList) {
            float max = float.MinValue;
            for (int i = 0; i < Y.Length; i++) if (seriesList[i].visible) max = Math.Max(Y[i]+Yrange[i], max);
            return max;
        }
    }

    public class Timecourse {
        private object mutex;
        private List<ChartEntry> list;
        public Timecourse() {
            mutex = new object();
            list = new List<ChartEntry>();
        }

        public bool IsClear()  {
            lock (mutex) { return list.Count == 0; }
        }

        public void Add(ChartEntry entry) {
            lock (mutex) { list.Add(entry); }
        }

        private void Inner_Bounds(List<Series> seriesList, out float minX, out float maxX, out float minY, out float maxY) {
            if (list.Count() == 0) { minX = 0; maxX = 0; minY = 0; maxY = 0; return; }
            minX = float.MaxValue;
            maxX = float.MinValue;
            minY = float.MaxValue;
            maxY = float.MinValue;
            for (int i = 0; i < list.Count(); i++) {
                minX = Math.Min(minX, list[i].X);
                maxX = Math.Max(maxX, list[i].X);
                minY = Math.Min(minY, list[i].MinY(seriesList));
                maxY = Math.Max(maxY, list[i].MaxY(seriesList));
            }
        }

        private float XlocOfXvalInPlotarea(float Xval, float minX, float maxX, SKSize plotSize) {
            return (Xval / (maxX - minX)) * plotSize.Width;
        }
        private float XvalOfXlocInPlotarea(float Xloc, float minX, float maxX, SKSize plotSize) {
            return Xloc / plotSize.Width * (maxX - minX);
        }
        private float YlocOfYvalInPlotarea(float Yval, float minY, float maxY, SKSize plotSize) {  // the Y axis is flipped
            return ((maxY - Yval) * plotSize.Height) / (maxY - minY);
        }
        private float YvalOfYlocInPlotarea(float Yloc, float minY, float maxY, SKSize plotSize) {  // the Y axis is flipped
            return maxY - (Yloc * (maxY - minY)) / plotSize.Height;
        }
        private float YlocRangeOfYvalRangeInPlotarea(float YvalRange, float minY, float maxY, SKSize plotSize) {
            return (YvalRange * plotSize.Height) / (maxY - minY);
        }

        private SKRect Inner_DrawLabel_Hor(SKCanvas canvas, string text, float X, float Y, bool hor, float textSize, SKColor textColor, Swipe pinchPan) {
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

        private SKRect Inner_DrawLabel_Ver(SKCanvas canvas, string text, float X, float Y, bool hor, float textSize, SKColor textColor, Swipe pinchPan) {
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

        private void Inner_CalculatePoints(SKPoint plotOrigin, SKSize plotSize, float minX, float maxX, float minY, float maxY) {
            for (int i = 0; i < list.Count; i++) {
                ChartEntry entry = list[i];
                SKPoint[] points = new SKPoint[entry.Y.Length];
                float x = plotOrigin.X + XlocOfXvalInPlotarea(entry.X, minX, maxX, plotSize);
                for (int j = 0; j < entry.Y.Length; j++) {
                    var y = plotOrigin.Y + YlocOfYvalInPlotarea(entry.Y[j], minY, maxY, plotSize);
                    entry.Ypoint[j] = new SKPoint(x, y);
                    entry.YpointRange[j] = (entry.Yrange[j] == 0) ? 0 : YlocRangeOfYvalRangeInPlotarea(entry.Yrange[j], minY, maxY, plotSize);
                }
            }
        }

        private void Inner_DrawLine(SKCanvas canvas, int seriesIndex, LineStyle lineStyle, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                using (var paint = new SKPaint {
                    Style = SKPaintStyle.Stroke,
                    Color = color,
                    StrokeWidth = lineStyle == LineStyle.Thick ? 6 : 2,
                    IsAntialias = true,
                }) {
                    var path = new SKPath();
                    path.MoveTo(pinchPan % list[0].Ypoint[seriesIndex]);
                    for (int i = 0; i < list.Count; i++) path.LineTo(pinchPan % list[i].Ypoint[seriesIndex]);
                    canvas.DrawPath(path, paint);
                }
            }
        }

        private void Inner_DrawLineRange(SKCanvas canvas, int seriesIndex, LineStyle lineStyle, SKColor color, Swipe pinchPan) {
            if (list.Count > 1) {
                using (var paint = new SKPaint {
                    Style = SKPaintStyle.Fill,
                    Color = color,
                    IsAntialias = true,
                }) {
                    var path = new SKPath();
                    ChartEntry entry0 = list[0];
                    SKPoint meanPoint0 = entry0.Ypoint[seriesIndex];
                    float range0 = entry0.YpointRange[seriesIndex];
                    path.MoveTo(pinchPan % new SKPoint(meanPoint0.X, meanPoint0.Y + range0));
                    path.LineTo(pinchPan % new SKPoint(meanPoint0.X, meanPoint0.Y - range0));
                    for (int i = 0; i < list.Count; i++) {
                        ChartEntry entry = list[i];
                        SKPoint meanPoint = entry.Ypoint[seriesIndex];
                        float range = entry.YpointRange[seriesIndex];
                        path.LineTo(pinchPan % new SKPoint(meanPoint.X, meanPoint.Y - range));
                    }
                    for (int i = list.Count - 1; i >= 0; i--) {
                        ChartEntry entry = list[i];
                        SKPoint meanPoint = entry.Ypoint[seriesIndex];
                        float range = entry.YpointRange[seriesIndex];
                        path.LineTo(pinchPan % new SKPoint(meanPoint.X, meanPoint.Y + range));
                    }
                    path.Close();
                    canvas.DrawPath(path, paint);
                }
            }
        }

        private void Inner_DrawLines(SKCanvas canvas, List<Series> seriesList, Swipe pinchPan) {
            for (int j = 0; j < seriesList.Count(); j++) {
                Series series = seriesList[j];
                if (series.visible) {
                    if (series.lineMode == LineMode.Line) {
                        Inner_DrawLine(canvas, j, series.lineStyle, series.color, pinchPan);
                    } else if (series.lineMode == LineMode.Range && list.Count > 1) {
                        Inner_DrawLineRange(canvas, j, series.lineStyle, series.color, pinchPan);
                    }
                }
            }
        }

        private void Inner_DrawXLabels(SKCanvas canvas, SKPoint plotOrigin, SKSize plotSize, float textHeight, SKColor axisTextColor, float minX, float maxX, Swipe pinchPan) {
            float Xloc = XlocOfXvalInPlotarea(0, minX, maxX, plotSize); // initialize to screen coordinates height of X=0
            do {
                float Xval = XvalOfXlocInPlotarea(Xloc, minX, maxX, plotSize);
                SKRect bounds = Inner_DrawLabel_Hor(canvas, Xval.ToString("g3"), plotOrigin.X + Xloc, plotOrigin.Y + plotSize.Height, true, textHeight, axisTextColor, pinchPan);
                Xloc += bounds.Width + 2 * textHeight; //using textHeigth for horizontal spacing
            } while (Xloc < plotSize.Width - plotOrigin.X);
        }

        private void Inner_DrawYLabels(SKCanvas canvas, SKPoint plotOrigin, SKSize plotSize, float textHeight, SKColor axisTextColor, float minY, float maxY, Swipe pinchPan) {
            float Yloc; // initialize to screen coordinates location of Y=0
            // draw >=0 labels going upwards from 0 or minY
            Yloc = YlocOfYvalInPlotarea(Math.Max(0,minY), minY, maxY, plotSize);
            while (Yloc > textHeight) {
                float Yval = YvalOfYlocInPlotarea(Yloc, minY, maxY, plotSize);
                Inner_DrawLabel_Ver(canvas, Yval.ToString("g3"), plotOrigin.X, plotOrigin.Y + Yloc, false, textHeight, axisTextColor, pinchPan);
                Yloc -= 3 * textHeight;
            }
            // draw <=0 labels goind downwards from 0 or maxY
            Yloc = YlocOfYvalInPlotarea(Math.Min(0,maxY), minY, maxY, plotSize);
            while (Yloc < plotSize.Height - 2 * textHeight) {
                float Yval = YvalOfYlocInPlotarea(Yloc, minY, maxY, plotSize);
                Inner_DrawLabel_Ver(canvas, Yval.ToString("g3"), plotOrigin.X, plotOrigin.Y + Yloc, false, textHeight, axisTextColor, pinchPan);
                Yloc += 3 * textHeight;
            }
        }

        public void DrawContent(SKCanvas canvas, SKPoint plotOrigin, SKSize plotSize, List<Series> seriesList, float textHeight, SKColor axisTextColor, Swipe pinchPan) {
            lock (mutex) {
                this.Inner_Bounds(seriesList, out float minX, out float maxX, out float minY, out float maxY);
                Inner_DrawXLabels(canvas, plotOrigin, plotSize, textHeight, axisTextColor, minX, maxX, pinchPan);
                Inner_DrawYLabels(canvas, plotOrigin, plotSize, textHeight, axisTextColor, minY, maxY, pinchPan);
                Inner_CalculatePoints(plotOrigin, plotSize, minX, maxX, minY, maxY);
                Inner_DrawLines(canvas, seriesList, pinchPan);
            }
        }

    }

    public class Chart {
        private string title = "";
        private string model = "";
        private float margin { get; set; } = 20;
        private float textHeight { get; set; } = 20;
        private SKColor backgroundColor { get; set; } = SKColors.White;
        private SKColor axisTextColor { get; set; } = SKColors.Gray;
        public static byte transparency { get; set; } = 32;

        private List<Series> seriesList;  
        private Timecourse timecourse;

        public Chart(string title, string model) {
            // after a chart is initialized, seriesList should not change, but timecourse will be changed by a concurrent thread
            this.title = title;
            this.model = model;
            this.seriesList = new List<Series>();
            this.timecourse = new Timecourse();
        }

        public Chart(string title, string model, List<Series> seriesList, Timecourse timecourse) {
            // after a chart is initialized, seriesList should not change, but timecourse will be changed by a concurrent thread
            this.title = title;
            this.model = model;
            this.seriesList = seriesList;
            this.timecourse = timecourse;
        }

        public Swipe pinchPan = new Swipe(1.0f, new SKPoint(0, 0));
        public bool displayPinchOrigin = false;
        public SKPoint pinchOrigin;

        public void Draw(SKCanvas canvas, int width, int height) {
            canvas.Clear(this.backgroundColor);
            if (displayPinchOrigin) GraphLayout.CanvasDrawCircle(canvas, pinchOrigin, 20, false, SKColors.LightGray);
            float footerHeight = CalculateFooterHeight();
            float headerHeight = CalculateHeaderHeight();
            SKSize plotSize = CalculatePlotSize(width, height, footerHeight, headerHeight);
            SKPoint plotOrigin = CalculatePlotOrigin(footerHeight);
            timecourse.DrawContent(canvas, plotOrigin, plotSize, seriesList, textHeight, axisTextColor, pinchPan);
        }

        private SKSize CalculatePlotSize(int width, int height, float footerHeight, float headerHeight) {
            var w = width - 2*this.margin;
            var h = height - this.margin - footerHeight - headerHeight; // somehow a margin is added to the footer?
            return new SKSize(w, h);
        }

        private SKPoint CalculatePlotOrigin(float headerHeight) {
            return new SKPoint(margin, headerHeight);
        }

        private float CalculateFooterHeight() {
            var result = this.margin;
            result += this.textHeight + this.margin;
            return result;
        }

        private float CalculateHeaderHeight() {
            return this.margin;
        }

        public void InvertVisible(string seriesName) {
            foreach (Series series in seriesList) {
                if (series.name == seriesName) {
                    series.visible = !series.visible;
                    return;
                }
            }
        }

    }
}