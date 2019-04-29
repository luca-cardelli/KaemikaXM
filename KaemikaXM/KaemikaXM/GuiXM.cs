using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using Microcharts;
using KaemikaXM.Pages;

namespace Kaemika {

    public class ByTime : IComparer<ChartEntry> {
        public int Compare(ChartEntry e1, ChartEntry e2) {
            if (e1.X < e2.X) return -1;
            else if (e1.X == e2.X) return 0;
            else return 1;
        }
    }

    public delegate Xamarin.Forms.View CustomTextEditorDelegate();

    public class GUI_Xamarin : GuiInterface {

        // INITIALIZE

        public GUI_Xamarin() {
            ChartClear("");
        }

        public static CustomTextEditorDelegate customTextEditor = null;

        // INPUT

        public override string InputGetText() {
            return MainTabbedPage.theModelEntryPage.GetText();
        }

        public override void InputSetText(string text) {
            MainTabbedPage.theModelEntryPage.SetText(text);
        }

        public override void InputInsertText(string text) {
            MainTabbedPage.theModelEntryPage.InsertText(text);
        }

        public override async void InputSetErrorSelection(int lineNumber, int columnNumber, string failMessage) {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(async () => {
                await MainTabbedPage.theModelEntryPage.DisplayAlert("eeek", failMessage, "not ok");
                MainTabbedPage.theMainTabbedPage.SwitchToTab("Network");
                (MainTabbedPage.theModelEntryPage.editor as ICustomTextEdit).SetFocus();
                (MainTabbedPage.theModelEntryPage.editor as ICustomTextEdit).SetSelectionLineChar(lineNumber, columnNumber);
            });
        }

        // OUTPUT

        public override void OutputSetText(string text) {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                MainTabbedPage.theOutputPage.SetText(text);
            });
        }

        public override string OutputGetText() {
            return MainTabbedPage.theOutputPage.GetText();
        }

        public override void OutputAppendText(string text) {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                MainTabbedPage.theOutputPage.SetText(MainTabbedPage.theOutputPage.GetText() + text);
            });
        }

        // CHART

        private string title = "";
        private List<Series> seriesList;
        private Timecourse timecourse;                  // assumes points they arrive in time equal or increasing
        private ChartEntry lastEntry;                   // the last entry to accumulate the equal-time points
        private int lastEntryCount;                     // to know when we have completed the last entry
        private Dictionary<string, int> seriesIndex;    // maintaining the connection between seriesList and timecourse

        public override void ChartUpdate() {
            VisibilityRestore();
            MainTabbedPage.theChartPage.SetChart(
                new Chart(title, MainTabbedPage.theModelEntryPage.modelInfo.title, seriesList, timecourse, seriesIndex));
        }

        public void ChartUpdateLandscape() {
            MainTabbedPage.theChartPageLandscape.SetChart(
                new Chart(title, MainTabbedPage.theModelEntryPage.modelInfo.title, seriesList, timecourse, seriesIndex));
        }

        public override void LegendUpdate() {
            VisibilityRestore();
            MainTabbedPage.theChartPage.SetLegend(seriesList);
        }

        public override void ChartClear(string title) {
            this.title = title;
            this.seriesList = new List<Series>() { };
            this.timecourse = new Timecourse() { };
            this.seriesIndex = new Dictionary<string, int>();
            this.lastEntry = null;
            this.lastEntryCount = 0;
            ChartUpdate();
            LegendUpdate();
        }

        private void UpdateIndexes() {
            seriesIndex.Clear();
            for (int i = 0; i < seriesList.Count; i++) seriesIndex.Add(seriesList[i].name, i);
        }

        public override string ChartAddSeries(string legend, Color color, Noise noise) {
            if (seriesList.Exists(e => e.name == legend)) return null; // give null on duplicate series
            if (noise == Noise.None) {
                seriesList.Add(new Series(legend, color, LineMode.Line, LineStyle.Thick));
            } else if (noise == Noise.Sigma || noise == Noise.SigmaSq || noise == Noise.CV || noise == Noise.Fano) {
                seriesList.Add(new Series(legend, color, LineMode.Line, LineStyle.Thin));
            } else if (noise == Noise.SigmaRange || noise == Noise.SigmaSqRange) {
                seriesList.Add(new Series(legend, Color.FromArgb(Chart.transparency, color), LineMode.Range, LineStyle.Thin));
            } else throw new Error("ChartAddSeries");
            UpdateIndexes();
            return legend;
        }

        private void AddPoint(string seriesName, float t, float mean) {
            AddRange(seriesName, t, mean, 0);
        }
        private void AddRange(string seriesName, float t, float mean, float variance) {
            if (float.IsNaN(mean)) mean = 0;            // these have been converted from double
            if (float.IsNaN(variance)) variance = 0;    // these have been converted from double
            if (seriesIndex.ContainsKey(seriesName)) {  // if not, it may be due to a concurrent invocations of plotting before the previous one has finished
                int index = seriesIndex[seriesName];
                if (lastEntry == null) {
                    var Y = new float[seriesList.Count];
                    var Yrange = new float[seriesList.Count];
                    Y[index] = mean;
                    Yrange[index] = variance;
                    lastEntry = new ChartEntry(X: t, Y: Y, Yrange: Yrange);
                    lastEntryCount = 1;
                } else  {
                    lastEntry.Y[index] = mean;
                    lastEntry.Yrange[index] = variance;
                    lastEntryCount++;
                }
                if (lastEntryCount == seriesList.Count) {
                    timecourse.Add(lastEntry);
                    lastEntry = null;
                    lastEntryCount = 0;
                }
            }
        }

        public override void ChartAddPoint(string seriesName, double t, double mean, double variance, Noise noise) {
            if (seriesName != null) {
                if (noise == Noise.None) AddPoint(seriesName, (float)t, (float)mean);
                if (noise == Noise.SigmaSq) AddPoint(seriesName, (float)t, (float)variance);
                if (noise == Noise.Sigma) AddPoint(seriesName, (float)t, (float)Math.Sqrt(variance));
                if (noise == Noise.CV) AddPoint(seriesName, (float)t, (float)((mean == 0.0) ? 0.0 : (Math.Sqrt(variance) / mean)));
                if (noise == Noise.Fano) AddPoint(seriesName, (float)t, (float)((mean == 0.0) ? 0.0 : (variance / mean)));
                if (noise == Noise.SigmaSqRange) AddRange(seriesName, (float)t, (float)mean, (float)variance);
                if (noise == Noise.SigmaRange) AddRange(seriesName, (float)t, (float)mean, (float)Math.Sqrt(variance));
            }
        }

        public override string ChartAddPointAsString(string seriesName, double t, double mean, double variance, Noise noise) {
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

        public override Noise NoiseSeries() {
            return MainTabbedPage.theModelEntryPage.noisePickerSelection;
        }

        public override bool ScopeVariants() {
            return true;             // ### 
        }

        public override bool RemapVariants() {
            return true;             // ### 
        }

        public override void SaveInput() {
            //### throw new Error("GUI_Xamarin : not implemented");
        }

        public override void RestoreInput() {
            //### throw new Error("GUI_Xamarin : not implemented");
        }

        // this-thread cache of main-thread state
        private bool stopButtonIsEnabled = false;

        public override void StopEnable(bool b) {
            stopButtonIsEnabled = b;
            // calling BeginInvokeOnMainThread here often causes a deadlock
            // maybe because the main thread has called StopEnable via the Stop button callback and is waiting for it to return?
            // so just avoid changing the appearance of the Stop/Start buttons, but remember their intended state in this thread

            //Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
            //    MainTabbedPage.theModelEntryPage.startButton.IsEnabled = !b;
            //    MainTabbedPage.theChartPage.stopButton.IsEnabled = b;
            //});
        }

        public override bool StopEnabled() {
            return stopButtonIsEnabled;
        }

        private bool continueButtonIsEnabled = false;
        public override void ContinueEnable(bool b) {
            continueButtonIsEnabled = b;
            if (continueButtonIsEnabled) MainTabbedPage.theModelEntryPage.SetStartButtonToContinue(); else MainTabbedPage.theModelEntryPage.SetStartButtonToStart();
        }

        public override bool ContinueEnabled() {
            return continueButtonIsEnabled;
        }

        public override bool TraceComputational() {
            return true;             // ###
        }

        public override string Solver() {
            return "OSLO RK547M";    // ###
        }

        private static Dictionary<string, Dictionary<string, bool>> visibilityCache = 
            new Dictionary<string, Dictionary<string, bool>>();

        private Dictionary<string,bool> Visibility() {
            string theModel = MainTabbedPage.theModelEntryPage.modelInfo.title;
            if (!visibilityCache.ContainsKey(theModel)) visibilityCache[theModel] = new Dictionary<string, bool>();
            return visibilityCache[theModel];
        }

        public void VisibilityRemember() {
            Dictionary<string, bool> visibility = Visibility();
            foreach (var series in seriesList) visibility[series.name] = series.visible;
        }

        public void VisibilityRestore() {
            Dictionary<string, bool> visibility = Visibility();
            foreach (var keyPair in visibility) {
                if (seriesIndex.ContainsKey(keyPair.Key))
                    seriesList[seriesIndex[keyPair.Key]].visible = keyPair.Value;
            }
        }

        public override void ChartListboxAddSeries(string legend){
        }

        public override void ClipboardSetText(string text) {
        }

    }
}
