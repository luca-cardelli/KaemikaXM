using System;
using System.Collections.Generic;
using KaemikaXM.Pages;
using Xamarin.Forms;

namespace Kaemika {

    public class ByTime : IComparer<KChartEntry> {
        public int Compare(KChartEntry e1, KChartEntry e2) {
            if (e1.X < e2.X) return -1;
            else if (e1.X == e2.X) return 0;
            else return 1;
        }
    }

    public delegate ICustomTextEdit CustomTextEditorDelegate();

    public class GUI_Xamarin : ToGui {

        // INITIALIZE

        private static CustomTextEditorDelegate customTextEditor = null;

        public GUI_Xamarin(CustomTextEditorDelegate customTextEditorDelegate) {
            customTextEditor = customTextEditorDelegate;
            //ChartInit("");
            //KChartHandler.ChartClear("");
        }

        public static ICustomTextEdit TextEditor() {
            return customTextEditor();
        }

        // DEVICE

        public override void DeviceUpdate() {
            if (MainTabbedPage.theChartPage != null && MainTabbedPage.theChartPage.deviceView != null)
                //MainTabbedPage.theChartPage.deviceView.InvalidateSurface();
                //###iOS required:
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                    MainTabbedPage.theChartPage.deviceView.InvalidateSurface();
                });

        }

        public override void DeviceShow() {
            if (MainTabbedPage.theChartPage != null && MainTabbedPage.theChartPage.deviceView != null)
                MainTabbedPage.theChartPage.SwitchToDeviceView();
        }

        public override void DeviceHide() {
            if (MainTabbedPage.theChartPage != null && MainTabbedPage.theChartPage.deviceView != null)
                MainTabbedPage.theChartPage.SwitchToPlotView();
        }

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
                MainTabbedPage.theModelEntryPage.editor.SetFocus();
                MainTabbedPage.theModelEntryPage.editor.SetSelectionLineChar(lineNumber, columnNumber, length);
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

        public override void ParametersClear() {
            MainTabbedPage.theChartPage.ParametersClear();
        }

        public override void OutputCopy() { }

        public override void OutputClear(string title) {
            MainTabbedPage.theOutputPage.OutputClear();
            KChartHandler.SetSampleName(title);
            //this.title = title;
        }

        // CHART

        //private string title = "";
        //private KTimecourse timecourse;                  // assumes points arrive in time equal or increasing


        // ORIGINAL Android
        //public override void ChartUpdate() {
        //    timecourse.VisibilityRestore(MainTabbedPage.theModelEntryPage.Visibility());
        //    MainTabbedPage.theChartPage.SetChart(
        //        new KChart(title, MainTabbedPage.theModelEntryPage.modelInfo.title, timecourse));
        //}

        public override void ChartUpdate() {
            KChartHandler.VisibilityRestore(); // this is needed to hide the series in the chart
            MainTabbedPage.theChartPage.InvalidateChart();
        }

        public override void LegendUpdate() {
            KChartHandler.VisibilityRestore(); // this is needed to hide the series in the legend
            MainTabbedPage.theChartPage.SetLegend(KChartHandler.Legend());
        }

        public bool IsChartClear() {
            return KChartHandler.IsClear();
        }

        //private void ChartInit(string title) {
        //    this.title = title;
        //    this.timecourse = new KTimecourse(title) { };
        //}

        //public override void ChartClear(string title) {
        //    ChartInit(title);
        //    ChartUpdate();
        //    LegendUpdate();
        //}

        //public override void ChartClearData() {
        //    this.timecourse.ClearData();
        //}

        public override void ChartSnap() { }

        public override void ChartSnapToSvg() { }

        public override void ChartData() { }
        
        //public override string ChartAddSeries(string legend, System.Drawing.Color color, Noise noise) {
        //    if (noise == Noise.None) {
        //        return timecourse.AddSeries(new KSeries(legend, color, KLineMode.Line, KLineStyle.Thick));
        //    } else if (noise == Noise.Sigma || noise == Noise.SigmaSq || noise == Noise.CV || noise == Noise.Fano) {
        //        return timecourse.AddSeries(new KSeries(legend, color, KLineMode.Line, KLineStyle.Thin));
        //    } else if (noise == Noise.SigmaRange || noise == Noise.SigmaSqRange) {
        //        return timecourse.AddSeries(new KSeries(legend, System.Drawing.Color.FromArgb(KChart.transparency, color), KLineMode.Range, KLineStyle.Thin));
        //    } else throw new Error("ChartAddSeries");
        //}

        //public override void ChartAddPoint(string seriesName, double t, double mean, double variance, Noise noise) {
        //    if (seriesName != null) {
        //        if (noise == Noise.None) timecourse.AddPoint(seriesName, (float)t, (float)mean);
        //        if (noise == Noise.SigmaSq) timecourse.AddPoint(seriesName, (float)t, (float)variance);
        //        if (noise == Noise.Sigma) timecourse.AddPoint(seriesName, (float)t, (float)Math.Sqrt(variance));
        //        if (noise == Noise.CV) timecourse.AddPoint(seriesName, (float)t, (float)((mean == 0.0) ? 0.0 : (Math.Sqrt(variance) / mean)));
        //        if (noise == Noise.Fano) timecourse.AddPoint(seriesName, (float)t, (float)((mean == 0.0) ? 0.0 : (variance / mean)));
        //        if (noise == Noise.SigmaSqRange) timecourse.AddRange(seriesName, (float)t, (float)mean, (float)variance);
        //        if (noise == Noise.SigmaRange) timecourse.AddRange(seriesName, (float)t, (float)mean, (float)Math.Sqrt(variance));
        //    }
        //}

        //public override string ChartAddPointAsString(string seriesName, double t, double mean, double variance, Noise noise) {
        //    string s = "";
        //    if (seriesName != null) {
        //        s += seriesName + "=";
        //        if (noise == Noise.None) s += mean.ToString();
        //        if (noise == Noise.SigmaSq) s += variance.ToString();
        //        if (noise == Noise.Sigma) s += Math.Sqrt(variance);
        //        if (noise == Noise.CV) s += ((mean == 0.0) ? 0.0 : (Math.Sqrt(variance) / mean)).ToString();
        //        if (noise == Noise.Fano) s += ((mean == 0.0) ? 0.0 : (variance / mean)).ToString();
        //        if (noise == Noise.SigmaSqRange) s += mean.ToString() + "±" + variance.ToString();
        //        if (noise == Noise.SigmaRange) { double sd = Math.Sqrt(variance); s += mean.ToString() + "±" + sd.ToString(); }
        //    }
        //    return s;
        //}

        //public void VisibilityRemember() {
        //    timecourse.VisibilityRemember(MainTabbedPage.theModelEntryPage.Visibility());
        //}

        //public void InvertVisible(string name) {
        //    timecourse.InvertVisible(name);
        //}

        public override void AddParameter(string parameter, double drawn, string distribution, double[] arguments) {
            MainTabbedPage.theChartPage.AddParameter(parameter, drawn, distribution, arguments);
        }

        public override double ParameterOracle(string parameter) { // returns NAN if oracle not available
            return MainTabbedPage.theChartPage.ParameterOracle(parameter);
        }

        public override void ParametersUpdate() {
            MainTabbedPage.theChartPage.ParametersUpdate();
        }

        //public override Noise NoiseSeries() {
        //    return MainTabbedPage.theModelEntryPage.noisePickerSelection;
        //}

        //public override bool ScopeVariants() {
        //    return true;             // ### 
        //}

        //public override bool RemapVariants() {
        //    return true;             // ### 
        //}

        public override void SaveInput() {
            // throw new Error("GUI_Xamarin : not implemented");
        }

        public override void RestoreInput() {
            // throw new Error("GUI_Xamarin : not implemented");
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

        //public static string currentSolver = "RK547M"; // "GearBDF" or "RK547M"
        //public override string Solver() {
        //    return currentSolver;
        //}

        //public override bool PrecomputeLNA() {
        //    return false; // apparently zero benefit in precomputing the drift matrix
        //}

        public override void ClipboardSetText(string text) {
            // this was for Export output to the clipboard, but we do not need to do this in Android
        }

        // PLATFORM NEUTRAL TEXT EDITOR

        public class NeutralTextEditView : View, ICustomTextEdit {
            private Editor editText; 
            private string text;       // cache text content between deallocation/reallocation
            private bool editable;     // cache editable state in case it is set while editText is null
            public const float defaultFontSize = 12; // Dip
            private float fontSize = defaultFontSize; // cache fontSize state as well

            public NeutralTextEditView() : base() {
                this.editText = new Editor();
            }

            public View AsView(){
                return editText;
            }

            public void SetEditText(Editor newEditText) {
                this.editText = newEditText;
                SetText(this.text);
                SetEditable(this.editable);
                SetFontSize(this.fontSize);
            }
            public void ClearEditText() {
                this.text = editText.Text; // save the last text before deallocation
                this.editText = null;
            }
            public string GetText() {
                if (editText == null) return "";
                return editText.Text;
            }
            public void SetText(string text) {
                this.text = text;
                if (editText == null) return;
                editText.Text = text;
            }
            public void InsertText(string insertion) {
                if (editText == null) return;
                GetSelection(out int start, out int end);
                text = editText.Text;
                text = text.Substring(0, start) + insertion + text.Substring(end, text.Length - end);
                editText.Text = text;
                SetSelection(start + insertion.Length, start + insertion.Length);
            }
            public void SetFocus() {
                if (editText == null) return;
                editText.Focus();
            }
            public void ShowInputMethod() {
                if (editText == null) return;
            }
            public void HideInputMethod() {
                if (editText == null) return;
            }

            // ### on iOS, you use UITextView's SelectedRange = new Foundation.NSRange(startIndex, endIndex);

            public void SelectAll() {
                if (editText == null) return;
                //### editText.SelectAll();
            }
            public void GetSelection(out int start, out int end) {
                if (editText == null) { start = 0; end = 0; return; }
                //### start = editText.SelectionStart;
                //### end = editText.SelectionEnd;
                start = 0; end = 0;
            }
            public void SetSelection(int start, int end) {
                if (editText == null) return;
                start = Math.Max(start, 0);
                end = Math.Min(end, editText.Text.Length - 1);
                if (end < start) end = start;
                //### editText.SetSelection(start, end);
            }
            public void SetSelectionLineChar(int line, int chr, int tokenlength) {
                if (editText == null) return;
                if (line < 0 || chr < 0) return;
                string text = GetText();
                int i = 0;
                while (i < text.Length && line > 0) {
                    if (text[i] == '\n') line--;
                    i++;
                }
                if (i < text.Length && text[i] == '\r') i++;
                int linestart = i;
                while (i < text.Length && chr > 0) {chr--; i++; }
                int tokenstart = i;
                //SetSelection(linestart, tokenstart);
                SetSelection(tokenstart, tokenstart + tokenlength);
                //SetSelection(tokenstart, text.Length - 1);
            }
            public float GetFontSize() {
                return this.fontSize;
            }
            public void SetFontSize(float size) {
                this.fontSize = size;
                if (editText == null) return;
                editText.FontSize = size;
            }
            public void SetEditable(bool editable) {
                this.editable = editable;
                if (editText == null) return;
                // ### This works, but makes the editor non-scrollable and the text non-copiable
                // ### maybe just add a scroll frame on top? Still we need a custom editor for setting selection
                editText.InputTransparent = !editable;
            }
            public bool IsEditable() {
                return this.editable;
            }

            public void OnTextChanged(TextChangedDelegate del) {
                this.editText.TextChanged += (sender, e) => del(this);
            }

            public void OnFocusChange(FocusChangeDelegate del) {
                this.editText.Focused += (sender, e) => del(this);
                this.editText.Unfocused += (sender, e) => del(this);
            }
        }

    }
}
