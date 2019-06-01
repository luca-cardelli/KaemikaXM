using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using Microcharts;
using KaemikaXM.Pages;
using QuickGraph;

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
            ChartInit();
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

        public override async void InputSetErrorSelection(int lineNumber, int columnNumber, int length, string failMessage) {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(async () => {
                await MainTabbedPage.theModelEntryPage.DisplayAlert("eeek", failMessage, "not ok");
                MainTabbedPage.SwitchToTab(MainTabbedPage.theModelEntryPageNavigation);
                (MainTabbedPage.theModelEntryPage.editor as ICustomTextEdit).SetFocus();
                (MainTabbedPage.theModelEntryPage.editor as ICustomTextEdit).SetSelectionLineChar(lineNumber, columnNumber, length);
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
                MainTabbedPage.theOutputPage.AppendText(text);
            });
        }

        public override void ProcessOutput() {
            MainTabbedPage.theOutputPage.ProcessOutput();
        }

        public override void ProcessGraph(string graphFamily) {
            MainTabbedPage.theOutputPage.ProcessGraph(graphFamily);
        }

        // CHART

        private string title = "";
        private Timecourse timecourse;                  // assumes points arrive in time equal or increasing

        public override void ChartUpdate() {
            timecourse.VisibilityRestore(MainTabbedPage.theModelEntryPage.Visibility());
            MainTabbedPage.theChartPage.SetChart(
                new Chart(title, MainTabbedPage.theModelEntryPage.modelInfo.title, timecourse),
                MainTabbedPage.theModelEntryPage.modelInfo);
        }

        public void ChartUpdateLandscape() {
            MainTabbedPage.theChartPageLandscape.SetChart(
                new Chart(title, MainTabbedPage.theModelEntryPage.modelInfo.title, timecourse));
        }

        public override void LegendUpdate() {
            timecourse.VisibilityRestore(MainTabbedPage.theModelEntryPage.Visibility());
            MainTabbedPage.theChartPage.SetLegend(timecourse.Legend());
        }

        public override void ParametersClear() {
            MainTabbedPage.theChartPage.ParametersClear();
        }

        public bool IsChartClear() {
            return this.timecourse.IsClear();
        }

        private void ChartInit() {
            this.title = "";
            this.timecourse = new Timecourse() { };
        }

        public override void ChartClear(string title) {
            ChartInit();
            this.title = title;
            ChartUpdate();
            LegendUpdate();
        }

        public override void OutputClear(string title) {
            MainTabbedPage.theOutputPage.OutputClear();
            this.title = title;
        }
        
        public override string ChartAddSeries(string legend, Color color, Noise noise) {
            if (noise == Noise.None) {
                return timecourse.AddSeries(new Series(legend, color, LineMode.Line, LineStyle.Thick));
            } else if (noise == Noise.Sigma || noise == Noise.SigmaSq || noise == Noise.CV || noise == Noise.Fano) {
                return timecourse.AddSeries(new Series(legend, color, LineMode.Line, LineStyle.Thin));
            } else if (noise == Noise.SigmaRange || noise == Noise.SigmaSqRange) {
                return timecourse.AddSeries(new Series(legend, Color.FromArgb(Chart.transparency, color), LineMode.Range, LineStyle.Thin));
            } else throw new Error("ChartAddSeries");
        }

        public override void ChartAddPoint(string seriesName, double t, double mean, double variance, Noise noise) {
            if (seriesName != null) {
                if (noise == Noise.None) timecourse.AddPoint(seriesName, (float)t, (float)mean);
                if (noise == Noise.SigmaSq) timecourse.AddPoint(seriesName, (float)t, (float)variance);
                if (noise == Noise.Sigma) timecourse.AddPoint(seriesName, (float)t, (float)Math.Sqrt(variance));
                if (noise == Noise.CV) timecourse.AddPoint(seriesName, (float)t, (float)((mean == 0.0) ? 0.0 : (Math.Sqrt(variance) / mean)));
                if (noise == Noise.Fano) timecourse.AddPoint(seriesName, (float)t, (float)((mean == 0.0) ? 0.0 : (variance / mean)));
                if (noise == Noise.SigmaSqRange) timecourse.AddRange(seriesName, (float)t, (float)mean, (float)variance);
                if (noise == Noise.SigmaRange) timecourse.AddRange(seriesName, (float)t, (float)mean, (float)Math.Sqrt(variance));
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

        public void VisibilityRemember() {
            timecourse.VisibilityRemember(MainTabbedPage.theModelEntryPage.Visibility());
        }

        public void InvertVisible(string name) {
            timecourse.InvertVisible(name);
        }

        public override void AddParameter(string parameter, double drawn, string distribution, double[] arguments) {
            MainTabbedPage.theChartPage.AddParameter(parameter, drawn, distribution, arguments);
        }

        public override double ParameterOracle(string parameter) { // returns NAN if oracle not available
            return MainTabbedPage.theChartPage.ParameterOracle(parameter);
        }

        public override void ParametersUpdate() {
            MainTabbedPage.theChartPage.ParametersUpdate();
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
       
        public override void BeginningExecution() {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                MainTabbedPage.Executing(true);
            });
        }

        public override void EndingExecution() {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                MainTabbedPage.Executing(false);
            });
        }

        private bool continueButtonIsEnabled = false;
        public override void ContinueEnable(bool b) {
            continueButtonIsEnabled = b;
            if (continueButtonIsEnabled) MainTabbedPage.theModelEntryPage.SetStartButtonToContinue(); else MainTabbedPage.theModelEntryPage.SetContinueButtonToStart();
        }

        public override bool ContinueEnabled() {
            return continueButtonIsEnabled;
        }

        public static string currentSolver = "RK547M"; // "GearBDF" or "RK547M"
        public override string Solver() {
            return currentSolver;
        }

        public override bool PrecomputeLNA() {
            return false; // appaarently zero benefit in precomputing the drift matrix
        }

        public override void ChartListboxAddSeries(string legend){ }

        public override void ClipboardSetText(string text) {
            // this was for Export output to the clipboard, but we do not need to do this in Android
        }

    }
}
