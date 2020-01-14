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
    public class GUI_Mac : ToGui {
        public ViewController form;
        private NSObject nsForm;

        public GUI_Mac(ViewController form) {
            this.form = form;
            this.nsForm = NSObject.FromObject(form);
        }

        // https://forums.xamarin.com/discussion/24218/is-there-a-way-to-determine-if-you-are-on-the-main-thread

        public class Ack {};
        public static Ack ack = new Ack();

        public static Task<T> BeginInvokeOnMainThreadAsync<T>(Func<T> a) {   
            var tcs = new TaskCompletionSource<T>();
            NSObject nsViewCon = NSObject.FromObject(MainClass.form);
            nsViewCon.BeginInvokeOnMainThread(() => {
                try {
                    var result = a();
                    tcs.SetResult(result);
                } catch (Exception ex) { tcs.SetException(ex); }
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
            } else return BeginInvokeOnMainThreadAsync(() => { return InputGetText(); }).Result;
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
                txtTarget.SetSelectedRange(new NSRange(0,0));
                txtTarget.ScrollRangeToVisible(new NSRange(0,0));
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
                if (lineNumber >=0 && columnNumber>=0) this.SetSelectionLineChar(lineNumber, columnNumber, length);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { InputSetErrorSelection(lineNumber, columnNumber, length, failMessage); return ack; }).Result; }
        } 
        public override void OutputSetText(string text) {
            if (NSThread.IsMain) {
                var txtTarget = form.textOutput.DocumentView as AppKit.NSTextView;
                txtTarget.Editable = true;
                txtTarget.SelectAll(nsForm); txtTarget.Delete(nsForm); 
                DamnShutOffAutomaticFormatting(txtTarget);
                txtTarget.InsertText(NSObject.FromObject(text), new NSRange(0, 0));
                txtTarget.SetSelectedRange(new NSRange(0,0));
                txtTarget.ScrollRangeToVisible(new NSRange(0,0));
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
                txtTarget.SetSelectedRange(new NSRange(0,0));
                txtTarget.ScrollRangeToVisible(new NSRange(0,0));
                txtTarget.Editable = false;
           } else { _ = BeginInvokeOnMainThreadAsync(() => { OutputAppendText(text); return ack; }).Result; }
        }
        public override void BeginningExecution() { // signals that execution is starting
            if (NSThread.IsMain) {
                MainClass.form.clickerHandler.Executing(true);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { BeginningExecution(); return ack; }).Result; }
        }  
        public override void EndingExecution() { // signals that execution has ended (run to end, or stopped)
             if (NSThread.IsMain) {
                 MainClass.form.clickerHandler.Executing(false);
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
                MainClass.form.clickerHandler.SetLegend(KChartHandler.Legend());
            } else { _ = BeginInvokeOnMainThreadAsync(() => { LegendUpdate(); return ack; }).Result; }
        }

        // Output

        public override void OutputClear(string title){
            OutputSetText("");
        }
        public override void ProcessOutput() {
            Exec.currentOutputAction.action();
        }
        public override void ProcessGraph(string graphFamily) {
            OutputSetText(Export.ProcessGraph(graphFamily));
        }

        // Parameters

        public override void ParametersClear() {
            if (NSThread.IsMain) {
                MainClass.form.clickerHandler.ParametersClear();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { ParametersClear(); return ack; }).Result; }
        }
        public override void AddParameter(string parameter, double drawn, string distribution, double[] args) {
            if (NSThread.IsMain) {
                MainClass.form.clickerHandler.AddParameter(parameter, drawn, distribution, args);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { AddParameter(parameter, drawn, distribution, args); return ack; }).Result; }
        } 
        public override double ParameterOracle(string parameter) { // returns NAN if oracle not available
            if (NSThread.IsMain) {
                return MainClass.form.clickerHandler.ParameterOracle(parameter);
            } else return BeginInvokeOnMainThreadAsync(() => { return ParameterOracle(parameter); }).Result;
        }
        public override void ParametersUpdate() {
            if (NSThread.IsMain) {
                MainClass.form.clickerHandler.ParametersUpdate();
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
            try {
                if (!string.IsNullOrEmpty(text)) {
                    var pasteboard = NSPasteboard.GeneralPasteboard;
                    pasteboard.ClearContents();
                    pasteboard.WriteObjects(new NSString[] { new NSString(text) });
                }
            } catch { }
        }

        public static Environment.SpecialFolder defaultUserDataDirectoryPath = Environment.SpecialFolder.MyDocuments;
        public static Environment.SpecialFolder defaultKaemikaDataDirectoryPath = Environment.SpecialFolder.ApplicationData;

        // when running sandboxed or not, this will be ~/Documents
        public static string defaultUserDataDirectory = Environment.GetFolderPath(defaultUserDataDirectoryPath);
        // when running sandboxed, this will be  ~/Library/Containers/com.kaemika.KaemicaMac/Data/.config/Kaemika:
        // when running not sandboxed, this will be ~/.config/Kaemika
        public static string defaultKaemikaDataDirectory = Environment.GetFolderPath(defaultKaemikaDataDirectoryPath) + "/Kaemika";

        public static string CreateKaemikaDataDirectory() {
            try {
                Directory.CreateDirectory(defaultKaemikaDataDirectory);
                return defaultKaemikaDataDirectory;
            } catch { return null; }
        }

        public override void SaveInput() {
            try {
                string path = CreateKaemikaDataDirectory() + "/save.txt";
                File.WriteAllText(path, this.InputGetText());
            } catch { }
        }

        public override void RestoreInput() {
            try {
                string path = CreateKaemikaDataDirectory() + "/save.txt";
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
            SvgCanvas theCanvas = null; // store the canvas internally generated by GenPainter for use in writing the SVG out
            Func<Colorer> GenColorer = () => {
                return new CGColorer();
            };
            Func<SKSize, ChartPainter> GenPainter = (SKSize canvasSize) => {
                SvgCanvas canvas = new SvgCanvas(canvasSize, new SKSize(29.7f, 21.0f));
                theCanvas = canvas;
                return new SvgChartPainter(canvas); 
            };
            CGSize cgChartSize = form.kaemikaChart.Frame.Size;
            KChartHandler.Snap(GenColorer, GenPainter, new SKSize((float)cgChartSize.Width, (float)cgChartSize.Height));

            if (MainClass.form.macClicker != null) MainClass.form.macClicker.menuExport.Selected(true);
            var dlg = new NSSavePanel ();
            dlg.Title = "Save SVG File";
            dlg.AllowedFileTypes = new string[] { "svg" };
            dlg.Directory = ViewController.modelsDirectory;
            if (dlg.RunModal () == 1) {
                var path = "";
                try {
                    path = dlg.Url.Path;
                    File.WriteAllText(path, theCanvas.Close(), System.Text.Encoding.Unicode);
                } catch {
                    var alert = new NSAlert () {
                        AlertStyle = NSAlertStyle.Critical,
                        MessageText = "Could not write this file:",
                        InformativeText = path
                    };
                    alert.RunModal ();
                }
            }
            if (MainClass.form.macClicker != null) MainClass.form.macClicker.menuExport.Selected(false);
        }

        public override void ChartData() {
            // because of sandboxing, we must use the official Save Dialog to automatically create an exception and allow writing the file?
            if (MainClass.form.macClicker != null) MainClass.form.macClicker.menuExport.Selected(true);
            //else MainClass.form.menu_Export.Selected();
            var dlg = new NSSavePanel ();
            dlg.Title = "Save CSV File";
            dlg.AllowedFileTypes = new string[] { "csv" };
            dlg.Directory = ViewController.modelsDirectory;
            if (dlg.RunModal () == 1) {
                var path = "";
                try {
                    path = dlg.Url.Path;
                    File.WriteAllText(path, KChartHandler.ToCSV(), System.Text.Encoding.Unicode);
                } catch {
                    var alert = new NSAlert () {
                        AlertStyle = NSAlertStyle.Critical,
                        MessageText = "Could not write this file:",
                        InformativeText = path
                    };
                    alert.RunModal ();
                }
            }
            if (MainClass.form.macClicker != null) MainClass.form.macClicker.menuExport.Selected(false);
            //else MainClass.form.menu_Export.Deselected();
        }

        public override void OutputCopy() {
            ClipboardSetText(OutputGetText());
            // Removed from export menu
        }

    }
}
