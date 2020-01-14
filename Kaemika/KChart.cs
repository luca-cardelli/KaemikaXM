using System;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp; // just for the structs SKColor, SKPoint, SKSize, SKRect; not for SKCanvas

namespace Kaemika {

    public interface ChartPainter : Painter { // interface for platform-dependent chart rendering
        void DrawLine(List<KChartEntry> list, int seriesIndex, float pointSize, SKColor color, Swipe pinchPan);
        void DrawLineRange(List<KChartEntry> list, int seriesIndex, SKColor color, Swipe pinchPan);
    }

    // "The" current KChart is stored inside the XXChartView of each platform, is viewed inside the GUI by the XXChartView, and is painted by XXChartPainter
    // The platform-independent GUIinterface accesses the platform-dependent GUI and through it the XXChartView

    public abstract class KChartHandler {

        public static Style executionStyle = null;
        private static KChart chart = null;             // <<============== the only chart
        private static Dictionary<string, Dictionary<string, bool>> visibilityCache =  // a visibility cache for each Chart.model
            new Dictionary<string, Dictionary<string, bool>>();

        //public static bool Exists() {
        //    return chart != null;
        //}
        //public static void Start(string title, string model, KTimecourse timecourse) {
        //    chart = new KChart(title, model, timecourse);
        //}
        //public static KChart ReStart(string model) {
        //    if (chart == null) chart = new KChart("", new KTimecourse(model));
        //    else {
        //        chart = new KChart(chart.Title(), chart.timecourse);
        //        chart.timecourse.SetModel(model);
        //    }
        //    return chart; // for Android/iOS binding to a property
        //}
        //public static void Stop() {
        //    chart = null;
        //}
        public static string SampleName() {
            if (chart == null) return "";
            return chart.SampleName(); 
        }
        public static void SetSampleName(string sampleName) {
            if (chart == null) return;
            chart.SetSampleName(sampleName); 
        }
        public static void SetStyle(Style style) {
            if (chart == null) return;
            executionStyle = style;
        }
        public static void ChartClear(string sampleName) {
            //// do not test for char==null: we must create the first chart!
            chart = new KChart(sampleName);
        }
        public static KChart ChartCopy() {
            // for Xamarin (Android/iOS) we must assign a fresh value to the chart view to cause it to invalidate and redraw;
            // this produces a fresh value for the same chart: it is essentially an Invalidate command
            if (chart == null) chart = new KChart("");
            else chart = new KChart(chart.SampleName(), chart.timecourse);
            return chart;
        }
        public static void ChartClearData() {
            if (chart == null) return;
            chart.timecourse.ClearData();
        }
        public static bool IsClear() {
            if (chart == null) return true;
            return chart.IsClear();
        }
        public static KSeries ChartSeriesNamed(string name) {
            if (name == null) return null;
            if (chart == null) return null;
            return chart.timecourse.SeriesNamed(name);
        }
        public static string ChartAddSeries(string legend, Flow asFlow, Color color, Noise noise) {
            if (chart == null) return null;
            if (noise == Noise.None) {
                return chart.timecourse.AddSeries(new KSeries(legend, asFlow, color, KLineMode.Line, KLineStyle.Thick));
            } else if (noise == Noise.Sigma || noise == Noise.SigmaSq || noise == Noise.CV || noise == Noise.Fano) {
                return chart.timecourse.AddSeries(new KSeries(legend, asFlow, color, KLineMode.Line, KLineStyle.Thin));
            } else if (noise == Noise.SigmaRange || noise == Noise.SigmaSqRange) {
                return chart.timecourse.AddSeries(new KSeries(legend, asFlow, System.Drawing.Color.FromArgb(KChart.transparency, color), KLineMode.Range, KLineStyle.Thin));
            } else throw new Error("ChartAddSeries");
        }
        public static void ChartAddPoint(string seriesName, double t, double mean, double variance, Noise noise) {
            if (chart == null) return;
            if (seriesName != null) {
                if (noise == Noise.None) chart.timecourse.AddPoint(seriesName, (float)t, (float)mean);
                if (noise == Noise.SigmaSq) chart.timecourse.AddPoint(seriesName, (float)t, (float)variance);
                if (noise == Noise.Sigma) chart.timecourse.AddPoint(seriesName, (float)t, (float)Math.Sqrt(variance));
                if (noise == Noise.CV) chart.timecourse.AddPoint(seriesName, (float)t, (float)((mean == 0.0) ? 0.0 : (Math.Sqrt(variance) / mean)));
                if (noise == Noise.Fano) chart.timecourse.AddPoint(seriesName, (float)t, (float)((mean == 0.0) ? 0.0 : (variance / mean)));
                if (noise == Noise.SigmaSqRange) chart.timecourse.AddRange(seriesName, (float)t, (float)mean, (float)variance);
                if (noise == Noise.SigmaRange) chart.timecourse.AddRange(seriesName, (float)t, (float)mean, (float)Math.Sqrt(variance));
            }
        }
        public static string ChartAddPointAsString(string seriesName, double t, double mean, double variance, Noise noise) {
            string s = "";
            if (seriesName != null) {
                s += seriesName + "=";
                if (noise == Noise.None) s += mean.ToString();
                if (noise == Noise.SigmaSq) s += variance.ToString();
                if (noise == Noise.Sigma) s += Math.Sqrt(variance);
                if (noise == Noise.CV) s += ((mean == 0.0) ? 0.0 : (Math.Sqrt(variance) / mean)).ToString();
                if (noise == Noise.Fano) s += ((mean == 0.0) ? 0.0 : (variance / mean)).ToString();
                if (noise == Noise.SigmaSqRange) s += mean.ToString() + "±" + variance.ToString();
                if (noise == Noise.SigmaRange) { double sd = Math.Sqrt(variance); s += mean.ToString() + "±" + sd.ToString(); }
            }
            return s;
        }
        public static KSeries[] Legend() {
            if (chart == null) return new KSeries[0];
            return chart.timecourse.Legend();
        }
        public static void VisibilityRemember() {
            if (chart == null) return;
            chart.timecourse.VisibilityRemember(visibilityCache, chart.SampleName());
        }
        public static void VisibilityRestore() {
            if (chart == null) return;
            chart.timecourse.VisibilityRestore(visibilityCache, chart.SampleName());
        }
        public static void SetVisible(string seriesName, bool visible) {
            if (chart == null) return;
            chart.timecourse.SetVisible(seriesName, visible);
        }
        public static void InvertVisible(string seriesName) {
            if (chart == null) return;
            chart.timecourse.InvertVisible(seriesName);
        }
        public static void ShowEndNames(bool show) {
            if (chart == null) return;
            chart.timecourse.ShowSeriesTags(show);
        }
        public static void SetMeanFlowDictionary(Dictionary<SpeciesValue, Flow> dictionary) {
            if (chart == null) return;
            chart.timecourse.SetMeanFlowDictionary(dictionary);
        }
        public static void Draw(ChartPainter painter, int originX, int originY, int width, int height) {
            if (chart == null) return;
            chart.Draw(painter, originX, originY, width, height);
        }
        public static SKSize MeasureLegend(Colorer colorer, float textHeight) {
            if (chart == null) return new SKSize(0,0);
            return chart.MeasureLegend(colorer, textHeight);
        }
        public static void DrawLegend(Painter painter, SKPoint origin, SKSize size, float textHeight) {
            if (chart == null) return;
            chart.DrawLegend(painter, origin, size, textHeight);
        }
        public static string HitListTooltip(SKPoint target, float yRadius) {
            if (chart == null) return "";
            (string xValue, string[] yFlows, string[] yValues, string endNameInfo) = chart.HitList(target, yRadius);
            if (endNameInfo != null) return endNameInfo;
            else return chart.HitListTooltip(xValue, yFlows, yValues);
        }
        public static string ToCSV() {
            if (chart == null) return "";
            return chart.ToCSV();
        }
        public static void Snap(Func<Colorer> GenColorer, Func<SKSize, ChartPainter> GenPainter, SKSize chartSize) {
            try {
                var pointSize = 12.0f;
                var header = 3 * pointSize;
                var spacer = 2 * pointSize;

                Colorer colorer = GenColorer();

                CRN crn = Exec.lastExecution != null ? Exec.lastExecution.lastCRN : null;
                SKSize legendSize = KChartHandler.MeasureLegend(colorer, pointSize);
                SKSize reactionsSize = (crn != null) ? crn.Measure(colorer, pointSize, Exec.lastExecution.style) : new SKSize(0,0);
                SKSize canvasSize = new SKSize(chartSize.Width + legendSize.Width + spacer + reactionsSize.Width, Math.Max(chartSize.Height, Math.Max(header+legendSize.Height, header+reactionsSize.Height)));

                ChartPainter chartPainter = GenPainter(canvasSize);

                Draw(chartPainter, 0, 0, (int)chartSize.Width, (int)chartSize.Height);
                DrawLegend(chartPainter, new SKPoint(chartSize.Width, header), legendSize, pointSize);
                if (crn != null) crn.Draw(chartPainter, new SKPoint(chartSize.Width + legendSize.Width + spacer, header), reactionsSize, pointSize, Exec.lastExecution.style);
            } catch { }
        }
    }

    public class KChart {
        private float pointSize;
        private float margin; // margin around the left, right, bottom, and between the top and the tile
        private float textHeight;  // for axis labels text, >= margin
        private float titleHeight; // for title text, >= textHeight
        private SKColor backgroundColor = SKColors.White;
        private SKColor axisColor = SKColors.Gray;
        private SKColor zeroColor = SKColors.Purple;
        public static byte transparency { get; set; } = 32;

        public KTimecourse timecourse;

        private void Init(string newSampleName, KTimecourse timecourse) {
            this.timecourse = timecourse;
            this.timecourse.SetSampleName(newSampleName);
            var plat = Gui.platform;
            float platformScaling = (plat == Platform.Windows || plat == Platform.macOS) ? 0.6f : (plat == Platform.Android || plat == Platform.iOS) ? 1.5f : 1.0f; //### should make it propotional to the canvas size
            this.margin = 20.0f; // * platformScaling;
            this.textHeight = 20.0f; // * platformScaling;
            this.titleHeight = 22.0f; // * platformScaling;
        }

        public KChart(string sampleName) {
            Init(sampleName, new KTimecourse(sampleName));
        }

        public KChart(string newSampleName, KTimecourse timecourse) {
            Init(newSampleName, timecourse);
        }

        public string SampleName() { return this.timecourse.SampleName(); }
        public void SetSampleName(string model) { this.timecourse.SetSampleName(model); }

        public bool IsClear() {
            return timecourse.IsClear();
        }

        public Swipe pinchPan = Swipe.Id;
        public bool displayPinchOrigin = false;
        public SKPoint pinchOrigin;

        public float PointSize(int width, int height) {
            // determine how big one "point" should be relative to the available drawing resolution
            var textHeight = height / 14.0f;                                     // at least 14 text lines should fit on the Y axis (5 ticks)
            var pointSize = textHeight / 20.0f;                                  // resolution is set at 14x20 = 280 "points"
            pointSize = Math.Min(pointSize, (float)Math.Sqrt(pointSize));        // but do not let the pointsize get too big as the screen grows
            return pointSize;
        }

        // called asynchronously by the GUI
        public void Draw(ChartPainter painter, int originX, int originY, int width, int height) {
            this.pointSize = PointSize(width, height);
            this.margin = 12.0f * pointSize;
            this.textHeight = 12.0f * pointSize;
            this.titleHeight = 14.0f * pointSize;
            painter.Clear(this.backgroundColor);
            if (displayPinchOrigin) using (var paint = painter.FillPaint(SKColors.LightGray)) { painter.DrawCircle(pinchOrigin, 20, paint); }
            float headerHeight = CalculateHeaderHeight();
            float footerHeight = CalculateFooterHeight();
            SKSize plotSize = CalculatePlotSize(width, height, footerHeight, headerHeight);
            SKPoint plotOrigin = CalculatePlotOrigin(headerHeight); // in screen coordinates, Y pointing down
            timecourse.DrawContent(painter, new SKPoint(originX + plotOrigin.X, originY + plotOrigin.Y), plotSize, pointSize, margin, this.titleHeight, this.textHeight, zeroColor, axisColor, pinchPan); // lock protected
        }

        public void DrawLegend(Painter painter, SKPoint origin, SKSize size, float textHeight) {
            timecourse.DrawLegend(painter, origin, size, textHeight);
        }

        public SKSize MeasureLegend(Colorer colorer, float textHeight) {
            return timecourse.MeasureLegend(colorer, textHeight);
        }

        private SKSize CalculatePlotSize(int width, int height, float footerHeight, float headerHeight) {
            var w = width - 2*this.margin;
            var h = height - footerHeight - headerHeight;
            return new SKSize(w, h);
        }

        private SKPoint CalculatePlotOrigin(float headerHeight) {
            return new SKPoint(margin, headerHeight);
        }

        private float CalculateHeaderHeight() {
            return this.titleHeight + this.margin;
        }

        private float CalculateFooterHeight() {
            return this.margin;
        }

        public (string xValue, string[] yFlows, string[] yValues, string endName) HitList(SKPoint target, float radius) {
            return this.timecourse.HitList(target, radius);
        }

        public string HitListTooltip(string xValue, string[] yFlows, string[] yValues) {
            if (yFlows == null) return "";
            string time = "time";
            int yFlowsWidth = 0;
            for (int i = 0; i < yFlows.Length; i++)
                if (yFlows[i] != null)
                    yFlowsWidth = Math.Max(yFlowsWidth, yFlows[i].Length);
            yFlowsWidth = Math.Max(yFlowsWidth, time.Length);
            string toolTip = "";
            for (int i = 0; i < yFlows.Length; i++)
                if (yFlows[i] != null)
                    toolTip += yFlows[i] + new string(' ', yFlowsWidth - yFlows[i].Length + 1) + yValues[i] + Environment.NewLine;
            toolTip += time + new string(' ', yFlowsWidth - time.Length + 1) + xValue;
            return toolTip;
        }

        public string ToCSV() {
            return timecourse.ToCSV();
        }

    }

    public enum KLineMode { Line, Range }
    public enum KLineStyle { Thin, Thick }

    public class KSeries {
        public string name;
        public Flow asFlow;
        public SKColor color;
        public KLineMode lineMode;    // this tells us if it is a mean (Line) or mean+-variance (Range) series
        public KLineStyle lineStyle;  // a KLineMode.Line can be KLineStyle.Thin or KLineStyle.Thick (but a KLineMode.Range can only be KLineStyle.Thin?)
        public bool visible;
        public KSeries(string name, Flow asFlow, Color color, KLineMode lineMode, KLineStyle lineStyle) {
            this.name = name;
            this.asFlow = asFlow;
            this.color = new SKColor(color.R, color.G, color.B, color.A);
            this.lineMode = lineMode;
            this.lineStyle = lineStyle;
            this.visible = true;
        }
    }

    public class KChartEntry {
        public float X;                                // This is value on the X axis, e.g. time point
        public float[] Y;                              // This are the values on the Y axis.
        public float[] Yrange;                         // This are the ranges + or - over the Y value.
        public SKPoint[] Ypoint;                       // This is where the Y points are plotted
        public float[] YpointRange;                    // This is + or - the ranges of where the points are plotted

        public KChartEntry(float X, float[] Y, float[] Yrange) { //, string[] Ylabel) {
            this.X = X;
			this.Y = Y;
            this.Ypoint = new SKPoint[Y.Length];
            this.Yrange = Yrange;
            this.YpointRange = new float[Yrange.Length];
        }

        public float MinY(List<KSeries> seriesList) {
            float min = float.MaxValue;
            for (int i = 0; i < Y.Length; i++) if (seriesList[i].visible) min = Math.Min(Y[i]-Yrange[i], min);
            return min;
        }

        public float MaxY(List<KSeries> seriesList) {
            float max = float.MinValue;
            for (int i = 0; i < Y.Length; i++) if (seriesList[i].visible) max = Math.Max(Y[i]+Yrange[i], max);
            return max;
        }
    }

    public class KTimecourse {                           // assumes points arrive in time equal or increasing
        private object mutex;
        private string sampleName;
        private List<KSeries> seriesList;                // seriesList[i] matches list[t].Y[i] for all t
        private List<KChartEntry> list;
        private KChartEntry lastEntry;                   // the last entry to accumulate the equal-time points
        private int lastEntryCount;                      // to know when we have completed the last entry
        private Dictionary<string, int> seriesIndex;     // maintaining the connection between seriesList and timecourse

        public KTimecourse(string sampleName) {
            this.mutex = new object();
            this.sampleName = sampleName;
            this.seriesList = new List<KSeries>();
            this.list = new List<KChartEntry>();
            this.seriesIndex = new Dictionary<string, int>();
            this.lastEntry = null;
            this.lastEntryCount = 0;
        }

        public string SampleName() { return this.sampleName; }
        public void SetSampleName(string sampleName) { this.sampleName = sampleName; }

        private Dictionary<SpeciesValue, Flow> meanFlowDictionary = null;
        public void SetMeanFlowDictionary(Dictionary<SpeciesValue, Flow> dictionary) {
            meanFlowDictionary = dictionary;
        }

        public void ClearData() {
            lock (mutex) {
                this.list = new List<KChartEntry>();
                this.lastEntry = null;
                this.lastEntryCount = 0;
            }
        }

        public KSeries SeriesNamed(string name) {
            if (seriesIndex.ContainsKey(name)) return seriesList[seriesIndex[name]];
            else return null;
        }

        public string AddSeries(KSeries series) {
            lock (mutex) {
                if (seriesList.Exists(e => e.name == series.name)) return null;  // give null on duplicate series
                seriesList.Add(series);
                seriesIndex.Clear();
                for (int i = 0; i < seriesList.Count; i++) seriesIndex.Add(seriesList[i].name, i);
                return series.name;
            }
        }

        public KSeries[] Legend() {
            lock (mutex) { return this.seriesList.ToArray(); }
        }

        public bool IsClear()  {
            lock (mutex) { return list.Count == 0; }
        }

        public void AddPoint(string seriesName, float t, float mean) {
            // locks mutex
            AddRange(seriesName, t, mean, 0); 
        }

        public void AddRange(string seriesName, float t, float mean, float variance) {
            lock (mutex) {
                if (float.IsNaN(mean)) mean = 0;            // these have been converted from double
                if (float.IsNaN(variance)) variance = 0;    // these have been converted from double
                if (seriesIndex.ContainsKey(seriesName)) {  // if not, it may be due to a concurrent invocations of plotting before the previous one has finished
                    int index = seriesIndex[seriesName];
                    if (lastEntry == null) {
                        var Y = new float[seriesList.Count];
                        var Yrange = new float[seriesList.Count];
                        //var Ylabel = new string[seriesList.Count];
                        Y[index] = mean;
                        Yrange[index] = variance;
                        //Ylabel[index] = seriesName;
                        lastEntry = new KChartEntry(X: t, Y: Y, Yrange: Yrange); //, Ylabel: Ylabel);
                        lastEntryCount = 1;
                    } else  {
                        lastEntry.Y[index] = mean;
                        lastEntry.Yrange[index] = variance;
                        //lastEntry.Ylabel[index] = seriesName;
                        lastEntryCount++;
                    }
                    if (lastEntryCount == seriesList.Count) {
                        list.Add(lastEntry);
                        lastEntry = null;
                        lastEntryCount = 0;
                    }
                }
            }
        }

        public (string xValue, string[] yFlows, string[] yValues, string seriesTag) HitList(SKPoint target, float radius) {
            // return the series that are under target within radius (or null)
            // target and radius are in screen plot coordinates, checked agains the precomputed Ypoint and YpointRange
            lock (mutex) {

                // find hits on the series tags
                if (showSeriesTags && meanFlowDictionary != null && seriesTagLocations != null) {
                    KSeries seriesHit = null;
                    foreach (var keyPair in seriesTagLocations) {
                        KSeries series = keyPair.Key;
                        SKRect location = keyPair.Value;
                        if (location.Contains(new SKRect(target.X, target.Y, target.X + 1, target.Y + 1))) {
                            seriesHit = series;
                            break;
                        }
                    }
                    if (seriesHit != null) {
                        if (seriesHit.lineMode == KLineMode.Line && seriesHit.lineStyle == KLineStyle.Thick) {
                            string seriesTag = "";
                            foreach (var keyPair in meanFlowDictionary) {
                                SpeciesValue headSpecies = keyPair.Key;
                                Flow derivative = keyPair.Value; 
                                if (seriesHit.asFlow.Involves(new List<SpeciesValue> { headSpecies }))
                                    seriesTag += derivative.FormatAsODE(headSpecies, KChartHandler.executionStyle, "∂ ") + Environment.NewLine;
                            }
                            if (seriesTag != "") return (null, null, null, seriesTag.Substring(0, seriesTag.Length - 1));
                            else return (null, null, null, "");  // mask the data under the series tag
                        } else return (null, null, null, "");  // mask the data under the series tag
                    }
                }

                // find hits on the species trajectories
                int speciesNo = seriesList.Count;
                // first find the X value of the closest X-Y hit
                float closestXhit = float.MaxValue;
                KChartEntry closestEntry = null;
                foreach (KChartEntry entry in list) {
                    for (int s = 0; s < speciesNo; s++) {
                        if (seriesList[s].visible) {
                            SKPoint point = entry.Ypoint[s];
                            float range = entry.YpointRange[s];
                            if ((point.X >= (target.X - radius)) && (point.X <= (target.X + radius)) &&                      // point.X intersects target.X+-radius
                                ((point.Y + range) >= (target.Y - radius)) && ((point.Y - range) <= (target.Y + radius))) {  // point.Y+-range intersects targetY+-radius
                                float distX = Math.Abs(point.X - target.X);
                                if (distX < closestXhit) { closestXhit = distX; closestEntry = entry; }
                            }
                        }
                    }
                }
                // then for that X value, return the Y values that are hits
                if (closestEntry == null) return (null, null, null, null);
                string xValue = "= " + closestEntry.X.ToString("g3");
                string[] yFlows = new string[speciesNo]; // indexed by (speciesNo - 1 - s) instead of (s), to reverse species list into natural order
                string[] yValues = new string[speciesNo]; // indexed by (speciesNo - 1 - s)
                for (int s = 0; s < speciesNo; s++) {
                    if (seriesList[s].visible) {
                        SKPoint point = closestEntry.Ypoint[s];
                        float range = closestEntry.YpointRange[s];
                        if (((point.Y + range) >= (target.Y - radius)) && ((point.Y - range) <= (target.Y + radius))) {
                            yFlows[speciesNo - 1 - s] = seriesList[s].name;
                            yValues[speciesNo - 1 - s] =
                                "= " + closestEntry.Y[s].ToString("g3")
                                + ((closestEntry.Yrange[s] > 0.0f) ? " ± " + closestEntry.Yrange[s].ToString("g3") : "");
                        }
                    }
                }
                return (xValue, yFlows, yValues, null);
            }
        }

        public void VisibilityClear() {
            lock (mutex) {
                foreach (var series in seriesList) series.visible = true;
            }
        }

        public void VisibilityRemember(Dictionary<string, Dictionary<string, bool>> visibility, string model) {
            lock (mutex) {
                if (!visibility.ContainsKey(model)) visibility[model] = new Dictionary<string, bool>();
                foreach (var series in seriesList) visibility[model][series.name] = series.visible;
            }
        }

        public void VisibilityRestore(Dictionary<string, Dictionary<string, bool>> visibility, string model) {
            lock (mutex) {
                if (!visibility.ContainsKey(model)) visibility[model] = new Dictionary<string, bool>();
                foreach (var keyPair in visibility[model]) {
                    if (seriesIndex.ContainsKey(keyPair.Key))
                        seriesList[seriesIndex[keyPair.Key]].visible = keyPair.Value;
                }
            }
        }

        public void SetVisible(string seriesName, bool visible) {
            lock (mutex) {
                foreach (KSeries series in seriesList) {
                    if (series.name == seriesName) {
                        series.visible = visible;
                        return;
                    }
                }
            }
        }

        public void InvertVisible(string seriesName) {
            lock (mutex) {
                foreach (KSeries series in seriesList) {
                    if (series.name == seriesName) {
                        series.visible = !series.visible;
                        return;
                    }
                }
            }
        }

        private (float minX, float maxX, float minY, float maxY) Inner_Bounds() {
            if (list.Count == 0) { return(0,0,0,0); }
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < list.Count; i++) {
                minX = Math.Min(minX, list[i].X);
                maxX = Math.Max(maxX, list[i].X);
                minY = Math.Min(minY, list[i].MinY(seriesList));
                maxY = Math.Max(maxY, list[i].MaxY(seriesList));
            }
            return (minX, maxX, minY, maxY);
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
       
        private void Inner_CalculatePoints(SKPoint plotOrigin, SKSize plotSize, float minX, float maxX, float minY, float maxY) {
            for (int i = 0; i < list.Count; i++) {
                KChartEntry entry = list[i];
                float x = plotOrigin.X + XlocOfXvalInPlotarea(entry.X, minX, maxX, plotSize);
                for (int j = 0; j < entry.Y.Length; j++) {
                    var y = plotOrigin.Y + YlocOfYvalInPlotarea(entry.Y[j], minY, maxY, plotSize);
                    entry.Ypoint[j] = new SKPoint(x, y);
                    entry.YpointRange[j] = (entry.Yrange[j] == 0) ? 0 : YlocRangeOfYvalRangeInPlotarea(entry.Yrange[j], minY, maxY, plotSize);
                }
            }
        }

        private void Inner_DrawTitle(ChartPainter painter, string title, SKPoint plotOrigin, SKSize plotSize, float margin, float titleHeight, Swipe pinchPan) {
            using (var paint = painter.TextPaint(painter.font, pinchPan % titleHeight, SKColors.Black)) {
                paint.TextAlign = SKTextAlign.Center;
                painter.DrawText(title, pinchPan % new SKPoint(plotOrigin.X + plotSize.Width / 2.0f, plotOrigin.Y - margin), paint);
            }
        }

        private float SignificantOne(float n) {
            return float.Parse(n.ToString("G1"));
        }

        private void Inner_DrawXMark(ChartPainter painter, float X, float Y, float markLength, SKColor theColor, Swipe pinchPan) {
            var r = pinchPan % new SKRect(X - 1, Y, X + 1, Y + markLength);
            using (var paint = painter.FillPaint(theColor)) {
                painter.DrawRect(new SKRect(r.Left, Y + markLength - r.Height, r.Right, Y + markLength), paint); // Clamped to bottom of hor axis
            }
        }

        private void Inner_DrawXLabel(ChartPainter painter, string text, float X, float Y, float markLength, float textHeight, SKColor theColor, Swipe pinchPan){
            using (var paint = painter.TextPaint(painter.font, pinchPan % textHeight, theColor)) {
                painter.DrawText(text, new SKPoint((pinchPan % new SKPoint(X + 6, 0)).X, Y + textHeight), paint); // Clamped to hor axis
            }
        }

        private void Inner_DrawXLabels(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float margin, float textHeight, SKColor zeroColor, SKColor color, float minX, float maxX, Swipe pinchPan) {
            const float epsilon = 0.001f;
            if (minX == maxX) return;
            float labelSize = painter.MeasureText("| 0.001 ms  ", painter.TextPaint(painter.font, textHeight, SKColors.Red)).Width;
            float minXtickVal = SignificantOne(minX);
            float maxXtickVal = SignificantOne(maxX);

            float minXTickLoc = XlocOfXvalInPlotarea(minXtickVal, minX, maxX, plotSize);
            float maxXTickLoc = XlocOfXvalInPlotarea(maxXtickVal, minX, maxX, plotSize);
            int numOfTicks = (int)((maxXTickLoc - minXTickLoc) / labelSize);
            if (numOfTicks == 0) return;
            int power = 1; while (power < numOfTicks) power *= 2;
            numOfTicks = power; if (numOfTicks >= 2) numOfTicks /= 2; // set numOfTicks to biggest power of 2 less then numOfTicks

            float incrXtickVal = (maxXtickVal - minXtickVal) / numOfTicks; 
            if (incrXtickVal == 0) return;

            float iXtickVal = minXtickVal;
            float iXtickLoc;
            do {
                iXtickLoc = XlocOfXvalInPlotarea(iXtickVal, minX, maxX, plotSize);
                var X = plotOrigin.X + iXtickLoc;
                var Y = plotOrigin.Y + plotSize.Height;
                SKColor theColor = (iXtickVal == 0.0f) ? zeroColor : color;
                if (iXtickLoc >= -epsilon && iXtickLoc <= plotSize.Width + epsilon)
                    Inner_DrawXMark(painter, X, Y, margin, theColor, pinchPan);
                if (iXtickLoc >= -epsilon && iXtickLoc <= plotSize.Width - labelSize + epsilon)
                    Inner_DrawXLabel(painter, Kaemika.Gui.FormatUnit(iXtickVal, " ", "s", "g3"), X, Y - 2, margin, textHeight, theColor, pinchPan);
                iXtickVal += incrXtickVal;
            } while (iXtickLoc <= plotSize.Width + epsilon);
            // the point series from the simulation may not reach quite up to the given time limit, hence omitting the last mark by a hair, so we plot it anyway:
            if (iXtickLoc < plotSize.Width + margin) Inner_DrawXMark(painter, plotOrigin.X + maxXTickLoc, plotOrigin.Y + plotSize.Height, margin, zeroColor, pinchPan);
        }

        private void Inner_DrawYMark(ChartPainter painter, float X, float Y, float markLength, SKColor theColor, Swipe pinchPan) {
            var r = pinchPan % new SKRect(X, Y - 1, X + markLength, Y + 1);
            using (var paint = painter.FillPaint(theColor)) {
                painter.DrawRect(new SKRect(X, r.Top, X + r.Width, r.Bottom), paint); // Clamped to left of ver axis
            }
       }

        private void Inner_DrawYLabel(ChartPainter painter, string text, float X, float Y, float markLength, float textHeight, SKColor theColor, Swipe pinchPan){
            using (var paint = painter.TextPaint(painter.font, pinchPan % textHeight, theColor)) {
                painter.DrawText(text, new SKPoint(X, (pinchPan % new SKPoint(0, Y - 6)).Y), paint); // Clamped to ver axis
            }
        }

        private bool Inner_DrawYLabelsUpward(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float margin, float textHeight, SKColor zeroColor, SKColor color, float minY, float maxY, Swipe pinchPan,
                                             float minYtickVal, float maxYtickVal, float incrYtickVal, bool paintZero) {
            float locEpsilon = 2; // pixels
            bool paintedZero = false;
            float iYtickVal = minYtickVal;
            float iYtickLoc;
            do {
                iYtickLoc = YlocOfYvalInPlotarea(iYtickVal, minY, maxY, plotSize);
                bool isZero = iYtickVal == 0.0f;
                if (iYtickLoc >= -locEpsilon && iYtickLoc <= plotSize.Height + locEpsilon && (paintZero || !isZero)) {
                    if (isZero) paintedZero = true;
                    var X = plotOrigin.X - margin;
                    var Y = plotOrigin.Y + iYtickLoc;
                    SKColor theColor = isZero ? zeroColor : color;
                    Inner_DrawYMark(painter, X, Y, margin, theColor, pinchPan);
                    Inner_DrawYLabel(painter, Kaemika.Gui.FormatUnit(iYtickVal, " ", "M", "g3"), X + 2, Y, margin, textHeight, theColor, pinchPan);
                }
                iYtickVal += incrYtickVal;
            } while (iYtickLoc >= -locEpsilon);
            return paintedZero;
        }

        private bool Inner_DrawYLabelsDownward(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float margin, float textHeight, SKColor zeroColor, SKColor color, float minY, float maxY, Swipe pinchPan,
                                               float minYtickVal, float maxYtickVal, float incrYtickVal, bool paintZero) {
            float locEpsilon = 2; // pixels
            bool paintedZero = false;
            float iYtickVal = maxYtickVal;
            float iYtickLoc;
            do {
                iYtickLoc = YlocOfYvalInPlotarea(iYtickVal, minY, maxY, plotSize);
                bool isZero = iYtickVal == 0.0f;
                if (iYtickLoc >= -locEpsilon && iYtickLoc <= plotSize.Height + locEpsilon && (paintZero || !isZero)) {
                    if (isZero) paintedZero = true;
                    var X = plotOrigin.X - margin;
                    var Y = plotOrigin.Y + iYtickLoc;
                    SKColor theColor = isZero ? zeroColor : color;
                    Inner_DrawYMark(painter, X, Y, margin, theColor, pinchPan);
                    Inner_DrawYLabel(painter, Kaemika.Gui.FormatUnit(iYtickVal, " ", "M", "g3"), X + 2, Y, margin, textHeight, theColor, pinchPan);
                }
                iYtickVal -= incrYtickVal;
            } while (iYtickLoc <= plotSize.Height + locEpsilon);
            return paintedZero;
        }

        private void Inner_DrawYLabels(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float margin, float textHeight, SKColor zeroColor, SKColor color, float minY, float maxY, Swipe pinchPan) {
            float labelSize = 3 * textHeight;
            float minYtickVal = SignificantOne(minY);
            float maxYtickVal = SignificantOne(maxY);

            float minYTickLoc = YlocOfYvalInPlotarea(minYtickVal, minY, maxY, plotSize);
            float maxYTickLoc = YlocOfYvalInPlotarea(maxYtickVal, minY, maxY, plotSize);
            int numOfTicks = (int)((minYTickLoc - maxYTickLoc) / labelSize); // y axis inversion
            if (numOfTicks <= 0) return;
            int power = 1; while (power < numOfTicks) power *= 2;
            numOfTicks = power; if (numOfTicks >= 2) numOfTicks /= 2; // set numOfTicks to biggest power of 2 less then numOfTicks

            float incrYtickVal = SignificantOne(maxYtickVal - minYtickVal) / numOfTicks;  // round interval off by SignificantOne
            if (incrYtickVal <= 0) return;

            if (minY > 0.0f)      
                Inner_DrawYLabelsUpward  (painter, plotOrigin, plotSize, margin, textHeight, zeroColor, color, minY, maxY, pinchPan,
                                          minYtickVal, maxYtickVal, incrYtickVal, true);
            else if (maxY < 0.0f) 
                Inner_DrawYLabelsDownward(painter, plotOrigin, plotSize, margin, textHeight, zeroColor, color, minY, maxY, pinchPan,
                                          minYtickVal, maxYtickVal, incrYtickVal, true);
            else {
                bool paintedZero =
                Inner_DrawYLabelsDownward(painter, plotOrigin, plotSize, margin, textHeight, zeroColor, color, minY, maxY, pinchPan,
                                          minYtickVal, 0.0f, incrYtickVal, true);
                Inner_DrawYLabelsUpward  (painter, plotOrigin, plotSize, margin, textHeight, zeroColor, color, minY, maxY, pinchPan,
                                          0.0f, maxYtickVal, incrYtickVal, !paintedZero);
            }
        }

        private Dictionary<KSeries, SKRect> seriesTagLocations = null;

        private SKRect PositionSeriesTag(KSeries series, SKPoint point, SKRect textBounds)  {
            SKRect position = new SKRect(point.X - textBounds.Width, point.Y - textBounds.Height, point.X, point.Y);
            foreach (var conflict in seriesTagLocations) {
                SKRect inter = SKRect.Intersect(position, conflict.Value);
                if (inter.Width > 0 && inter.Height > 0)
                    return PositionSeriesTag(series, new SKPoint(conflict.Value.Left - 3, point.Y), textBounds);
            }
            seriesTagLocations[series] = position;
            return position;
        }

        private bool showSeriesTags = false;

        public void ShowSeriesTags(bool show) {
            showSeriesTags = show;
        }

        private void Inner_DrawSeriesTag(ChartPainter painter, int seriesIndex, float textHeight, float margin, Swipe pinchPan) {
            if (showSeriesTags && list.Count > 0) {
                SKPaint textPaint = painter.TextPaint(painter.font, pinchPan % textHeight, seriesList[seriesIndex].color);
                string name = seriesList[seriesIndex].name;
                SKPoint endPoint = list[list.Count - 1].Ypoint[seriesIndex];
                SKRect textBounds = SKRect.Inflate(painter.MeasureText(name, textPaint), 3, 3);
                if (float.IsNaN(endPoint.X) || float.IsInfinity(endPoint.X) || float.IsNaN(endPoint.Y) || float.IsInfinity(endPoint.Y)) return;
                //SKRect position = PositionSeriesTag(seriesList[seriesIndex], new SKPoint(endPoint.X + margin - 4, endPoint.Y), textBounds);
                SKRect position = PositionSeriesTag(seriesList[seriesIndex], new SKPoint(endPoint.X - 2, endPoint.Y), textBounds);
                painter.DrawRoundRect(position, 2, painter.FillPaint(SKColors.Gray));
                painter.DrawRoundRect(SKRect.Inflate(position,-1,-1), 2, painter.FillPaint(SKColors.White));
                painter.DrawText(name, new SKPoint(position.Left + 3, position.Bottom - 3), textPaint);
            }
        }

        private void Inner_DrawSeriesTags(ChartPainter painter, float textHeight, float margin, Swipe pinchPan) {
            seriesTagLocations = new Dictionary<KSeries, SKRect>();
            for (int j = 0; j < seriesList.Count; j++) {
                KSeries series = seriesList[j];
                if (series.visible) {
                    if (series.lineMode == KLineMode.Line) {
                        Inner_DrawSeriesTag(painter, j, textHeight, margin, pinchPan);
                    } else if (series.lineMode == KLineMode.Range && list.Count > 1) {
                    }
                }
            }
        }

        private void Inner_DrawLines(ChartPainter painter, float pointSize, float margin, Swipe pinchPan) {
            for (int j = 0; j < seriesList.Count; j++) {
                KSeries series = seriesList[j];
                if (series.visible) {
                    if (series.lineMode == KLineMode.Line) {
                        var weight = series.lineStyle == KLineStyle.Thin ? 1 * pointSize : 3 * pointSize;
                        painter.DrawLine(list, j, weight, series.color, pinchPan);
                    } else if (series.lineMode == KLineMode.Range && list.Count > 1) {
                        painter.DrawLineRange(list, j, series.color, pinchPan);
                    }
                }
            }
        }

        public void DrawContent(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float pointSize, float margin, float titleHeight, float textHeight, SKColor zeroColor, SKColor axisColor, Swipe pinchPan) {
            lock (mutex) {
                (float minX, float maxX, float minY, float maxY) = Inner_Bounds();
                Inner_DrawTitle(painter, this.sampleName, plotOrigin, plotSize, margin, titleHeight, pinchPan);
                Inner_DrawXLabels(painter, plotOrigin, plotSize, margin, textHeight, zeroColor, axisColor, minX, maxX, pinchPan);
                Inner_DrawYLabels(painter, plotOrigin, plotSize, margin, textHeight, zeroColor, axisColor, minY, maxY, pinchPan);
                Inner_CalculatePoints(plotOrigin, plotSize, minX, maxX, minY, maxY);
                Inner_DrawLines(painter, pointSize, margin, pinchPan);
                Inner_DrawSeriesTags(painter, textHeight, margin, pinchPan);
            }
        }

        public SKSize MeasureLegend(Colorer colorer, float textHeight) {
            lock (mutex) {
                SKPoint origin = new SKPoint(0,0);
                float sizeX = 0.0f;
                float sizeY = 0.0f;
                float header = 3 * textHeight;
                float lineW = 4 * textHeight;
                float textX = origin.X + lineW + textHeight;
                for (int s = 0; s < this.seriesList.Count; s++) {
                    var series = seriesList[s];
                    float centerY = origin.Y + header + (this.seriesList.Count - 1 - s) * textHeight + 0.5f * textHeight;
                    float textY = centerY + 0.4f * textHeight;
                    using (var paint = colorer.TextPaint(colorer.font, textHeight, SKColors.Black)) { 
                        SKRect r = colorer.MeasureText(seriesList[s].name, paint);
                        sizeX = Math.Max(sizeX, textX + r.Width);
                        sizeY = textY + r.Height;
                    }
                }
                return new SKSize(sizeX, sizeY);
            }
        }

        public void DrawLegend(Painter painter, SKPoint origin, SKSize size, float textHeight) {
            lock (mutex) {
                float lineW = 4 * textHeight;
                float lineX = origin.X;
                float textX = origin.X + lineW + textHeight;
                for (int s = 0; s < this.seriesList.Count; s++) {
                    var series = seriesList[s];
                    float lineH = (series.lineStyle == KLineStyle.Thick) ? 3.0f : (series.lineMode == KLineMode.Line) ? 1.0f : 8.0f; 
                    float centerY = origin.Y + (this.seriesList.Count - 1 - s) * textHeight + 0.5f * textHeight;
                    float lineY = centerY - lineH / 2.0f;
                    float textY = centerY + 0.4f * textHeight;
                    using (var paint = painter.FillPaint(seriesList[s].color)) {
                        painter.DrawRect(new SKRect(lineX, lineY, lineX + lineW, lineY + lineH), paint);
                    }
                    using (var paint = painter.TextPaint(painter.font, textHeight, SKColors.Black)) {
                        painter.DrawText(seriesList[s].name, new SKPoint(textX, textY), paint);
                    }
                }
            }
        }

        public string ToCSV() {
            lock (mutex) {
                string csvContent = "";
                var theSeries = seriesList.ToArray();
                for (int s = theSeries.Length-1; s >= 0; s--) {
                    if (theSeries[s].visible) {
                        string seriesName = theSeries[s].name;
                        int pointCount = list.Count;
                        for (int p = 0; p < pointCount; p++) {
                            KChartEntry point = list[p];
                            var csvLine = "";
                            if (theSeries[s].lineMode == KLineMode.Line) {
                               csvLine = seriesName + "," + point.X + "," + point.Y[s];
                            } else if (theSeries[s].lineMode == KLineMode.Range) {
                               csvLine = seriesName + "," + point.X + "," + (point.Y[s] - point.Yrange[s]) + "," + (point.Y[s] + point.Yrange[s]);
                            }
                            csvContent += csvLine + Environment.NewLine;
                        }
                    }
                }
                return csvContent;
            }
        }

    }
}
