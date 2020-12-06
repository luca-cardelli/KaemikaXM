using System;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp; // just for the structs SKColor, SKPoint, SKSize, SKRect; not for SKCanvas

namespace Kaemika {

    public interface ChartPainter : Painter { // interface for platform-dependent chart rendering
        void DrawCourse(List<KChartEntry> list, int seriesIndex, float pointSize, SKColor color, Swipe pinchPan);
        void DrawCourseRange(List<KChartEntry> list, int seriesIndex, SKColor color, Swipe pinchPan);
        void DrawCourseFill(List<KChartEntry> list, int seriesIndex, float bottom, SKColor color, Swipe pinchPan);
    }

    public static class Palette {
        private static Color[] palette = { Color.Red, Color.Green, Color.Blue, Color.Gold, Color.Cyan, Color.GreenYellow, Color.Violet, Color.Purple };
        public static Color GetColor(int no) {
            return palette[no % palette.Length];
        }
    }

    public abstract class KChartHandler {

        //private static KTouchable chartControl;          // <<============== the only chart GUI panel, registered from platforms when the GUI loads

        //public static void Register(KTouchable control) {
        //    chartControl = control;
        //}

        private static KChart chart = null;             // <<============== the only chart
        private static Dictionary<string, Dictionary<string, bool>> visibilityCache =  // a visibility cache for each Chart.model
            new Dictionary<string, Dictionary<string, bool>>();

        private static KTouchClientData touch = null;
        public static void RegisterKTouchClientData(KTouchClientData data) {
            touch = data;
        }
        public static void KTouchClientDataReset() {
            if (touch != null) touch.Reset();
        }

        private static DateTime lastUpdate = DateTime.MinValue;
        // make sure to do a final non-incremental update after incremental updates
        public static void ChartUpdate(Style style, bool incremental = false) {
            if (style != null && !style.chartOutput) return; // style can be null only on OnClick-callback from SetLegend or LegendView-selectionChanged, in which case it is ok to update the chart
            if (!incremental) {
                lastUpdate = DateTime.MinValue;
                KGui.gui.GuiChartUpdate();
            } else if (TimeLib.Precedes(lastUpdate, DateTime.Now.AddSeconds(-0.03))) {
                KGui.gui.GuiChartUpdate(); 
                lastUpdate = DateTime.Now; 
            }
        }
        public static void LegendUpdate(Style style) {
            if (!style.chartOutput) return;
            KGui.gui.GuiLegendUpdate();
        }
        public static void ChartClear(string sampleName, string baseUnitX, string baseUnitY, Style style) {
            //// do not test for chart==null: we must create the first chart!
            if (!style.chartOutput) return;
            lastUpdate = DateTime.MinValue;
            chart = new KChart(sampleName, baseUnitX, baseUnitY, style);
            KChartHandler.KTouchClientDataReset();
            ChartUpdate(style);
        }
        public static void ChartClearData(Style style) {
            lastUpdate = DateTime.MinValue;
            if (chart == null || !style.chartOutput) return;
            chart.timecourse.ClearData();
        }
        public static bool IsClear() {
            if (chart == null) return true;
            return chart.IsClear();
        }
        public static KSeries ChartSeriesNamed(string name) {
            if (chart == null) return null;
            if (name == null) return null;
            return chart.timecourse.SeriesNamed(name);
        }
        // ChartAddSeries for Plot (functions) and DensityPlot (random variables)
        public static string ChartAddSeries(string legend, Flow asFlow, Color color, KLineMode lineMode, KLineStyle lineStyle) {
            return chart.timecourse.AddSeries(new KSeries(legend, asFlow, color, lineMode, lineStyle));
        }
        // ChartAddSeries for chemical ractions
        public static string ChartAddSeries(string legend, Flow asFlow, Color color, Noise noise) {
            if (chart == null) return null;
            if (noise == Noise.None) {
                return chart.timecourse.AddSeries(new KSeries(legend, asFlow, color, KLineMode.Line, KLineStyle.Thick));
            } else if (noise == Noise.Sigma || noise == Noise.SigmaSq || noise == Noise.CV || noise == Noise.Fano) {
                return chart.timecourse.AddSeries(new KSeries(legend, asFlow, color, KLineMode.Line, KLineStyle.Thin));
            } else if (noise == Noise.SigmaRange || noise == Noise.SigmaSqRange) {
                return chart.timecourse.AddSeries(new KSeries(legend, asFlow, Color.FromArgb(KChart.transparency, color), KLineMode.Range, KLineStyle.Thin));
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
        public static void ChartAddPole(string seriesName, double t, KLineStyle lineStyle) {
            if (chart == null) return;
            if (seriesName != null) chart.timecourse.AddPole(seriesName, (float)t, lineStyle);
        }

        public static Swipe GetManualPinchPan() {
            if (chart == null) return Swipe.Id();
            return chart.GetManualPinchPan();
        }
        public static void SetManualPinchPan(Swipe manualPinchPan) {
            if (chart == null) return;
            chart.SetManualPinchPan(manualPinchPan);
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
        public static void ShiftInvertVisible(string seriesName) {
            if (chart == null) return;
            chart.timecourse.ShiftInvertVisible(seriesName);
        }
        public static void ShowEndNames(bool show) {
            if (chart == null) return;
            chart.timecourse.ShowSeriesTags(show);
        }
        public static void SetMeanFlowDictionary(Dictionary<SpeciesValue, Flow> dictionary, Style style) {
            if (chart == null || !style.chartOutput) return;
            chart.timecourse.SetMeanFlowDictionary(dictionary);
        }
        public static void Draw(ChartPainter painter, int originX, int originY, int width, int height) {
            if (chart == null) painter.Clear(SKColors.White);
            else chart.Draw(painter, originX, originY, width, height);
        }
        public static void DrawOver(ChartPainter painter, int originX, int originY, int width, int height) {
            if (chart == null) return;
            chart.DrawOver(painter, originX, originY, width, height);
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
        public static TimecourseFlow ToTimecourse(Symbol name, Flow series, Style style) {
            if (chart == null) return null;
            return chart.ToTimecourse(name, series, style);
        }
    }

    public class KChart {
        private SKSize sizeOfLastDrawn;
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
            this.sizeOfLastDrawn = new SKSize(0, 0);
        }

        public KChart(string sampleName, string baseUnitX, string baseUnitY, Style style) {
            Init(sampleName, new KTimecourse(sampleName, baseUnitX, baseUnitY, style));
        }

        public KChart(string newSampleName, KTimecourse timecourse) {
            Init(newSampleName, timecourse);
        }

        public string SampleName() { return this.timecourse.SampleName(); }
        public void SetSampleName(string model) { this.timecourse.SetSampleName(model); }

        public bool IsClear() {
            return timecourse.IsClear();
        }

        private Swipe pinchPan = Swipe.Id(); // updated by ChartView from KaemikaXM and SKChartView from KaemikaWPF
        public bool displayPinchOrigin = false;
        public SKPoint pinchOrigin;

        public Swipe GetManualPinchPan() {
            return pinchPan;
        }
        public void SetManualPinchPan(Swipe manualPinchPan) {
            pinchPan = manualPinchPan;
        }

        public float PointSize(int width, int height) {
            // determine how big one "point" should be relative to the available drawing resolution
            var textHeight = height / 14.0f;                                     // at least 14 text lines should fit on the Y axis (5 ticks)
            var pointSize = textHeight / 20.0f;                                  // resolution is set at 14x20 = 280 "points"
            pointSize = Math.Min(pointSize, (float)Math.Sqrt(pointSize));        // but do not let the pointsize get too big as the screen grows
            return pointSize;
        }

        public SKSize Size() { // better to use Gui.gui.ChartSize(), which is the size of the chart in actual GUI
            return this.sizeOfLastDrawn;
        }

        // called asynchronously by the GUI
        public void Draw(ChartPainter painter, int originX, int originY, int width, int height) {
            painter.Clear(this.backgroundColor);
            DrawOver(painter, originX, originY, width, height);
        }
        public void DrawOver(ChartPainter painter, int originX, int originY, int width, int height) {
            this.sizeOfLastDrawn = new SKSize(width, height);
            this.pointSize = PointSize(width, height);
            this.margin = 12.0f * pointSize;
            this.textHeight = 12.0f * pointSize;
            this.titleHeight = 14.0f * pointSize;
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
            string dimensionX = (timecourse.baseUnitX == "s") ? "time" : "-x->";
            int yFlowsWidth = 0;
            for (int i = 0; i < yFlows.Length; i++)
                if (yFlows[i] != null)
                    yFlowsWidth = Math.Max(yFlowsWidth, yFlows[i].Length);
            yFlowsWidth = Math.Max(yFlowsWidth, dimensionX.Length);
            string toolTip = "";
            for (int i = 0; i < yFlows.Length; i++)
                if (yFlows[i] != null)
                    toolTip += yFlows[i] + new string(' ', yFlowsWidth - yFlows[i].Length + 1) + yValues[i] + Environment.NewLine;
            toolTip += dimensionX + new string(' ', yFlowsWidth - dimensionX.Length + 1) + xValue;
            return toolTip;
        }

        public string ToCSV() {
            return timecourse.ToCSV();
        }

        public TimecourseFlow ToTimecourse(Symbol name, Flow series, Style style) {
            return timecourse.ToTimecourse(name, series, style);
        }

    }

    public enum KLineMode { Line, Range, FillUnder }
    public enum KLineStyle { Thin, Thick }

    public class KSeries {
        public string name;
        public Flow asFlow;
        public SKColor color;
        public KLineMode lineMode;    // this tells us if it is a mean (Line) or mean+-variance (Range) series
        public KLineStyle lineStyle;  // a KLineMode.Line can be KLineStyle.Thin or KLineStyle.Thick (but a KLineMode.Range can only be KLineStyle.Thin?)
        public bool visible;
        public KButton lineButton;
        public KButton nameButton;
        public KSeries(string name, Flow asFlow, Color color, KLineMode lineMode, KLineStyle lineStyle) {
            this.name = name;
            this.asFlow = asFlow;
            this.color = new SKColor(color.R, color.G, color.B, color.A);
            this.lineMode = lineMode;
            this.lineStyle = lineStyle;
            this.visible = true;
            this.lineButton = null;
            this.nameButton = null;
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

    public class KChartPole{
        public string seriesName;
        public float X;
        public KLineStyle lineStyle;
        public SKPoint YpointHi;  // this is where the top of the pole is plotted
        public SKPoint YpointLo;  // this is where the botton of the pole is plotted

        public KChartPole(string seriesName, float X, KLineStyle lineStyle)  {
            this.seriesName = seriesName;
            this.X = X;
            this.lineStyle = lineStyle;
        }
    }

    public class KTimecourse {                           // assumes points arrive in time equal or increasing
        private object mutex;
        private string sampleName;
        private List<KSeries> seriesList;                // seriesList[i] matches list[t].Y[i] for all t
        private List<KChartEntry> list;
        private KChartEntry lastEntry;                   // the last entry to accumulate the equal-time points
        private int lastEntryCount;                      // to know when we have completed the last entry
        private List<KChartPole> poles;
        private Dictionary<string, int> seriesIndex;     // maintaining the connection between seriesList and timecourse
        private Style style;
        public string baseUnitX;
        public string baseUnitY;

        public KTimecourse(string sampleName, string baseUnitX, string baseUnitY, Style style) {
            this.mutex = new object();
            this.sampleName = sampleName;
            this.baseUnitX = baseUnitX;
            this.baseUnitY = baseUnitY;
            this.seriesList = new List<KSeries>();
            this.list = new List<KChartEntry>();
            this.poles = new List<KChartPole>();
            this.seriesIndex = new Dictionary<string, int>();
            this.style = style;
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
                this.poles = new List<KChartPole>();
            }
        }

        public KSeries SeriesNamed(string name) {
            if (seriesIndex.ContainsKey(name)) return seriesList[seriesIndex[name]];
            else return null;
        }

        public string AddSeries(KSeries series) {
            lock (mutex) {
                if (seriesList.Exists(e => e.name == series.name)) {
                    Gui.Log("Warning: Duplicated series in chart is ignored: " + series.name);
                    return null;  // give null on duplicate series
                }
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
                        Y[index] = mean;
                        Yrange[index] = variance;
                        lastEntry = new KChartEntry(X: t, Y: Y, Yrange: Yrange); //, Ylabel: Ylabel);
                        lastEntryCount = 1;
                    } else  {
                        lastEntry.Y[index] = mean;
                        lastEntry.Yrange[index] = variance;
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

        public void AddPole(string seriesName, float t, KLineStyle lineStyle) {
            lock (mutex) {
                if (float.IsNaN(t)) return;            // these have been converted from double
                if (seriesIndex.ContainsKey(seriesName)) {  // if not, it may be due to a concurrent invocations of plotting before the previous one has finished
                    int index = seriesIndex[seriesName];
                    poles.Add(new KChartPole(seriesName, t, lineStyle));
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
                                if (seriesHit.asFlow != null && seriesHit.asFlow.Involves(new List<SpeciesValue> { headSpecies }))
                                    seriesTag += derivative.FormatAsODE(headSpecies, this.style, "∂ ") + Environment.NewLine;
                            }
                            if (seriesTag != "") return (null, null, null, seriesTag.Substring(0, seriesTag.Length - 1));
                            else return (null, null, null, "");  // mask the data under the series tag
                        } else return (null, null, null, "");  // mask the data under the series tag
                    }
                }

                int speciesNo = seriesList.Count;

                // find hits on the species trajectories
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
                if (closestEntry != null) {
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

                // if no trajectory hits, find hits on the poles
                foreach (KChartPole pole in poles) {
                    if (seriesIndex.ContainsKey(pole.seriesName)) {
                        KSeries series = seriesList[seriesIndex[pole.seriesName]];
                        if (series.visible) {
                            if ((pole.YpointLo.X >= (target.X - radius)) && (pole.YpointLo.X <= (target.X + radius)) &&
                                (pole.YpointLo.Y >= target.Y) && (pole.YpointHi.Y <= target.Y)) {
                                return ("= " + pole.X.ToString("g3"), new string[0], new string[0], null);
                            }
                        }
                    }
                }

                // no hits
                return (null, null, null, null);
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

        public void AllOtherVisible(string seriesName, bool visible) {
            lock (mutex) {
                foreach (KSeries series in seriesList)
                    if (series.name != seriesName) series.visible = visible;
            }
        }

        public void ShiftInvertVisible(string seriesName) {
            lock (mutex) {
                KSeries thisSeries = null;
                bool allOtherOff = true;
                bool allOtherOn = true;
                foreach (KSeries series in seriesList) {
                    if (series.name == seriesName)
                        thisSeries = series;
                    else {
                        allOtherOff = allOtherOff && !series.visible;
                        allOtherOn = allOtherOn && series.visible;
                    }
                }
                if (thisSeries == null) return;

                // thisOn & not allOtherOff -> thisOn & allOtherOff
                if (thisSeries.visible && !allOtherOff) AllOtherVisible(seriesName, false);
                // thisOn & allOtherOff -> thisOn & allOtherOn
                else if (thisSeries.visible && allOtherOff) { thisSeries.visible = true; AllOtherVisible(seriesName, true); }
                // thisOff & not allOtherOff -> thisOn & allOtherOff
                else if ((!thisSeries.visible) && !allOtherOff) { thisSeries.visible = true; AllOtherVisible(seriesName, false); }
                // thisOff & allOtherOff -> thisOn & allOtherOn
                else if ((!thisSeries.visible) && allOtherOff) { thisSeries.visible = true; AllOtherVisible(seriesName, true); }
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

        public const decimal maxDecimal = decimal.MaxValue/2 - 1; // need to be careful to avoid decimal overflow when computing spans of maxValue-minValue and other computations
        public const decimal minDecimal = decimal.MinValue/2 + 1;
        public static decimal ToDecimal(float x) {
            if (x > (float)maxDecimal) return maxDecimal;
            if (x < (float)minDecimal) return minDecimal;
            if (float.IsNaN(x)) return 0;
            return (decimal)x;
        }
        public static decimal ToDecimal(double x) {
            if (x > (double)maxDecimal) return maxDecimal;
            if (x < (double)minDecimal) return minDecimal;
            if (double.IsNaN(x)) return 0;
            return (decimal)x;
        }
        public static decimal ToDecimal(decimal x) {
            if (x > maxDecimal) return maxDecimal;
            if (x < minDecimal) return minDecimal;
            return (decimal)x;
        }
        public bool NotDecimal(double x) {
            return double.IsNaN(x) || x < (double)minDecimal || x > (double)maxDecimal;
        }

        // use decimal instead of float to prevent rounding error when computing axis tick marks
        private (decimal minX, decimal maxX, decimal minY, decimal maxY) Inner_Bounds() {
            if (list.Count == 0) { return(0,0,0,0); }
            decimal minX = maxDecimal;
            decimal maxX = minDecimal;
            decimal minY = maxDecimal;
            decimal maxY = minDecimal;
            for (int i = 0; i < list.Count; i++) {
                minX = Math.Min(minX, ToDecimal(list[i].X));
                maxX = Math.Max(maxX, ToDecimal(list[i].X));
                minY = Math.Min(minY, ToDecimal(list[i].MinY(seriesList)));
                maxY = Math.Max(maxY, ToDecimal(list[i].MaxY(seriesList)));
            }
            if (minX == maxDecimal && maxX == minDecimal) { minX = minDecimal; maxX = maxDecimal; };
            if (minY == maxDecimal && maxY == minDecimal) { minY = minDecimal; maxY = maxDecimal; };
            return (minX, maxX, minY, maxY);
        }

        private decimal XlocOfXvalInPlotarea(decimal Xval, decimal minX, decimal maxX, SKSize plotSize) {
            if (minX == maxX) return 0;
            if (minX == minDecimal || maxX == maxDecimal) return minDecimal;
            double inaccurate = ((double)Xval - (double)minX) / ((double)maxX - (double)minX) * plotSize.Width; // protect against decimal overflow
            if (NotDecimal(inaccurate)) return ToDecimal(inaccurate);
            return ((Xval - minX) / (maxX - minX)) * ToDecimal(plotSize.Width); 
            //return ((Xval - minX) * ToDecimal(plotSize.Width)) / (maxX - minX); // decimal overflow possible
            //return ToDecimal((((float)Xval - (float)minX) * plotSize.Width) / ((float)maxX - (float)minX));
        }
        private decimal XvalOfXlocInPlotarea(decimal Xloc, decimal minX, decimal maxX, SKSize plotSize) {
            if (plotSize.Width == 0) return 0;
            if (minX == minDecimal || maxX == maxDecimal) return minDecimal;
            return minX + (Xloc * (maxX - minX)) / ToDecimal(plotSize.Width); // decimal overflow?
        }
        private decimal YlocOfYvalInPlotarea(decimal Yval, decimal minY, decimal maxY, SKSize plotSize) {  // the Y axis is flipped
            if (minY == maxY || minY == minDecimal || maxY == maxDecimal) return ToDecimal(plotSize.Height);
            double inaccurate = (((double)maxY - (double)Yval) / ((double)maxY - (double)minY)) * plotSize.Height; // protect against decimal overflow
            if (NotDecimal(inaccurate)) return ToDecimal(inaccurate);
            return ((maxY - Yval)  / (maxY - minY)) * ToDecimal(plotSize.Height);
            // return ((maxY - Yval) * ToDecimal(plotSize.Height)) / (maxY - minY); // decimal overflow possible
            // return ToDecimal((((float)maxY - (float)Yval) * plotSize.Height) / ((float)maxY - (float)minY));
        }
        private decimal YvalOfYlocInPlotarea(decimal Yloc, decimal minY, decimal maxY, SKSize plotSize) {  // the Y axis is flipped
            if (plotSize.Height == 0.0f) return 0;
            if (minY == minDecimal || maxY == maxDecimal) return minDecimal;
            return maxY - (Yloc * (maxY - minY)) / ToDecimal(plotSize.Height); // decimal overflow?
        }
        private decimal YlocRangeOfYvalRangeInPlotarea(decimal YvalRange, decimal minY, decimal maxY, SKSize plotSize) {
            if (minY == maxY) return 0;
            if (minY == minDecimal || maxY == maxDecimal) return minDecimal;
            double inaccurate = ((double)YvalRange / ((double)maxY - (double)minY)) * plotSize.Height; // protect against decimal overflow
            if (NotDecimal(inaccurate)) return ToDecimal(inaccurate);
            return (YvalRange / (maxY - minY)) * ToDecimal(plotSize.Height);
            // return (YvalRange * ToDecimal(plotSize.Height)) / (maxY - minY); // decimal overflow possible
            // return ToDecimal((((float)YvalRange * plotSize.Height)) / ((float)maxY - (float)minY));
        }

        private void Inner_CalculatePoints(SKPoint plotOrigin, SKSize plotSize, decimal minX, decimal maxX, decimal minY, decimal maxY) {
            for (int i = 0; i < list.Count; i++) {
                KChartEntry entry = list[i];
                float x = plotOrigin.X + (float)XlocOfXvalInPlotarea(ToDecimal(entry.X), minX, maxX, plotSize);
                for (int j = 0; j < entry.Y.Length; j++) {
                    float y = plotOrigin.Y + (float)YlocOfYvalInPlotarea(ToDecimal(entry.Y[j]), minY, maxY, plotSize);
                    entry.Ypoint[j] = new SKPoint(x, y);
                    entry.YpointRange[j] = (entry.Yrange[j] == 0) ? 0 : (float)YlocRangeOfYvalRangeInPlotarea(ToDecimal(entry.Yrange[j]), minY, maxY, plotSize);
                }
            }
            for (int i = 0; i < poles.Count; i++) {
                KChartPole pole = poles[i];
                float x = plotOrigin.X + (float)XlocOfXvalInPlotarea(ToDecimal(pole.X), minX, maxX, plotSize);
                pole.YpointLo = new SKPoint(x, plotOrigin.Y + (float)YlocOfYvalInPlotarea(minY, minY, maxY, plotSize));
                pole.YpointHi = new SKPoint(x, plotOrigin.Y + (float)YlocOfYvalInPlotarea(maxY, minY, maxY, plotSize));
            }
        }

        private void Inner_DrawTitle(ChartPainter painter, string title, SKPoint plotOrigin, SKSize plotSize, float margin, float titleHeight, Swipe pinchPan) {
            using (var paint = painter.TextPaint(painter.fixedFont, pinchPan % titleHeight, SKColors.Black)) {
                paint.TextAlign = SKTextAlign.Center;
                painter.DrawText(title, pinchPan % new SKPoint(plotOrigin.X + plotSize.Width / 2.0f, plotOrigin.Y - margin), paint);
            }
        }

        public double RoundToSignificantDigits(double d, int digits) {
            if(d == 0) return 0;
            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
            return scale * Math.Round(d / scale, digits);
        }
        static double TruncateToSignificantDigits(double d, int digits) {
            if(d == 0) return 0;
            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1 - digits);
            return scale * Math.Truncate(d / scale);
        }

        //// instead of Smooth, we now use (decimal) instead of (float): that seems to have solved rounding problems when using Ceiling and Floor
        //private (float minS, float maxS) Smooth(float min, float max) {
        //    // make sure that when formatting a number via 'g3' we end up with e.g. 1 mSec instead of 1e3 uSec because of numbers being slightly off
        //    float minEqui = (float)RoundToSignificantDigits((double)min, 1);
        //    float maxEqui = (float)RoundToSignificantDigits((double)max, 1);
        //    if (minEqui == maxEqui) {
        //        minEqui = (float)RoundToSignificantDigits((double)min, 2);
        //        maxEqui = (float)RoundToSignificantDigits((double)max, 2);
        //    }
        //    if (minEqui == maxEqui) {
        //        minEqui = (float)RoundToSignificantDigits((double)min, 3);
        //        maxEqui = (float)RoundToSignificantDigits((double)max, 3);
        //    }
        //    if (minEqui == maxEqui) {
        //        minEqui = (float)RoundToSignificantDigits((double)min, 4);
        //        maxEqui = (float)RoundToSignificantDigits((double)max, 4);
        //    }
        //    //int maxMagn = (int)Math.Ceiling(Math.Log10((double)Math.Abs(max)));
        //    //minEqui = (Math.Abs(minEqui) < Math.Pow(10, maxMagn - 3)) ? 0.0f : minEqui;
        //    return (minEqui, maxEqui);
        //}

        private (decimal adjustedMin, decimal adjustedMax, decimal divisions) TrySubdivide(decimal min, decimal max, decimal magnitudeTenths, int subdivisions) {
            // subdivide interval [min..max] into subdivisions
            decimal subdivisionSize = (max - min) / subdivisions;
            // adjust the subdivision size to fit well with magnitudeTenths
            decimal adjustedSubdivisionsSize = magnitudeTenths * Math.Ceiling(subdivisionSize / magnitudeTenths);
            // compute the new [adjustedMin..adjustedMax] interval
            decimal adjustedMin = ToDecimal(adjustedSubdivisionsSize * Math.Floor(min / adjustedSubdivisionsSize));
            decimal adjustedMax = ToDecimal(adjustedSubdivisionsSize * Math.Ceiling(max / adjustedSubdivisionsSize));
            // compute the resulting number of subdivisions
            decimal divisions = (adjustedMax - adjustedMin) / adjustedSubdivisionsSize;
            return (adjustedMin, adjustedMax, divisions);
        }

        private (decimal minBound, decimal maxBound, decimal divisions) Subdivide(decimal min, decimal max) {
            if (min == minDecimal && max == maxDecimal) return (min, max, 0); // do not compute max - min or it will overflow 
            decimal span = max - min;
            if (span == 0) return (min, max, 1);

            // compute magnitude of the interval, so we can work at all magnitudes
            decimal magnitude = ToDecimal(Math.Pow(10, Math.Floor(Math.Log10(Math.Abs((double)span)))));
            // our tick scale will be based on tenths of the magnitude
            decimal magnitudeTenths = magnitude / 10.0M;

            // try several subdivisions 
            (decimal adjMin4, decimal adjMax4, decimal divisions4) = TrySubdivide(min, max, magnitudeTenths, 4);
            (decimal adjMin5, decimal adjMax5, decimal divisions5) = TrySubdivide(min, max, magnitudeTenths, 5);
            (decimal adjMin6, decimal adjMax6, decimal divisions6) = TrySubdivide(min, max, magnitudeTenths, 6);
            (decimal adjMin7, decimal adjMax7, decimal divisions7) = TrySubdivide(min, max, magnitudeTenths, 7);
            (decimal adjMin8, decimal adjMax8, decimal divisions8) = TrySubdivide(min, max, magnitudeTenths, 8);

            // choose the ones that works best in terms of the adjusted interval being tight w.r.t. the initial interval (the slack will end up off-screen)
            // within those, chose the ones with the most subdivisions
            decimal slackBest = maxDecimal; decimal adjMinBest = 0; decimal adjMaxBest = 0; decimal divisionsBest = 0;
            decimal slack8 = (adjMax8 - adjMin8) - span; if (slack8 < slackBest) { slackBest = slack8; adjMinBest = adjMin8; adjMaxBest = adjMax8; divisionsBest = divisions8; }
            decimal slack7 = (adjMax7 - adjMin7) - span; if (slack7 < slackBest) { slackBest = slack7; adjMinBest = adjMin7; adjMaxBest = adjMax7; divisionsBest = divisions7; }
            decimal slack6 = (adjMax6 - adjMin6) - span; if (slack6 < slackBest) { slackBest = slack6; adjMinBest = adjMin6; adjMaxBest = adjMax6; divisionsBest = divisions6; }
            decimal slack5 = (adjMax5 - adjMin5) - span; if (slack5 < slackBest) { slackBest = slack5; adjMinBest = adjMin5; adjMaxBest = adjMax5; divisionsBest = divisions5; }
            decimal slack4 = (adjMax4 - adjMin4) - span; if (slack4 < slackBest) { slackBest = slack4; adjMinBest = adjMin4; adjMaxBest = adjMax4; divisionsBest = divisions4; }

            // once we have determined the adjusted span and number of divisions, we will draw them up from zero and down from zero, so a tick for zero will always be included (if within the span)
            return (adjMinBest, adjMaxBest, divisionsBest);
        }

        private void Inner_DrawXMark(ChartPainter painter, float X, float Y, float markLength, SKColor theColor, Swipe pinchPan) {
            var r = pinchPan % new SKRect(X - 1, Y, X + 1, Y + markLength);
            var p = pinchPan % new SKPoint(X, Y);
            r.Top = Math.Max(r.Top, r.Bottom - markLength);
            r.Left = Math.Max(r.Left, p.X - 1);
            r.Right = Math.Min(r.Right, p.X + 1);
            using (var paint = painter.FillPaint(theColor)) {
                painter.DrawRect(new SKRect(r.Left, Y + markLength - r.Height, r.Right, Y + markLength), paint); // Clamped to bottom of hor axis
            }
        }

        //private void Inner_DrawXLabel(ChartPainter painter, string text, float X, float Y, float markLength, float pointSize, float textHeight, SKColor theColor, Swipe pinchPan){
        //   float gap = 6 * pointSize; // between the mark and the label
        //   using (var paint = painter.TextPaint(painter.fixedFont, pinchPan % textHeight, theColor)) {
        //        painter.DrawText(text, new SKPoint((pinchPan % new SKPoint(X + gap, 0)).X, Y + textHeight), paint); // Clamped to hor axis
        //    }
        //}

        private void Inner_DrawXLabel(ChartPainter painter, string text, float X, float Y, float markLength, float pointSize, float textHeight, SKColor theColor, Swipe pinchPan){
            float h = pinchPan % textHeight;
            float gap = pinchPan % (6 * pointSize); // between the mark and the label
            SKPoint p = pinchPan % new SKPoint(X, Y);
            h = Math.Min(h, textHeight);
            gap = Math.Min(gap, 6 * pointSize);
            using (var paint = painter.TextPaint(painter.fixedFont, h, theColor)) {
                painter.DrawText(text, new SKPoint(p.X + gap, Y + textHeight), paint); // Clamped to hor axis
            }
        }

        private void Inner_DrawXLabels(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float margin, float pointSize, float textHeight, SKColor zeroColor, SKColor color, decimal minX, decimal maxX, Swipe pinchPan) {
            float minLabelSize = painter.MeasureText("0.01 ms_", painter.TextPaint(painter.fixedFont, textHeight, SKColors.Red)).Width;
            (decimal minTickVal, decimal maxTickVal, decimal incrTickVal) = MeasureXTicks(minX, maxX, plotSize);

            if (minX > 0.0M)      
                Inner_DrawXLabelsUpward  (painter, plotOrigin, plotSize, margin, pointSize, minLabelSize, textHeight, zeroColor, color, minX, maxX, pinchPan,
                                          minTickVal, maxTickVal, incrTickVal, true);
            else if (maxX < 0.0M) 
                Inner_DrawXLabelsDownward(painter, plotOrigin, plotSize, margin, pointSize, minLabelSize, textHeight, zeroColor, color, minX, maxX, pinchPan,
                                          minTickVal, maxTickVal, incrTickVal, true);
            else {
                bool paintedZero =
                Inner_DrawXLabelsDownward(painter, plotOrigin, plotSize, margin, pointSize, minLabelSize, textHeight, zeroColor, color, minX, maxX, pinchPan,
                                          minTickVal, 0, incrTickVal, true);
                Inner_DrawXLabelsUpward  (painter, plotOrigin, plotSize, margin, pointSize, minLabelSize, textHeight, zeroColor, color, minX, maxX, pinchPan,
                                          0, maxTickVal, incrTickVal, !paintedZero);
            }
        }

        private (decimal minTickVal, decimal maxTickVal, decimal incrTickVal) MeasureXTicks(decimal min, decimal max, SKSize plotSize) {
            (decimal minTickVal, decimal maxTickVal, decimal divisions) = Subdivide(min, max);
            if (minTickVal == minDecimal || maxTickVal == maxDecimal) return (0, 0, 0);
            decimal incrTickVal = (divisions == 0) ? 0 : (maxTickVal - minTickVal) / divisions;
            return (minTickVal, maxTickVal, incrTickVal);
        }

        private bool Inner_DrawXLabelsUpward(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float margin, float pointSize, float labelSize, float textHeight, SKColor zeroColor, SKColor color, decimal min, decimal max, Swipe pinchPan,
                                             decimal minTickVal, decimal maxTickVal, decimal incrTickVal, bool paintZero) {
            float locEpsilon = 2; // pixels
            float gap = 2 * pointSize; // between the mark and the label
            bool paintedZero = false;
            decimal iTickVal = minTickVal;
            float iTickLoc; 
            float lastTickLoc = paintZero ? float.MinValue : (float)XlocOfXvalInPlotarea(0, min, max, plotSize);
            int limit = 100;
            do {
                if ((double)iTickVal + (double)incrTickVal > (double)maxDecimal || max == maxDecimal || min == minDecimal) { return paintedZero; } // protect against decimal overflow, and don't draw silly positions
                iTickLoc = (float)XlocOfXvalInPlotarea(iTickVal, min, max, plotSize);
                bool isZero = iTickVal == 0;
                if (iTickLoc >= -locEpsilon && iTickLoc <= plotSize.Width + locEpsilon && (paintZero || !isZero)) { //### minus labelSize for labels
                    if (isZero) paintedZero = true;
                    var X = plotOrigin.X + iTickLoc;
                    var Y = plotOrigin.Y + plotSize.Height;
                    SKColor theColor = isZero ? zeroColor : color;
                    if (lastTickLoc + labelSize < iTickLoc) { // skips ticks and labels that do not fit
                        Inner_DrawXMark(painter, X, Y, margin, theColor, pinchPan);
                        if (iTickLoc <= plotSize.Width - labelSize + locEpsilon) { // skips the last label to the right 
                            Inner_DrawXLabel(painter, Kaemika.Gui.FormatUnit(iTickVal, " ", this.baseUnitX, "g3"), X, Y - gap, margin, pointSize, textHeight, theColor, pinchPan);
                        }
                        lastTickLoc = iTickLoc;
                    }
                }
                iTickVal += incrTickVal; limit--;
                if (incrTickVal == 0.0M) { if (iTickVal == maxTickVal) return paintedZero; iTickVal = maxTickVal; }
            } while (iTickLoc <= plotSize.Width + locEpsilon && limit > 0);
            return paintedZero;
        }

        private bool Inner_DrawXLabelsDownward(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float margin, float pointSize, float labelSize, float textHeight, SKColor zeroColor, SKColor color, decimal min, decimal max, Swipe pinchPan,
                                               decimal minTickVal, decimal maxTickVal, decimal incrTickVal, bool paintZero) {
            float locEpsilon = 2; // pixels
            float gap = 2 * pointSize; // between the mark and the label
            bool paintedZero = false;
            decimal iTickVal = maxTickVal;
            float iTickLoc;
            float lastTickLoc = float.MaxValue;
            int limit = 100;
            do {
                if ((double)iTickVal - (double)incrTickVal < (double)minDecimal || max == maxDecimal || min == minDecimal) { return paintedZero; } // protect against decimal overflow, and don't draw silly positions
                iTickLoc = (float)XlocOfXvalInPlotarea(iTickVal, min, max, plotSize);
                bool isZero = iTickVal == 0;
                if (iTickLoc >= -locEpsilon && iTickLoc <= plotSize.Width + locEpsilon && (paintZero || !isZero)) { 
                    if (isZero) paintedZero = true;
                    var X = plotOrigin.X + iTickLoc;
                    var Y = plotOrigin.Y + plotSize.Height;
                    SKColor theColor = isZero ? zeroColor : color;
                    if (lastTickLoc - labelSize > iTickLoc) { // skips ticks and labels that do not fit
                        Inner_DrawXMark(painter, X, Y, margin, theColor, pinchPan);
                        Inner_DrawXLabel(painter, Kaemika.Gui.FormatUnit(iTickVal, " ", this.baseUnitX, "g3"), X, Y - gap, margin, pointSize, textHeight, theColor, pinchPan);
                        lastTickLoc = iTickLoc;
                    }
                }
                iTickVal -= incrTickVal; limit--;
                if (incrTickVal == 0) { if (iTickVal == minTickVal) return paintedZero; iTickVal = minTickVal; }
            } while (iTickLoc >= -locEpsilon && limit > 0); //margin?
            return paintedZero;
        }

        private void Inner_DrawYMark(ChartPainter painter, float X, float Y, float markLength, SKColor theColor, Swipe pinchPan) {
            var r = pinchPan % new SKRect(X, Y - 1, X + markLength, Y + 1);
            var p = pinchPan % new SKPoint(X, Y);
            r.Right = Math.Min(r.Right, r.Left + markLength);
            r.Top = Math.Max(r.Top, p.Y - 1);
            r.Bottom = Math.Min(r.Bottom, p.Y + 1);
            using (var paint = painter.FillPaint(theColor)) {
                painter.DrawRect(new SKRect(X, r.Top, X + r.Width, r.Bottom), paint); // Clamped to left of ver axis
            }
        }

        private void Inner_DrawYLabel(ChartPainter painter, string text, float X, float Y, float markLength, float pointSize, float textHeight, SKColor theColor, Swipe pinchPan){
            float h = pinchPan % textHeight;
            float gap = pinchPan % (6 * pointSize); // between the mark and the label
            SKPoint p = pinchPan % new SKPoint(X, Y);
            h = Math.Min(h, textHeight);
            gap = Math.Min(gap, 6 * pointSize);
            using (var paint = painter.TextPaint(painter.fixedFont, h, theColor)) {
                painter.DrawText(text, new SKPoint(X, p.Y - gap), paint); // Clamped to ver axis
            }
        }

        private void Inner_DrawYLabels(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float margin, float pointSize, float textHeight, SKColor zeroColor, SKColor color, decimal minY, decimal maxY, Swipe pinchPan) {
            // float labelSize = 3 * textHeight;
            (decimal minTickVal, decimal maxTickVal, decimal incrTickVal) = MeasureYTicks(minY, maxY, plotSize);

            if (minY > 0)      
                Inner_DrawYLabelsUpward  (painter, plotOrigin, plotSize, margin, pointSize, textHeight, zeroColor, color, minY, maxY, pinchPan,
                                          minTickVal, maxTickVal, incrTickVal, true);
            else if (maxY < 0) 
                Inner_DrawYLabelsDownward(painter, plotOrigin, plotSize, margin, pointSize, textHeight, zeroColor, color, minY, maxY, pinchPan,
                                          minTickVal, maxTickVal, incrTickVal, true);
            else {
                bool paintedZero =
                Inner_DrawYLabelsDownward(painter, plotOrigin, plotSize, margin, pointSize, textHeight, zeroColor, color, minY, maxY, pinchPan,
                                          minTickVal, 0, incrTickVal, true);
                Inner_DrawYLabelsUpward  (painter, plotOrigin, plotSize, margin, pointSize, textHeight, zeroColor, color, minY, maxY, pinchPan,
                                          0, maxTickVal, incrTickVal, !paintedZero);
            }
        }

        private (decimal minTickVal, decimal maxTickVal, decimal incrTickVal) MeasureYTicks(decimal min, decimal max, SKSize plotSize) {
            (decimal minTickVal, decimal maxTickVal, decimal divisions) = Subdivide(min, max);
            if (minTickVal == minDecimal || maxTickVal == maxDecimal) return (0, 0, 0);
            decimal incrTickVal = (divisions == 0) ? 0 : (maxTickVal - minTickVal) / divisions;
            return (minTickVal, maxTickVal, incrTickVal);
        }

        private bool Inner_DrawYLabelsUpward(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float margin, float pointSize, float textHeight, SKColor zeroColor, SKColor color, decimal min, decimal max, Swipe pinchPan,
                                             decimal minTickVal, decimal maxTickVal, decimal incrTickVal, bool paintZero) {
            float locEpsilon = 2; // pixels
            float gap = 2 * pointSize; // between the mark and the label
            bool paintedZero = false;
            decimal iTickVal = minTickVal;
            float iTickLoc;
            int limit = 100;
            do {
                if ((double)iTickVal + (double)incrTickVal > (double)maxDecimal || max == maxDecimal || min == minDecimal) { return paintedZero; } // protect against decimal overflow, and don't draw silly positions
                iTickLoc = (float)YlocOfYvalInPlotarea(iTickVal, min, max, plotSize);
                bool isZero = iTickVal == 0;
                if (iTickLoc >= -locEpsilon && iTickLoc <= plotSize.Height + locEpsilon && (paintZero || !isZero)) {
                    if (isZero) paintedZero = true;
                    var X = plotOrigin.X - margin;
                    var Y = plotOrigin.Y + iTickLoc;
                    SKColor theColor = isZero ? zeroColor : color;
                    Inner_DrawYMark(painter, X, Y, margin, theColor, pinchPan);
                    Inner_DrawYLabel(painter, Kaemika.Gui.FormatUnit(iTickVal, " ", this.baseUnitY, "g3"), X + gap, Y, margin, pointSize, textHeight, theColor, pinchPan);
                }
                iTickVal += incrTickVal; limit--;
                if (incrTickVal == 0) { if (iTickVal == maxTickVal) return paintedZero; iTickVal = maxTickVal; }
            } while (iTickLoc >= -locEpsilon && limit > 0);
            return paintedZero;
        }

        private bool Inner_DrawYLabelsDownward(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float margin, float pointSize, float textHeight, SKColor zeroColor, SKColor color, decimal min, decimal max, Swipe pinchPan,
                                               decimal minTickVal, decimal maxTickVal, decimal incrTickVal, bool paintZero) {
            float locEpsilon = 2; // pixels
            float gap = 2 * pointSize; // between the mark and the label
            bool paintedZero = false;
            decimal iTickVal = maxTickVal;
            float iTickLoc;
            int limit = 100;
            do {
                if ((double)iTickVal - (double)incrTickVal < (double)minDecimal || max == maxDecimal || min == minDecimal) { return paintedZero; } // protect against decimal overflow, and don't draw silly positions
                iTickLoc = (float)YlocOfYvalInPlotarea(iTickVal, min, max, plotSize);
                bool isZero = iTickVal == 0;
                if (iTickLoc >= -locEpsilon && iTickLoc <= plotSize.Height + locEpsilon && (paintZero || !isZero)) {
                    if (isZero) paintedZero = true;
                    var X = plotOrigin.X - margin;
                    var Y = plotOrigin.Y + iTickLoc;
                    SKColor theColor = isZero ? zeroColor : color;
                    Inner_DrawYMark(painter, X, Y, margin, theColor, pinchPan);
                    Inner_DrawYLabel(painter, Kaemika.Gui.FormatUnit(iTickVal, " ", this.baseUnitY, "g3"), X + gap, Y, margin, pointSize, textHeight, theColor, pinchPan);
                }
                iTickVal -= incrTickVal; limit--;
                if (incrTickVal == 0) { if (iTickVal == minTickVal) return paintedZero; iTickVal = minTickVal; }
            } while (iTickLoc <= plotSize.Height + locEpsilon && limit > 0);
            return paintedZero;
        }

        private Dictionary<KSeries, SKRect> seriesTagLocations = null;

        private SKRect PositionSeriesTag(KSeries series, SKPoint point, SKSize tagSize)  {
            SKRect position = new SKRect(point.X - tagSize.Width, point.Y - tagSize.Height, point.X, point.Y);
            foreach (var conflict in seriesTagLocations) {
                SKRect inter = SKRect.Intersect(position, conflict.Value);
                if (inter.Width > 0 && inter.Height > 0)
                    return PositionSeriesTag(series, new SKPoint(conflict.Value.Left - 3, point.Y), tagSize);
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
                SKPoint endPoint = list[list.Count - 1].Ypoint[seriesIndex];
                if (float.IsNaN(endPoint.X) || float.IsInfinity(endPoint.X) || float.IsNaN(endPoint.Y) || float.IsInfinity(endPoint.Y)) return;
                string name = seriesList[seriesIndex].name;
                float padding = textHeight / 4.0f;
                SKPaint textPaint = painter.TextPaint(painter.fixedFont, textHeight, seriesList[seriesIndex].color);
                SKRect textBounds = painter.MeasureText(name, textPaint);
                SKSize tagSize = new SKSize(textBounds.Width + 2*padding, textBounds.Height + 2*padding);
                SKRect position = pinchPan % PositionSeriesTag(seriesList[seriesIndex], new SKPoint(endPoint.X - 2, endPoint.Y), tagSize);
                float textHeightPP = pinchPan % textHeight;
                float paddingPP = pinchPan % padding;
                textPaint = painter.TextPaint(painter.fixedFont, textHeightPP, seriesList[seriesIndex].color);
                painter.DrawRoundRect(position, 2, painter.FillPaint(SKColors.Gray));
                painter.DrawRoundRect(SKRect.Inflate(position, -paddingPP / 3, -paddingPP / 3), 2, painter.FillPaint(SKColors.White));
                painter.DrawText(name, new SKPoint(position.Left + paddingPP - textBounds.Left * pinchPan.scale, position.Top + paddingPP - textBounds.Top * pinchPan.scale), textPaint);
            }
        }

        private void Inner_DrawSeriesTags(ChartPainter painter, float textHeight, float margin, Swipe pinchPan) {
            seriesTagLocations = new Dictionary<KSeries, SKRect>();
            for (int j = 0; j < seriesList.Count; j++) {
                KSeries series = seriesList[j];
                if (series.visible) {
                    if (series.lineMode == KLineMode.Line || series.lineMode == KLineMode.FillUnder) {
                        Inner_DrawSeriesTag(painter, j, textHeight, margin, pinchPan);
                    } else if (series.lineMode == KLineMode.Range && list.Count > 1) {
                    }
                }
            }
        }

        private void Inner_DrawLines(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float pointSize, Swipe pinchPan) {
            float thinWeigth = 1 * pointSize;
            float thickWeigth = 3 * pointSize;
            for (int j = 0; j < seriesList.Count; j++) {
                KSeries series = seriesList[j];
                if (series.visible) {
                    if (series.lineMode == KLineMode.FillUnder){
                        SKColor color = new SKColor(series.color.Red, series.color.Green, series.color.Blue, KChart.transparency);
                        painter.DrawCourseFill(list, j, plotOrigin.Y + plotSize.Height, color, pinchPan);
                    }
                }
            }
            for (int j = 0; j < seriesList.Count; j++) {
                KSeries series = seriesList[j];
                if (series.visible) {
                    if (series.lineMode == KLineMode.Line) {
                        var weight = series.lineStyle == KLineStyle.Thin ? thinWeigth : thickWeigth;
                        painter.DrawCourse(list, j, weight, series.color, pinchPan);
                    } else if (series.lineMode == KLineMode.Range && list.Count > 1) {
                        painter.DrawCourseRange(list, j, series.color, pinchPan);
                    } else if (series.lineMode == KLineMode.FillUnder){
                        var weight = series.lineStyle == KLineStyle.Thin ? thinWeigth : thickWeigth;
                        painter.DrawCourse(list, j, weight, series.color, pinchPan);
                    }
                }
            }
        }

        private void Inner_DrawPoles(ChartPainter painter, float pointSize, Swipe pinchPan) {
            float thinWeigth = 1 * pointSize;
            float thickWeigth = 3 * pointSize;
            for (int i = 0; i < poles.Count; i++) {
                KChartPole pole = poles[i];
                if (seriesIndex.ContainsKey(pole.seriesName)) {
                    KSeries series = seriesList[seriesIndex[pole.seriesName]];
                    if (series.visible)  {
                        var weight = poles[i].lineStyle == KLineStyle.Thin ? thinWeigth : thickWeigth;
                        painter.DrawLine(new List<SKPoint> { pinchPan % poles[i].YpointLo, pinchPan % poles[i].YpointHi }, painter.LinePaint(weight, series.color));
                    }
                }
            }
        }

        public void DrawContent(ChartPainter painter, SKPoint plotOrigin, SKSize plotSize, float pointSize, float margin, float titleHeight, float textHeight, SKColor zeroColor, SKColor axisColor, Swipe pinchPan) {
            lock (mutex) {
                (decimal minX, decimal maxX, decimal minY, decimal maxY) = Inner_Bounds();
                Inner_DrawTitle(painter, this.sampleName, plotOrigin, plotSize, margin, titleHeight, pinchPan);
                Inner_DrawXLabels(painter, plotOrigin, plotSize, margin, pointSize, textHeight, zeroColor, axisColor, minX, maxX, pinchPan);
                Inner_DrawYLabels(painter, plotOrigin, plotSize, margin, pointSize, textHeight, zeroColor, axisColor, minY, maxY, pinchPan);
                Inner_CalculatePoints(plotOrigin, plotSize, minX, maxX, minY, maxY);
                Inner_DrawLines(painter, plotOrigin, plotSize, pointSize, pinchPan);
                Inner_DrawPoles(painter, pointSize, pinchPan);
                Inner_DrawSeriesTags(painter, textHeight, margin, pinchPan);
            }
        }

        public SKSize MeasureLegend(Colorer colorer, float textHeight) { // Used to write images to clipboard
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
                    using (var paint = colorer.TextPaint(colorer.fixedFont, textHeight, SKColors.Black)) { 
                        SKRect r = colorer.MeasureText(seriesList[s].name, paint);
                        sizeX = Math.Max(sizeX, textX + r.Width);
                        sizeY = textY + r.Height;
                    }
                }
                return new SKSize(sizeX, sizeY);
            }
        }

        public void DrawLegend(Painter painter, SKPoint origin, SKSize size, float textHeight) { // Used to write images to clipboard
            lock (mutex) {
                float lineW = 4 * textHeight;
                float lineX = origin.X;
                float textX = origin.X + lineW + textHeight;
                for (int s = 0; s < this.seriesList.Count; s++) {
                    var series = seriesList[s];
                    float lineH = 1.0f;
                    if (series.lineMode == KLineMode.Line && series.lineStyle == KLineStyle.Thin) lineH = 1.0f;
                    if (series.lineMode == KLineMode.Line && series.lineStyle == KLineStyle.Thick) lineH = 3.0f;
                    if (series.lineMode == KLineMode.Range) lineH = 8.0f;
                    if (series.lineMode == KLineMode.FillUnder && series.lineStyle == KLineStyle.Thin) lineH = 1.0f;
                    if (series.lineMode == KLineMode.FillUnder && series.lineStyle == KLineStyle.Thick) lineH = 3.0f;
                    float centerY = origin.Y + (this.seriesList.Count - 1 - s) * textHeight + 0.5f * textHeight;
                    float lineY = centerY - lineH / 2.0f;
                    float textY = centerY + 0.4f * textHeight;
                    using (var paint = painter.FillPaint(seriesList[s].color)) {
                        painter.DrawRect(new SKRect(lineX, lineY, lineX + lineW, lineY + lineH), paint);
                    }
                    using (var paint = painter.TextPaint(painter.fixedFont, textHeight, SKColors.Black)) {
                        painter.DrawText(seriesList[s].name, new SKPoint(textX, textY), paint);
                    }
                }
            }
        }

        public string ToCSV() {
            // in case of stochastic flows, the flow name indicates the meaning of the two data columns: 
            // their values are mean-sd/var and mean+sd/var, just as they are rendered in the chart
            // unfortunately Excel does not read Unicode correctly
            lock (mutex) {
                KSeries[] theSeries = seriesList.ToArray();
                string csvContent = "SEP=," + Environment.NewLine; // tell Excel to use ',' as separator: default separator is region dependent!
                // Column headers
                csvContent += "time"; 
                for (int s = theSeries.Length - 1; s >= 0; s--) {
                    if (theSeries[s].visible) {
                        string seriesName = theSeries[s].name;
                        seriesName = seriesName.Replace("μ", "mu").Replace("σ", "sd").Replace("²", "^2");
                        if (theSeries[s].lineMode == KLineMode.Range) {
                            csvContent += "," + seriesName.Replace("±", "mu - ");
                            csvContent += "," + seriesName.Replace("±", "mu + ");
                        } else {
                            csvContent += "," + seriesName;
                        }
                    }
                }
                csvContent += Environment.NewLine;

                int pointCount = list.Count;
                for (int p = 0; p < pointCount; p++) {
                    KChartEntry point = list[p];
                    string csvLine = "" + point.X;
                    for (int s = theSeries.Length-1; s >= 0; s--) {
                        if (theSeries[s].visible) {
                            if (theSeries[s].lineMode == KLineMode.Line) {
                                csvLine += "," + point.Y[s];
                            } else if (theSeries[s].lineMode == KLineMode.Range) {
                                csvLine += "," + (point.Y[s] - point.Yrange[s]) + "," + (point.Y[s] + point.Yrange[s]);
                            } else if (theSeries[s].lineMode == KLineMode.FillUnder) {
                                csvLine += "," + point.Y[s];
                            }
                        }
                    }
                    csvContent += csvLine + Environment.NewLine;
                }

                //// Output the data sequentially
                //csvContent += "Flow, Time" + Environment.NewLine; // column heading
                //KSeries[] theSeries = seriesList.ToArray();
                //for (int s = theSeries.Length-1; s >= 0; s--) {
                //    if (theSeries[s].visible) {
                //        string seriesName = theSeries[s].name;
                //        seriesName = seriesName.Replace("±", "mu +/- ").Replace("μ", "mu").Replace("σ", "sd").Replace("²", "^2");
                //        int pointCount = list.Count;
                //        for (int p = 0; p < pointCount; p++) {
                //            KChartEntry point = list[p];
                //            var csvLine = "";
                //            if (theSeries[s].lineMode == KLineMode.Line) {
                //               csvLine = seriesName + "," + point.X + "," + point.Y[s];
                //            } else if (theSeries[s].lineMode == KLineMode.Range) {
                //               csvLine = seriesName + "," + point.X + "," + (point.Y[s] - point.Yrange[s]) + "," + (point.Y[s] + point.Yrange[s]);
                //            } else if (theSeries[s].lineMode == KLineMode.FillUnder) {
                //                csvLine = seriesName + "," + point.X + "," + point.Y[s];
                //            }
                //            csvContent += csvLine + Environment.NewLine;
                //        }
                //    }
                //}
                return csvContent;
            }
        }

        public TimecourseFlow ToTimecourse(Symbol name, Flow series, Style style) {
            lock (mutex) {
                KSeries[] kseries = seriesList.ToArray();
                double[] times = new double[list.Count];
                double[] values = new double[list.Count];
                bool found = false;
                for (int s = kseries.Length-1; s >= 0; s--) {
                    if (kseries[s].asFlow.EqualFlow(series)) { // with LNA active this will pick the deterministic series from kseries because, going backwards, it finds it before the related stochastic one
                        found = true;
                        for (int p = 0; p < list.Count; p++) {
                            KChartEntry point = list[p];
                            times[p] = point.X;
                            values[p] = point.Y[s];
                        }
                        break; // we found it
                    }
                }
                if (!found) throw new Error("timecourse " + series.Format(style) + " not found (need to enable LNA?)");
                return new TimecourseFlow(name, KControls.SelectNoiseSelectedItem, series, times, values); //, ranges);
            }
        }

        //public (KSeries[] series, float[] time, float[,] mean, float[,] range) ToFlows() {
        //    lock (mutex) {
        //        KSeries[] series = seriesList.ToArray();
        //        float[] time = new float[list.Count];
        //        float[,] mean = new float[series.Length, list.Count];
        //        float[,] range = null;
        //        for (int s = series.Length-1; s >= 0; s--) {
        //            for (int p = 0; p < list.Count; p++) {
        //                KChartEntry point = list[p];
        //                time[p] = point.X;
        //                if (series[s].lineMode == KLineMode.Line) {
        //                    mean[s,p] = point.Y[s];
        //                } else if (series[s].lineMode == KLineMode.Range) {
        //                    mean[s,p] = point.Y[s];
        //                    if (range == null) range = new float[series.Length, list.Count];
        //                    range[s,p] = point.Yrange[s];
        //                } else if (series[s].lineMode == KLineMode.FillUnder) {
        //                    mean[s,p] = point.Y[s];
        //                }
        //            }
        //        }
        //        return (series, time, mean, range);
        //    }
        //}

    }

    // -----------------
    // TIME
    // -----------------
    public static class TimeLib {

        public static string TimeStamp(DateTime time) {
            //        return DateTime.Now.ToUniversalTime().ToString("HH:mm:ss.fff");
            if (time == DateTime.MinValue) return "MinValue";
            if (time == DateTime.MaxValue) return "MaxValue";
            return time.ToLocalTime().ToString("HH:mm:ss.fff");
        }

        public static bool Precedes(DateTime t1, DateTime t2) { // t1 is STRICTLY LESS than t2
            return DateTime.Compare(t1, t2) < 0;
        }

        public static DateTime MinTime(DateTime t1, DateTime t2) {
            if (Precedes(t1, t2)) return t1; else return t2;
        }
 
        public static DateTime MaxTime(DateTime t1, DateTime t2) {
            if (Precedes(t1, t2)) return t2; else return t1;
        }
    
        private static Dictionary<string, DateTime> oncePerDict = new Dictionary<string, DateTime>();

        public static bool OncePerMilliseconds(string key, int millisecs) {
            if (!oncePerDict.ContainsKey(key)) oncePerDict[key] = DateTime.MinValue;
            if (Precedes(oncePerDict[key].AddMilliseconds(millisecs), DateTime.Now)) {
                oncePerDict[key] = DateTime.Now;
                return true;
            } else return false;
        }

        public static bool OncePerSeconds(string key, int secs) {
            if (!oncePerDict.ContainsKey(key)) oncePerDict[key] = DateTime.MinValue;
            if (Precedes(oncePerDict[key].AddSeconds(secs), DateTime.Now)) {
                oncePerDict[key] = DateTime.Now;
                return true;
            } else return false;
        }

        public static bool OncePerMinutes(string key, int mins) {
            if (!oncePerDict.ContainsKey(key)) oncePerDict[key] = DateTime.MinValue;
            if (Precedes(oncePerDict[key].AddMinutes(mins), DateTime.Now)) {
                oncePerDict[key] = DateTime.Now;
                return true;
            } else return false;
        }
    }
}
