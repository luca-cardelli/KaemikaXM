using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using Kaemika;
using KaemikaAssets;
using CoreGraphics;
using SkiaSharp;

namespace KaemikaMAC {
    // This all runs in the gui thread: external-thread calls should be made through GuiInterface.

    public partial class GuiToMac : NSViewController, KGuiControl {

        private PlatformTexter texter;
        private static Dictionary<float, NSFont> fonts;
        private static Dictionary<float, NSFont> fontsFixed;

        /* GUI INITIALIZATION */

        public MacControls macControls;              // set up platform-specific gui controls 
        public KControls kControls;      // bind actions to them (non-platform specific) //####### Register them with KGui like on the Windows side

        // Constructor, invoked by ??? and bound to guiToMac by ViewDidLoad
        public GuiToMac(IntPtr handle) : base(handle) {
            // see ViewDidLoad
        }

        public NSFont GetFont(float pointSize, bool fixedWidth) {
            if (fixedWidth) {
                if (!fontsFixed.ContainsKey(pointSize)) fontsFixed[pointSize] = NSFont.FromFontName(this.texter.fixedFontFamily, pointSize);
                return fontsFixed[pointSize];
            } else {
                if (!fonts.ContainsKey(pointSize)) fonts[pointSize] = NSFont.FromFontName(this.texter.fontFamily, pointSize);
                return fonts[pointSize];
            }
        }

        public void SetSnapshotSize() { // do not call on ViewDidLoad: crash
            var frame = this.View.Window.Frame;
            frame.Size = new CGSize(1168, 688); // for 1280x800 snapshot
            this.View.Window.SetFrame(frame, true, true);
        }

        // ====  ON LOAD =====

        private NSObject nsForm;

        public override void ViewDidLoad() {
            base.ViewDidLoad();

            this.nsForm = NSObject.FromObject(this);

            Gui.platform = Kaemika.Platform.macOS;
            MainClass.guiToMac = this;                              // of type GuiToMac : ViewController,  will contain a clicker: GuiControls

            KGui.Register(this);

            this.texter = new PlatformTexter();
            fonts = new Dictionary<float, NSFont>();
            fontsFixed = new Dictionary<float, NSFont>();

            leftPanelClicker.Activated += (object sender, EventArgs e) => { MainClass.guiToMac.kControls.CloseOpenMenu(); };
            rightPanelClicker.Activated += (object sender, EventArgs e) =>{ MainClass.guiToMac.kControls.CloseOpenMenu(); };

            // Controls

            macControls = new MacControls();                        // set up platform-specific gui controls 
            kControls = new KControls(macControls);      // bind actions to them (non-platform specific)
            macControls.RestorePreferences(); //needs kControls initialized

            // Text Areas

            inputTextView.alwaysDisableIBeamCursor = false; // disable only when menus are up
            outputTextView.alwaysDisableIBeamCursor = true;
            (textInput.DocumentView as AppKit.NSTextView).Font = GetFont(12.0F, true);
            (textOutput.DocumentView as AppKit.NSTextView).Font = GetFont(12.0F, true);

            // Device

            { NSBox x = deviceBox; NSDeviceView y = kaemikaDevice; }  // just checking: these are the Outlets from Main.storyboard through XCode

            // Score

            { NSBox x = scoreBox; NSScoreView y = kaemikaScore; }  // just checking: these are the Outlets from Main.storyboard through XCode

            // Chart

            { NSChartView x = kaemikaChart; } // just checking: this is the Outlet from Main.storyboard through XCode

            SetChartTooltip("", new CGPoint(0,0), new CGRect(0,0,0,0));

            // Legend

            { NSBox x = legendFlyoutBox; NSGridView y = legendFlyoutMenu; } // just checking: these are the Outlets from Main.storyboard through XCode

            // Saved state

            KGui.gui.GuiRestoreInput();

            // Dark Mode Detection

            var interfaceStyle = NSUserDefaults.StandardUserDefaults.StringForKey("AppleInterfaceStyle");
            MacControls.darkMode = interfaceStyle == "Dark";
            MacControls.SwitchMode();

            NSDistributedNotificationCenter.GetDefaultCenter().
                AddObserver(this,
                new ObjCRuntime.Selector("themeChanged:"),
                new NSString("AppleInterfaceThemeChangedNotification"),
                null);

            // Keyboard Events
            // https://stackoverflow.com/questions/32446978/swift-capture-keydown-from-nsviewcontroller
            // list of keycodes:
            // https://stackoverflow.com/questions/3202629/where-can-i-find-a-list-of-mac-virtual-key-codes

            // for modifier keys
            NSEvent.AddLocalMonitorForEventsMatchingMask(NSEventMask.FlagsChanged,
                (NSEvent e) => { try { if (MyModifiersChanged(e)) return null; else return e; } catch { return e; } });
            // for normal, unmodified, keys
            //NSEvent.AddLocalMonitorForEventsMatchingMask(NSEventMask.KeyDown,
            //    (NSEvent e) => { try { if (MyKeyDown(e)) return null; else return e; } catch { return e; } });
        }

        // ====  Dark mode callback =====

        [Export("themeChanged:")] // THIS EXPORT MUST BE HERE INSIDE THE NSViewContoller CLASS
        public void ThemeChanged(NSObject change) {
            var interfaceStyle = NSUserDefaults.StandardUserDefaults.StringForKey("AppleInterfaceStyle");
            MacControls.darkMode = interfaceStyle == "Dark";
            MacControls.SwitchMode();
        }

        // SCORES - Interface between the KScoreNSControl kaemikaScore control and its enclosing NSBox scoreBox

        public void ScoreHide() {
            scoreBox.Hidden = true;
        }

        public void ScoreShow() {
            scoreBox.Hidden = false;
        }



        // ====  Mouse and Keyboard =====

        private static bool shiftKeyDown = false;

        public static bool IsShiftDown() {
            return shiftKeyDown;
        }

        private bool MyModifiersChanged(NSEvent e) {
            // handle only if current window has focus, i.e. is keyWindow
            var locWindow = this.View.Window;
            if (NSApplication.SharedApplication.KeyWindow != locWindow) return false;
            // keycodes
            if (e.ModifierFlags.HasFlag(NSEventModifierMask.ShiftKeyMask)) { //0x38 kVK_Shift
                shiftKeyDown = true;
                kaemikaChart.MyModifiersChanged(e);
                return true;
            } else {
                shiftKeyDown = false;
                kaemikaChart.MyModifiersChanged(e);
               return false;
            }
        }

        //private bool MyKeyDown(NSEvent e) {
        //    // handle keyDown only if current window has focus, i.e. is keyWindow
        //    var locWindow = this.View.Window;
        //    if (NSApplication.SharedApplication.KeyWindow != locWindow) return false;
        //    // keycodes
        //    if (e.KeyCode == 0x00) { //kVK_ANSI_A
        //        shiftKeyDown = true;
        //        return true;
        //    } else {
        //        shiftKeyDown = false;
        //        return false;
        //    }
        //}

        public void SetChartTooltip(string tip, CGPoint at, CGRect within) {
            charTooltip.Font = GetFont(8,true);
            charTooltip.StringValue = tip;
            charTooltip.SizeToFit();
            if (tip == "") {
                charTooltip.Hidden = true;
            } else {
                float off = 6;
                float pointerSize = 16;
                var tipX = (at.X < within.Size.Width / 2) ? at.X + off : at.X - charTooltip.Frame.Size.Width - off;
                var tipY = (at.Y < within.Size.Height / 2) ? at.Y + off : at.Y - charTooltip.Frame.Size.Height - off;
                if ((at.X < within.Size.Width / 2) && !(at.Y < within.Size.Height / 2)) tipX += pointerSize;
                CGPoint p = new CGPoint(tipX, tipY);
                charTooltip.SetFrameOrigin(p);
                charTooltip.Hidden = false;
                kaemikaChart.Invalidate(); // otherwise there will be shadows left on the chart when moving the tooltip quickly
            }
        }
                        
        public void SetStartButtonToContinue() {
            MainClass.guiToMac.kControls.ContinueEnable(true);

        }
        public void SetContinueButtonToStart() {
            MainClass.guiToMac.kControls.ContinueEnable(false);
        }

        //public class NSIntrinsicBox : NSBox {
        //    CGSize size;
        //    public NSIntrinsicBox(CGSize size, int border, NSColor fillColor, NSColor borderColor) {
        //        this.size = new CGSize(size.Width+2*border, size.Height+2*border);
        //        this.BoxType = NSBoxType.NSBoxCustom;
        //        this.BorderType = NSBorderType.LineBorder;
        //        this.BorderWidth = border;
        //        this.FillColor = fillColor;
        //        this.BorderColor = borderColor;
        //    }
        //    public override CGSize IntrinsicContentSize => this.size;
        //}

        //private CGSize LegendLineSize(KSeries series) {
        //    float width = 40;
        //    float height = (series.lineStyle == KLineStyle.Thick) ? 3 : (series.lineMode == KLineMode.Line) ? 1 : 8;  //############# get the widths from painter
        //    return new CGSize(width, height);
        //    }

        //private NSIntrinsicBox LegendLine (KSeries series, NSColor background) {
        //    float R = ((float)series.color.Red) / 255.0f;
        //    float G = ((float)series.color.Green) / 255.0f;
        //    float B = ((float)series.color.Blue) / 255.0f;
        //    float A = ((float)series.color.Alpha) / 255.0f;
        //    CGColor colorOverWhite = new CGColor(R*A+(1.0f-A), G*A+(1.0f-A), B*A+(1.0f-A), 1.0f);
        //    return new NSIntrinsicBox(
        //        LegendLineSize(series), 3,
        //        NSColor.FromCGColor(colorOverWhite),
        //        background);
        //}

        // #### SaveInput also on change of focus on input text area
        public override void ViewWillDisappear() {
            base.ViewWillDisappear();
            KGui.gui.GuiSaveInput();
        }
 
        public override NSObject RepresentedObject {
            get { return base.RepresentedObject; }
            set { base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }

        // ====  KGuiControl INTERFACE =====

        // INVOKE ON MAIN THREAD ASYNC
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

        public /* Interface KGuiControl */ string GuiInputGetText() {
            if (NSThread.IsMain) {
                var txtTarget = this.textInput.DocumentView as AppKit.NSTextView;
                return txtTarget.String;
            } else return BeginInvokeOnMainThreadAsync(() => { return GuiInputGetText(); }).Result;
        }

        public /* Interface KGuiControl */ void GuiInputSetText(string text) {
            if (NSThread.IsMain) {
                var txtTarget = this.textInput.DocumentView as AppKit.NSTextView;
                txtTarget.SelectAll(nsForm); txtTarget.Delete(nsForm);
                DamnShutOffAutomaticFormatting(txtTarget);
                txtTarget.InsertText(NSObject.FromObject(text), new NSRange(0, 0));
                txtTarget.SetSelectedRange(new NSRange(0, 0));
                txtTarget.ScrollRangeToVisible(new NSRange(0, 0));
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiInputSetText(text); return ack; }).Result; }
        }
        public /* Interface KGuiControl */ void GuiInputInsertText(string text) {
            if (NSThread.IsMain) {
                var txtTarget = this.textInput.DocumentView as AppKit.NSTextView;
                DamnShutOffAutomaticFormatting(txtTarget);
                txtTarget.InsertText(NSObject.FromObject(text), txtTarget.SelectedRange);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiInputInsertText(text); return ack; }).Result; }
        }
        public /* Interface KGuiControl */ void GuiInputSetErrorSelection(int lineNumber, int columnNumber, int length, string failCategory, string failMessage) {
            if (NSThread.IsMain) {
                GuiOutputAppendText(failCategory + ": " + failMessage);
                if (lineNumber >= 0 && columnNumber >= 0) this.SetSelectionLineChar(lineNumber, columnNumber, length);
                var alert = new NSAlert () { AlertStyle = NSAlertStyle.Critical, MessageText = failCategory, InformativeText = failMessage };
                alert.RunModal ();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiInputSetErrorSelection(lineNumber, columnNumber, length, failCategory, failMessage); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiOutputTextShow() {
            if (NSThread.IsMain) {
                this.textOutput.Hidden = false;
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiOutputTextShow(); return ack; }).Result; }
        }
        public /* Interface KGuiControl */ void GuiOutputTextHide() {
            if (NSThread.IsMain) {
                this.textOutput.Hidden = true;
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiOutputTextHide(); return ack; }).Result; }
        }
        private static CGRect visibleText = new CGRect(0,0,0,0); // save last visible text position
        public /* Interface KGuiControl */ void GuiOutputSetText(string text, bool savePosition = false) {
            if (NSThread.IsMain) {
                var txtTarget = this.textOutput.DocumentView as AppKit.NSTextView;
                if (txtTarget.String != "" && text == "") visibleText = txtTarget.VisibleRect();
                txtTarget.Editable = true;
                txtTarget.SelectAll(nsForm); txtTarget.Delete(nsForm);
                DamnShutOffAutomaticFormatting(txtTarget);
                txtTarget.InsertText(NSObject.FromObject(text), new NSRange(0, 0));
                //txtTarget.SetSelectedRange(new NSRange(0, 0));
                //txtTarget.ScrollRangeToVisible(new NSRange(0, 0));
                if (text != "") txtTarget.ScrollRectToVisible(visibleText);
                txtTarget.Editable = false;
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiOutputSetText(text, savePosition); return ack; }).Result; }
        }
        public /* Interface KGuiControl */ string GuiOutputGetText() {
            if (NSThread.IsMain) {
                var txtTarget = this.textOutput.DocumentView as AppKit.NSTextView;
                return txtTarget.String;
            } else return BeginInvokeOnMainThreadAsync(() => { return GuiOutputGetText(); }).Result;
        }
        public /* Interface KGuiControl */ void GuiOutputAppendText(string text) {
            if (NSThread.IsMain) {
                var txtTarget = this.textOutput.DocumentView as AppKit.NSTextView;
                txtTarget.Editable = true;
                DamnShutOffAutomaticFormatting(txtTarget);
                txtTarget.InsertText(NSObject.FromObject(text), new NSRange(txtTarget.String.Length, 0));
                //txtTarget.SetSelectedRange(new NSRange(0, 0));
                //txtTarget.ScrollRangeToVisible(new NSRange(0, 0));
                txtTarget.ScrollRectToVisible(visibleText);
                txtTarget.Editable = false;
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiOutputAppendText(text); return ack; }).Result; }
        }
        public /* Interface KGuiControl */ void GuiBeginningExecution() { // signals that execution is starting
            if (NSThread.IsMain) {
                MainClass.guiToMac.kControls.Executing(true);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiBeginningExecution(); return ack; }).Result; }
        }
        public /* Interface KGuiControl */ void GuiEndingExecution() { // signals that execution has ended (run to end, or stopped)
            if (NSThread.IsMain) {
                MainClass.guiToMac.kControls.Executing(false);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiEndingExecution(); return ack; }).Result; }
        }

        private bool continueButtonIsEnabled = false;
        public /* Interface KGuiControl */ void GuiContinueEnable(bool b) {
            if (NSThread.IsMain) {
                continueButtonIsEnabled = b;
                if (continueButtonIsEnabled) this.SetStartButtonToContinue(); else this.SetContinueButtonToStart();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiContinueEnable(b); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ bool GuiContinueEnabled() {
            return continueButtonIsEnabled;
        }

        public /* Interface KGuiControl */ void GuiChartUpdate() {
            if (NSThread.IsMain) {
                KChartHandler.VisibilityRestore(); // this is needed to hide the series in the chart
                this.kaemikaChart.Invalidate();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiChartUpdate(); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiLegendUpdate() {
            if (NSThread.IsMain) {
                KChartHandler.VisibilityRestore(); // this is needed to hide the series in the legend
                MainClass.guiToMac.kControls.SetLegend();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiLegendUpdate(); return ack; }).Result; }
        }

        // Output

        public /* Interface KGuiControl */ void GuiOutputClear() {
            GuiOutputSetText("");
        }
        public /* Interface KGuiControl */ void GuiProcessOutput() {
            Exec.currentOutputAction.action();
        }
        public /* Interface KGuiControl */ void GuiProcessGraph(string graphFamily) {
            GuiOutputSetText(Export.ProcessGraph(graphFamily));
        }

        // Parameters

        public /* Interface KGuiControl */ void GuiParametersUpdate() {
            if (NSThread.IsMain) {
                MainClass.guiToMac.kControls.ParametersUpdate();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiParametersUpdate(); return ack; }).Result; }
        }

        // Device

        public /* Interface KGuiControl */ void GuiDeviceUpdate() {
            if (NSThread.IsMain) {
                if (this.deviceBox == null || this.kaemikaDevice == null) return;
                if (this.deviceBox.Hidden) { MainClass.guiToMac.macControls.onOffDeviceView.Selected(!MainClass.guiToMac.macControls.onOffDeviceView.IsSelected()); return; }
                this.kaemikaDevice.SetFrameSize(this.deviceBox.Frame.Size);
                this.kaemikaDevice.Invalidate();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiDeviceUpdate(); return ack; }).Result; }
        }
        public /* Interface KGuiControl */ void GuiDeviceShow() { }
        public /* Interface KGuiControl */ void GuiDeviceHide() { }

        // https://docs.microsoft.com/en-us/xamarin/mac/app-fundamentals/copy-paste
        public /* Interface KGuiControl */ void GuiClipboardSetText(string text) {
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

        public /* Interface KGuiControl */ void GuiSaveInput() {
            try {
                string path = MacControls.CreateKaemikaDataDirectory() + "/save.txt";
                File.WriteAllText(path, this.GuiInputGetText());
            }  catch { }
        }

        public /* Interface KGuiControl */ void GuiRestoreInput() {
            try {
                string path = MacControls.CreateKaemikaDataDirectory() + "/save.txt";
                if (File.Exists(path)) {
                    this.GuiInputSetText(File.ReadAllText(path));
                } else {
                    this.GuiInputSetText(SharedAssets.TextAsset("StartHere.txt"));
                }
            } catch { }
        }

        // https://docs.microsoft.com/en-us/xamarin/mac/app-fundamentals/copy-paste

        public /* Interface KGuiControl */ void GuiChartSnap() {
            if (NSThread.IsMain) {
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

                CGSize cgChartSize = this.kaemikaChart.Frame.Size;
                Export.Snap(GenColorer, GenPainter, new SKSize((float)cgChartSize.Width, (float)cgChartSize.Height));
                try { DoPaste(theCanvas); } catch { }
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiChartSnap(); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiChartSnapToSvg() {
            if (NSThread.IsMain) {
                string svg = Export.SnapToSVG(GuiChartSize());

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
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiChartSnapToSvg(); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ SKSize GuiChartSize() {
            if (NSThread.IsMain) {
                CGSize cgChartSize = this.kaemikaChart.Frame.Size;
                return new SKSize((float)cgChartSize.Width, (float)cgChartSize.Height);
            } else return BeginInvokeOnMainThreadAsync(() => { return GuiChartSize(); }).Result;
        }

        public /* Interface KGuiControl */ void GuiChartData() {
            if (NSThread.IsMain) {
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
            } else { _ = BeginInvokeOnMainThreadAsync(() => { GuiChartData(); return ack; }).Result; }
        }

        public /* Interface KGuiControl */ void GuiOutputCopy() {
            GuiClipboardSetText(GuiOutputGetText());
            // Removed from export menu
        }

        public void SetSelectionLineChar(int line, int chr, int tokenlength) {
            if (line < 0 || chr < 0) return;
            var txtArea = this.textInput.DocumentView as AppKit.NSTextView;
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

        private void DamnShutOffAutomaticFormatting(AppKit.NSTextView txtTarget) {
            txtTarget.AutomaticDashSubstitutionEnabled = false;
            txtTarget.AutomaticDataDetectionEnabled = false;
            txtTarget.AutomaticLinkDetectionEnabled = false;
            txtTarget.AutomaticQuoteSubstitutionEnabled = false;
            txtTarget.AutomaticSpellingCorrectionEnabled = false;
            txtTarget.AutomaticTextReplacementEnabled = false;
            txtTarget.AutomaticTextCompletionEnabled = false;
        }


    }
}