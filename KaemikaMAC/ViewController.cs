using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using AppKit;
using Foundation;
using Kaemika;
using KaemikaAssets;
using CoreGraphics;
using SkiaSharp;

namespace KaemikaMAC
{
    public partial class ViewController : NSViewController
    {
        public ViewController(IntPtr handle) : base(handle) { }

       // https://stackoverflow.com/questions/42482434/block-mouse-bubble-in-xamarin-mac
       // BLOCK MOUSE BUBBLE

        public static NSColor nsHoverHighlightColorLM = NSColor.FromRgba(163,218,255,255); 
        public static NSColor nsHoverHighlightColorDM = NSColor.FromRgba(255-163,255-218,255-255,255); 
        public static NSColor nsHoverHighlightColor = NSColor.FromRgba(163,218,255,255);

        public static NSColor nsMainButtonDeselected = NSColor.UnemphasizedSelectedContentBackgroundColor; // AutoDarkmode
        public static NSColor nsMainButtonSelected = NSColor.TertiaryLabelColor; // AutoDarkMode
        public static NSColor nsMainButtonText = NSColor.ControlText; // AutoDarkMode

        public static NSColor nsMenuButtonDeselected = NSColor.WindowBackground; // AutoDarkmode
        public static NSColor nsMenuButtonSelected = NSColor.TertiaryLabelColor; // AutoDarkMode
        public static NSColor nsMenuButtonText = NSColor.ControlText; // AutoDarkMode
        public static NSColor nsMenuButtonHotText = NSColor.SystemBlueColor; //NSColor.FromRgba(59,142,249,255); // Ok for dark mode

        public static NSColor nsPanelButtonDeselected = nsMainButtonDeselected;
        public static NSColor nsPanelButtonSelected = nsMainButtonSelected;
        public static NSColor nsPanelButtonText = nsMenuButtonHotText;

        public static string fontFamily = "Helvetica";
        public static string fontFixedFamily = "Menlo";

        private static Dictionary<float, NSFont> fonts;
        private static Dictionary<float, NSFont> fontsFixed;
        public static NSFont GetFont(float pointSize, bool fixedWidth) {
            if (fixedWidth) {
                if (!fontsFixed.ContainsKey(pointSize)) fontsFixed[pointSize] = NSFont.FromFontName(fontFixedFamily, pointSize);
                return fontsFixed[pointSize];
            } else {
                if (!fonts.ContainsKey(pointSize)) fonts[pointSize] = NSFont.FromFontName(fontFamily, pointSize);
                return fonts[pointSize];
            }
        }

        // button hover highlight: https://stackoverflow.com/questions/6094763/simple-mouseover-effect-on-nsbutton

        // Entirely to disable the IBeam cursor showing through the menus:
        [Register("NSTextViewPlus")] // export class to use in Xcode gui builder
        public class NSTextViewPlus : NSTextView {
            #region Constructors // the magic needed by Xcode
            public NSTextViewPlus() { Initialize(); }
            public NSTextViewPlus(IntPtr handle) : base (handle) { Initialize(); }
            [Export ("initWithFrame:")]
            public NSTextViewPlus(CGRect frameRect) : base(frameRect) { Initialize(); }
            private void Initialize() {
                this.WantsLayer = true; this.LayerContentsRedrawPolicy = NSViewLayerContentsRedrawPolicy.OnSetNeedsDisplay;
                Tracking();
                }
            #endregion
            public bool alwaysDisableIBeamCursor = false;
            private NSTrackingArea trackingArea = null;
            private void Tracking() {
                if (trackingArea != null) RemoveTrackingArea(trackingArea);
                trackingArea = new NSTrackingArea(this.Frame,
                    NSTrackingAreaOptions.MouseMoved |
                    NSTrackingAreaOptions.ActiveInKeyWindow,
                    this, null);
                AddTrackingArea(trackingArea);
            }
            public override void SetFrameSize(CGSize newSize) {
                base.SetFrameSize(newSize);
                Tracking();
            }
            public override void MouseMoved(NSEvent theEvent) {
                base.MouseMoved(theEvent);
                if (disableIBeamCursor || alwaysDisableIBeamCursor) NSCursor.ArrowCursor.Set(); else NSCursor.IBeamCursor.Set();
            }
            public override void MouseDown(NSEvent theEvent) {
                base.MouseDown(theEvent);
                MainClass.form.clickerHandler.CloseOpenMenu();
            }
        }
        public static bool disableIBeamCursor = false;

        // No longer needed, but still linked in Xcode to the menu backing buttons
        [Register("NSMenuBacking")] // export class to use in Xcode gui builder
        public class NSMenuBacking : NSButton {
            #region Constructors // the magic needed by Xcode
            public NSMenuBacking() { Initialize(); }
            public NSMenuBacking(IntPtr handle) : base (handle) { Initialize(); }
            [Export ("initWithFrame:")]
            public NSMenuBacking(CGRect frameRect) : base(frameRect) { Initialize(); }
            private void Initialize() { this.WantsLayer = true; this.LayerContentsRedrawPolicy = NSViewLayerContentsRedrawPolicy.OnSetNeedsDisplay; }
            #endregion
        }

        //public static List<ButtonPlus> allImageButtons = new List<ButtonPlus>();

        // To highlite buttons and menu selections when the mouse hovers over them:
        [Register("NSButtonPlus")] // export class to use in Xcode gui builder
        public class NSButtonPlus : NSButton {
            #region Constructors // the magic needed by Xcode
            public NSButtonPlus() { Initialize(); }
            public NSButtonPlus(IntPtr handle) : base (handle) { Initialize(); }
            [Export ("initWithFrame:")]
            public NSButtonPlus(CGRect frameRect) : base(frameRect) { Initialize(); }
            private void Initialize() {  this.WantsLayer = true; this.LayerContentsRedrawPolicy = NSViewLayerContentsRedrawPolicy.OnSetNeedsDisplay; }
            #endregion
            #region Hovering Highight
            public bool activated = false; // avoid multiple event registrations
            public bool hover = true;
            private bool backgroundColorSet = false;
            private NSColor backgroundColor; // remember background for hovering highlight
            private NSTrackingArea trackingArea = null;
            public void SetBackgroundColor(NSColor color) {
                this.backgroundColor = color;
                this.Cell.BackgroundColor = color;
                backgroundColorSet = true;
            }
            //### should try override SetFrameSize, as above, instead of ResetCursorRects
            public override void ResetCursorRects() { // the only routine that seems to succesfully establish a tracking area
                base.ResetCursorRects();
                if (this.trackingArea != null) RemoveTrackingArea(this.trackingArea);
                this.trackingArea = new NSTrackingArea(this.Bounds,
                    NSTrackingAreaOptions.MouseEnteredAndExited |
                    NSTrackingAreaOptions.CursorUpdate |
                    NSTrackingAreaOptions.ActiveAlways,
                    this, null);
                AddTrackingArea(this.trackingArea);
            }
            public override void CursorUpdate(NSEvent theEvent) {
                // must have requested NSTrackingAreaOptions.CursorUpdate above for this to be notified
                base.CursorUpdate(theEvent);
            }
            public override void MouseEntered(NSEvent theEvent) {
                base.MouseEntered(theEvent);
                // Warning: trying to inspect this.backgroundColor or select colors out of it in any way crashes and is undebuggable
                //if (backgroundColorSet) this.backgroundColor.GetRgba(out nfloat r, out nfloat g, out nfloat b, out nfloat a);
                if (this.Enabled && this.hover && backgroundColorSet) this.Cell.BackgroundColor = nsHoverHighlightColor;
            }
            public override void MouseExited(NSEvent theEvent) {
                base.MouseExited(theEvent);
                if (this.hover && backgroundColorSet) this.Cell.BackgroundColor = this.backgroundColor;
            }
            public override void MouseDown(NSEvent theEvent) {
                base.MouseDown(theEvent);
                if (this.hover && backgroundColorSet) this.Cell.BackgroundColor = this.backgroundColor;
            }
            #endregion

        }

        // ====  COMMON ONOFF BUTTONS =====

        public static List<MacOnOffButton> allMacOnOffButtons = new List<MacOnOffButton>();
        public static List<MacFlyoutMenu> allMacFlyoutMenus = new List<MacFlyoutMenu>();

        private static void SetupButton(NSButtonPlus buttonPlus, NSColor textColor, NSColor backgroundColor) {
            buttonPlus.SetBackgroundColor(backgroundColor);
            buttonPlus.ContentTintColor = textColor;
            buttonPlus.Bordered = false;
        }

        public class MacControl : KControl {
            public NSControl control;
        }

        public class MacOnOffButton : MacControl, KButton {
            public NSButtonPlus button;
            private NSColor textColor;
            private NSColor backgroundColor;
            private NSColor selectedColor;
            private bool isSelected;
            private NSImage imageLM;
            private NSImage imageDM;
            public MacOnOffButton (NSButtonPlus button, NSColor nsButtonText, NSColor nsButtonDeselected, NSColor nsButtonSelected) {
                this.control = button;
                this.button = button;
                this.textColor = nsButtonText;
                this.backgroundColor = nsButtonDeselected;
                this.selectedColor = nsButtonSelected;
                this.isSelected = false;
                SetupButton(button, nsButtonText, nsButtonDeselected);
                allMacOnOffButtons.Add(this);
            }
            public void SetImage(string imageName) {
                NSImage imageLM = null;
                NSImage imageDM = null;
                this.button.Title = "";
                if (imageName == "icons8stop40") { imageLM = NSImage.ImageNamed("icons8stop40"); imageDM = NSImage.ImageNamed("icons8stop40"); }
                if (imageName == "icons8play40") { imageLM = NSImage.ImageNamed("icons8play40"); imageDM = NSImage.ImageNamed("icons8play40"); }
                if (imageName == "Noise_None_W_48x48") { imageLM = NSImage.ImageNamed("Noise_None.W.48x48"); imageDM = NSImage.ImageNamed("DMNoise_None.W.48x48.DM");}
                if (imageName == "Noise_SigmaRange_W_48x48") { imageLM = NSImage.ImageNamed("Noise_SigmaRange.W.48x48"); imageDM = NSImage.ImageNamed("DMNoise_SigmaRange.W.48x48.DM");}
                if (imageName == "Noise_Sigma_W_48x48") { imageLM = NSImage.ImageNamed("Noise_Sigma.W.48x48"); imageDM = NSImage.ImageNamed("DMNoise_Sigma.W.48x48.DM");}
                if (imageName == "Noise_CV_W_48x48") { imageLM = NSImage.ImageNamed("Noise_CV.W.48x48"); imageDM = NSImage.ImageNamed("DMNoise_CV.W.48x48.DM");}
                if (imageName == "Noise_SigmaSqRange_W_48x48") { imageLM = NSImage.ImageNamed("Noise_SigmaSqRange.W.48x48"); imageDM = NSImage.ImageNamed("DMNoise_SigmaSqRange.W.48x48.DM");}
                if (imageName == "Noise_SigmaSq_W_48x48") { imageLM = NSImage.ImageNamed("Noise_SigmaSq.W.48x48"); imageDM = NSImage.ImageNamed("DMNoise_SigmaSq.W.48x48.DM");}
                if (imageName == "Noise_Fano_W_48x48") { imageLM = NSImage.ImageNamed("Noise_Fano.W.48x48"); imageDM = NSImage.ImageNamed("DMNoise_Fano.W.48x48.DM");}
                if (imageName == "Computation_48x48") { imageLM = NSImage.ImageNamed("Computation.48x48"); imageDM = NSImage.ImageNamed("DMComputation.48x48.DM");}
                if (imageName == "icons8device_OFF_48x48") { imageLM = NSImage.ImageNamed("icons8device.OFF.48x48"); imageDM = NSImage.ImageNamed("icons8device.OFF.48x48");}
                if (imageName == "icons8device_ON_48x48") { imageLM = NSImage.ImageNamed("icons8device.ON.48x48"); imageDM = NSImage.ImageNamed("icons8device.ON.48x48");}
                if (imageName == "deviceBorder_W_48x48") { imageLM = NSImage.ImageNamed("deviceBorder.W.48x48"); imageDM = NSImage.ImageNamed("DMdeviceBorder.W.48x48.DM"); }
                if (imageName == "FontSizePlus_W_48x48") { imageLM = NSImage.ImageNamed("FontSizePlus.W.48x48"); imageDM = NSImage.ImageNamed("DMFontSizePlus.W.48x48.DM"); }
                if (imageName == "FontSizeMinus_W_48x48") { imageLM = NSImage.ImageNamed("FontSizeMinus.W.48x48"); imageDM = NSImage.ImageNamed("DMFontSizeMinus.W.48x48.DM");}
                if (imageName == "FileSave_48x48") { imageLM = NSImage.ImageNamed("FileSave.48x48"); imageDM = NSImage.ImageNamed("DMFileSave.48x48.DM");}
                if (imageName == "FileLoad_48x48") { imageLM = NSImage.ImageNamed("FileLoad.48x48"); imageDM = NSImage.ImageNamed("DMFileLoad.48x48.DM");}
                if (imageName == "icons8pauseplay40") { imageLM = NSImage.ImageNamed("icons8pauseplay40"); imageDM = NSImage.ImageNamed("icons8pauseplay40");}
                if (imageName == "icons8combochart96_W_48x48") { imageLM = NSImage.ImageNamed("icons8combochart96.W.48x48"); imageDM = NSImage.ImageNamed("DMicons8combochart96.W.48x48.DM");}
                if (imageName == "icons8_share_384_W_48x48") { imageLM = NSImage.ImageNamed("icons8-share-384.W.48x48");  imageDM = NSImage.ImageNamed("DMicons8-share-384.W.48x48.DM");}
                if (imageName == "icons8_keyboard_96_W_48x48") { imageLM = NSImage.ImageNamed("icons8-keyboard-96.W.48x48"); imageDM = NSImage.ImageNamed("DMicons8-keyboard-96.W.48x48.DM");}
                if (imageName == "icons8_settings_384_W_48x48") { imageLM = NSImage.ImageNamed("icons8-settings-384.W.48x48"); imageDM = NSImage.ImageNamed("DMicons8-settings-384.W.48x48.DM");}
                if (imageName == "icons8text_48x48") { imageLM = NSImage.ImageNamed("icons8text.48x48"); imageDM = NSImage.ImageNamed("DMicons8text.48x48.DM");}
                if (imageName == "Parameters_W_48x48") { imageLM = NSImage.ImageNamed("Parameters.W.48x48"); imageDM = NSImage.ImageNamed("DMParameters.W.48x48.DM");}
                if (imageLM == null | imageDM == null) throw new Error("SetImage");
                this.imageLM = imageLM;
                this.imageDM = imageDM;
                this.button.Image = darkMode ? imageDM : imageLM;
            }
            private void SetImage(NSImage image) {
                 this.button.Image = image;
               }
            public void SetLegendImage(KSeries series) {
                NSImage nsImage;
                int thickness = (series.lineStyle == KLineStyle.Thick) ? 3 : (series.lineMode == KLineMode.Line) ? 1 : 8;
                const int height = 16; const int width = 50; const int padding = 1; const int frame = 3; const int left = 4;
                int framedH = thickness + 2*frame; int framedY = (height-2*padding-framedH)/2;
                int framedW = width - 2*padding; int framedX = left + padding;
                using (CGBitmapContext bitmap = CG.Bitmap(left+width,height)) {
                    CG.DrawRect(bitmap, new CGRect(0,0,left+width,height), CG.Color(new SKColor(0,0,0,0)));  // transparent
                    if (series.visible) {
                        CG.DrawRect(bitmap, new CGRect(framedX,framedY,framedW,framedH), CG.Color(SKColors.White));
                        CG.DrawRect(bitmap, new CGRect(framedX+frame,framedY+frame,framedW-2*frame,thickness), CG.Color(series.color));
                    }
                    using (CGImage cgImage = bitmap.ToImage()) {
                        nsImage = new NSImage(cgImage, new CGSize(cgImage.Width, cgImage.Height));
                    }
                }
                this.imageDM = nsImage;
                this.imageLM = nsImage;
                this.button.Image = nsImage;
            }
            public void SwitchMode(bool darkMode) {
                if (this.button == MainClass.form.buttonNoise) SetImage(ImageOfNoise(ClickerHandler.SelectNoiseSelectedItem, darkMode));
                else SetImage(darkMode ? this.imageDM : this.imageLM);
            }
            public string GetText() {
                return this.button.Title;
            }
            public void SetText(string text) {
                if (text != null) this.button.Title = text;
                this.button.SetFrameSize(new CGSize(200,20));
            }
            public void SetFont(NSFont font) {
                this.button.Font = font;
            }
            public bool IsVisible() {
                return !this.button.Hidden;
            }
            public void Visible(bool b) {
                this.button.Hidden = !b;
            }
            public bool IsEnabled() {
                return this.button.Enabled;
            }
            public void Enabled(bool b) {
                this.button.Enabled = b;
            }
            public bool IsSelected() {
                return this.isSelected;
            }
            public void Selected(bool b) {
                this.isSelected = b;
                this.button.SetBackgroundColor(b ? selectedColor : backgroundColor);
            }
            public void Hover(bool b) {
                this.button.hover = b;
                }
            public void OnClick(EventHandler handler) {
                this.button.Activated += handler;
            }
        }

        public class MacSlider : MacControl, KSlider {
            public NSSlider trackbar;
            NSColor cMenuButtonText;
            NSColor cMenuButtonDeselected;
            NSColor cMenuButtonSelected;
            public MacSlider(NSSlider trackbar, NSColor cMenuButtonText, NSColor cMenuButtonDeselected, NSColor cMenuButtonSelected) {
                this.control = trackbar;
                this.trackbar = trackbar;
                this.cMenuButtonText = cMenuButtonText;
                this.cMenuButtonDeselected = cMenuButtonDeselected;
                this.cMenuButtonSelected = cMenuButtonSelected;
            }
            public void SetBounds(int min, int max) {
                this.trackbar.MinValue = min;
                this.trackbar.MaxValue = max;
            }
            public void SetValue(int value) {
                this.trackbar.IntValue = value;
            }
            public int GetValue() {
                return this.trackbar.IntValue;
            }
            public void OnClick(EventHandler handler) {
                this.trackbar.Activated += handler;
            }
        }

        public enum FlyoutAttachment { RightDown, LeftDown, RightUp, LeftUp, RightTop, LeftTop, TextOutputLeft, TextOutputRight };

        public class MacFlyoutMenu : MacOnOffButton, KFlyoutMenu {
            private NSBox buttonBar;
            private NSBox menuBox;
            private NSGridView menu;
            private FlyoutAttachment attachment;
            public bool autoClose { get; set; }
            private float pointSize;
            private bool fixedWidth;
            private NSColor nsMenuButtonText;
            private NSColor nsMenuButtonDeselected;
            private NSColor nsMenuButtonSelected;
            public KButton selectedItem { get; set; }
            public MacFlyoutMenu(NSButtonPlus button, NSGridView menu, NSBox menuBox, NSBox buttonBar, FlyoutAttachment attachment, float pointSize, bool fixedWidth,
                                 NSColor nsMainButtonText, NSColor nsMainButtonDeselected, NSColor nsMainButtonSelected, NSColor nsMenuButtonText, NSColor nsMenuButtonDeselected, NSColor nsMenuButtonSelected)
                                : base(button, nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected) {
                this.menu = menu;
                this.menu.RowSpacing = 1;
                this.menu.ColumnSpacing = 6;
                this.menuBox = menuBox;
                this.buttonBar = buttonBar;
                this.autoClose = false;
                this.pointSize = pointSize;
                this.fixedWidth = fixedWidth;
                this.attachment = attachment;
                this.nsMenuButtonText = nsMenuButtonText;
                this.nsMenuButtonDeselected = nsMenuButtonDeselected;
                this.nsMenuButtonSelected = nsMenuButtonSelected;
                menuBox.FillColor = nsMenuButtonDeselected;
                menuBox.Hidden = true;
                allMacFlyoutMenus.Add(this);
            }
            public void ClearMenuItems() {
                this.menu.Subviews = new NSView[]{ }; // we really need to work hard to clean it up:
                while (this.menu.RowCount > 0) for (int i = 0; i < this.menu.RowCount; i++) {this.menu.RemoveRow(i); }
                while (this.menu.ColumnCount > 0) for (int j = 0; j < this.menu.ColumnCount; j++) {this.menu.RemoveColumn(j); }
            }
            public void AddMenuItem(KControl item) {
                this.menu.AddRow(new NSView[]{ ((MacControl)item).control });
                this.menu.SetFrameOrigin(new CGPoint(0,0)); // w.r.t. the menuBox
            }
            public void AddMenuItems(KControl[] items) {
                NSGridView row = new NSGridView();
                row.ColumnSpacing = 0;
                for (int i = 0; i < items.Length; i++)
                    row.AddColumn(new NSView[1]{ ((MacControl)items[i]).control });
                this.menu.AddRow(new NSView[1]{ row });
                this.menu.SetFrameOrigin(new CGPoint(0,0)); // w.r.t. the menuBox
            }
            public void AddMenuRow(KControl[] items) {
                NSView[] row = new NSView[items.Length];
                for (int i = 0; i < items.Length; i++) row[i] = ((MacControl)items[i]).control;
                this.menu.AddRow(row);
                this.menu.SetFrameOrigin(new CGPoint(0,0)); // w.r.t. the menuBox
            }
            public void AddMenuGrid(KControl[,] items) {
                var colNo = items.GetLength(0);
                var rowNo = items.GetLength(1);
                for(int r = 0; r < rowNo; r++) {
                    var row = new KControl[colNo];
                    for (int c = 0; c < colNo; c++) row[c] = items[c,r];
                    AddMenuRow(row);
                }
            }
            public void AddSeparator() {
                NSBox separator = new NSBox();
                separator.BoxType = NSBoxType.NSBoxCustom;
                separator.BorderType = NSBorderType.LineBorder;
                separator.BorderWidth = 1;
                separator.SetFrameSize(new CGSize(10,2));
                separator.FillColor = nsMainButtonDeselected;
                separator.BorderColor = nsMainButtonDeselected;
                this.menu.AddRow(new NSView[1] { separator });
                separator.LeftAnchor.ConstraintEqualToAnchor(this.menu.LeftAnchor).Active = true;
                separator.RightAnchor.ConstraintEqualToAnchor(this.menu.RightAnchor).Active = true;
                this.menu.SetFrameOrigin(new CGPoint(0,0)); // w.r.t. the menuBox
            }
            public KButton NewMenuSection(int level = 1) {
                var sectionButton = new MacOnOffButton(new NSButtonPlus(), nsMenuButtonHotText, this.nsMenuButtonDeselected, this.nsMenuButtonSelected);
                sectionButton.SetFont(GetFont(this.pointSize - (level - 1), this.fixedWidth));
                sectionButton.Hover(false);
                return sectionButton;
            }
            public KButton NewMenuItemButton() {
                var itemButton = new MacOnOffButton(new NSButtonPlus(), this.nsMenuButtonText, this.nsMenuButtonDeselected, this.nsMenuButtonSelected);
                itemButton.SetFont(GetFont(this.pointSize, this.fixedWidth));
                return itemButton;
            }
            public KSlider NewMenuItemTrackBar() {
                var trackBar = new MacSlider(new NSSlider(), this.nsMenuButtonText, this.nsMenuButtonDeselected, this.nsMenuButtonSelected);
                return trackBar;
            }
            public bool IsOpen() {
                return !this.menuBox.Hidden;
            }
            public void Attach() {
                //var grid = this.menuBox.Subviews[1].Subviews[0];
                if (this.attachment == FlyoutAttachment.RightDown) // w.r.t. button
                    this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X + this.button.Frame.Width, this.buttonBar.Frame.Y + (this.button.Frame.Y + this.button.Frame.Height) - this.menu.Frame.Height));
                else if (this.attachment == FlyoutAttachment.LeftDown) // w.r.t. button
                    this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X - this.menu.Frame.Width, this.buttonBar.Frame.Y + (this.button.Frame.Y + this.button.Frame.Height) - this.menu.Frame.Height));
                else if (this.attachment == FlyoutAttachment.LeftUp) // w.r.t. button
                    this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X - this.menu.Frame.Width, this.buttonBar.Frame.Y + this.button.Frame.Y));
                else if (this.attachment == FlyoutAttachment.RightUp) // w.r.t. button
                    this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X + this.button.Frame.Width, this.buttonBar.Frame.Y + this.button.Frame.Y));
                else if (this.attachment == FlyoutAttachment.RightTop) // w.r.t. window
                    this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X + this.button.Frame.Width, this.buttonBar.Frame.Y + this.buttonBar.Frame.Height - this.menu.Frame.Height));
                else if (this.attachment == FlyoutAttachment.LeftTop) // w.r.t. window
                    this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X - this.menu.Frame.Width, this.buttonBar.Frame.Y + this.buttonBar.Frame.Height - this.menu.Frame.Height));
                else if (this.attachment == FlyoutAttachment.TextOutputLeft) { // w.r.t. textOutput
                    var txtOut = MainClass.form.textOutput;
                    this.menuBox.SetFrameOrigin(new CGPoint(0, txtOut.Frame.Height - this.menu.Frame.Height));
                } else if (this.attachment == FlyoutAttachment.TextOutputRight) { // w.r.t. textOutput
                    var txtOut = MainClass.form.textOutput;
                    this.menuBox.SetFrameOrigin(new CGPoint(txtOut.Frame.Width - this.menu.Frame.Width, txtOut.Frame.Height - this.menu.Frame.Height));
                }
                else throw new Error("Attach");
            }
            public void Open() {
                this.menu.SetFrameOrigin(new CGPoint(0,0));
                this.menu.SetFrameSize(this.menu.FittingSize);
                this.menuBox.SetFrameSize(this.menu.Frame.Size);
                this.menu.SetFrameOrigin(new CGPoint(0,0));
                this.menu.SetFrameSize(this.menu.FittingSize);
                this.menuBox.SetFrameSize(this.menu.Frame.Size);
                Attach();
                Selected(true);
                this.menuBox.Hidden = false;
                if (this.autoClose) disableIBeamCursor = true;
            }
            public void Close() {
                this.menuBox.Hidden = true;
                Selected(false);
                if (this.autoClose) disableIBeamCursor = false;
           }
        }

        public static string modelsDirectory = string.Empty;

        public class MacClicker : FromGui {
            public KButton onOffStop { get; }
            public KButton onOffEval { get; }
            public KButton onOffDevice { get; }
            public KButton onOffDeviceView { get; }
            public KButton onOffFontSizePlus { get; }
            public KButton onOffFontSizeMinus { get; }
            public KButton onOffSave { get; }
            public KButton onOffLoad { get; }
            public KFlyoutMenu menuTutorial { get; }
            public KFlyoutMenu menuNoise { get; }
            public KFlyoutMenu menuOutput { get; }
            public KFlyoutMenu menuExport { get; }
            public KFlyoutMenu menuMath { get; }
            public KFlyoutMenu menuLegend { get; }
            public KFlyoutMenu menuParameters { get; }
            public KFlyoutMenu menuSettings { get; }
            public MacClicker() {
                modelsDirectory = Environment.GetFolderPath(GUI_Mac.defaultUserDataDirectoryPath);
                RestoreDirectories();
                onOffStop = MenuButton((NSButtonPlus)MainClass.form.buttonStop);
                onOffEval = MenuButton((NSButtonPlus)MainClass.form.buttonPlay);
                onOffDevice = MenuButton((NSButtonPlus)MainClass.form.buttonDevice);
                onOffDeviceView = MenuButton((NSButtonPlus)MainClass.form.buttonDeviceView);
                onOffFontSizePlus = MenuButton((NSButtonPlus)MainClass.form.buttonFontBigger);
                onOffFontSizeMinus = MenuButton((NSButtonPlus)MainClass.form.buttonFontSmaller);
                onOffSave = MenuButton((NSButtonPlus)MainClass.form.buttonSave);
                onOffLoad = MenuButton((NSButtonPlus)MainClass.form.buttonLoad);
                menuTutorial = new MacFlyoutMenu((NSButtonPlus)MainClass.form.buttonTutorial, MainClass.form.tutorialFlyoutMenu, MainClass.form.tutorialFlyoutBox, MainClass.form.leftButtonPanel, FlyoutAttachment.RightTop, 14.0F, false,
                    nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
                menuNoise = new MacFlyoutMenu((NSButtonPlus)MainClass.form.buttonNoise, MainClass.form.noiseFlyoutMenu, MainClass.form.noiseFlyoutBox, MainClass.form.rightButtonPanel, FlyoutAttachment.LeftDown, 14.0F, false,
                    nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
                menuOutput = new MacFlyoutMenu((NSButtonPlus)MainClass.form.buttonCompute, MainClass.form.computeFlyoutMenu, MainClass.form.computeFlyoutBox, MainClass.form.rightButtonPanel, FlyoutAttachment.LeftDown, 18.0F, false,
                    nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
                menuExport = new MacFlyoutMenu((NSButtonPlus)MainClass.form.buttonShare, MainClass.form.shareFlyoutMenu, MainClass.form.shareFlyoutBox, MainClass.form.leftButtonPanel, FlyoutAttachment.RightDown, 18.0F, false,
                    nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
                menuMath = new MacFlyoutMenu((NSButtonPlus)MainClass.form.buttonKeyboard, MainClass.form.keyboardFlyoutMenu, MainClass.form.keyboardFlyoutBox, MainClass.form.leftButtonPanel, FlyoutAttachment.RightDown, 20.0F, true,
                    nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
                menuLegend = new MacFlyoutMenu((NSButtonPlus)MainClass.form.buttonLegend, MainClass.form.legendFlyoutMenu, MainClass.form.legendFlyoutBox, MainClass.form.rightButtonPanel, FlyoutAttachment.TextOutputRight, 12.0F, true,
                    nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
                menuParameters = new MacFlyoutMenu((NSButtonPlus)MainClass.form.buttonParameters, MainClass.form.parametersFlyoutMenu, MainClass.form.parameterBox, MainClass.form.leftButtonPanel, FlyoutAttachment.TextOutputLeft, 12.0F, true,
                    nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
                menuSettings = new MacFlyoutMenu((NSButtonPlus)MainClass.form.buttonSettings, MainClass.form.settingsFlyoutMenu, MainClass.form.settingsFlyoutBox, MainClass.form.rightButtonPanel, FlyoutAttachment.LeftUp, 14.0F, false,
                    nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
            }
            private KButton MenuButton(NSButtonPlus button) {
                return new MacOnOffButton(button, nsMenuButtonText, nsMainButtonDeselected, nsMainButtonSelected);
            }
            public bool IsMicrofluidicsVisible() { 
                return !MainClass.form.deviceBox.Hidden; 
            }
            public void MicrosfluidicsVisible(bool on) {
                MainClass.form.deviceBox.Hidden = !on;
            }
            public void MicrofluidicsOn() {
                MainClass.form.deviceBox.Hidden = false;
            }
            public void MicrofluidicsOff() {
                MainClass.form.deviceBox.Hidden = true;
            }
            public void IncrementFont(float pointSize) {
                var txtInput = MainClass.form.textInput.DocumentView as AppKit.NSTextView;
                SetFont((float)(txtInput.Font.FontDescriptor.PointSize + pointSize));
            }

            public void SplashOff() {
                MainClass.form.splashImageBacking.Hidden = true;
                MainClass.form.splashImage.Hidden = true;
            }
            public void Save() {
                var dlg = new NSSavePanel ();
                dlg.Title = "Save Text File";
                dlg.AllowedFileTypes = new string[] { "txt" };
                dlg.Directory = modelsDirectory;
                if (dlg.RunModal () == 1) {
                    var path = "";
                    try {
                        path = dlg.Url.Path;
                        File.WriteAllText(path, Gui.gui.InputGetText(), System.Text.Encoding.Unicode);
                    } catch {
                        var alert = new NSAlert () {
                            AlertStyle = NSAlertStyle.Critical,
                            MessageText = "Could not write this file:",
                            InformativeText = path
                        };
                        alert.RunModal ();
                    }
                }
            }
            public void Load() {
                var dlg = NSOpenPanel.OpenPanel;
                dlg.Title = "Open Text File";
                dlg.CanChooseFiles = true;
                dlg.CanChooseDirectories = false;
                dlg.Directory = modelsDirectory;
                dlg.AllowedFileTypes = new string[] { "txt" };
                if (dlg.RunModal () == 1) {
                    var path = "";
                    try {
                        path = dlg.Urls[0].Path;
                        Gui.gui.InputSetText(File.ReadAllText(path, System.Text.Encoding.Unicode));
                    } catch {
                        var alert = new NSAlert () {
                            AlertStyle = NSAlertStyle.Critical,
                            MessageText = "Could not load this file:",
                            InformativeText = path
                        };
                        alert.RunModal ();
                    }
                }
            }
            public void SetDirectory() {
                var initialDirectory = modelsDirectory;
                var dlg = NSOpenPanel.OpenPanel;
                dlg.CanChooseFiles = false;
                dlg.CanChooseDirectories = true;
                if (initialDirectory != string.Empty) dlg.DirectoryUrl = new NSUrl(initialDirectory);
                if (dlg.RunModal () == 1) {
                    modelsDirectory = dlg.Urls[0].Path;
                    SaveDirectories();
                }
            }
            public void SaveDirectories() {
                try {
                    string path2 = GUI_Mac.CreateKaemikaDataDirectory() + "/modelsdir.txt";
                    File.WriteAllText(path2, modelsDirectory);
                } catch { }
            }

            public void RestoreDirectories() {
                try {
                    string path2 = GUI_Mac.CreateKaemikaDataDirectory() + "/modelsdir.txt";
                    if (File.Exists(path2)) { modelsDirectory = File.ReadAllText(path2); }
                } catch { }
            }
        }

        public static void SetFont(float size) {
            if (size >= 6){
                var txtInput = MainClass.form.textInput.DocumentView as AppKit.NSTextView;
                var txtOutput = MainClass.form.textOutput.DocumentView as AppKit.NSTextView;
                var newFont = NSFont.FromFontName(txtInput.Font.FontName, size);
                txtInput.Font = newFont;
                txtOutput.Font = newFont;
            }
        }

        private static NSImage ImageOfNoise(Noise noise, bool isDarkMode) {
            if (noise == Noise.None) { if (isDarkMode) return NSImage.ImageNamed("DMNoise_None.W.48x48.DM"); else return NSImage.ImageNamed("Noise_None.W.48x48"); }
            if (noise == Noise.SigmaRange) { if (isDarkMode) return NSImage.ImageNamed("DMNoise_SigmaRange.W.48x48.DM"); else return NSImage.ImageNamed("Noise_SigmaRange.W.48x48"); }
            if (noise == Noise.Sigma) { if (isDarkMode) return NSImage.ImageNamed("DMNoise_Sigma.W.48x48.DM"); else return NSImage.ImageNamed("Noise_Sigma.W.48x48"); }
            if (noise == Noise.CV) { if (isDarkMode) return NSImage.ImageNamed("DMNoise_CV.W.48x48.DM"); else return NSImage.ImageNamed("Noise_CV.W.48x48"); }
            if (noise == Noise.SigmaSqRange) { if (isDarkMode) return NSImage.ImageNamed("DMNoise_SigmaSqRange.W.48x48.DM"); else return NSImage.ImageNamed("Noise_SigmaSqRange.W.48x48"); }
            if (noise == Noise.SigmaSq) { if (isDarkMode) return NSImage.ImageNamed("DMNoise_SigmaSq.W.48x48.DM"); else return NSImage.ImageNamed("Noise_SigmaSq.W.48x48"); }
            if (noise == Noise.Fano) { if (isDarkMode) return NSImage.ImageNamed("DMNoise_Fano.W.48x48.DM"); else return NSImage.ImageNamed("Noise_Fano.W.48x48"); }
            throw new Error("ImageOfNoise");
        }

        // ====  Dark mode callback =====

        public static bool darkMode = false;
        public static void SwitchMode() {
            nsHoverHighlightColor = darkMode ? nsHoverHighlightColorDM : nsHoverHighlightColorLM;
            MainClass.form.splashImageBacking.FillColor = darkMode ? NSColor.Black : NSColor.White;
            MainClass.form.splashImage.Image = darkMode ? NSImage.ImageNamed("DMSplash.589.DM") : NSImage.ImageNamed("Splash.589");
            foreach (var b in allMacOnOffButtons) b.SwitchMode(darkMode);
            foreach (var b in allMacFlyoutMenus) b.SwitchMode(darkMode);
        }

        [Export("themeChanged:")]
        public void ThemeChanged(NSObject change) {
            var interfaceStyle = NSUserDefaults.StandardUserDefaults.StringForKey("AppleInterfaceStyle");
            darkMode = interfaceStyle == "Dark";
            SwitchMode();
        }

        // ====  VIEW CONTROLLER =====

        public MacClicker macClicker;              // set up platform-specific gui controls 
        public ClickerHandler clickerHandler;      // bind actions to them (non-platform specific)

        public override void ViewDidLoad() {
            base.ViewDidLoad();
            // Do any additional setup after loading the view.

            MainClass.form = this;                        // of type ViewController
            MainClass.gui = new GUI_Mac(MainClass.form);  // of type GUI_Mac : GuiInterface
            Gui.gui = MainClass.gui;                      // of type GuiInterface

            fonts = new Dictionary<float, NSFont>();
            fontsFixed = new Dictionary<float, NSFont>();

            leftPanelClicker.Activated += (object sender, EventArgs e) => { MainClass.form.clickerHandler.CloseOpenMenu(); };
            rightPanelClicker.Activated += (object sender, EventArgs e) =>{ MainClass.form.clickerHandler.CloseOpenMenu(); };

            (MainClass.form.textInput.DocumentView as AppKit.NSTextView).Font = GetFont(12.0F, true);
            (MainClass.form.textOutput.DocumentView as AppKit.NSTextView).Font = GetFont(12.0F, true);

            // Text Areas

            inputTextView.alwaysDisableIBeamCursor = false; // disable only when menus are up
            outputTextView.alwaysDisableIBeamCursor = true;

            // Buttons and Menus: import platform-independent logic

            macClicker = new MacClicker();                        // set up platform-specific gui controls 
            clickerHandler = new ClickerHandler(macClicker);      // bind actions to them (non-platform specific)

            // Chart

            { NSChartView x = kaemikaChart; } // just checking: this is the Outlet from Main.storyboard through XCode

            SetChartTooltip("", new CGPoint(0,0), new CGRect(0,0,0,0));

            // Legend

            { NSBox x = legendFlyoutBox; NSGridView y = legendFlyoutMenu; } // just checking: these are the Outlets from Main.storyboard through XCode

            // Device

            { NSBox x = deviceBox; NSDeviceView y = kaemikaDevice; }  // just checking: these are the Outlets from Main.storyboard through XCode

            // Saved state

            Gui.gui.RestoreInput();

            // Dark Mode Detection

            var interfaceStyle = NSUserDefaults.StandardUserDefaults.StringForKey("AppleInterfaceStyle");
            darkMode = interfaceStyle == "Dark";
            SwitchMode();

            NSDistributedNotificationCenter.GetDefaultCenter().
                AddObserver(this,
                new ObjCRuntime.Selector("themeChanged:"),
                new NSString("AppleInterfaceThemeChangedNotification"),
                null);
        }

        public void SetChartTooltip(string tip, CGPoint at, CGRect within) {
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
            MainClass.form.clickerHandler.ContinueEnable(true);
        }
        public void SetContinueButtonToStart() {
            MainClass.form.clickerHandler.ContinueEnable(false);
        }

        public class NSIntrinsicBox : NSBox {
            CGSize size;
            public NSIntrinsicBox(CGSize size, int border, NSColor fillColor, NSColor borderColor) {
                this.size = new CGSize(size.Width+2*border, size.Height+2*border);
                this.BoxType = NSBoxType.NSBoxCustom;
                this.BorderType = NSBorderType.LineBorder;
                this.BorderWidth = border;
                this.FillColor = fillColor;
                this.BorderColor = borderColor;
            }
            public override CGSize IntrinsicContentSize => this.size;
        }

        private CGSize LegendLineSize(KSeries series) {
            float width = 40;
            float height = (series.lineStyle == KLineStyle.Thick) ? 3 : (series.lineMode == KLineMode.Line) ? 1 : 8;  //############# get the widths from painter
            return new CGSize(width, height);
            }

        private NSIntrinsicBox LegendLine (KSeries series, NSColor background) {
            float R = ((float)series.color.Red) / 255.0f;
            float G = ((float)series.color.Green) / 255.0f;
            float B = ((float)series.color.Blue) / 255.0f;
            float A = ((float)series.color.Alpha) / 255.0f;
            CGColor colorOverWhite = new CGColor(R*A+(1.0f-A), G*A+(1.0f-A), B*A+(1.0f-A), 1.0f);
            return new NSIntrinsicBox(
                LegendLineSize(series), 3,
                NSColor.FromCGColor(colorOverWhite),
                background);
        }

        // #### SaveInput also on change of focus on input text area
        public override void ViewWillDisappear() {
            base.ViewWillDisappear();
            Gui.gui.SaveInput();
        }
 
        public override NSObject RepresentedObject {
            get { return base.RepresentedObject; }
            set { base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        } 
 
    }
}