using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KaemikaXM.Pages;
using Xamarin.Forms;
using Xamarin.Essentials;
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

    public class XMGui : KGuiControl {

        // INITIALIZE

        private static CustomTextEditorDelegate customTextEditor = null;

        public XMGui(CustomTextEditorDelegate customTextEditorDelegate) {
            customTextEditor = customTextEditorDelegate;
        }

        public static ICustomTextEdit TextEditor() {
            return customTextEditor();
        }

        // INVOKE ON MAIN THREAD ASYNC

        // https://forums.xamarin.com/discussion/24218/is-there-a-way-to-determine-if-you-are-on-the-main-thread
        // YES: https://docs.microsoft.com/en-us/xamarin/essentials/main-thread?content=xamarin/xamarin-forms#determining-if-code-is-running-on-the-main-thread

        public class Ack { };
        public static Ack ack = new Ack();

        public static Task<T> BeginInvokeOnMainThreadAsync<T>(Func<T> a) {
            var tcs = new TaskCompletionSource<T>();
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                try {
                    var result = a();
                    tcs.SetResult(result);
                }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        // DEVICE

        public /* Interface KGuiControl */ void GuiDeviceUpdate() {
            if (MainThread.IsMainThread) {
                if (MainTabbedPage.theChartPage != null && MainTabbedPage.theChartPage.deviceView != null)
                     MainTabbedPage.theChartPage.deviceView.InvalidateSurface();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiDeviceUpdate(); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiDeviceShow() {
            if (MainThread.IsMainThread) {
                if (MainTabbedPage.theChartPage != null && MainTabbedPage.theChartPage.deviceView != null)
                    MainTabbedPage.theChartPage.SwitchToDeviceView();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiDeviceShow(); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiDeviceHide() {
            if (MainThread.IsMainThread) {
                if (MainTabbedPage.theChartPage != null && MainTabbedPage.theChartPage.deviceView != null)
                    MainTabbedPage.theChartPage.SwitchToPlotView();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiDeviceHide(); return ack; }).Result; }
        }

        // INPUT

        public /* Interface KGuiControl */ string GuiInputGetText() {
            if (MainThread.IsMainThread) {
                return MainTabbedPage.theModelEntryPage.GetText();
            } else return BeginInvokeOnMainThreadAsync(() => { return GuiInputGetText(); }).Result;
        }

        public /* Interface KGuiControl */ void GuiInputSetText(string text) {
            if (MainThread.IsMainThread) {
                MainTabbedPage.theModelEntryPage.SetText(text);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiInputSetText(text); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiInputInsertText(string text) {
            if (MainThread.IsMainThread) {
                MainTabbedPage.theModelEntryPage.InsertText(text);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiInputInsertText(text); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ async void GuiInputSetErrorSelection(int lineNumber, int columnNumber, int length, string failCategory, string failMessage) {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(async () => {
                await MainTabbedPage.theModelEntryPage.DisplayAlert(failCategory, failMessage, "not ok");
                MainTabbedPage.SwitchToTab(MainTabbedPage.theModelEntryPageNavigation);
                MainTabbedPage.theModelEntryPage.editor.SetFocus();
                MainTabbedPage.theModelEntryPage.editor.SetSelectionLineChar(lineNumber, columnNumber, length);
            });
        }

        // OUTPUT

        public /* Interface KGuiControl */ void GuiOutputTextShow() { 
            //###
        }
        public /* Interface KGuiControl */ void GuiOutputTextHide() {
            //###
        }

        public /* Interface KGuiControl */ void GuiOutputSetText(string text, bool savePosition = false) {
            if (MainThread.IsMainThread) {
                MainTabbedPage.theOutputPage.SetText(text);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiOutputSetText(text, savePosition); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ string GuiOutputGetText() {
            if (MainThread.IsMainThread) {
                return MainTabbedPage.theOutputPage.GetText();
            } else return BeginInvokeOnMainThreadAsync(() => { return GuiOutputGetText(); }).Result;
        }

        public /* Interface KGuiControl */ void GuiOutputAppendText(string text) {
            if (MainThread.IsMainThread) {
                MainTabbedPage.theOutputPage.AppendText(text);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiOutputAppendText(text); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiProcessOutput() {
            if (MainThread.IsMainThread) {
                MainTabbedPage.theOutputPage.ProcessOutput();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiProcessOutput(); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiProcessGraph(string graphFamily) {
            if (MainThread.IsMainThread) {
                MainTabbedPage.theOutputPage.ProcessGraph(graphFamily);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiProcessGraph(graphFamily); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiOutputCopy() { }

        public /* Interface KGuiControl */ void GuiOutputClear() {
            if (MainThread.IsMainThread) {
                MainTabbedPage.theOutputPage.OutputClear();
                //KChartHandler.SetSampleName(title); ???
                //this.title = title;
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiOutputClear(); return ack; }).Result; }
        }

        // CHART

        public /* Interface KGuiControl */ void GuiChartUpdate() {
            if (MainThread.IsMainThread) {
                KChartHandler.VisibilityRestore(); // this is needed to hide the series in the chart
                MainTabbedPage.theChartPage.InvalidateChart();
             } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiChartUpdate(); return ack; }).Result; }
       }

        public /* Interface KGuiControl */ void GuiLegendUpdate() {
            if (MainThread.IsMainThread) {
                KChartHandler.VisibilityRestore(); // this is needed to hide the series in the legend
                MainTabbedPage.theChartPage.SetLegend();
             } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiLegendUpdate(); return ack; }).Result; }
        }

        public bool IsChartClear() {
            return KChartHandler.IsClear();
        }

        public /* Interface KGuiControl */ void GuiChartSnap() { }

        public /* Interface KGuiControl */ void GuiChartSnapToSvg() { }

        public /* Interface KGuiControl */ SKSize GuiChartSize() {
            if (MainThread.IsMainThread) {
                return MainTabbedPage.theChartPage.ChartSize();
            } else return BeginInvokeOnMainThreadAsync(() => { return GuiChartSize(); }).Result;
        }

        public /* Interface KGuiControl */ void GuiChartData() { }

        public /* Interface KGuiControl */ void GuiParametersUpdate() {
            if (MainThread.IsMainThread) {
                MainTabbedPage.theChartPage.ParametersUpdate();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiParametersUpdate(); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiSaveInput() {
            // throw new Error("GUI_Xamarin : not implemented");
        }

        public /* Interface KGuiControl */ void GuiRestoreInput() {
            // throw new Error("GUI_Xamarin : not implemented");
        }

        public /* Interface KGuiControl */ void GuiBeginningExecution() {
            if (MainThread.IsMainThread) {
                MainTabbedPage.Executing(true);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiBeginningExecution(); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiEndingExecution() {
            if (MainThread.IsMainThread) {
                MainTabbedPage.Executing(false);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiEndingExecution(); return ack; }).Result; }
        }

        private bool continueButtonIsEnabled = false;

        public /* Interface KGuiControl */ void GuiContinueEnable(bool b) {
            if (MainThread.IsMainThread) {
                continueButtonIsEnabled = b;
                if (continueButtonIsEnabled) MainTabbedPage.theModelEntryPage.SetStartButtonToContinue(); else MainTabbedPage.theModelEntryPage.SetContinueButtonToStart();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiContinueEnable(b); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ bool GuiContinueEnabled() {
            return continueButtonIsEnabled;
        }

        public /* Interface KGuiControl */ void GuiClipboardSetText(string text) {
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
