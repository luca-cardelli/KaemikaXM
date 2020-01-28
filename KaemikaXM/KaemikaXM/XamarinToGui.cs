using System;
using System.Collections.Generic;
using KaemikaXM.Pages;
using Xamarin.Forms;
using SkiaSharp;

namespace Kaemika {

    public class ByTime : IComparer<KChartEntry> {
        public int Compare(KChartEntry e1, KChartEntry e2) {
            if (e1.X < e2.X) return -1;
            else if (e1.X == e2.X) return 0;
            else return 1;
        }
    }

    public delegate ICustomTextEdit CustomTextEditorDelegate();

    public class XamarinToGui : ToGui {

        // INITIALIZE

        private static CustomTextEditorDelegate customTextEditor = null;

        public XamarinToGui(CustomTextEditorDelegate customTextEditorDelegate) {
            customTextEditor = customTextEditorDelegate;
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

        public override void SetTraceComputational() {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                MainTabbedPage.theOutputPage.SetTraceComputational();
            });
        }

        public override void ProcessOutput() {
            MainTabbedPage.theOutputPage.ProcessOutput();
        }

        public override void ProcessGraph(string graphFamily) {
            MainTabbedPage.theOutputPage.ProcessGraph(graphFamily);
        }

        public override void OutputCopy() { }

        public override void OutputClear(string title) {
            MainTabbedPage.theOutputPage.OutputClear();
            KChartHandler.SetSampleName(title);
            //this.title = title;
        }

        // CHART

        public override void ChartUpdate() {
            KChartHandler.VisibilityRestore(); // this is needed to hide the series in the chart
            MainTabbedPage.theChartPage.InvalidateChart();
        }

        public override void LegendUpdate() {
            KChartHandler.VisibilityRestore(); // this is needed to hide the series in the legend
            MainTabbedPage.theChartPage.SetLegend();
        }

        public bool IsChartClear() {
            return KChartHandler.IsClear();
        }

        public override void ChartSnap() { }

        public override void ChartSnapToSvg() { }

        public override SKSize ChartSize() {
            return MainTabbedPage.theChartPage.ChartSize();
        }

        public override void ChartData() { }

        public override void ParametersUpdate() {
            MainTabbedPage.theChartPage.ParametersUpdate();
        }

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
            }
            public void GetSelection(out int start, out int end) {
                if (editText == null) { start = 0; end = 0; return; }
                start = 0; end = 0;
            }
            public void SetSelection(int start, int end) {
                if (editText == null) return;
                start = Math.Max(start, 0);
                end = Math.Min(end, editText.Text.Length - 1);
                if (end < start) end = start;
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
                SetSelection(tokenstart, tokenstart + tokenlength);
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
