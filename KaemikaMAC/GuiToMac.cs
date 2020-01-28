using System;
using System.IO;
using System.Collections.Generic;
using AppKit;
using Foundation;
using Kaemika;
using CoreGraphics;
using SkiaSharp;

namespace KaemikaMAC {
    // This all runs in the gui thread: external-thread calls should be made through GuiInterface.

    public partial class GuiToMac : NSViewController {

        private PlatformTexter texter;
        private static Dictionary<float, NSFont> fonts;
        private static Dictionary<float, NSFont> fontsFixed;

        /* GUI INITIALIZATION */

        public MacControls macControls;              // set up platform-specific gui controls 
        public KControls kControls;      // bind actions to them (non-platform specific)

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

        // ====  ON LOAD =====

        public override void ViewDidLoad() {
            base.ViewDidLoad();
            // Do any additional setup after loading the view.

            Gui.platform = Kaemika.Platform.macOS;
            MainClass.guiToMac = this;                              // of type GuiToMac : ViewController,  will contain a clicker: GuiControls
            MainClass.macToGui = new MacToGui(MainClass.guiToMac);  // of type MacToGui : ToGui
            Gui.toGui = MainClass.macToGui;                         // of type ToGui
            //Gui.guiControls = MainClass.guiToMac.macControls;            // of type GuiControls

            this.texter = new PlatformTexter();
            fonts = new Dictionary<float, NSFont>();
            fontsFixed = new Dictionary<float, NSFont>();

            leftPanelClicker.Activated += (object sender, EventArgs e) => { MainClass.guiToMac.kControls.CloseOpenMenu(); };
            rightPanelClicker.Activated += (object sender, EventArgs e) =>{ MainClass.guiToMac.kControls.CloseOpenMenu(); };

            // Clicker

            macControls = new MacControls();                        // set up platform-specific gui controls 
            kControls = new KControls(macControls);      // bind actions to them (non-platform specific)

            // Text Areas

            inputTextView.alwaysDisableIBeamCursor = false; // disable only when menus are up
            outputTextView.alwaysDisableIBeamCursor = true;
            (textInput.DocumentView as AppKit.NSTextView).Font = GetFont(12.0F, true);
            (textOutput.DocumentView as AppKit.NSTextView).Font = GetFont(12.0F, true);

            // Device

            { NSBox x = deviceBox; NSDeviceView y = kaemikaDevice; }  // just checking: these are the Outlets from Main.storyboard through XCode

            // Chart

            { NSChartView x = kaemikaChart; } // just checking: this is the Outlet from Main.storyboard through XCode

            SetChartTooltip("", new CGPoint(0,0), new CGRect(0,0,0,0));

            // Legend

            { NSBox x = legendFlyoutBox; NSGridView y = legendFlyoutMenu; } // just checking: these are the Outlets from Main.storyboard through XCode

            // Saved state

            Gui.toGui.RestoreInput();

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
            Gui.toGui.SaveInput();
        }
 
        public override NSObject RepresentedObject {
            get { return base.RepresentedObject; }
            set { base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        } 
 
    }
}