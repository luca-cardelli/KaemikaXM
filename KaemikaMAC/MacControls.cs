using System;
using System.IO;
using System.Collections.Generic;
using AppKit;
using Foundation;
using Kaemika;
using CoreGraphics;
using SkiaSharp;

namespace KaemikaMAC {

    public class MacControls : GuiControls {

       // User Directory

        public static string modelsDirectory = string.Empty;
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

        // Colors

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

        // Controls

        public static bool disableIBeamCursor = false;
        public static List<MacButton> allMacOnOffButtons = new List<MacButton>();
        public static List<MacFlyoutMenu> allMacFlyoutMenus = new List<MacFlyoutMenu>();

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
        public MacControls() {
            modelsDirectory = Environment.GetFolderPath(defaultUserDataDirectoryPath);
            onOffStop = MenuButton((NSButtonPlus)MacGui.macGui.buttonStop);
            onOffEval = MenuButton((NSButtonPlus)MacGui.macGui.buttonPlay);
            onOffDevice = MenuButton((NSButtonPlus)MacGui.macGui.buttonDevice);
            onOffDeviceView = MenuButton((NSButtonPlus)MacGui.macGui.buttonDeviceView);
            onOffFontSizePlus = MenuButton((NSButtonPlus)MacGui.macGui.buttonFontBigger);
            onOffFontSizeMinus = MenuButton((NSButtonPlus)MacGui.macGui.buttonFontSmaller);
            onOffSave = MenuButton((NSButtonPlus)MacGui.macGui.buttonSave);
            onOffLoad = MenuButton((NSButtonPlus)MacGui.macGui.buttonLoad);
            menuTutorial = new MacFlyoutMenu((NSButtonPlus)MacGui.macGui.buttonTutorial, MacGui.macGui.tutorialFlyoutMenu, MacGui.macGui.tutorialFlyoutBox, MacGui.macGui.leftButtonPanel, FlyoutAttachment.RightTop, 14.0F, false, new CGSize(8,4),
                nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
            menuNoise = new MacFlyoutMenu((NSButtonPlus)MacGui.macGui.buttonNoise, MacGui.macGui.noiseFlyoutMenu, MacGui.macGui.noiseFlyoutBox, MacGui.macGui.rightButtonPanel, FlyoutAttachment.LeftDown, 14.0F, false, new CGSize(1,1),
                nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
            menuOutput = new MacFlyoutMenu((NSButtonPlus)MacGui.macGui.buttonCompute, MacGui.macGui.computeFlyoutMenu, MacGui.macGui.computeFlyoutBox, MacGui.macGui.rightButtonPanel, FlyoutAttachment.LeftDown, 18.0F, false, new CGSize(8,4),
                nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
            menuExport = new MacFlyoutMenu((NSButtonPlus)MacGui.macGui.buttonShare, MacGui.macGui.shareFlyoutMenu, MacGui.macGui.shareFlyoutBox, MacGui.macGui.leftButtonPanel, FlyoutAttachment.RightDown, 18.0F, false, new CGSize(8,4),
                nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
            menuMath = new MacFlyoutMenu((NSButtonPlus)MacGui.macGui.buttonKeyboard, MacGui.macGui.keyboardFlyoutMenu, MacGui.macGui.keyboardFlyoutBox, MacGui.macGui.leftButtonPanel, FlyoutAttachment.RightDown, 20.0F, true, new CGSize(1,1),
                nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
            menuLegend = new MacFlyoutMenu((NSButtonPlus)MacGui.macGui.buttonLegend, MacGui.macGui.legendFlyoutMenu, MacGui.macGui.legendFlyoutBox, MacGui.macGui.rightButtonPanel, FlyoutAttachment.TextOutputRight, 12.0F, true, new CGSize(8,4),
                nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
            menuParameters = new MacFlyoutMenu((NSButtonPlus)MacGui.macGui.buttonParameters, MacGui.macGui.parametersFlyoutMenu, MacGui.macGui.parameterBox, MacGui.macGui.leftButtonPanel, FlyoutAttachment.TextOutputLeft, 12.0F, true, new CGSize(8,4),
                nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
            menuSettings = new MacFlyoutMenu((NSButtonPlus)MacGui.macGui.buttonSettings, MacGui.macGui.settingsFlyoutMenu, MacGui.macGui.settingsFlyoutBox, MacGui.macGui.rightButtonPanel, FlyoutAttachment.LeftUp, 14.0F, false, new CGSize(8,4),
                nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected, nsMenuButtonText, nsMenuButtonDeselected, nsMenuButtonSelected);
        }
        private KButton MenuButton(NSButtonPlus button) {
            return new MacButton(button, nsMenuButtonText, nsMainButtonDeselected, nsMainButtonSelected);
        }
        public bool IsShiftDown() {
            return MacGui.IsShiftDown();
        }
        public bool IsMicrofluidicsVisible() { 
            return !MacGui.macGui.deviceBox.Hidden; 
        }
        public void MicrosfluidicsVisible(bool on) {
            MacGui.macGui.deviceBox.Hidden = !on;
        }
        public void MicrofluidicsOn() {
            MacGui.macGui.deviceBox.Hidden = false;
        }
        public void MicrofluidicsOff() {
            MacGui.macGui.deviceBox.Hidden = true;
        }
        public void IncrementFont(float pointSize) {
            var txtInput = MacGui.macGui.textInput.DocumentView as AppKit.NSTextView;
            SetTextFont((float)(txtInput.Font.FontDescriptor.PointSize + pointSize));
        }
        public void PrivacyPolicyToClipboard() {
            MacGui.ClipboardPasteText("http://lucacardelli.name/Artifacts/Kaemika/KaemikaUWP/privacy_policy.html");
        }
        public void SplashOff() {
            MacGui.macGui.splashImageBacking.Hidden = true;
            MacGui.macGui.splashImage.Hidden = true;
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
                    File.WriteAllText(path, KGui.gui.GuiInputGetText(), System.Text.Encoding.Unicode);
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
                    KGui.gui.GuiInputSetText(File.ReadAllText(path, System.Text.Encoding.Unicode));
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
                SavePreferences();
            }
        }
        public void SavePreferences() {
            try {
                string path2 = CreateKaemikaDataDirectory() + "/modelsdir.txt";
                File.WriteAllText(path2, modelsDirectory);
            } catch { }
            try {
                string path2 = CreateKaemikaDataDirectory() + "/outputaction.txt";
                File.WriteAllText(path2, Exec.currentOutputAction.name);
            } catch (Exception) { }
        }

        public void RestorePreferences() {
            try {
                string path2 = CreateKaemikaDataDirectory() + "/modelsdir.txt";
                if (File.Exists(path2)) { modelsDirectory = File.ReadAllText(path2); }
            } catch { }
            try {
                string path2 = CreateKaemikaDataDirectory() + "/outputaction.txt";
                if (File.Exists(path2)) { KGui.kControls.SetOutputSelection(File.ReadAllText(path2)); }
            } catch (Exception) { }
        }

        public static void SetTextFont(float size) {
            if (size >= 6){
                var txtInput = MacGui.macGui.textInput.DocumentView as AppKit.NSTextView;
                var txtOutput = MacGui.macGui.textOutput.DocumentView as AppKit.NSTextView;
                var newFont = NSFont.FromFontName(txtInput.Font.FontName, size);
                txtInput.Font = newFont;
                txtOutput.Font = newFont;
            }
        }

        public void SetSnapshotSize() {
            MacGui.macGui.SetSnapshotSize();
        }


        // ====  Dark mode callback =====

        public static bool darkMode = false;

        public static void SwitchMode() {
            nsHoverHighlightColor = darkMode ? nsHoverHighlightColorDM : nsHoverHighlightColorLM;
            MacGui.macGui.splashImageBacking.FillColor = darkMode ? NSColor.Black : NSColor.White;
            MacGui.macGui.splashImage.Image = darkMode ? NSImage.ImageNamed("DMSplash.589.DM") : NSImage.ImageNamed("Splash.589");
            foreach (var b in allMacOnOffButtons) b.SwitchMode(darkMode);
            foreach (var b in allMacFlyoutMenus) b.SwitchMode(darkMode);
        }

    }

    // ====== Controls ======

    public class MacControl : KControl {
        public NSControl control;
    }

    // ====  COMMON ONOFF BUTTONS =====


    public class MacButton : MacControl, KButton {
        public NSButtonPlus button;
        private NSColor textColor;
        private NSColor backgroundColor;
        private NSColor selectedColor;
        private bool isSelected;
        private NSImage imageLM;
        private NSImage imageDM;

        private static void SetupButton(NSButtonPlus buttonPlus, NSColor textColor, NSColor backgroundColor) {
            buttonPlus.SetBackgroundColor(backgroundColor);
            buttonPlus.ContentTintColor = textColor;
            buttonPlus.Bordered = false;
        }

        public MacButton (NSButtonPlus button, NSColor nsButtonText, NSColor nsButtonDeselected, NSColor nsButtonSelected) {
            this.control = button;
            this.button = button;
            this.textColor = nsButtonText;
            this.backgroundColor = nsButtonDeselected;
            this.selectedColor = nsButtonSelected;
            this.isSelected = false;
            SetupButton(button, nsButtonText, nsButtonDeselected);
            MacControls.allMacOnOffButtons.Add(this);
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
            this.button.Image = MacControls.darkMode ? imageDM : imageLM;
        }
        private void SetImage(NSImage image) {
                this.button.Image = image;
            }
        public void SetLegendImage(KSeries series) {
            NSImage nsImage;
            int thickness = (series.lineStyle == KLineStyle.Thick) ? 3 : (series.lineMode == KLineMode.Line) ? 1 : 8;
            const int height = 16; const int width = 50; const int padding = 1; const int frame = 3; const int left = 0;
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
            if (this.button == MacGui.macGui.buttonNoise) SetImage(ImageOfNoise(KControls.SelectNoiseSelectedItem, darkMode));
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

    }

    public class MacSlider : MacControl, KSlider {
        public NSSlider trackbar;
        public MacSlider(NSSlider trackbar) {
            this.control = trackbar;
            this.trackbar = trackbar;
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

    public class MacNumerical : MacControl, KNumerical {
        public NSTextField numerical;
        public double lo;
        public double hi;
        public MacNumerical(NSTextField numerical) {
            this.control = numerical;
            this.numerical = numerical;
            this.lo = double.MinValue;
            this.hi = double.MaxValue;
            this.numerical.StringValue = "NaN";
        }
        public void SetBounds(double min, double max) {
            lo = min;
            hi = max;
        }
        public void SetValue(double value) {
            value = Math.Min(value, hi);
            value = Math.Max(value, lo);
            this.numerical.StringValue = value.ToString();
        }
        public double GetValue() {
            try { return double.Parse(this.numerical.StringValue); } catch { return double.NaN; }
        }
        public void OnClick(EventHandler handler) {
            this.numerical.Changed += handler;
        }
    }

    public enum FlyoutAttachment { RightDown, LeftDown, RightUp, LeftUp, RightTop, LeftTop, TextOutputLeft, TextOutputRight };

    public class MacFlyoutMenu : MacButton, KFlyoutMenu {
        private NSBox buttonBar;
        private NSBox menuBox;
        private NSGridView menu;
        private Dictionary<string, KButton> namedControls;
        private FlyoutAttachment attachment;
        public bool autoClose { get; set; }
        private float pointSize;
        private bool fixedWidth;
        private CGSize padding; // menu padding on each of Left,Right (padding.Width) and Top,Bottom (padding.Height)
        private NSColor nsMenuButtonText;
        private NSColor nsMenuButtonDeselected;
        private NSColor nsMenuButtonSelected;
        public KButton selectedItem { get; set; }
        public MacFlyoutMenu(NSButtonPlus button, NSGridView menu, NSBox menuBox, NSBox buttonBar, FlyoutAttachment attachment, float pointSize, bool fixedWidth, CGSize padding,
                                NSColor nsMainButtonText, NSColor nsMainButtonDeselected, NSColor nsMainButtonSelected, NSColor nsMenuButtonText, NSColor nsMenuButtonDeselected, NSColor nsMenuButtonSelected)
                            : base(button, nsMainButtonText, nsMainButtonDeselected, nsMainButtonSelected) {
            this.menu = menu;
            this.namedControls = new Dictionary<string, KButton>();
            this.menu.RowSpacing = 1;
            this.menu.ColumnSpacing = 6;
            this.menuBox = menuBox;
            this.menuBox.BorderWidth = 1; // does not affect the origin
            this.menuBox.CornerRadius = 0;
            this.menuBox.BorderColor = NSColor.TertiaryLabelColor;
            this.buttonBar = buttonBar;
            this.autoClose = false;
            this.pointSize = pointSize;
            this.fixedWidth = fixedWidth;
            this.padding = padding;
            this.attachment = attachment;
            this.nsMenuButtonText = nsMenuButtonText;
            this.nsMenuButtonDeselected = nsMenuButtonDeselected;
            this.nsMenuButtonSelected = nsMenuButtonSelected;
            menuBox.FillColor = nsMenuButtonDeselected;
            menuBox.Hidden = true;
            MacControls.allMacFlyoutMenus.Add(this);
        }
        public void ClearMenuItems() {
            this.namedControls = new Dictionary<string, KButton>();
            this.menu.Subviews = new NSView[]{ }; // we really need to work hard to clean it up:
            while (this.menu.RowCount > 0) for (int i = 0; i < this.menu.RowCount; i++) {this.menu.RemoveRow(i); }
            while (this.menu.ColumnCount > 0) for (int j = 0; j < this.menu.ColumnCount; j++) {this.menu.RemoveColumn(j); }
        }
        public void SetSelection(string name) {
            if (!this.namedControls.ContainsKey(name)) return;
            KButton control = this.namedControls[name];
            KControls.ItemSelected(this, control);
        }
        public void AddMenuItem(KControl item, string name = null) {
            if (name != null && item is KButton asKButton) this.namedControls[name] = asKButton;
            this.menu.AddRow(new NSView[]{ ((MacControl)item).control });
            this.menu.SetFrameOrigin(new CGPoint(0,0)); // w.r.t. the menuBox
        }
        public void AddMenuItems(KControl[] items) {
            NSGridView row = new NSGridView();
            row.ColumnSpacing = 0;
            row.SetContentHuggingPriorityForOrientation(255, NSLayoutConstraintOrientation.Horizontal);
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
            separator.SetFrameSize(new CGSize(2,2));
            separator.FillColor = MacControls.nsMainButtonDeselected;
            separator.BorderColor = MacControls.nsMainButtonDeselected;
            this.menu.AddRow(new NSView[1] { separator });
            separator.LeftAnchor.ConstraintEqualToAnchor(this.menu.LeftAnchor).Active = true;
            separator.RightAnchor.ConstraintEqualToAnchor(this.menu.RightAnchor).Active = true;
            this.menu.SetFrameOrigin(new CGPoint(0,0)); // w.r.t. the menuBox
        }
        public KButton NewMenuSection(int level = 1) {
            var sectionButton = new MacButton(new NSButtonPlus(), MacControls.nsMenuButtonHotText, this.nsMenuButtonDeselected, this.nsMenuButtonSelected);
            sectionButton.SetFont(MacGui.macGui.GetFont(this.pointSize - (level - 1), this.fixedWidth));
            sectionButton.Hover(false); //### should give a hover argument to new NSButtonPlus()
            return sectionButton;
        }
        public KButton NewMenuItemButton(bool multiline = false) {
            // multiline: nothing to worry about in macOS version
            var itemButton = new MacButton(new NSButtonPlus(), this.nsMenuButtonText, this.nsMenuButtonDeselected, this.nsMenuButtonSelected);
            itemButton.SetFont(MacGui.macGui.GetFont(this.pointSize, this.fixedWidth));
            return itemButton;
        }
        public KSlider NewMenuItemTrackBar() {
            var trackBar = new MacSlider(new NSSlider());
            return trackBar;
        }
        public KNumerical NewMenuItemNumerical() {
            return new MacNumerical(new NSTextField());
        }
        public bool IsOpen() {
            return !this.menuBox.Hidden;
        }
        public void Attach() {
            //var grid = this.menuBox.Subviews[1].Subviews[0];
            if (this.attachment == FlyoutAttachment.RightDown) // w.r.t. button
                this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X + this.button.Frame.Width, this.buttonBar.Frame.Y + (this.button.Frame.Y + this.button.Frame.Height) - this.menuBox.Frame.Height));
            else if (this.attachment == FlyoutAttachment.LeftDown) // w.r.t. button
                this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X - this.menuBox.Frame.Width, this.buttonBar.Frame.Y + (this.button.Frame.Y + this.button.Frame.Height) - this.menuBox.Frame.Height));
            else if (this.attachment == FlyoutAttachment.LeftUp) // w.r.t. button
                this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X - this.menuBox.Frame.Width, this.buttonBar.Frame.Y + this.button.Frame.Y));
            else if (this.attachment == FlyoutAttachment.RightUp) // w.r.t. button
                this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X + this.button.Frame.Width, this.buttonBar.Frame.Y + this.button.Frame.Y));
            else if (this.attachment == FlyoutAttachment.RightTop) // w.r.t. window
                this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X + this.button.Frame.Width, this.buttonBar.Frame.Y + this.buttonBar.Frame.Height - this.menuBox.Frame.Height));
            else if (this.attachment == FlyoutAttachment.LeftTop) // w.r.t. window
                this.menuBox.SetFrameOrigin(new CGPoint(this.buttonBar.Frame.X - this.menuBox.Frame.Width, this.buttonBar.Frame.Y + this.buttonBar.Frame.Height - this.menuBox.Frame.Height));
            else if (this.attachment == FlyoutAttachment.TextOutputLeft) { // w.r.t. textOutput
                var txtOut = MacGui.macGui.textOutput;
                this.menuBox.SetFrameOrigin(new CGPoint(0, txtOut.Frame.Height - this.menuBox.Frame.Height));
            } else if (this.attachment == FlyoutAttachment.TextOutputRight) { // w.r.t. textOutput
                var txtOut = MacGui.macGui.textOutput;
                this.menuBox.SetFrameOrigin(new CGPoint(txtOut.Frame.Width - this.menuBox.Frame.Width, txtOut.Frame.Height - this.menuBox.Frame.Height));
            }
            else throw new Error("Attach");
        }
        private void ForceSize() {
            this.menu.SetFrameOrigin(new CGPoint(padding.Width,padding.Height));
            this.menu.SetFrameSize(this.menu.FittingSize);
            this.menuBox.SetFrameSize(new CGSize(this.menu.Frame.Size.Width+2*padding.Width+2*this.menuBox.BorderWidth, this.menu.Frame.Size.Height+2*padding.Height+2*this.menuBox.BorderWidth));
            }
        public void Open() {
            ForceSize();
            ForceSize();
            Attach();
            Selected(true);
            this.menuBox.Hidden = false;
            if (this.autoClose) MacControls.disableIBeamCursor = true;
        }
        public void Close() {
            this.menuBox.Hidden = true;
            Selected(false);
            if (this.autoClose) MacControls.disableIBeamCursor = false;
        }
    }

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
            if (this.Enabled && this.hover && backgroundColorSet) this.Cell.BackgroundColor = MacControls.nsHoverHighlightColor;
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

    // BLOCK MOUSE BUBBLE: https://stackoverflow.com/questions/42482434/block-mouse-bubble-in-xamarin-mac
    // BUTTON HOVER HIGHLIGHT: https://stackoverflow.com/questions/6094763/simple-mouseover-effect-on-nsbutton

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
            if (MacControls.disableIBeamCursor || alwaysDisableIBeamCursor) NSCursor.ArrowCursor.Set(); else NSCursor.IBeamCursor.Set();
        }
        public override void MouseDown(NSEvent theEvent) {
            base.MouseDown(theEvent);
            KGui.kControls.CloseOpenMenu();
        }
    }

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

}
