using System;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using AppKit;
using CoreGraphics;
using SkiaSharp;
using Kaemika;
using KaemikaAssets;

namespace KaemikaMAC
    {
    public class MacToGui : ToGui {
        public GuiToMac form;
        private NSObject nsForm;

        public MacToGui(GuiToMac form) {
            this.form = form;
            this.nsForm = NSObject.FromObject(form);
        }

        // https://forums.xamarin.com/discussion/24218/is-there-a-way-to-determine-if-you-are-on-the-main-thread

        public class Ack { };
        public static Ack ack = new Ack();

        public static Task<T> BeginInvokeOnMainThreadAsync<T>(Func<T> a) {
            var tcs = new TaskCompletionSource<T>();
            NSObject nsViewCon = NSObject.FromObject(MainClass.guiToMac);
            nsViewCon.BeginInvokeOnMainThread(() => {
                try {
                    var result = a();
                    tcs.SetResult(result);
                }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        public void SetSelectionLineChar(int line, int chr, int tokenlength) {
            if (line < 0 || chr < 0) return;
            var txtArea = form.textInput.DocumentView as AppKit.NSTextView;
            string text = txtArea.String;
            int i = 0;
            while (i < text.Length && line > 0) {
                if (text[i] == '\n') line--;
                i++;
            }
            if (i < text.Length && text[i] == '\r') i++;
            int linestart = i;
            while (i < text.Length && chr > 0) { chr--; i++; }
            int tokenstart = i;
            txtArea.SetSelectedRange(new NSRange(tokenstart, tokenlength));
            //txtArea.BecomeFirstResponder(); // THIS CAUSES A DEADLOCK
        }

        public override string InputGetText() {
            if (NSThread.IsMain) {
                var txtTarget = form.textInput.DocumentView as AppKit.NSTextView;
                return txtTarget.String;
            }
            else return BeginInvokeOnMainThreadAsync(() => { return InputGetText(); }).Result;
        }
        private void DamnShutOffAutomaticFormatting(AppKit.NSTextView txtTarget) {
            txtTarget.AutomaticDashSubstitutionEnabled = false;
            txtTarget.AutomaticDataDetectionEnabled = false;
            txtTarget.AutomaticLinkDetectionEnabled = false;
            txtTarget.AutomaticQuoteSubstitutionEnabled = false;
            txtTarget.AutomaticSpellingCorrectionEnabled = false;
            txtTarget.AutomaticTextReplacementEnabled = false;
            txtTarget.AutomaticTextCompletionEnabled = false;
        }
        public override void InputSetText(string text) {
            if (NSThread.IsMain) {
                var txtTarget = form.textInput.DocumentView as AppKit.NSTextView;
                txtTarget.SelectAll(nsForm); txtTarget.Delete(nsForm);
                DamnShutOffAutomaticFormatting(txtTarget);
                txtTarget.InsertText(NSObject.FromObject(text), new NSRange(0, 0));
                txtTarget.SetSelectedRange(new NSRange(0, 0));
                txtTarget.ScrollRangeToVisible(new NSRange(0, 0));
            } else { _ = BeginInvokeOnMainThreadAsync(() => { InputSetText(text); return ack; }).Result; }
        }
        public override void InputInsertText(string text) {
            if (NSThread.IsMain) {
                var txtTarget = form.textInput.DocumentView as AppKit.NSTextView;
                DamnShutOffAutomaticFormatting(txtTarget);
                txtTarget.InsertText(NSObject.FromObject(text), txtTarget.SelectedRange);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { InputSetText(text); return ack; }).Result; }
        }
        public override void InputSetErrorSelection(int lineNumber, int columnNumber, int length, string failMessage) {
            if (NSThread.IsMain) {
                OutputAppendText(failMessage);
                if (lineNumber >= 0 && columnNumber >= 0) this.SetSelectionLineChar(lineNumber, columnNumber, length);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { InputSetErrorSelection(lineNumber, columnNumber, length, failMessage); return ack; }).Result; }
        }
        private static CGRect visibleText = new CGRect(0,0,0,0); // save last visible text position
        public override void OutputSetText(string text) {
            if (NSThread.IsMain) {
                var txtTarget = form.textOutput.DocumentView as AppKit.NSTextView;
                if (txtTarget.String != "" && text == "") visibleText = txtTarget.VisibleRect();
                txtTarget.Editable = true;
                txtTarget.SelectAll(nsForm); txtTarget.Delete(nsForm);
                DamnShutOffAutomaticFormatting(txtTarget);
                txtTarget.InsertText(NSObject.FromObject(text), new NSRange(0, 0));
                //txtTarget.SetSelectedRange(new NSRange(0, 0));
                //txtTarget.ScrollRangeToVisible(new NSRange(0, 0));
                if (text != "") txtTarget.ScrollRectToVisible(visibleText);
                txtTarget.Editable = false;
            } else { _ = BeginInvokeOnMainThreadAsync(() => { OutputSetText(text); return ack; }).Result; }
        }
        public override string OutputGetText() {
            if (NSThread.IsMain) {
                var txtTarget = form.textOutput.DocumentView as AppKit.NSTextView;
                return txtTarget.String;
            } else return BeginInvokeOnMainThreadAsync(() => { return OutputGetText(); }).Result;
        }
        public override void OutputAppendText(string text) {
            if (NSThread.IsMain) {
                var txtTarget = form.textOutput.DocumentView as AppKit.NSTextView;
                txtTarget.Editable = true;
                DamnShutOffAutomaticFormatting(txtTarget);
                txtTarget.InsertText(NSObject.FromObject(text), new NSRange(txtTarget.String.Length, 0));
                //txtTarget.SetSelectedRange(new NSRange(0, 0));
                //txtTarget.ScrollRangeToVisible(new NSRange(0, 0));
                txtTarget.ScrollRectToVisible(visibleText);
                txtTarget.Editable = false;
            } else { _ = BeginInvokeOnMainThreadAsync(() => { OutputAppendText(text); return ack; }).Result; }
        }
        public override void SetTraceComputational() {
            if (NSThread.IsMain) {
                MainClass.guiToMac.kControls.SetTraceComputational();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { SetTraceComputational(); return ack; }).Result; }
        }
        public override void BeginningExecution() { // signals that execution is starting
            if (NSThread.IsMain) {
                MainClass.guiToMac.kControls.Executing(true);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { BeginningExecution(); return ack; }).Result; }
        }
        public override void EndingExecution() { // signals that execution has ended (run to end, or stopped)
            if (NSThread.IsMain) {
                MainClass.guiToMac.kControls.Executing(false);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { EndingExecution(); return ack; }).Result; }
        }

        private bool continueButtonIsEnabled = false;
        public override void ContinueEnable(bool b) {
            if (NSThread.IsMain) {
                continueButtonIsEnabled = b;
                if (continueButtonIsEnabled) form.SetStartButtonToContinue(); else form.SetContinueButtonToStart();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { ContinueEnable(b); return ack; }).Result; }
        }

        public override bool ContinueEnabled() {
            return continueButtonIsEnabled;
        }

        public override void ChartUpdate() {
            if (NSThread.IsMain) {
                KChartHandler.VisibilityRestore(); // this is needed to hide the series in the chart
                form.kaemikaChart.Invalidate();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { ChartUpdate(); return ack; }).Result; }
        }

        public override void LegendUpdate() {
            if (NSThread.IsMain) {
                KChartHandler.VisibilityRestore(); // this is needed to hide the series in the legend
                MainClass.guiToMac.kControls.SetLegend();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { LegendUpdate(); return ack; }).Result; }
        }

        // Output

        public override void OutputClear(string title) {
            OutputSetText("");
        }
        public override void ProcessOutput() {
            Exec.currentOutputAction.action();
        }
        public override void ProcessGraph(string graphFamily) {
            OutputSetText(Export.ProcessGraph(graphFamily));
        }

        // Parameters

        public override void ParametersUpdate() {
            if (NSThread.IsMain) {
                MainClass.guiToMac.kControls.ParametersUpdate();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { ParametersUpdate(); return ack; }).Result; }
        }

        // Device

        public override void DeviceUpdate() {
            if (NSThread.IsMain) {
                if (form.deviceBox == null || form.kaemikaDevice == null) return;
                if (form.deviceBox.Hidden) return;
                form.kaemikaDevice.SetFrameSize(form.deviceBox.Frame.Size);
                form.kaemikaDevice.Invalidate();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { DeviceUpdate(); return ack; }).Result; }
        }
        public override void DeviceShow() { }
        public override void DeviceHide() { }

        // https://docs.microsoft.com/en-us/xamarin/mac/app-fundamentals/copy-paste
        public override void ClipboardSetText(string text) {
            ClipboardPasteText(text);
        }
        public static void ClipboardPasteText(string text) {
            try {
                if (!string.IsNullOrEmpty(text)) {
                    var pasteboard = NSPasteboard.GeneralPasteboard;
                    pasteboard.ClearContents();
                    pasteboard.WriteObjects(new NSString[] { new NSString(text) });
                }
            } catch { }
        }

        public override void SaveInput() {
            try {
                string path = MacControls.CreateKaemikaDataDirectory() + "/save.txt";
                File.WriteAllText(path, this.InputGetText());
            }  catch { }
        }

        public override void RestoreInput() {
            try {
                string path = MacControls.CreateKaemikaDataDirectory() + "/save.txt";
                if (File.Exists(path)) {
                    this.InputSetText(File.ReadAllText(path));
                } else {
                    this.InputSetText(SharedAssets.TextAsset("StartHere.txt"));
                }
            } catch { }
        }

        // https://docs.microsoft.com/en-us/xamarin/mac/app-fundamentals/copy-paste

        public override void ChartSnap() {
            CGBitmapContext theCanvas = null; // store the canvas internally generated by GenPainter for use by DoPaste
            Func<Colorer> GenColorer = () => {
                return new CGColorer();
            };
            Func<SKSize, ChartPainter> GenPainter = (SKSize canvasSize) => {
                CGBitmapContext canvas = CG.Bitmap((int)canvasSize.Width, (int)canvasSize.Height);
                CG.FlipCoordinateSystem(canvas);
                theCanvas = canvas;
                return new CGChartPainter(canvas);
            };
            Action<CGBitmapContext> DoPaste = (CGBitmapContext canvas) => {
                var pasteboard = NSPasteboard.GeneralPasteboard;
                pasteboard.ClearContents();
                pasteboard.WriteObjects(new NSImage[] { new NSImage(canvas.ToImage(), new CGSize(canvas.Width, canvas.Height)) });
            };

            CGSize cgChartSize = form.kaemikaChart.Frame.Size;
            KChartHandler.Snap(GenColorer, GenPainter, new SKSize((float)cgChartSize.Width, (float)cgChartSize.Height));
            try { DoPaste(theCanvas); } catch { }

        }

        public override void ChartSnapToSvg() {
            string svg = KChartHandler.SnapToSVG(ChartSize());

            var dlg = new NSSavePanel();
            dlg.Title = "Save SVG File";
            dlg.AllowedFileTypes = new string[] { "svg" };
            dlg.Directory = MacControls.modelsDirectory;
            if (dlg.RunModal() == 1) {
                var path = "";
                try {
                    path = dlg.Url.Path;
                    File.WriteAllText(path, svg, System.Text.Encoding.Unicode);
                } catch {
                    var alert = new NSAlert() {
                        AlertStyle = NSAlertStyle.Critical,
                        MessageText = "Could not write this file:",
                        InformativeText = path
                    };
                    alert.RunModal();
                }
            }
        }

        public override SKSize ChartSize() {
            CGSize cgChartSize = form.kaemikaChart.Frame.Size;
            return new SKSize((float)cgChartSize.Width, (float)cgChartSize.Height);
        }

        public override void ChartData() {
            // because of sandboxing, we must use the official Save Dialog to automatically create an exception and allow writing the file?
            var dlg = new NSSavePanel();
            dlg.Title = "Save CSV File";
            dlg.AllowedFileTypes = new string[] { "csv" };
            dlg.Directory = MacControls.modelsDirectory;
            if (dlg.RunModal() == 1) {
                var path = "";
                try {
                    path = dlg.Url.Path;
                    File.WriteAllText(path, KChartHandler.ToCSV(), System.Text.Encoding.Unicode);
                } catch {
                    var alert = new NSAlert() {
                        AlertStyle = NSAlertStyle.Critical,
                        MessageText = "Could not write this file:",
                        InformativeText = path
                    };
                    alert.RunModal();
                }
            }
        }

        public override void OutputCopy() {
            ClipboardSetText(OutputGetText());
            // Removed from export menu
        }

    }
}
