using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using SkiaSharp;
using SkiaSharp.Views.Desktop; // for Extensions.ToBitmap method
using Kaemika;
using KaemikaAssets;

namespace KaemikaWPF {
    // This all runs in the gui thread: external-thread calls should be made through GUI_Interface.

    public partial class GuiToWin : Form {

        public static Font textFont;        // Fixed size fonts for textInput and textOutput
        public static Font kaemikaFont;     // Just to remember where it came from
        //public static string fontFamily = "Lucida Sans Unicode";
        public static string fontFamily = "Calibri";       // A ClearType font
        public static string fontFixedFamily = "Consolas"; // A ClearType font
        //public static string fontFixedFamily = "Courier New";
        //public static string fontFixedFamily = "Lucida Sans Typewriter"; // makes the unicode math symbols too small

        // Fonts for menus and buttons set in code. Menus and buttons set in Gui editor also use Lucida Sans Unicode.
        private static Dictionary<float, Font> fonts;
        private static Dictionary<float, Font> fontsFixed;
        public static Font GetFont(float pointSize, bool fixedWidth) {
            if (fixedWidth) {
                if (!fontsFixed.ContainsKey(pointSize)) fontsFixed[pointSize] = new Font(fontFixedFamily, pointSize, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                return fontsFixed[pointSize];
            } else {
                if (!fonts.ContainsKey(pointSize)) fonts[pointSize] = new Font(fontFamily, pointSize, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                return fonts[pointSize];
            }
        }

        public static Color cMainButtonDeselected = Color.FromArgb(255, 0, 122, 204);
        public static Color cMainButtonSelected = Color.FromArgb(255, 104, 33, 122);
        public static Color cMainButtonText = Color.White;

        public static Color cMenuButtonDeselected = cMainButtonSelected;
        public static Color cMenuButtonSelected = Color.FromArgb(255, 148, 46, 175);
        public static Color cMenuButtonText = cMainButtonText;
        public static Color cMenuButtonHotText = Color.HotPink;

        public static Color cPanelButtonDeselected = Color.FromArgb(255, 250, 232, 255);
        public static Color cPanelButtonSelected = Color.FromArgb(255, 255, 190, 239);
        public static Color cPanelButtonText = Color.Black;

        //public static Color lighterBlue = Color.FromArgb(255, 51, 153, 255);
        //public static Color palePurple = Color.FromArgb(255, 251, 239, 255);
        //public static Color buttonGrey = Color.FromArgb(255, 127, 127, 127);

        // ====  COMMON ONOFF BUTTONS =====

        private static void SetupButton(Button button, Color textColor, Color backgroundColor) {
            button.BackColor = backgroundColor;
            button.ForeColor = textColor;
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.UseVisualStyleBackColor = false;
            button.Padding = new Padding(0);
            button.Margin = new Padding(0);
        }

        public class WinControl : KControl {
            public Control control;
        }

        public class WinButton : WinControl, KButton {
            public Button button;
            private Color textColor;
            private Color backgroundColor;
            private Color selectedColor;
            private Color saveMouseOverBackColor;
            private Color saveMouseDownBackColor;
            public WinButton (Button button, Color textColor, Color backgroundColor, Color selectedColor) {
                this.control = button;
                this.button = button;
                this.textColor = textColor;
                this.backgroundColor = backgroundColor;
                this.selectedColor = selectedColor;
                SetupButton(button, textColor, backgroundColor);
                this.saveMouseOverBackColor = button.FlatAppearance.MouseOverBackColor;
                this.saveMouseDownBackColor = button.FlatAppearance.MouseDownBackColor;
            }
            public void SetImage(string imageName) {
                Bitmap image = null;
                if (imageName == "icons8stop40") image = Properties.Resources.icons8stop40;
                if (imageName == "icons8play40") image = Properties.Resources.icons8play40;
                if (imageName == "Noise_None_W_48x48") image = Properties.Resources.Noise_None_W_48x48;
                if (imageName == "Noise_SigmaRange_W_48x48") image = Properties.Resources.Noise_SigmaRange_W_48x48;
                if (imageName == "Noise_Sigma_W_48x48") image = Properties.Resources.Noise_Sigma_W_48x48;
                if (imageName == "Noise_CV_W_48x48") image = Properties.Resources.Noise_CV_W_48x48;
                if (imageName == "Noise_SigmaSqRange_W_48x48") image = Properties.Resources.Noise_SigmaSqRange_W_48x48;
                if (imageName == "Noise_SigmaSq_W_48x48") image = Properties.Resources.Noise_SigmaSq_W_48x48;
                if (imageName == "Noise_Fano_W_48x48") image = Properties.Resources.Noise_Fano_W_48x48;
                if (imageName == "Computation_48x48") image = Properties.Resources.Computation_48x48;
                if (imageName == "icons8device_OFF_48x48") image = Properties.Resources.icons8device_OFF_48x48;
                if (imageName == "icons8device_ON_48x48") image = Properties.Resources.icons8device_ON_48x48;
                if (imageName == "deviceBorder_W_48x48") image = Properties.Resources.deviceBorder_W_48x48;
                if (imageName == "FontSizePlus_W_48x48") image = Properties.Resources.FontSizePlus_W_48x48;
                if (imageName == "FontSizeMinus_W_48x48") image = Properties.Resources.FontSizeMinus_W_48x48;
                if (imageName == "FileSave_48x48") image = Properties.Resources.FileSave_48x48;
                if (imageName == "FileLoad_48x48") image = Properties.Resources.FileLoad_48x48;
                if (imageName == "icons8pauseplay40") image = Properties.Resources.icons8pauseplay40;
                if (imageName == "icons8combochart96_W_48x48") image = Properties.Resources.icons8combochart96_W_48x48;
                if (imageName == "icons8_share_384_W_48x48") image = Properties.Resources.icons8_share_384_W_48x48;
                if (imageName == "icons8_keyboard_96_W_48x48") image = Properties.Resources.icons8_keyboard_96_W_48x48;
                if (imageName == "icons8_settings_384_W_48x48") image = Properties.Resources.icons8_settings_384_W_48x48;
                if (imageName == "icons8text_48x48") image = Properties.Resources.icons8text_48x48;
                if (imageName == "Parameters_W_48x48") image = Properties.Resources.Parameters_W_48x48;
                if (image == null) throw new Error("SetImage");
                this.button.Image = image;
            }
            public void SetLegendImage(KSeries series) {
                Image sdImage;
                int thickness = (series.lineStyle == KLineStyle.Thick) ? 3 : (series.lineMode == KLineMode.Line) ? 1 : 8;
                const int height = 18; const int width = 50; const int padding = 1; const int frame = 3; const int left = 0;
                int framedH = thickness + 2 * frame; int framedY = (height - 2 * padding - framedH) / 2;
                int framedW = width - 2 * padding; int framedX = left + padding;
                using (SKBitmap skBitmap = new SKBitmap(left+width,height)) 
                using (SKCanvas skCanvas = new SKCanvas(skBitmap))
                using (SKPaint back = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(backgroundColor.R, backgroundColor.G, backgroundColor.B, backgroundColor.A)})
                using (SKPaint white = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(255, 255, 255, 255) })
                using (SKPaint color = new SKPaint { Style = SKPaintStyle.Fill, Color = series.color }) {
                    skCanvas.DrawRect(new SKRect(0, 0, left + width, height), back);
                    if (series.visible) {
                        skCanvas.DrawRect(new SKRect(framedX,framedY,framedX+framedW,framedY+framedH), white);
                        skCanvas.DrawRect(new SKRect(framedX+frame,framedY+frame,framedX+framedW-frame,framedY+frame+thickness), color);
                    }
                    sdImage = skBitmap.ToBitmap();
                }
                this.button.Image = sdImage;
            }
            public string GetText() {
                return this.button.Text;
            }
            public void SetText(string text) {
                this.button.Text = text;
            }
            public void SetFont(Font font) {
                this.button.Font = font;
            }
            public bool IsVisible() {
                return this.button.Visible;
            }
            public void Visible(bool b) {
                this.button.Visible = b;
            }
            public bool IsEnabled() {
                return this.button.Enabled;
            }
            public void Enabled(bool b) {
                if (b) {
                    this.button.Enabled = true;
                    this.button.BackColor = backgroundColor;
                } else {
                    this.button.Enabled = false;
                    this.button.BackColor = backgroundColor;
                }
            }
            public bool IsSelected() {
                return this.button.BackColor == selectedColor;
            }
            public void Selected(bool b) {
                if (b) this.button.BackColor = selectedColor;
                else this.button.BackColor = backgroundColor;
            }
            public void Hover(bool b) {
                if (b == false) { // prevent hover
                    this.saveMouseOverBackColor = this.button.FlatAppearance.MouseOverBackColor;
                    this.saveMouseDownBackColor = this.button.FlatAppearance.MouseDownBackColor;
                    this.button.FlatAppearance.MouseOverBackColor = this.button.BackColor;
                    this.button.FlatAppearance.MouseDownBackColor = this.button.BackColor;
                } else {
                    this.button.FlatAppearance.MouseOverBackColor = this.saveMouseOverBackColor;
                    this.button.FlatAppearance.MouseDownBackColor = this.saveMouseDownBackColor;
                }
            }
            public void OnClick(EventHandler handler) {
                this.button.Click += handler;
            }
        }

        public class WinSlider : WinControl, KSlider {
            public TrackBar trackbar;
            Color cMenuButtonText;
            Color cMenuButtonDeselected;
            Color cMenuButtonSelected;
            public WinSlider(TrackBar trackbar, Color cMenuButtonText, Color cMenuButtonDeselected, Color cMenuButtonSelected) {
                this.control = trackbar;
                this.trackbar = trackbar;
                this.trackbar.TickStyle = TickStyle.None;
                this.cMenuButtonText = cMenuButtonText;
                this.cMenuButtonDeselected = cMenuButtonDeselected;
                this.cMenuButtonSelected = cMenuButtonSelected;
            }
            public void SetBounds(int min, int max) {
                this.trackbar.Minimum = min;
                this.trackbar.Maximum = max;
            }
            public void SetValue(int value) {
                this.trackbar.Value = value;
            }
            public int GetValue() {
                return this.trackbar.Value;
            }
            public void OnClick(EventHandler handler) {
                this.trackbar.ValueChanged += handler;
            }
        }

        public enum FlyoutAttachment { RightDown, LeftDown, RightUp, LeftUp, RightTop, LeftTop, TextOutputLeft, TextOutputRight };

        public class WinFlyoutMenu : WinButton, KFlyoutMenu {
            private Panel buttonBar;
            private FlowLayoutPanel menu;
            private FlyoutAttachment attachment;
            public bool autoClose { get; set; }
            private float pointSize;
            private bool fixedWidth;
            private Color cMenuButtonText;
            private Color cMenuButtonDeselected;
            private Color cMenuButtonSelected;
            public KButton selectedItem { get; set; }
            public WinFlyoutMenu(Button button, FlowLayoutPanel menu, Panel buttonBar, FlyoutAttachment attachment, float pointSize, bool fixedWidth, 
                        Color cMainButtonText, Color cMainButtonDeselected, Color cMainButtonSelected, Color cMenuButtonText, Color cMenuButtonDeselected, Color cMenuButtonSelected) 
                        : base(button, cMainButtonText, cMainButtonDeselected, cMainButtonSelected) {
                this.menu = menu;
                this.buttonBar = buttonBar;
                this.autoClose = false;
                this.attachment = attachment;
                this.pointSize = pointSize;
                this.fixedWidth = fixedWidth;
                this.cMenuButtonText = cMenuButtonText;
                this.cMenuButtonDeselected = cMenuButtonDeselected;
                this.cMenuButtonSelected = cMenuButtonSelected;
                menu.BackColor = cMenuButtonDeselected;
                menu.AutoSize = true;
                menu.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                menu.FlowDirection = FlowDirection.TopDown;
                menu.AutoScroll = false; // or it will flow horizontally and add a scrollbar
                menu.Visible = false;
                menu.BringToFront();
            }
            public void ClearMenuItems() {
                this.menu.Controls.Clear();
            }
            public void AddMenuItem(KControl item) {
                this.menu.Controls.Add(((WinControl)item).control);
            }
            public void AddMenuItems(KControl[] items) {
                AddMenuRow(items);
            }
            public void AddMenuRow(KControl[] items) {
                FlowLayoutPanel rowItems = new FlowLayoutPanel();
                rowItems.FlowDirection = FlowDirection.LeftToRight;
                rowItems.AutoSize = true;
                rowItems.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                rowItems.BorderStyle = BorderStyle.None;
                rowItems.Padding = new Padding(0, 0, 0, 0);
                rowItems.Margin = new Padding(0, 0, 0, 0);
                for (int i = 0; i < items.Length; i++) 
                    rowItems.Controls.Add(((WinControl)items[i]).control);
                this.menu.Controls.Add(rowItems);
            }
            public void AddMenuGrid(KControl[,] items) {
                this.menu.AutoSize = false; // this makes them align in columns
                this.menu.AutoScroll = false; // otherwise it reserves space of a hor scrollbar
                foreach (KControl item in items) {
                    Control control = ((WinControl)item).control;
                    this.menu.Controls.Add(control);
                }
            }
            public void AddSeparator() {
                var label = new Label();
                label.AutoSize = false;
                label.Text = "";
                label.BorderStyle = BorderStyle.Fixed3D; // or FixedSingle then can change BackgroundColor
                label.Height = 2;
                label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                this.menu.Controls.Add(label);
            }
            public KButton NewMenuSection(int level = 1) {
                var sectionButton = new WinButton(new Button(), cMenuButtonHotText, this.cMenuButtonDeselected, this.cMenuButtonSelected);
                sectionButton.SetFont(GetFont(this.pointSize - (level - 1), this.fixedWidth));
                sectionButton.Hover(false);
                return sectionButton;
            }
            public KButton NewMenuItemButton() {
                var itemButton = new WinButton(new Button(), this.cMenuButtonText, this.cMenuButtonDeselected, this.cMenuButtonSelected);
                itemButton.SetFont(GetFont(this.pointSize, this.fixedWidth));
                return itemButton;
            }
            public KSlider NewMenuItemTrackBar() {
                var trackBar = new WinSlider(new TrackBar(), this.cMenuButtonText, this.cMenuButtonDeselected, this.cMenuButtonSelected);
                return trackBar;
            }
            public bool IsOpen() {
                return this.menu.Visible;
            }
            private void Attach() {
                if (this.attachment == FlyoutAttachment.RightDown) this.menu.Location = new Point(this.buttonBar.Location.X + this.buttonBar.Size.Width, this.buttonBar.Location.Y + this.button.Location.Y);
                else if (this.attachment == FlyoutAttachment.LeftDown) this.menu.Location = new Point(this.buttonBar.Location.X - this.menu.Size.Width, this.buttonBar.Location.Y + this.button.Location.Y);
                else if (this.attachment == FlyoutAttachment.RightUp) this.menu.Location = new Point(this.buttonBar.Location.X + this.buttonBar.Size.Width, this.buttonBar.Location.Y + this.button.Location.Y + this.button.Size.Height - this.menu.Size.Height);
                else if (this.attachment == FlyoutAttachment.LeftUp) this.menu.Location = new Point(this.buttonBar.Location.X - this.menu.Size.Width, this.buttonBar.Location.Y + this.button.Location.Y + this.button.Size.Height - this.menu.Size.Height);
                else if (this.attachment == FlyoutAttachment.RightTop) this.menu.Location = new Point(this.buttonBar.Location.X + this.buttonBar.Size.Width, this.buttonBar.Location.Y);
                else if (this.attachment == FlyoutAttachment.LeftTop) this.menu.Location = new Point(this.buttonBar.Location.X - this.menu.Size.Width, this.buttonBar.Location.Y);
                else if (this.attachment == FlyoutAttachment.TextOutputLeft) this.menu.Location = new Point(0, 0);
                else if (this.attachment == FlyoutAttachment.TextOutputRight) this.menu.Location = new Point(App.fromGui.txtTarget.Size.Width - this.menu.Width, 0);
            }
            public void Open() {
                Attach();
                Selected(true);
                this.menu.Visible = true;
                this.menu.BringToFront();
            }
            public void Close() {
                this.menu.Visible = false;
                Selected(false);
            }
        }

        public static string modelsDirectory = string.Empty;

        public class WinClicker : FromGui {
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
            public WinClicker() {
                modelsDirectory = Environment.GetFolderPath(defaultUserDataDirectoryPath);
                RestoreDirectories();
                onOffStop = MenuButton(App.fromGui.btnStop);
                onOffEval = MenuButton(App.fromGui.btnEval);
                onOffDevice = MenuButton(App.fromGui.button_Device);
                onOffDeviceView = MenuButton(App.fromGui.button_FlipMicrofluidics);
                onOffFontSizePlus = MenuButton(App.fromGui.button_FontSizePlus);
                onOffFontSizeMinus = MenuButton(App.fromGui.button_FontSizeMinus);
                onOffSave = MenuButton(App.fromGui.button_Source_Copy);
                onOffLoad = MenuButton(App.fromGui.button_Source_Paste);
                menuTutorial = new WinFlyoutMenu(App.fromGui.button_Tutorial, App.fromGui.flowLayoutPanel_Tutorial, App.fromGui.panel1,FlyoutAttachment.RightTop, 9.5F, false, //8.0F for Lucida Sans Serif
                    cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
                menuNoise = new WinFlyoutMenu(App.fromGui.button_Noise, App.fromGui.flowLayoutPanel_Noise, App.fromGui.panel2, FlyoutAttachment.LeftDown, 10.0F, false,
                    cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
                menuOutput = new WinFlyoutMenu(App.fromGui.button_Output, App.fromGui.flowLayoutPanel_Output, App.fromGui.panel2, FlyoutAttachment.LeftDown, 12.0F, false,
                    cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
                menuExport = new WinFlyoutMenu(App.fromGui.button_Export, App.fromGui.flowLayoutPanel_Export, App.fromGui.panel1, FlyoutAttachment.RightDown, 11.0F, false,
                    cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
                menuMath = new WinFlyoutMenu(App.fromGui.button_Math, App.fromGui.flowLayoutPanel_Math, App.fromGui.panel1, FlyoutAttachment.RightDown, 12.0F, false,
                    cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
                menuLegend = new WinFlyoutMenu(App.fromGui.button_EditChart, App.fromGui.flowLayoutPanel_Legend, App.fromGui.panel2, FlyoutAttachment.TextOutputRight, 9.0F, false,
                    cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cPanelButtonText, cPanelButtonDeselected, cPanelButtonSelected);
                menuParameters = new WinFlyoutMenu(App.fromGui.button_Parameters, App.fromGui.flowLayoutPanel_Parameters, App.fromGui.panel1, FlyoutAttachment.TextOutputLeft, 9.0F, false,
                    cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cPanelButtonText, cPanelButtonDeselected, cPanelButtonSelected);
                menuSettings = new WinFlyoutMenu(App.fromGui.button_Settings, App.fromGui.flowLayoutPanel_Settings, App.fromGui.panel2, FlyoutAttachment.LeftUp, 12.0F, false,
                    cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
            }
            private KButton MenuButton(Button button) {
                return new WinButton(button, cMainButtonText, cMainButtonDeselected, cMainButtonSelected);
            }
            public bool IsMicrofluidicsVisible() { 
                return App.fromGui.panel_Microfluidics.Visible; 
            }
            public void MicrosfluidicsVisible(bool on) {
                App.fromGui.panel_Microfluidics.Visible = on;
            }
            public void MicrofluidicsOn() {
                deviceControl.Size = App.fromGui.panel_Microfluidics.Size;
                App.fromGui.panel_Microfluidics.BringToFront();
                App.fromGui.panel_Microfluidics.Visible = true;
            }
            public void MicrofluidicsOff() {
                App.fromGui.panel_Microfluidics.Visible = false;
            }
            public void IncrementFont(float pointSize) {
                SetFont(App.fromGui.richTextBox.Font.Size + pointSize, true);
            }

            public void SplashOff() {
                App.fromGui.panel_Splash.Visible = false;
            }
            public void Save() {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog()) {
                    saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                    saveFileDialog.InitialDirectory = modelsDirectory;
                    saveFileDialog.FilterIndex = 1;
                    saveFileDialog.RestoreDirectory = false;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                        try {
                            File.WriteAllText(saveFileDialog.FileName, Gui.gui.InputGetText(), System.Text.Encoding.Unicode);
                        } catch {
                            MessageBox.Show(saveFileDialog.FileName, "Could not write this file:", MessageBoxButtons.OK);
                        }
                    }
                }
            }
            public void Load() {
                using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
                    openFileDialog.InitialDirectory = modelsDirectory;
                    openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = false;
                    if (openFileDialog.ShowDialog() == DialogResult.OK) {
                        try {
                            Gui.gui.InputSetText(File.ReadAllText(openFileDialog.FileName, System.Text.Encoding.Unicode));
                        } catch {
                            MessageBox.Show(openFileDialog.FileName, "Could not read this file:", MessageBoxButtons.OK);
                        }
                    }
                }
            }
            public void SetDirectory() {
                var initialDirectory = modelsDirectory;
                using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                    var folderPath = string.Empty;
                    dialog.SelectedPath = initialDirectory;
                    if (dialog.ShowDialog() == DialogResult.OK) folderPath = dialog.SelectedPath;
                    if (folderPath != string.Empty)  {
                        modelsDirectory = folderPath;
                        SaveDirectories();
                        MessageBox.Show(folderPath, "Directory set to:", MessageBoxButtons.OK);
                    }
                }
            }
            public void SaveDirectories() {
                try {
                    string path2 = CreateKaemikaDataDirectory() + "\\modelsdir.txt";
                    File.WriteAllText(path2, modelsDirectory);
                } catch (Exception) { }
            }
            public void RestoreDirectories() {
                try {
                    string path2 = CreateKaemikaDataDirectory() + "\\modelsdir.txt";
                    if (File.Exists(path2)) { modelsDirectory = File.ReadAllText(path2); }
                } catch (Exception) { }
            }
        }

       // ====  ONOFF BUTTONS =====

        public class OnOffButton {
            private Button button;
            private Color normal;
            private Color selected;
            private EventHandler handler;
            public OnOffButton(Button button, Color normal, Color selected, Bitmap image, EventHandler handler) {
                this.button = button;
                this.button.Image = image;
                this.normal = normal;
                this.selected = selected;
                this.handler = handler;
                this.button.Click += OnClick;
            }
            public void SetImage(Bitmap image) {
                this.button.Image = image;
            }
            public void Visible(bool b) {
                this.button.Visible = b;
            }
            public void Enabled() {
                this.button.Enabled = true;
                this.button.BackColor = normal;
            }
            public void Disabled() {
                this.button.Enabled = false;
                this.button.BackColor = normal;
            }
            public void Selected() {
                this.button.BackColor = selected;
            }
            public void Deselected() {
                this.button.BackColor = normal;
            }
            private void OnClick(object sender, EventArgs e) {
                this.handler(sender, e);
            }
        }

        // ====  FLYOUT MENUS =====

        //public delegate void Handler<T>(ButtonPlusT<T> selectedButton);  // callback for flyout-menu selections with user data
        ////// Use NoSelection to do nothing on selection and pop down the menu
        ////// Use null to do nothing on selection and keep the menu up
        //public void NoSelection<T>(FlyoutMenu flyoutMenu, ButtonPlusT<Noise> button) { }

        //public class ButtonPlus : Button {     // subclass of Button with HandleData method to handle user data via callback
        //    public ButtonPlus() : base() { }   // an indirection for ButtonPlus<T> for any T, so we can cast then inside selectionHandler
        //    public virtual FlyoutMenu FlyoutMenu() { return null; }
        //    public virtual bool NullHandler() { return true; }
        //    public virtual bool CloseOnSelection() { return true; }
        //    public virtual void HandleData() { }
        //    public void SetSelected() { this.FlyoutMenu().SetSelected(this); }
        //}
        //public class ButtonPlusT<T> : ButtonPlus {  // subclass of Button with HandleData method to handle T user data via T callback
        //    public FlyoutMenu flyoutMenu;
        //    public T data;
        //    private bool closeOnSelection;
        //    private Handler<T> handler;
        //    public ButtonPlusT(FlyoutMenu menu, T data, bool closeOnSelection, Handler<T> handler) : base() {
        //        this.flyoutMenu = menu;
        //        this.data = data;
        //        this.handler = handler;
        //        this.closeOnSelection = closeOnSelection;
        //    }
        //    public override FlyoutMenu FlyoutMenu() { return this.flyoutMenu; }
        //    public override bool NullHandler() { return this.handler == null; }
        //    public override bool CloseOnSelection() { return this.closeOnSelection; }
        //    public override void HandleData() { this.handler(this); }
        //}


        //private static FlyoutMenu currentlyUp = null;
        //public static bool IsOpenFlyoutMenu(FlyoutMenu menu) { return menu == currentlyUp; }
        //public static void CloseFlyoutMenu() { if (currentlyUp != null) currentlyUp.Close(); currentlyUp = null; }
        //public static void OpenFlyoutMenu(FlyoutMenu menu) { CloseFlyoutMenu(); currentlyUp = menu; currentlyUp.Open(); }

        //public class FlyoutMenu {
        //    private Panel buttonBar;
        //    private Button button;
        //    private Color normal;
        //    private Color highlight;
        //    private Color highlightSelected;
        //    private FlowLayoutPanel menu;
        //    private bool inAutoCloseGroup;
        //    private FlyoutAttachment menuAttachment;
        //    private ButtonPlus selected;
        //    public FlyoutMenu(Panel buttonBar,
        //            Button menuButton, Color normal, Color highlight, Color highlightSelected, Bitmap image,
        //            FlowLayoutPanel menu, bool autoSizeMenu, bool inAutoCloseGroup, FlyoutAttachment menuAttachment) {
        //        this.buttonBar = buttonBar;
        //        this.buttonBar.BackColor = normal;
        //        this.button = menuButton;
        //        this.button.Image = image;
        //        this.button.Enabled = true;
        //        this.button.BackColor = normal;
        //        this.button.Click += this.button_Click;
        //        this.normal = normal;
        //        this.highlight = highlight;
        //        this.highlightSelected = highlightSelected;
        //        this.menu = menu;
        //        this.inAutoCloseGroup = inAutoCloseGroup;
        //        this.menuAttachment = menuAttachment;
        //        this.menu.BackColor = highlight;
        //        this.menu.FlowDirection = FlowDirection.TopDown;
        //        this.menu.AutoSize = autoSizeMenu;
        //        if (autoSizeMenu) this.menu.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        //        this.menu.Visible = false;
        //        this.menu.BringToFront();
        //        this.selected = null;
        //    }

        //    public void SetImage(Bitmap image) {
        //        this.button.Image = image;
        //    }

        //    public void AddMenuItem(Control item) {
        //        this.menu.Controls.Add(item);
        //    }

        //    public void ClearMenuItems<T>() {
        //        this.menu.Controls.Clear();
        //    }

        //    public void SetMenuSize(Size size) {
        //        this.menu.Size = size;
        //    }

        //    public void SetSelected(int i) {
        //        SetSelected((ButtonPlus)menu.Controls[i]);
        //    }
        //    public void SetSelected(ButtonPlus button) {
        //        if (selected != null) selected.BackColor = highlight;
        //        selected = button;
        //        selected.BackColor = highlightSelected;
        //    }

        //    public void Open() {
        //        if (this.menuAttachment == FlyoutAttachment.RightDown) this.menu.Location = new Point(this.buttonBar.Location.X + this.buttonBar.Size.Width, this.buttonBar.Location.Y + this.button.Location.Y);
        //        else if (this.menuAttachment == FlyoutAttachment.LeftDown) this.menu.Location = new Point(this.buttonBar.Location.X - this.menu.Size.Width, this.buttonBar.Location.Y + this.button.Location.Y);
        //        else if (this.menuAttachment == FlyoutAttachment.RightUp) this.menu.Location = new Point(this.buttonBar.Location.X + this.buttonBar.Size.Width, this.buttonBar.Location.Y + this.button.Location.Y + this.button.Size.Height - this.menu.Size.Height);
        //        else if (this.menuAttachment == FlyoutAttachment.LeftUp) this.menu.Location = new Point(this.buttonBar.Location.X - this.menu.Size.Width, this.buttonBar.Location.Y + this.button.Location.Y + this.button.Size.Height - this.menu.Size.Height);
        //        else if (this.menuAttachment == FlyoutAttachment.RightTop) this.menu.Location = new Point(this.buttonBar.Location.X + this.buttonBar.Size.Width, this.buttonBar.Location.Y);
        //        else if (this.menuAttachment == FlyoutAttachment.LeftTop) this.menu.Location = new Point(this.buttonBar.Location.X - this.menu.Size.Width, this.buttonBar.Location.Y);
        //        else if (this.menuAttachment == FlyoutAttachment.TextOutput) {
        //            var txtOut = App.form.txtTarget;
        //            this.menu.Location = new Point(txtOut.Size.Width - App.form.flowLayoutPanel_Legend.Width, 0);
        //        }
        //        Selected();
        //        this.menu.BringToFront();
        //        this.menu.Visible = true;
        //    }

        //    public void Close() {
        //        this.menu.Visible = false;
        //        Deselected();
        //    }

        //    public void Visible(bool b) {
        //        if (b) {
        //            this.button.Visible = true;
        //        } else {
        //            this.button.Visible = false;
        //            this.Close();
        //        }
        //    }

        //    public void Enabled() {
        //        this.button.Enabled = true;
        //        this.button.BackColor = normal;
        //    }
        //    public void Disabled() {
        //        this.button.Enabled = false;
        //        this.Close();
        //    }
        //    public void Selected() {
        //        this.button.BackColor = highlight;
        //    }
        //    public void Deselected() {
        //        this.button.BackColor = normal;
        //    }

        //    private void button_Click(object sender, EventArgs e) {
        //        if (this.inAutoCloseGroup) {
        //            if (IsOpenFlyoutMenu(this)) CloseFlyoutMenu(); else OpenFlyoutMenu(this);
        //        } else {
        //            if (!this.menu.Visible) Open(); else Close();
        //        }
        //    }

        //    private void selectionHandler(object sender, EventArgs e) {
        //        ButtonPlus buttonHandler = (ButtonPlus)sender;
        //        if (buttonHandler.NullHandler()) return; // don't even close the menu
        //        // close the menu:
        //        if (buttonHandler.CloseOnSelection())
        //            CloseFlyoutMenu(); //buttonHandler.FlyoutMenu().Close();
        //        // client callback:
        //        buttonHandler.HandleData();
        //    }

        //    public ButtonPlusT<T> PlainButton<T>(string text, Color backColor, Color textColor, Font font, FlyoutMenu flyoutMenu, T data, bool closeOnSelection, Handler<T> onSelection) {
        //        ButtonPlusT<T> button = new ButtonPlusT<T>(flyoutMenu, data, closeOnSelection, onSelection);
        //        button.BackColor = backColor;
        //        button.ForeColor = textColor;
        //        button.AutoSize = true;
        //        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        //        button.FlatStyle = FlatStyle.Flat;
        //        button.FlatAppearance.BorderSize = 0;
        //        button.Margin = new Padding(0);
        //        button.Name = text; // used by onClick
        //        button.Text = text;
        //        button.Font = font;
        //        button.UseVisualStyleBackColor = false;
        //        button.Click += selectionHandler;
        //        return button;
        //    }
        //    public ButtonPlusT<T> ImageButton<T>(string text, Image image, FlyoutMenu flyoutMenu, T data, bool closeOnSelection, Handler<T> onSelection) {
        //        ButtonPlusT<T> button = new ButtonPlusT<T>(flyoutMenu, data, closeOnSelection, onSelection);
        //        button.FlatStyle = FlatStyle.Flat;
        //        button.FlatAppearance.BorderSize = 0;
        //        button.Margin = new Padding(0);
        //        button.AutoSize = false;
        //        button.Size = new Size(image.Size.Width + 2, image.Size.Height + 2); // somehow we need to enlarge the button to show the whole image
        //        button.Image = image;
        //        button.Name = text; // used by onClick
        //        button.Text = "";
        //        button.UseVisualStyleBackColor = false;
        //        button.Click += selectionHandler;
        //        return button;
        //    }

        //}


        // ====== Device Control ======

        public class DeviceSKControl : SkiaSharp.Views.Desktop.SKControl {

            // Implement this to draw on the canvas.
            protected override void OnPaintSurface(SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e) {
                // call the base method
                base.OnPaintSurface(e);

                var surface = e.Surface;
                var canvas = surface.Canvas;
                int canvasWidth = e.Info.Width;
                int canvasHeight = e.Info.Height;
                int canvasX = Location.X;
                int canvasY = Location.Y;

                // draw on the canvas

                ProtocolDevice.Draw(new SKDevicePainter(canvas), canvasX, canvasY, canvasWidth, canvasHeight);
            }

            protected override void OnMouseClick(MouseEventArgs e) {
                base.OnMouseClick(e);
                App.fromGui.clickerHandler.CloseOpenMenu();
            }
        }

        // ====== KChart Control ======

        public class KChartSKControl : SkiaSharp.Views.Desktop.SKControl {

            public KChartSKControl() : base() {
                Label toolTip = App.fromGui.label_Tooltip;
                toolTip.Visible = false;
                toolTip.BackColor = cPanelButtonDeselected;
                toolTip.Font = GetFont(8, true);
                toolTip.MouseEnter +=
                    (object sender, EventArgs e) => { UpdateTooltip(new Point(0, 0), ""); };
            }

            // Implement this to draw on the canvas.
            protected override void OnPaintSurface(SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e) {
                // call the base method
                base.OnPaintSurface(e);

                var surface = e.Surface;
                var canvas = surface.Canvas;
                int canvasWidth = e.Info.Width;
                int canvasHeight = e.Info.Height;
                int canvasX = Location.X;
                int canvasY = Location.Y;

                // draw on the canvas

                KChartHandler.Draw(new SKChartPainter(canvas), canvasX, canvasY, canvasWidth, canvasHeight);
            }

            // See also GuiToWin_KeyDown, GuiToWin_KeyUp
            private DateTime lastTooltipUpdate = DateTime.MinValue;

            protected override void OnMouseMove(MouseEventArgs e) {
                base.OnMouseMove(e);
                if (DateTime.Now.Subtract(lastTooltipUpdate).TotalSeconds > 0.01) {
                    KChartHandler.ShowEndNames(!shiftKeyDown);
                    UpdateTooltip(new Point(e.X, e.Y), KChartHandler.HitListTooltip(new SKPoint(e.X, e.Y), 10));
                    lastTooltipUpdate = DateTime.Now;
                    Invalidate();
                }
            }
            protected override void OnMouseEnter(EventArgs e) {
                base.OnMouseEnter(e);
                mouseInsideChartControl = true;
                KChartHandler.ShowEndNames(true);
                Invalidate();
            }
            protected override void OnMouseLeave(EventArgs e) {
                base.OnMouseLeave(e);
                mouseInsideChartControl = false;
                UpdateTooltip(new Point(0, 0), "");
                KChartHandler.ShowEndNames(false);
                Invalidate();
            }
            protected override void OnMouseClick(MouseEventArgs e) {
                base.OnMouseClick(e);
                App.fromGui.clickerHandler.CloseOpenMenu();
            }

            private void UpdateTooltip(Point point, string tip) {
                int off = 6;
                int pointerWidth = 16;
                Label toolTip = App.fromGui.label_Tooltip;
                Panel chart = App.fromGui.panel_KChart;
                if (tip == "") {
                    toolTip.Text = "";
                    toolTip.Visible = false;
                } else {
                    toolTip.Text = tip;
                    int tipX = (point.X < chart.Width / 2) ? (int)point.X + off : (int)point.X - toolTip.Width - off;
                    int tipY = (point.Y < chart.Height / 2) ? (int)point.Y + off : (int)point.Y - toolTip.Height - off;
                    if ((point.X < chart.Width / 2) && (point.Y < chart.Height / 2)) tipX += pointerWidth;
                    toolTip.Location = new Point(tipX, tipY);
                    toolTip.Visible = true;
                    toolTip.BringToFront();
                }
            }

        }

        /* GUI INITIALIZATION */

        public WinClicker winClicker;              // set up platform-specific gui controls 
        public ClickerHandler clickerHandler;      // bind actions to them (non-platform specific)

        public GuiToWin() {
            InitializeComponent();
            // this.KeyPreview = true; // needed by OnKeyDown to override Ctrl-V to paste text instead of pictures in RichTextBox control
            fonts = new Dictionary<float, Font>();
            fontsFixed = new Dictionary<float, Font>();
            textFont = GetFont(10, true);
            kaemikaFont = new Font("Matura MT Script Capitals", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            richTextBox.MouseClick += (object sender, MouseEventArgs e) => { App.fromGui.clickerHandler.CloseOpenMenu(); };
            txtTarget.MouseClick += (object sender, MouseEventArgs e) => { App.fromGui.clickerHandler.CloseOpenMenu(); };
            panel1.MouseClick += (object sender, MouseEventArgs e) => { App.fromGui.clickerHandler.CloseOpenMenu(); };
            panel2.MouseClick += (object sender, MouseEventArgs e) => { App.fromGui.clickerHandler.CloseOpenMenu(); };
            panel_Splash.MouseClick += (object sender, MouseEventArgs e) => { App.fromGui.clickerHandler.CloseOpenMenu(); };
        }

        // Microfluidics

        public static DeviceSKControl deviceControl = null;

        // KChart

        public static KChartSKControl chartControl = null;

        // OnOff Buttons

        //OnOffButton onOffStop;
        //OnOffButton onOffEval;
        //OnOffButton onOffDevice;
        //OnOffButton onOffDeviceView;
        //OnOffButton onOffParameters; 
        //OnOffButton onOffFontSizePlus;
        //OnOffButton onOffFontSizeMinus;
        //OnOffButton onOffSave;
        //OnOffButton onOffLoad;

        // Flyout Menus
        //FlyoutMenu menu_Tutorial;
        //FlyoutMenu menu_Output;
        //FlyoutMenu menu_Export;
        //FlyoutMenu menu_Noise;
        //FlyoutMenu menu_Legend;
        //FlyoutMenu menu_Math;
        //FlyoutMenu menu_Settings;

        //private Noise SelectNoiseSelectedItem = Noise.None;
        //private void SelectNoise(ButtonPlusT<Noise> button) {
        //    Noise oldNoise = SelectNoiseSelectedItem;
        //    Noise newNoise = button.data;
        //    SelectNoiseSelectedItem = newNoise;
        //    button.flyoutMenu.SetImage(ImageOfNoise(newNoise));
        //    if (newNoise != oldNoise) StartAction(forkWorker: true, autoContinue: false);
        //    button.SetSelected();
        //    this.panel_Splash.Visible = false;
        //}

        private Bitmap ImageOfNoise(Noise noise) {
            if (noise == Noise.None) return Properties.Resources.Noise_None_W_48x48;
            if (noise == Noise.SigmaRange) return Properties.Resources.Noise_SigmaRange_W_48x48;
            if (noise == Noise.Sigma) return Properties.Resources.Noise_Sigma_W_48x48;
            if (noise == Noise.CV) return Properties.Resources.Noise_CV_W_48x48;
            if (noise == Noise.SigmaSqRange) return Properties.Resources.Noise_SigmaSqRange_W_48x48;
            if (noise == Noise.SigmaSq) return Properties.Resources.Noise_SigmaSq_W_48x48;
            if (noise == Noise.Fano) return Properties.Resources.Noise_Fano_W_48x48;
            throw new Error("ImageOfNoise");
        }

        // ON LOAD

        public static void SetFont(float size, bool fixedWidth) {
            if (size >= 6){
                Font font = GetFont(size, fixedWidth);
                App.fromGui.richTextBox.Font = font;
                App.fromGui.txtTarget.Font = font;
            }
        }
    
        private void GUI2_Load(object senderLoad, EventArgs eLoad) {
            this.KeyPreview = true; //
            //this.AutoScaleMode = AutoScaleMode.None;
            //this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.Width = Math.Min(this.Width, Screen.PrimaryScreen.Bounds.Size.Width);
            this.Height = Math.Min(this.Height, Screen.PrimaryScreen.Bounds.Size.Height);
            this.CenterToScreen();

            this.BackColor = cMainButtonDeselected;
            this.panel1.BackColor = cMainButtonDeselected;
            this.panel2.BackColor = cMainButtonDeselected;
            this.splitContainer_Columns.BackColor = cMainButtonDeselected;

            // Splash

            this.panel_Splash.Location = this.panel_KChart.Location;
            this.panel_Splash.Size = this.panel_KChart.Size;
            this.panel_Splash.BringToFront();
            this.panel_Splash.Visible = true;

            // Text

            SetFont(10, true);

            //################### 
            winClicker = new WinClicker();                        // set up platform-specific gui controls 
            clickerHandler = new ClickerHandler(winClicker);      // bind actions to them (non-platform specific)
            //################### 

            // Parameters

            //button_Parameters.Visible = false;

            // Microfluidics

            this.panel_Microfluidics.Visible = false;
            this.panel_Microfluidics.BackColor = Color.FromArgb(ProtocolDevice.deviceBackColor.Alpha, ProtocolDevice.deviceBackColor.Red, ProtocolDevice.deviceBackColor.Green, ProtocolDevice.deviceBackColor.Blue);
            deviceControl = new DeviceSKControl();
            this.panel_Microfluidics.Controls.Add(deviceControl);
            deviceControl.Location = new Point(0, 0);

            // KChart

            this.panel_KChart.BackColor = Color.White;
            chartControl = new KChartSKControl();
            this.panel_KChart.Controls.Add(chartControl);
            chartControl.Location = new Point(0, 0);
            

            // Tutorial Flyout Menu

            //menu_Tutorial = new FlyoutMenu(panel1, button_Tutorial, darkerBlue, darkPurple, darkPurpleSelected,
            //    Properties.Resources.icons8text_48x48, flowLayoutPanel_Tutorial, false, true, FlyoutAttachment.RightTop);
            //List<ModelInfoGroup> groups = Tutorial.Groups();
            //foreach (ModelInfoGroup group in groups) {
            //    ButtonPlusT<ModelInfoGroup> groupButton = menu_Tutorial.PlainButton<ModelInfoGroup>(group.GroupHeading, darkPurple, hotText, GetFont(9), menu_Tutorial, group, false, null); // null: don't even close the menu if selected
            //    menu_Tutorial.AddMenuItem(groupButton);
            //    foreach (ModelInfo info in group) {
            //        menu_Tutorial.AddMenuItem(menu_Tutorial.PlainButton<ModelInfo>(info.title, darkPurple, whiteText, GetFont(8), menu_Tutorial, info, true,
            //            (ButtonPlusT<ModelInfo> selectedButton) => { InputSetText(selectedButton.data.text); }));
            //    }
            //}

            //// Noise Flyout Menu

            //menu_Noise = new FlyoutMenu(panel2, button_Noise, darkerBlue, darkPurple, darkPurpleSelected,
            //    ImageOfNoise(Noise.None), flowLayoutPanel_Noise, false, true, FlyoutAttachment.LeftDown);
            //ButtonPlusT<Noise> headingNoise = menu_Noise.PlainButton<Noise>(" LNA", darkPurple, hotText, font10, menu_Noise, Noise.None, false, null);
            //menu_Noise.AddMenuItem(headingNoise);
            //Size flowLayoutPanelSize = headingNoise.Size;
            //foreach (Noise noise in Gui.noise) {
            //    ButtonPlusT<Noise> button = menu_Noise.ImageButton(Gui.StringOfNoise(noise), ImageOfNoise(noise), menu_Noise, noise, true, SelectNoise);
            //    flowLayoutPanelSize = new Size(Math.Max(flowLayoutPanelSize.Width, button.Size.Width), flowLayoutPanelSize.Height + button.Size.Height);
            //    menu_Noise.AddMenuItem(button);
            //}
            //menu_Noise.SetMenuSize(flowLayoutPanelSize);
            //menu_Noise.SetSelected(1); // first item below heading

            //// Output Flyout Menu

            //menu_Output = new FlyoutMenu(panel2, button_Output, darkerBlue, darkPurple, darkPurpleSelected,
            //    Properties.Resources.Computation_48x48, flowLayoutPanel_Output, true, true, FlyoutAttachment.LeftDown);
            //menu_Output.AddMenuItem(menu_Output.PlainButton<ExportAction>("COMPUTED OUTPUT", darkPurple, hotText, GetFont(12), menu_Output, null, false, null));
            //foreach (ExportAction output in Exec.outputActionsList()) {
            //    menu_Output.AddMenuItem(menu_Output.PlainButton(output.name, darkPurple, whiteText, GetFont(12), menu_Output, output, true,
            //        (ButtonPlusT<ExportAction> selectedButton) => { selectedButton.SetSelected(); Exec.currentOutputAction = selectedButton.data; Exec.currentOutputAction.action(); }));
            //}
            //menu_Output.SetSelected(1); // first item below heading

            // Export Flyout Menu

            //menu_Export = new FlyoutMenu(panel1, button_Export, darkerBlue, darkPurple, darkPurpleSelected,
            //    Properties.Resources.icons8_share_384_W_48x48, flowLayoutPanel_Export, true, true, FlyoutAttachment.RightDown);
            //ButtonPlusT<ExportAction> headingExport = menu_Export.PlainButton<ExportAction>("SHARE", darkPurple, hotText, GetFont(12), menu_Export, null, false, null);
            //menu_Export.AddMenuItem(headingExport);
            //foreach (ExportAction export in Exec.exportActionsList()) {
            //    menu_Export.AddMenuItem(menu_Export.PlainButton(export.name, darkPurple, whiteText, GetFont(12), menu_Export, export, true,
            //        (ButtonPlusT<ExportAction> selectedButton) => { selectedButton.data.action(); }));
            //}

            //// Math Flyout Menu
            //menu_Math = new FlyoutMenu(panel1, button_Math, darkerBlue, darkPurple, darkPurpleSelected,
            //    Properties.Resources.icons8_keyboard_96_W_48x48, flowLayoutPanel_Math, false, true, FlyoutAttachment.RightDown);
            //foreach (string symbol in SharedAssets.symbols) {
            //    menu_Math.AddMenuItem(menu_Math.PlainButton(symbol, darkPurple, whiteText, GetFont(12), menu_Math, symbol, true,
            //        (ButtonPlusT<string> selectedButton) => { InputInsertText(selectedButton.data); }));
            //}

            //// Settings Flyout menu
            //menu_Settings = new FlyoutMenu(panel2, button_Settings, darkerBlue, darkPurple, darkPurpleSelected,
            //    Properties.Resources.icons8_settings_384_W_48x48, flowLayoutPanel_Settings, false, true, FlyoutAttachment.LeftUp);

            // Buttons

            //private void btnParse_Click(object sender, EventArgs e) { Exec.Execute_Starter(forkWorker: true, doParse: true); }
            //private void btnConstruct_Click(object sender, EventArgs e) { Exec.Execute_Starter(forkWorker: true, doAST: true); }
            //private void btnScope_Click(object sender, EventArgs e) { Exec.Execute_Starter(forkWorker: true, doScope: true); }

            //onOffEval = new OnOffButton(this.btnEval, darkerBlue, darkPurple, Properties.Resources.icons8play40,
            //    (object sender, EventArgs e) => {
            //        // if (!modelInfo.executable) return;
            //        CloseFlyoutMenu();
            //        StartAction(forkWorker: true, autoContinue: false);
            //    });
            //onOffEval.Visible(true);
            //onOffEval.Enabled();

            //onOffStop = new OnOffButton(this.btnStop, darkerBlue, darkPurple, Properties.Resources.icons8stop40,
            //    (object sender, EventArgs e) => {
            //        CloseFlyoutMenu();
            //        Exec.EndingExecution(); // signals that we should stop
            //    });
            //onOffStop.Visible(false);
            //onOffStop.Disabled();

            //menu_Legend = new FlyoutMenu(panel1, button_EditChart, darkerBlue, darkPurple, darkPurpleSelected,
            //    Properties.Resources.icons8combochart96_W_48x48, flowLayoutPanel_Legend, true, false, FlyoutAttachment.TextOutput); //#### FlyoutLocation.TextOutput);
            //menu_Legend.Visible(false);
            //menu_Legend.Enabled();

            //onOffDevice = new OnOffButton(this.button_Device, darkerBlue, darkPurple, Properties.Resources.icons8device_OFF_48x48,
            //    (object sender, EventArgs e) => {
            //        CloseFlyoutMenu();
            //        if (!ProtocolDevice.Exists()) {
            //            ProtocolDevice.Start(30, 100);
            //            deviceControl.Size = this.panel_Microfluidics.Size;
            //            panel_Microfluidics.BringToFront();
            //            panel_Microfluidics.Visible = true;
            //            onOffDevice.SetImage(Properties.Resources.icons8device_ON_48x48);
            //            onOffDevice.Enabled();
            //            onOffDeviceView.Visible(true);
            //            onOffDeviceView.Selected();
            //            //button_FlipMicrofluidics.Visible = true;
            //            //button_FlipMicrofluidics.BackColor = darkPurple;
            //        } else {
            //            if (!Exec.IsExecuting()) {
            //                onOffDeviceView.Visible(false);
            //                //button_FlipMicrofluidics.Visible = false;
            //                panel_Microfluidics.Visible = false;
            //                onOffDevice.SetImage(Properties.Resources.icons8device_OFF_48x48);
            //                onOffDevice.Enabled();
            //                ProtocolDevice.Stop();
            //            }
            //        }
            //    });
            //onOffDevice.Visible(true);
            //onOffDevice.Enabled();

            //onOffDeviceView = new OnOffButton(this.button_FlipMicrofluidics, darkerBlue, darkPurple, Properties.Resources.deviceBorder_W_48x48,
            //    (object sender, EventArgs e) => {
            //        CloseFlyoutMenu();
            //        if (panel_Microfluidics.Visible) {
            //            panel_Microfluidics.Visible = false;
            //            onOffDeviceView.Deselected();
            //            //button_FlipMicrofluidics.BackColor = darkerBlue;
            //        } else {
            //            deviceControl.Size = this.panel_Microfluidics.Size;
            //            panel_Microfluidics.BringToFront();
            //            panel_Microfluidics.Visible = true;
            //            onOffDeviceView.Selected();
            //            //button_FlipMicrofluidics.BackColor = darkPurple;
            //        }
            //    });
            //onOffDeviceView.Visible(false);
            //onOffDeviceView.Enabled();

            //onOffFontSizePlus = new OnOffButton(this.button_FontSizePlus, darkerBlue, darkPurple, Properties.Resources.FontSizePlus_W_48x48,
            //    (object sender, EventArgs e) => {
            //        CloseFlyoutMenu();
            //        SetFont(richTextBox.Font.Size + 1);
            //    });
            //onOffFontSizePlus.Visible(true);
            //onOffParameters.Enabled();

            //onOffFontSizeMinus = new OnOffButton(this.button_FontSizeMinus, darkerBlue, darkPurple, Properties.Resources.FontSizeMinus_W_48x48,
            //    (object sender, EventArgs e) => {
            //        CloseFlyoutMenu();
            //        SetFont(richTextBox.Font.Size - 1);
            //    });
            //onOffFontSizeMinus.Visible(true);
            //onOffParameters.Enabled();

            //onOffSave = new OnOffButton(this.button_Source_Copy, darkerBlue, darkPurple, Properties.Resources.FileSave_48x48,
            //    (object sender, EventArgs e) => {
            //        CloseFlyoutMenu();
            //        // try { Clipboard.SetText(InputGetText()); } catch (ArgumentException) { };
            //        SaveFileDialog saveFileDialog = new SaveFileDialog();
            //        saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            //        saveFileDialog.InitialDirectory = this.modelsDirectory;
            //        saveFileDialog.FilterIndex = 1;
            //        saveFileDialog.RestoreDirectory = false;
            //        onOffSave.Selected();
            //        if (saveFileDialog.ShowDialog() == DialogResult.OK) {
            //            try {
            //                File.WriteAllText(saveFileDialog.FileName, Gui.gui.InputGetText(), System.Text.Encoding.Unicode);
            //            } catch {
            //                MessageBox.Show(saveFileDialog.FileName, "Could not write this file:", MessageBoxButtons.OK);
            //            }
            //        }
            //        onOffSave.Deselected();
            //    });
            //onOffSave.Visible(true);
            //onOffSave.Enabled();

            //onOffLoad = new OnOffButton(this.button_Source_Paste, darkerBlue, darkPurple, Properties.Resources.FileLoad_48x48,
            //    (object sender, EventArgs e) => {
            //        CloseFlyoutMenu();
            //        // new ModalPopUp(panel_ModalPopUp).PopUp("Do you really want to paste the clipboard", "and replace all source text?", () => { InputSetText(Clipboard.GetText()); }, () => { });
            //        onOffLoad.Selected();
            //        using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
            //            openFileDialog.InitialDirectory = this.modelsDirectory;
            //            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            //            openFileDialog.FilterIndex = 1;
            //            openFileDialog.RestoreDirectory = false;
            //            if (openFileDialog.ShowDialog() == DialogResult.OK) {
            //                try {
            //                    Gui.gui.InputSetText(File.ReadAllText(openFileDialog.FileName, System.Text.Encoding.Unicode));
            //                } catch {
            //                    MessageBox.Show(openFileDialog.FileName, "Could not read this file:", MessageBoxButtons.OK);
            //                }
            //            }
            //        }
            //        onOffLoad.Deselected();
            //    });
            //onOffLoad.Visible(true);
            //onOffLoad.Enabled();

            //onOffParameters = new OnOffButton(this.button_Parameters, darkerBlue, darkPurple, Properties.Resources.Parameters_W_48x48,
            //    (object sender, EventArgs e) => {
            //        App.form.clickerHandler.CloseOpenMenu();
            //        if (flowLayoutPanel_Parameters.Visible) {
            //            HideParameters();
            //        } else {
            //            ShowParameters();
            //        }
            //    });
            //onOffParameters.Visible(false);
            //onOffParameters.Enabled();

            //// Settings Panel

            //flowLayoutPanel_Settings.BackColor = darkPurple;
            //flowLayoutPanel_Settings.Visible = false;

            //label_SettingsPanel.ForeColor = hotText;

            //button_RK547M.BackColor = darkerBlue;
            //button_GearBDF.BackColor = buttonGrey;
            //button_PrecomputeLNA.BackColor = buttonGrey;

            //modelsDirectory = Environment.GetFolderPath(defaultUserDataDirectoryPath);
            //button_ModelsDirectory.BackColor = darkerBlue;

            //toolTip1.SetToolTip(button_ModelsDirectory, modelsDirectory);

            // Chart

            flowLayoutPanel_Legend.Visible = false;

            // Parameters

            flowLayoutPanel_Parameters.BackColor = cPanelButtonDeselected;
            flowLayoutPanel_Parameters.ForeColor = cPanelButtonText;
            flowLayoutPanel_Parameters.Visible = false;
            flowLayoutPanel_Parameters.BringToFront();

            // Export

            //this.menu_Export.Visible(false);

            // Saved state

            RestoreInput();
        }

        private void GUI2_FormClosing(object sender, FormClosingEventArgs e) {
            this.SaveInput();
        }

        public string InputGetText() {
            return this.richTextBox.Text;
        }

        public void InputSetText(string text) {
            this.richTextBox.Text = text;
        }

        public void InputInsertText(string text) {
            this.richTextBox.SelectedText = text;
        }

        public void SetSelectionLineChar(int line, int chr, int tokenlength) {
            if (line < 0 || chr < 0) return;
            string text = this.richTextBox.Text;
            int i = 0;
            while (i < text.Length && line > 0) {
                if (text[i] == '\n') line--;
                i++;
            }
            if (i < text.Length && text[i] == '\r') i++;
            int linestart = i;
            while (i < text.Length && chr > 0) { chr--; i++; }
            int tokenstart = i;
            this.richTextBox.HideSelection = false; // keep selection highlight on loss of focus
            this.richTextBox.Select(tokenstart, tokenlength);
        }

        private static Environment.SpecialFolder defaultUserDataDirectoryPath = Environment.SpecialFolder.MyDocuments;
        private static Environment.SpecialFolder defaultKaemikaDataDirectoryPath = Environment.SpecialFolder.ApplicationData;

        private static string defaultUserDataDirectory = Environment.GetFolderPath(defaultUserDataDirectoryPath);
        private static string defaultKaemikaDataDirectory = Environment.GetFolderPath(defaultKaemikaDataDirectoryPath) + "\\Kaemika";

        public static string CreateKaemikaDataDirectory() {
            try {
                Directory.CreateDirectory(defaultKaemikaDataDirectory);
                return defaultKaemikaDataDirectory;
            } catch { return null; }
        }

        public void SaveInput() {
            try {
                string path = CreateKaemikaDataDirectory() + "\\save.txt";
                File.WriteAllText(path, this.InputGetText());
            } catch (Exception) { }
        }

        public void RestoreInput() {
            try {
                string path = CreateKaemikaDataDirectory() + "\\save.txt";
                if (File.Exists(path)) {
                    this.InputSetText(File.ReadAllText(path));
                } else {
                    this.InputSetText(SharedAssets.TextAsset("StartHere.txt"));
                }
            } catch (Exception) { }
        }

        public void OutputSetText(string text) {
            txtTarget.Text = text;
            //txtTarget.SelectionStart = txtTarget.Text.Length;
            //txtTarget.SelectionLength = 0;
            //txtTarget.ScrollToCaret();
        }

        public string OutputGetText() {
            return this.txtTarget.Text;
        }

        public void OutputAppendText(string text) {
            txtTarget.AppendText(text);
            txtTarget.SelectionStart = 0;
            txtTarget.SelectionLength = 0;
            txtTarget.ScrollToCaret();
            //txtTarget.SelectionStart = txtTarget.Text.Length;
            //txtTarget.SelectionLength = 0;
            //txtTarget.ScrollToCaret();
        }

        //public void Executing(bool executing) {
        //    if (executing) {
        //        this.onOffDevice.Disabled();
        //        this.onOffEval.Disabled();
        //        this.onOffStop.Enabled(); this.onOffStop.Visible(true); //this.onOffStop.Focus();
        //        this.menu_Noise.Disabled();
        //        this.menu_Tutorial.Disabled();
        //        this.menu_Export.Disabled();
        //        this.menu_Output.Disabled();
        //    }
        //    else {
        //        this.onOffDevice.Enabled();
        //        this.onOffEval.Enabled(); //this.onOffEval.Focus();
        //        this.onOffStop.Visible(false); this.onOffStop.Disabled();
        //        this.menu_Legend.Visible(true);
        //        this.menu_Noise.Enabled();
        //        this.menu_Tutorial.Enabled();
        //        this.menu_Export.Visible(true); this.menu_Export.Enabled();
        //        this.menu_Output.Enabled();
        //    }
        //}

        //private void SetStartButtonToContinue() {
        //    onOffEval.SetImage(Properties.Resources.icons8pauseplay40);
        //    onOffEval.Enabled();
        //}
        //private void SetContinueButtonToStart() {
        //    onOffEval.SetImage(Properties.Resources.icons8play40);
        //    onOffEval.Disabled();

        //}

        //private bool continueButtonIsEnabled = false;
        //public void ContinueEnable(bool b) {
        //    continueButtonIsEnabled = b;
        //    if (continueButtonIsEnabled) SetStartButtonToContinue(); else SetContinueButtonToStart();
        //}

        //public bool ContinueEnabled() {
        //    return continueButtonIsEnabled;
        //}

        //public bool ScopeVariants() {
        //    return true; // checkBox_ScopeVariants.Checked;
        //}
        //public bool RemapVariants() {
        //    return true; // checkBox_RemapVariants.Checked;
        //}

        // CHARTS

        //private string title = "";
        //private KTimecourse timecourse;                  // assumes points arrive in time equal or increasing

        //private static Dictionary<string, bool> visibilityCache = new Dictionary<string, bool>();

        //public KSeries ChartSeriesNamed(string name) {
        //    if (name == null) return null;
        //    return timecourse.SeriesNamed(name);
        //}


        //public void ChartClear(string title) {
        //    //ChartListboxClear();   // <<<====== Legend Reset
        //    this.title = title;
        //    this.timecourse = new KTimecourse(title);
        //    ChartUpdate();
        //    LegendUpdate();
        //}

        //public void ChartClearData() {
        //    this.timecourse.ClearData();
        //}

        public void ChartUpdate() {
            KChartHandler.VisibilityRestore(); // this is needed to hide the series in the chart
            chartControl.Size = panel_KChart.Size;
            chartControl.Invalidate();
            chartControl.Update();
        }

        public void LegendUpdate() {
            KChartHandler.VisibilityRestore(); // this is needed to hide the series in the legend
            App.fromGui.clickerHandler.SetLegend(KChartHandler.Legend());
        }

        public class LineButton : Button {
            private int thickness;
            private Color color;
            public LineButton(int thickness, Color color) : base() {
                this.thickness = thickness;
                this.color = color;
            }
            protected override void OnPaint(PaintEventArgs pevent) {
                //base.OnPaint(pevent); 
                if (Text == "---") {
                    using (Pen p = new Pen(Color.White)) {
                        pevent.Graphics.FillRectangle(p.Brush, ClientRectangle);
                    }
                    using (Pen p = new Pen(this.color)) {
                        int leftRightMargin = 4;
                        pevent.Graphics.FillRectangle(p.Brush, 
                            new Rectangle(
                                ClientRectangle.X + leftRightMargin, 
                                ClientRectangle.Y+(ClientRectangle.Height - thickness)/2, 
                                ClientRectangle.Width - 2*leftRightMargin, 
                                thickness));
                    }
                } else {
                    using (Pen p = new Pen(BackColor)) {
                        pevent.Graphics.FillRectangle(p.Brush, ClientRectangle);
                    }
                }
            }
        }

        //private LineButton LegendLine(KSeries series, Color backColor) {
        //    int thickness = (series.lineStyle == KLineStyle.Thick) ? 4 : (series.lineMode == KLineMode.Line) ? 2 : 12;
        //    var button = new LineButton(thickness, ColorOverWhite(series.color));
        //    button.FlatStyle = FlatStyle.Flat;
        //    button.FlatAppearance.BorderSize = 0;
        //    button.Padding = new Padding(0, 0, 0, 0);
        //    button.Margin = new Padding(6, 0, 6, 0);
        //    button.BackColor = backColor;
        //    button.Enabled = false;
        //    return button;
        //}

        public static Color ColorOverWhite(SKColor color) {
            return ColorOverWhite(Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));
        }

        public static Color ColorOverWhite(Color color) { 
            // removes transparency but does the equivalent compositing over white
            float R = ((float)color.R) / 255.0f;
            float G = ((float)color.G) / 255.0f;
            float B = ((float)color.B) / 255.0f;
            float A = ((float)color.A) / 255.0f;
            return Color.FromArgb(255,
                (byte)((R * A + 1.0f - A) * 255.0f),
                (byte)((G * A + 1.0f - A) * 255.0f),
                (byte)((B * A + 1.0f - A) * 255.0f));
        }

        private void button_RK547M_Click(object sender, EventArgs e) {
            //button_RK547M.BackColor = darkerBlue;
            //button_GearBDF.BackColor = buttonGrey;
            //solver = "RK547M";
        }

        private void button_GearBDF_Click(object sender, EventArgs e) {
            //button_RK547M.BackColor = buttonGrey;
            //button_GearBDF.BackColor = darkerBlue;
            //solver = "GearBDF";
        }

        //public bool precomputeLNA = false;
        private void button_PrecomputeLNA_Click(object sender, EventArgs e) {
            //precomputeLNA = !precomputeLNA;
            //if (precomputeLNA) button_PrecomputeLNA.BackColor = darkerBlue;
            //else button_PrecomputeLNA.BackColor = buttonGrey;
        }

        //private int Transparency() {
        //    return 32;
        //}

        // ======== PARAMETERS ========= //

        //private static Dictionary<string, ParameterInfo> parameterInfoDict = new Dictionary<string, ParameterInfo>(); // persistent information
        //private static Dictionary<string, ParameterState> parameterStateDict = new Dictionary<string, ParameterState>();
        //private static object parameterLock = new object(); // protects parameterInfoDict and parameterStateDict

        //// clear the parameterStateDict at the beginning of every execution, but we keep the parametersInfoDict forever

        public void ParametersClear() {
            clickerHandler.ParametersClear();
            //lock (parameterLock) {
            //    parameterStateDict = new Dictionary<string, ParameterState>();
            //    flowLayoutPanel_Parameters.Controls.Clear();
            //}
        }

        //public class ParameterState {
        //    public string parameter;
        //    public double value;
        //    public int rangeSteps;
        //    public ParameterState(string parameter, ParameterInfo info) {
        //        this.parameter = parameter;
        //        this.value = info.drawn;
        //        this.rangeSteps = (info.distribution == "bernoulli") ? 1 : 100;
        //    }
        //}

        //// ask the gui if this parameter is locked

        public double ParameterOracle(string parameter) { // returns NAN if oracle not available
            return clickerHandler.ParameterOracle(parameter);
            //lock (parameterLock) {
            //    if (parameterInfoDict.ContainsKey(parameter) && parameterInfoDict[parameter].locked)
            //        // parameter does not exist yet in parameterStateDict but will exist at the end of the run, and it will be locked
            //        return parameterInfoDict[parameter].drawn;
            //    return double.NaN;
            //}
        }

        // reflect the parameter state into the gui

        public void ParametersUpdate() {
            clickerHandler.ParametersUpdate();
            //lock (parameterLock) {
            //    RefreshParameters();
            //}
            //if (parameterStateDict.Count > 0) {
            //    onOffParameters.Visible(true);
            //    ShowParameters();
            //} else { 
            //    HideParameters();
            //    onOffParameters.Visible(false);
            //}
        }
        //private void RefreshParameters() {
        //    // called with already locked parameterLock
        //    foreach (var kvp in parameterStateDict) {
        //        ParameterInfo info = parameterInfoDict[kvp.Key];
        //        ParameterState state = parameterStateDict[kvp.Key];
        //        const int width = 300;
        //        CheckBox newCheckBox = new CheckBox();
        //        newCheckBox.Width = width - 30; // space for scrollbar
        //        newCheckBox.Margin = new Padding(10, 0, 0, 0);
        //        newCheckBox.Text = info.ParameterLabel(false);
        //        newCheckBox.Checked = info.locked;
        //        newCheckBox.CheckedChanged += (object source, EventArgs e) => {
        //            lock (parameterLock) {
        //                ParameterInfo paramInfo = parameterInfoDict[info.parameter];
        //                ParameterState paramState = parameterStateDict[info.parameter];
        //                paramInfo.locked = newCheckBox.Checked;
        //            }
        //        };
        //        TrackBar newTrackBar = new TrackBar(); newTrackBar.Width = width - 30;
        //        newTrackBar.Minimum = 0;
        //        newTrackBar.Maximum = state.rangeSteps;
        //        newTrackBar.Value = (info.range == 0.0) ? (state.rangeSteps / 2) : (int)(state.rangeSteps * (info.drawn - info.rangeMin) / info.range);
        //        newTrackBar.ValueChanged += (object source, EventArgs e) => {
        //            lock (parameterLock) {
        //                if ((!parameterStateDict.ContainsKey(info.parameter)) || (!parameterInfoDict.ContainsKey(info.parameter))) return;
        //                ParameterInfo paramInfo = parameterInfoDict[info.parameter];
        //                ParameterState paramState = parameterStateDict[info.parameter];
        //                paramInfo.drawn = paramInfo.rangeMin + newTrackBar.Value / ((double)paramState.rangeSteps) * paramInfo.range;
        //                newCheckBox.Text = paramInfo.ParameterLabel(false);
        //            }
        //        };
        //        flowLayoutPanel_Parameters.Controls.Add(newCheckBox);
        //        flowLayoutPanel_Parameters.Controls.Add(newTrackBar);
        //        int oldWidth = flowLayoutPanel_Parameters.Width;
        //        Point oldLocation = flowLayoutPanel_Parameters.Location;
        //        flowLayoutPanel_Parameters.Width = width;
        //        flowLayoutPanel_Parameters.Location = new Point(oldLocation.X + oldWidth - width, oldLocation.Y); // stick to the right when changing size
        //    }
        //}

        public void AddParameter(string parameter, double drawn, string distribution, double[] arguments) {
            clickerHandler.AddParameter(parameter, drawn, distribution, arguments);
            //lock (parameterLock) {
            //    if (!parameterInfoDict.ContainsKey(parameter)) {
            //        parameterInfoDict[parameter] = new ParameterInfo(parameter, drawn, distribution, arguments);
            //        parameterStateDict[parameter] = new ParameterState(parameter, parameterInfoDict[parameter]);
            //    }
            //}
            //parameterStateDict[parameter] = new ParameterState(parameter, parameterInfoDict[parameter]); // use the old value, not the one from drawn
            //if (parameterInfoDict.ContainsKey(parameter) && parameterInfoDict[parameter].locked) return; // do not change the old value if locked
            //ParameterInfo info = new ParameterInfo(parameter, drawn, distribution, arguments);           // use the new value, from drawn
            //ParameterState state = new ParameterState(parameter, info);                                  // update the value
            //parameterInfoDict[parameter] = info;
            //parameterStateDict[parameter] = state;
        }

        /* GUI EVENTS */

        /* CHART PAN/ZOOM */

        // System.Windows.Forms.DataVisualization.Charting 
        // https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.datavisualization.charting?view=netframework-4.7.2
        // https://www.dotnetperls.com/chart

        //private void chart1_DoubleClick(object sender, EventArgs e) {
        //    //try {
        //    //    ((Chart)sender).ChartAreas[0].AxisX.ScaleView.ZoomReset();
        //    //    ((Chart)sender).ChartAreas[0].AxisY.ScaleView.ZoomReset();
        //    //    mouseToolTip.RemoveAll();
        //    //} catch { }
        //}
        private bool dragging = false;
        private Point mouseDown;
        private Point mouseMove;
        private double mouseDownViewMininumX;
        private double mouseDownViewMininumY;
        private ToolTip mouseToolTip = new ToolTip();
        //private void chart1_MouseDown(object sender, MouseEventArgs e) {
        //    //mouseDown = e.Location;
        //    //mouseDownViewMininumX = ((Chart)sender).ChartAreas[0].AxisX.ScaleView.ViewMinimum;
        //    //mouseDownViewMininumY = ((Chart)sender).ChartAreas[0].AxisY.ScaleView.ViewMinimum;
        //    //dragging = true;
        //}
        //private bool Zoomed(Chart chart) {
        //    var xAxis = chart.ChartAreas[0].AxisX;
        //    var yAxis = chart.ChartAreas[0].AxisY;
        //    return chart.Series.Count > 0 && chart.Series[0].Points.Count > 0 &&
        //           (xAxis.ScaleView.ViewMinimum != xAxis.Minimum || xAxis.ScaleView.ViewMaximum != xAxis.Maximum
        //           || yAxis.ScaleView.ViewMinimum != yAxis.Minimum || yAxis.ScaleView.ViewMaximum != yAxis.Maximum);
        //}
        //private void chart1_MouseMove(object sender, MouseEventArgs e) {
        //    if (e.Location == mouseMove) return;
        //    try {
        //        var chart = (Chart)sender;
        //        var xAxis = chart.ChartAreas[0].AxisX;
        //        var yAxis = chart.ChartAreas[0].AxisY;
        //        if (dragging) {
        //            var dX = xAxis.PixelPositionToValue(e.Location.X) - xAxis.PixelPositionToValue(mouseDown.X);
        //            var dY = yAxis.PixelPositionToValue(e.Location.Y) - yAxis.PixelPositionToValue(mouseDown.Y);
        //            xAxis.ScaleView.Scroll(mouseDownViewMininumX - dX);
        //            yAxis.ScaleView.Scroll(mouseDownViewMininumY - dY);
        //        } else {
        //            //+ " (X=" + xAxis.PixelPositionToValue(e.Location.X).ToString("G4") + ", Y=" + yAxis.PixelPositionToValue(e.Location.Y).ToString("G4") + ")"
        //            mouseToolTip.RemoveAll();
        //            string pointTag = "";
        //            var hits = chart.HitTest(e.X, e.Y, false, ChartElementType.DataPoint);
        //            foreach (HitTestResult hit in hits) {
        //                if (hit.Object is DataPoint) {
        //                    DataPoint point = (DataPoint)hit.Object;
        //                    if (point.YValues.Count() == 1)
        //                        pointTag += point.Tag + "  [x = " + point.XValue.ToString("G4") + ", y = " + point.YValues[0].ToString("G4") + "]";
        //                    else if (point.YValues.Count() == 2) // YValues[0] is mean - (variance or s.d.), YValues[1] is mean + (variance or s.d.)
        //                        pointTag += point.Tag + "  [x = " + point.XValue.ToString("G4") + ", y = " + ((point.YValues[0] + point.YValues[1]) / 2).ToString("G4") + "±" + ((point.YValues[1] - point.YValues[0]) / 2).ToString("G4") + "]";
        //                    pointTag += Environment.NewLine;
        //                }
        //            }
        //            if (pointTag != "") pointTag = pointTag.Substring(0, pointTag.Length - 1); // remove last newLine
        //            if (pointTag == "" && Zoomed(chart)) pointTag = "Zoomed!";
        //            mouseToolTip.Show(pointTag, chart1, e.Location.X + 32, e.Location.Y - 15);
        //        }
        //    } catch { }
        //    mouseMove = e.Location;
        //}
        //private void chart1_MouseUp(object sender, MouseEventArgs e) {
        //    //dragging = false;
        //}

        //private void chart1_MouseWheel(object sender, MouseEventArgs e) {
        //    //var chart = (Chart)sender;
        //    //var xAxis = chart.ChartAreas[0].AxisX;
        //    //var yAxis = chart.ChartAreas[0].AxisY;
        //    //var xMin = xAxis.ScaleView.ViewMinimum;
        //    //var xMax = xAxis.ScaleView.ViewMaximum;
        //    //var yMin = yAxis.ScaleView.ViewMinimum;
        //    //var yMax = yAxis.ScaleView.ViewMaximum;
        //    //const double scale = 0.2;
        //    //try {
        //    //    var posX = xAxis.PixelPositionToValue(e.Location.X);
        //    //    var posY = yAxis.PixelPositionToValue(e.Location.Y);
        //    //    if (posX < xMin || posX > xMax || posY < yMin || posY > yMax) return;
        //    //    if (e.Delta < 0) { // Scrolled down: zoom out around mouse position              
        //    //        var posXStart = xMin - (posX - xMin) * scale;
        //    //        var posXFinish = xMax + (xMax - posX) * scale;
        //    //        var posYStart = yMin - (posY - yMin) * scale;
        //    //        var posYFinish = yMax + (yMax - posY) * scale;
        //    //        if (posXStart < xAxis.Minimum) posXStart = xAxis.Minimum;
        //    //        if (posXFinish > xAxis.Maximum) posXFinish = xAxis.Maximum;
        //    //        if (posYStart < yAxis.Minimum) posYStart = yAxis.Minimum;
        //    //        if (posYFinish > yAxis.Maximum) posYFinish = yAxis.Maximum;
        //    //        xAxis.ScaleView.Zoom(posXStart, posXFinish);
        //    //        yAxis.ScaleView.Zoom(posYStart, posYFinish);
        //    //        if (posXStart == xAxis.Minimum && posXFinish == xAxis.Maximum) xAxis.ScaleView.ZoomReset();
        //    //        if (posYStart == yAxis.Minimum && posYFinish == yAxis.Maximum) yAxis.ScaleView.ZoomReset();
        //    //    } else if (e.Delta > 0) { // Scrolled up: zoom in around mouse position
        //    //        var posXStart = xMin + (posX - xMin) * scale;
        //    //        var posXFinish = xMax - (xMax - posX) * scale;
        //    //        var posYStart = yMin + (posY - yMin) * scale;
        //    //        var posYFinish = yMax - (yMax - posY) * scale;
        //    //        xAxis.ScaleView.Zoom(posXStart, posXFinish);
        //    //        yAxis.ScaleView.Zoom(posYStart, posYFinish);
        //    //    }
        //    //    if (!Zoomed(chart)) mouseToolTip.RemoveAll();
        //    //}
        //    //catch { }
        //}

        //private void ShowParameters() {
        //    onOffParameters.Selected();
        //    flowLayoutPanel_Parameters.BringToFront();
        //    flowLayoutPanel_Parameters.Visible = true;
        //}

        //private void HideParameters() {
        //    flowLayoutPanel_Parameters.Visible = false;
        //    onOffParameters.Deselected();
        //}

       public void ChartSnap() {
            SKBitmap theBitmap = null; // store the bitmap internally generated by GenPainter for use by DoPaste
            Func<Colorer> GenColorer = () => {
                return new SKColorer();
            };
            Func<SKSize, ChartPainter> GenPainter = (SKSize canvasSize) => {
                SKBitmap bm = new SKBitmap((int)canvasSize.Width, (int)canvasSize.Height);
                SKCanvas canvas = new SKCanvas(bm);
                theBitmap = bm;
                return new SKChartPainter(canvas);
            };
            Action<SKBitmap> DoPaste = (SKBitmap bitmap) => {
                Clipboard.SetImage(Extensions.ToBitmap(bitmap));
            };

            Size chartSize = App.fromGui.panel_KChart.Size;
            KChartHandler.Snap(GenColorer, GenPainter, new SKSize(chartSize.Width, chartSize.Height));
            try { DoPaste(theBitmap); } catch { }

        }

        public void ChartSnapToSvg() {
            SvgCanvas theCanvas = null; // store the canvas internally generated by GenPainter for use in writing the SVG out
            Func<Colorer> GenColorer = () => {
                return new SKColorer();
            };
            Func<SKSize, ChartPainter> GenPainter = (SKSize canvasSize) => {
                SvgCanvas canvas = new SvgCanvas(canvasSize, new SKSize(29.7f, 21.0f));
                theCanvas = canvas;
                return new SvgChartPainter(canvas); 
            };
            Size chartSize = App.fromGui.panel_KChart.Size;
            KChartHandler.Snap(GenColorer, GenPainter, new SKSize(chartSize.Width, chartSize.Height));

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "SVG files (*.svg)|*.svg|All files (*.*)|*.*";
            saveFileDialog.InitialDirectory = modelsDirectory;
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = false;
            winClicker.menuExport.Selected(true);
            if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                try {
                    File.WriteAllText(saveFileDialog.FileName, theCanvas.Close(), System.Text.Encoding.Unicode);
                } catch {
                    MessageBox.Show(saveFileDialog.FileName, "Could not write this file:", MessageBoxButtons.OK);
                }
            }
            winClicker.menuExport.Selected(false);
        }

        public void ChartData() {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            saveFileDialog.InitialDirectory = modelsDirectory;
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = false;
            winClicker.menuExport.Selected(true);
            if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                try {
                    File.WriteAllText(saveFileDialog.FileName, KChartHandler.ToCSV(), System.Text.Encoding.Unicode);
                } catch {
                    MessageBox.Show(saveFileDialog.FileName, "Could not write this file:", MessageBoxButtons.OK);
                }
            }
            winClicker.menuExport.Selected(false);
        }


        //https://social.msdn.microsoft.com/Forums/windows/en-US/4cced4a8-6e66-40f6-8710-deb99d962b91/clipboard-and-metafiles-compatibility?forum=winforms
        public class ClipboardMetafileHelper {
            [DllImport("user32.dll")]
            static extern bool OpenClipboard(IntPtr hWndNewOwner);
            [DllImport("user32.dll")]
            static extern bool EmptyClipboard();
            [DllImport("user32.dll")]
            static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
            [DllImport("user32.dll")]
            static extern bool CloseClipboard();
            [DllImport("gdi32.dll")]
            static extern IntPtr CopyEnhMetaFile(IntPtr hemfSrc, IntPtr hNULL);
            [DllImport("gdi32.dll")]
            static extern bool DeleteEnhMetaFile(IntPtr hemf);
            // Metafile mf is set to a state that is not valid inside this function.
            static public bool PutEnhMetafileOnClipboard(IntPtr hWnd, Metafile mf) {
                bool bResult = false;
                IntPtr hEMF1;
                IntPtr hEMF2;
                hEMF1 = mf.GetHenhmetafile();
                if (!hEMF1.Equals(new IntPtr(0))) {
                    hEMF2 = CopyEnhMetaFile(hEMF1, new IntPtr(0));
                    if (!hEMF2.Equals(new IntPtr(0))) {
                        if (OpenClipboard(hWnd)) {
                            if (EmptyClipboard()) {
                                IntPtr hRes = SetClipboardData(14 /*CF_ENHMETAFILE*/, hEMF2);
                                bResult = hRes.Equals(hEMF2);
                                CloseClipboard();
                            }
                        }
                    }
                    DeleteEnhMetaFile(hEMF1);
                }
                return bResult;
            }
        }

        ///* EXECUTE */

        //public void StartAction(bool forkWorker, bool autoContinue = false) {
        //    this.panel_Splash.Visible = false;
        //    if (Exec.IsExecuting() && !Gui.gui.ContinueEnabled()) return; // we are already running a simulation, don't start a concurrent one
        //    if (Exec.IsExecuting() && Gui.gui.ContinueEnabled()) { // we are already running a simulation; make start button work as continue button
        //        Protocol.continueExecution = true;
        //    } else { // do a start
        //        Exec.Execute_Starter(forkWorker, autoContinue: autoContinue); // This is where it all happens
        //    }
        //}

        /* OTHERS */

        public void OutputCopy()
        {
            try { Clipboard.SetText(OutputGetText()); } catch (ArgumentException) { };
        }

        private void panel_Microfluidics_SizeChanged(object sender, EventArgs e)
        {
            deviceControl.Size = this.panel_Microfluidics.Size;
        }

        private void panel_KChart_SizeChanged(object sender, EventArgs e)
        {
            chartControl.Size = this.panel_KChart.Size;
        }

        //private void SetFont(float size) {
        //    if (size >= 6) {
        //        Font font = GetFont(size);
        //        richTextBox.Font = font;
        //        txtTarget.Font = font;
        //    }
        //}

        //private string BrowseDirectory(string initialDirectory) {
        //    var folderPath = string.Empty;
        //    FolderBrowserDialog dialog = new FolderBrowserDialog();
        //    dialog.SelectedPath = initialDirectory;
        //    if (dialog.ShowDialog() == DialogResult.OK) folderPath = dialog.SelectedPath;
        //    //using (OpenFileDialog folderBrowser = new OpenFileDialog()) {
        //    //    folderBrowser.InitialDirectory = initialDirectory;
        //    //    folderBrowser.ValidateNames = false;
        //    //    folderBrowser.CheckFileExists = false;
        //    //    folderBrowser.CheckPathExists = true;
        //    //    folderBrowser.FileName = "1. click ON a folder, 2. click Open.";
        //    //    folderBrowser.RestoreDirectory = false;
        //    //    if (folderBrowser.ShowDialog() == DialogResult.OK) {
        //    //        folderPath = Path.GetDirectoryName(folderBrowser.FileName);
        //    //    }
        //    //}
        //    if (folderPath == string.Empty)
        //        return initialDirectory;
        //    else {
        //        MessageBox.Show(folderPath, "Directory set to:", MessageBoxButtons.OK);
        //        return folderPath;
        //    }
        //}

        //public string modelsDirectory = string.Empty;
        private void button_ModelsDirectory_Click(object sender, EventArgs e) {
            //modelsDirectory = BrowseDirectory(modelsDirectory);
            //toolTip1.SetToolTip(button_ModelsDirectory, modelsDirectory);
            //SaveDirectories();
        }

        private void splitContainer_Columns_SplitterMoved(object sender, SplitterEventArgs e)
        {
            //this.splitContainer_Columns.BackColor = darkPurple;  // in case it is dragged to the edge and would be hard to see
        }

        private void splitContainer_Rows_SplitterMoved(object sender, SplitterEventArgs e)
        {
            //this.splitContainer_Columns.BackColor = darkPurple;  // in case it is dragged to the edge and would be hard to see
        }

        // https://stackoverflow.com/questions/6521731/refresh-the-panels-of-a-splitcontainer-as-the-splitter-moves

        private void splitContainer_Columns_MouseDown(object sender, MouseEventArgs e) {
            // This disables the normal move behavior
            ((SplitContainer)sender).IsSplitterFixed = true;
        }

        private void splitContainer_Columns_MouseUp(object sender, MouseEventArgs e) {
            // This allows the splitter to be moved normally again
            ((SplitContainer)sender).IsSplitterFixed = false;
        }

        private void splitContainer_Columns_MouseMove(object sender, MouseEventArgs e) {
            // Check to make sure the splitter won't be updated by the
            // normal move behavior also
            if (((SplitContainer)sender).IsSplitterFixed)
            {
                // Make sure that the button used to move the splitter
                // is the left mouse button
                if (e.Button.Equals(MouseButtons.Left))
                {
                    // Checks to see if the splitter is aligned Vertically
                    if (((SplitContainer)sender).Orientation.Equals(Orientation.Vertical))
                    {
                        // Only move the splitter if the mouse is within
                        // the appropriate bounds
                        if (e.X > 0 && e.X < ((SplitContainer)sender).Width)
                        {
                            // Move the splitter & force a visual refresh
                            ((SplitContainer)sender).SplitterDistance = e.X;
                            ((SplitContainer)sender).Refresh();
                        }
                    }
                    // If it isn't aligned vertically then it must be
                    // horizontal
                    else
                    {
                        // Only move the splitter if the mouse is within
                        // the appropriate bounds
                        if (e.Y > 0 && e.Y < ((SplitContainer)sender).Height)
                        {
                            // Move the splitter & force a visual refresh
                            ((SplitContainer)sender).SplitterDistance = e.Y;
                            ((SplitContainer)sender).Refresh();
                            //((SplitContainer)sender).Invalidate();
                        }
                    }
                }
                // If a button other than left is pressed or no button
                // at all
                else
                {
                    // This allows the splitter to be moved normally again
                    ((SplitContainer)sender).IsSplitterFixed = false;
                }
            }
        }

        private void splitContainer_Rows_MouseDown(object sender, MouseEventArgs e) {
            // This disables the normal move behavior
            ((SplitContainer)sender).IsSplitterFixed = true;
        }

        private void splitContainer_Rows_MouseUp(object sender, MouseEventArgs e)
        {
            // This allows the splitter to be moved normally again
            ((SplitContainer)sender).IsSplitterFixed = false;
        }

        private void splitContainer_Rows_MouseMove(object sender, MouseEventArgs e) {
            // Check to make sure the splitter won't be updated by the
            // normal move behavior also
            if (((SplitContainer)sender).IsSplitterFixed)
            {
                // Make sure that the button used to move the splitter
                // is the left mouse button
                if (e.Button.Equals(MouseButtons.Left))
                {
                    // Checks to see if the splitter is aligned Vertically
                    if (((SplitContainer)sender).Orientation.Equals(Orientation.Vertical))
                    {
                        // Only move the splitter if the mouse is within
                        // the appropriate bounds
                        if (e.X > 0 && e.X < ((SplitContainer)sender).Width)
                        {
                            // Move the splitter & force a visual refresh
                            ((SplitContainer)sender).SplitterDistance = e.X;
                            ((SplitContainer)sender).Refresh();
                        }
                    }
                    // If it isn't aligned vertically then it must be
                    // horizontal
                    else
                    {
                        // Only move the splitter if the mouse is within
                        // the appropriate bounds
                        if (e.Y > 0 && e.Y < ((SplitContainer)sender).Height)
                        {
                            // Move the splitter & force a visual refresh
                            ((SplitContainer)sender).SplitterDistance = e.Y;
                            ((SplitContainer)sender).Refresh();
                        }
                    }
                }
                // If a button other than left is pressed or no button
                // at all
                else
                {
                    // This allows the splitter to be moved normally again
                    ((SplitContainer)sender).IsSplitterFixed = false;
                }
            }
        }

        ////Make sure in your form initialization you add: ====>>>>   this.KeyPreview = true
        private static bool shiftKeyDown = false;
        private static bool mouseInsideChartControl = false;
        private void GuiToWin_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.ShiftKey) {
                shiftKeyDown = true;
                if (mouseInsideChartControl) {
                    KChartHandler.ShowEndNames(!shiftKeyDown);
                    chartControl.Invalidate();
                }
            }
        }
        private void GuiToWin_KeyUp(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.ShiftKey) {
                shiftKeyDown = false;
                if (mouseInsideChartControl) {
                    KChartHandler.ShowEndNames(!shiftKeyDown);
                    chartControl.Invalidate();
                }
            }
        }
        //public static bool ShiftKeyDown() {
        //    //https://stackoverflow.com/questions/570577/detect-shift-key-is-pressed-without-using-events-in-windows-forms
        //    // True if shift is the only modifier key down, but another non-modifier key may be down too:
        //    return Control.ModifierKeys == Keys.Shift;
        //}

        //// For RichTextBox
        //// Overrides system Ctrl-V, which would paste a picture instead of text from clipboard
        //// no need to add this method as an event handler in the form
        ////https://stackoverflow.com/questions/173593/how-to-catch-control-v-in-c-sharp-app
        ////Make sure in your form initialization you add: ====>>>>   this.KeyPreview = true
        //protected override void OnKeyDown(KeyEventArgs e)
        //{
        //    if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control) {
        //        // MessageBox.Show("Hello world"); // test: works only if mainForm.KeyPreview = true
        //        txtInput.Paste(DataFormats.GetFormat(DataFormats.Text)); // assign clipboard to RichTextBox.SelectedText
        //        e.Handled = true; // prevents the system from doing another Ctrl-V after this action
        //    }
        //    base.OnKeyDown(e);
        //}
    }
}

//// Modal PopUp

//panel_ModalPopUp.BackColor = lighterBlue;
//panel_ModalPopUp.Visible = false;
//button_ModalPopUp_OK.BackColor = darkPurple;
//button_ModalPopUp_Cancel.BackColor = darkerBlue;

// ====  MODAL POPUP =====

//public class ModalPopUp {
//    Panel panel;
//    private Button okButton;
//    private EventHandler okHander;
//    private Button cancelButton;
//    private EventHandler cancelHandler;
//    public ModalPopUp(Panel panel) {
//        this.panel = panel;
//    }

//    public void PopUp(string line1, string line2, Action okAction, Action cancelAction) {
//        this.panel.Location = new Point(0, 0);
//        this.panel.Size = GUI2.ActiveForm.Size;

//        ((Label)this.panel.Controls.Find("label_ModalPopUpText", true)[0]).Text = line1;
//        ((Label)this.panel.Controls.Find("label_ModalPopUpText2", true)[0]).Text = line2;

//        okButton = (Button)this.panel.Controls.Find("button_ModalPopUp_OK", true)[0];
//        if (okAction == null) {
//            okHander = null;
//            okButton.Visible = false;
//        } else {
//            okHander = (object o, EventArgs e) => { okAction(); PopDown(); };
//            okButton.Click += okHander;
//            okButton.Visible = true;
//        }

//        cancelButton = (Button)this.panel.Controls.Find("button_ModalPopUp_Cancel", true)[0];
//        if (cancelAction == null) {
//            cancelHandler = null;
//            cancelButton.Visible = false;
//        } else {
//            cancelHandler = (object o, EventArgs e) => { cancelAction(); PopDown(); };
//            cancelButton.Click += cancelHandler;
//            cancelButton.Visible = true;
//        }

//        this.panel.BringToFront();
//        this.panel.Visible = true;
//    }

//    private void PopDown() {
//        okButton.Click -= okHander;
//        cancelButton.Click -= cancelHandler;
//        this.panel.Visible = false;
//    }
//}

//// OLD SETTINGS
///


//private void button_Settings_Click(object sender, EventArgs e) {
//     if (!panel_Settings.Visible) {
//         int buttonLocationX = panel2.Location.X + button_Settings.Location.X + 2;
//         int buttonLocationY = panel2.Location.Y + button_Settings.Location.Y + button_Settings.Size.Height;
//         panel_Settings.Location = new Point(
//             buttonLocationX - panel_Settings.Size.Width,
//             buttonLocationY - panel_Settings.Size.Height
//         );
//         button_Settings.BackColor = darkPurple;
//         panel_Settings.BringToFront();
//         panel_Settings.Visible = true;
//     } else {
//         panel_Settings.Visible = false;
//         button_Settings.BackColor = darkerBlue;
//     }
// }


//// OLD CHART
///

//this.panel_Splash.Location = this.chart1.Location;
//this.panel_Splash.Size = this.chart1.Size;


//public void ChartClear(string title) {
//    //ChartListboxClear();     // <<<====== Legend Reset
//    //this.chart1.Titles.Clear();
//    //this.chart1.Series.Clear();
//    //this.chart1.ChartAreas[0].AxisX.Minimum = 0;
//    //this.chart1.ChartAreas[0].AxisX.LabelStyle.Format = "G4";
//    //this.chart1.ChartAreas[0].AxisY.LabelStyle.Format = "G4";
//    //if ((title != null) && (title != "")) chart1.Titles.Add(title);
//    //foreach (var legend in this.chart1.Legends) {
//    //    legend.LegendItemOrder = LegendItemOrder.SameAsSeriesOrder;
//    //    // legend.Font = Program.chartFont;
//    //}
//    //// we are inserting series in reverse to get Red on top, and this will somehow actually reverse back the legend
//    //ChartSetNoGrid(ChartListboxRemembered(" <NoGrid> "));
//    //ChartSetAxes(ChartListboxRemembered(" <Axes> "));
//}

//public Series ChartSeriesNamed(string name) {
//    if (name == null) return null;
//    int index = this.chart1.Series.IndexOf(name);
//    if (index >= 0) return this.chart1.Series[index]; else return null;
//}
//public void ChartClearData() {
//    foreach (Series series in this.chart1.Series) series.Points.Clear();
//}
//public void ChartUpdate() {
//    this.chart1.Series.ResumeUpdates();
//    //this.chart1.Update();
//    this.chart1.Series.SuspendUpdates();
//}
//public Series ChartAddSeries(string legend, Color color, Noise noise) {
//    if (!this.chart1.Series.IsUniqueName(legend)) return null;
//    Series series = this.chart1.Series.Add(legend);
//    if (noise == Noise.None) {
//        series.ChartType = SeriesChartType.Line;
//        series.Color = color;
//        series.BorderWidth = 3; // line width
//    } else if (noise == Noise.Sigma || noise == Noise.SigmaSq || noise == Noise.CV || noise == Noise.Fano) {
//        series.ChartType = SeriesChartType.Line;
//        series.Color = color;
//        series.BorderWidth = 1; // line width
//    } else if (noise == Noise.SigmaRange || noise == Noise.SigmaSqRange) {
//        series.ChartType = SeriesChartType.Range;
//        series.Color = Color.FromArgb(this.Transparency(), color);
//    } else throw new Error("ChartAddSeries");
//    return series;
//}        //private void ChartSeriesVisible(string legend, bool visible, bool showMu, bool showSigma) {
//    Series series = ChartSeriesNamed(legend);
//    if (series != null) series.Enabled = visible;
//    foreach (Noise noise in Gui.noise) {
//        string noiseString = Gui.StringOfNoise(noise);
//        if (ChartListboxMu(noiseString)) {
//            Series seriesLNA = ChartSeriesNamed(legend + noiseString);
//            if (seriesLNA != null) seriesLNA.Enabled = visible && showMu;
//        }
//        if (ChartListboxSigma(noiseString)) {
//            Series seriesLNA = ChartSeriesNamed(legend + noiseString);
//            if (seriesLNA != null) seriesLNA.Enabled = visible && showSigma;
//        }
//    }
//}
//private void ChartSeriesVisible_KChart(string legend, bool visible, bool showMu, bool showSigma) {
//    timecourse.SetVisible(legend, visible);
//    //foreach (Noise noise in Gui.noise) {
//    //    string noiseString = Gui.StringOfNoise(noise);
//    //    if (ChartListboxMu(noiseString)) {
//    //        timecourse.SetVisible(legend + noiseString, visible && showMu);
//    //    }
//    //    if (ChartListboxSigma(noiseString)) {
//    //        timecourse.SetVisible(legend + noiseString, visible && showSigma);
//    //    }
//    //}
//    chartControl.Invalidate();
//    chartControl.Update();
//}

//public void ChartAddPoint(Series series, double t, double mean, double variance, Noise noise) {
//    if (double.IsNaN(mean) || double.IsNaN(variance)) return;
//    if (double.IsInfinity(mean) || double.IsInfinity(variance)) return;
//    if (series != null) {
//        int i = -1;
//        if (noise == Noise.None) i = series.Points.AddXY(t, mean);
//        if (noise == Noise.SigmaSq) i = series.Points.AddXY(t, variance);
//        if (noise == Noise.Sigma) i = series.Points.AddXY(t, Math.Sqrt(variance));
//        if (noise == Noise.CV) i = series.Points.AddXY(t, ((mean == 0.0) ? 0.0 : (Math.Sqrt(variance) / mean)));
//        if (noise == Noise.Fano) i = series.Points.AddXY(t, ((mean == 0.0) ? 0.0 : (variance / mean)));
//        if (noise == Noise.SigmaSqRange) i = series.Points.AddXY(t, mean - variance, mean + variance);
//        if (noise == Noise.SigmaRange) { double sd = Math.Sqrt(variance); i = series.Points.AddXY(t, mean - sd, mean + sd); }
//        if (i >= 0) series.Points[i].Tag = series.Name; // add tag to show the point's series when hovering on it
//    }
//}

//checkedListBox_Series.BackColor = palePurple;
//checkedListBox_Series.Visible = false;
//checkedListBox_Series.BringToFront();

//this.chart1.SuppressExceptions = true;
//this.chart1.MouseWheel += chart1_MouseWheel; // for zooming
//this.chart1.ChartAreas[0].AxisX.ScrollBar.Enabled = false; // not really useful when zooming
//this.chart1.ChartAreas[0].AxisY.ScrollBar.Enabled = false; // not really useful when zooming
//this.chart1.MouseDown += chart1_MouseDown; // for scrolling
//this.chart1.MouseMove += chart1_MouseMove; // for scrolling
//this.chart1.MouseUp += chart1_MouseUp; // for scrolling
//this.chart1.DoubleClick += chart1_DoubleClick; // for resetting zoom/scroll


//public string ChartAddPointAsString(Series series, double t, double mean, double variance, Noise noise) {
//    // do what ChartAddPoint does, but return it as a string for exporting/printing data
//    if (double.IsNaN(mean) || double.IsNaN(variance)) return "";
//    if (double.IsInfinity(mean) || double.IsInfinity(variance)) return "";
//    string s = "";
//    if (series != null) {
//        s += series.Name + "=";
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

//private void ChartSetNoGrid(bool checkd) {
//    if (checkd) {
//        this.chart1.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
//        this.chart1.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;
//    } else {
//        this.chart1.ChartAreas[0].AxisX.MajorGrid.LineWidth = 1;
//        this.chart1.ChartAreas[0].AxisY.MajorGrid.LineWidth = 1;
//    }
//}

//private void ChartSetAxes(bool checkd) {
//    if (checkd) {
//        chart1.ChartAreas[0].AxisX.Enabled = AxisEnabled.True;
//        chart1.ChartAreas[0].AxisY.Enabled = AxisEnabled.True;
//    } else {
//        chart1.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
//        chart1.ChartAreas[0].AxisY.Enabled = AxisEnabled.False;
//    }
//}

//public void ChartListboxClear() {
//    ChartListboxRemember();
//    checkedListBox_Series.Items.Clear();
//    checkedListBox_Series.ColumnWidth = 100;
//    CheckedListboxAdd(" <Axes> ", ChartListboxRemembered(" <Axes> "));
//    CheckedListboxAdd(" <NoGrid> ", ChartListboxRemembered(" <NoGrid> "));
//    if (NoiseSeries() != Noise.None) {
//        CheckedListboxAdd(" <show μ> ", ChartListboxRemembered(" <show μ> "));
//        CheckedListboxAdd(" <show σ> ", ChartListboxRemembered(" <show σ> "));
//    }
//    CheckedListboxAdd(" <ALL species> ");
//    ChartListboxRestore(); // reflect those series just added
//}

//private int CheckedListboxAdd(string s, bool checkd = true) {
//    int checkboxsize = 30;
//    int slen = TextRenderer.MeasureText(s, checkedListBox_Series.Font).Width + checkboxsize;
//    // need to consider size of checkboxes: 30 will work up to 300% DPI.
//    // N.B. needs to start the app at that DPI to see the real effect at that DPI (not just switching DPI with the app open)
//    checkedListBox_Series.ColumnWidth = Math.Max(checkedListBox_Series.ColumnWidth, slen);
//    int index = checkedListBox_Series.Items.Add(s);
//    checkedListBox_Series.SetItemChecked(index, checkd);
//    return index;
//}

//private static Dictionary<string, bool> chartListboxRemember =
//    new Dictionary<string, bool>();
//private bool ChartListboxRemembered(string item) {
//    if (chartListboxRemember.ContainsKey(item)) return chartListboxRemember[item]; else return true;
//}
//private void ChartListboxRemember() {
//    foreach (var item in checkedListBox_Series.Items)
//        if (!ChartListboxAll(item.ToString())) chartListboxRemember[item.ToString()] = false;
//    foreach (var item in checkedListBox_Series.CheckedItems)
//        if (!ChartListboxAll(item.ToString())) chartListboxRemember[item.ToString()] = true;
//}
//public void ChartListboxRestore() {
//    foreach (var keyPair in chartListboxRemember) {
//        int i = checkedListBox_Series.Items.IndexOf(keyPair.Key);
//        if (i >= 0) checkedListBox_Series.SetItemChecked(i, keyPair.Value);
//    }
//}
//private void ChartListboxForget() {
//    chartListboxRemember = new Dictionary<string, bool>();
//    int i = checkedListBox_Series.Items.IndexOf(" <ALL species> ");
//    if (i >= 0) {
//        checkedListBox_Series.SetItemChecked(i, false);
//        checkedListBox_Series.SetItemChecked(i, true);
//    }
//    i = checkedListBox_Series.Items.IndexOf(" <show μ> ");
//    if (i >= 0) {
//        checkedListBox_Series.SetItemChecked(i, false);
//        checkedListBox_Series.SetItemChecked(i, true);
//    }
//    i = checkedListBox_Series.Items.IndexOf(" <show σ> ");
//    if (i >= 0) {
//        checkedListBox_Series.SetItemChecked(i, false);
//        checkedListBox_Series.SetItemChecked(i, true);
//    }
//    i = checkedListBox_Series.Items.IndexOf(" <Axes> ");
//    if (i >= 0) {
//        checkedListBox_Series.SetItemChecked(i, false);
//        checkedListBox_Series.SetItemChecked(i, true);
//    }
//    i = checkedListBox_Series.Items.IndexOf(" <NoGrid> ");
//    if (i >= 0) {
//        checkedListBox_Series.SetItemChecked(i, false);
//        checkedListBox_Series.SetItemChecked(i, true);
//    }
//}
//private bool ChartListboxChecked(string legend) {
//    int i = checkedListBox_Series.Items.IndexOf(legend);
//    if (i >= 0) return checkedListBox_Series.GetItemChecked(i);
//    else return false;
//}
//private void ChartListboxSet(string legend, bool state) {
//    int i = checkedListBox_Series.Items.IndexOf(legend);
//    if (i >= 0) checkedListBox_Series.SetItemChecked(i, state);
//}
//public void ChartListboxAddSeries(string legend) {
//    CheckedListboxAdd(legend);
//    ChartListboxRestore();
//}

//private void checkedListBox_Series_SelectedIndexChanged(object sender, EventArgs e) {
//}
//private void checkedListBox_Series_ItemCheck(object sender, ItemCheckEventArgs e)
//{ // the checkbox change happens AFTER this event, so we must infer what changed from 'e'.
//    //string legend = checkedListBox_Series.Items[e.Index].ToString();
//    //if (ChartListboxAxes(legend)) {
//    //    //ChartSetAxes(e.NewValue == CheckState.Checked);
//    //} else if (ChartListboxNoGrid(legend)) {
//    //    //ChartSetNoGrid(e.NewValue == CheckState.Checked);
//    //} else if (ChartListboxAll(legend)) {
//    //    List<string> items = new List<string>(); // copy enumerator because it will change while iterating
//    //    foreach (var item in checkedListBox_Series.Items) items.Add(item.ToString());
//    //    foreach (var item in items) {
//    //        if (!ChartListboxGroup(item)) {
//    //            int i = checkedListBox_Series.Items.IndexOf(item);
//    //            checkedListBox_Series.SetItemChecked(i, e.NewValue == CheckState.Checked);
//    //        }
//    //    }
//    //} else if (ChartListboxShowMu(legend)) {
//    //    List<string> items = new List<string>(); // copy enumerator because it will change while iterating
//    //    foreach (var item in checkedListBox_Series.Items) items.Add(item.ToString());
//    //    foreach (var item in items) {
//    //        if (!ChartListboxGroup(item)) {
//    //            int i = checkedListBox_Series.Items.IndexOf(item);
//    //            //ChartSeriesVisible(item, checkedListBox_Series.GetItemChecked(i), e.NewValue == CheckState.Checked, ChartListboxChecked(" <show σ> "));
//    //            ChartSeriesVisible_KChart(item, checkedListBox_Series.GetItemChecked(i), e.NewValue == CheckState.Checked, ChartListboxChecked(" <show σ> "));
//    //        }
//    //    }
//    //    //if (e.NewValue == CheckState.Unchecked && !ChartListboxChecked(" <show σ> ")) ChartListboxSet(" <show σ> ", true); // does not work
//    //} else if (ChartListboxShowSigma(legend)) {
//    //    List<string> items = new List<string>(); // copy enumerator because it will change while iterating
//    //    foreach (var item in checkedListBox_Series.Items) items.Add(item.ToString());
//    //    foreach (var item in items) {
//    //        if (!ChartListboxGroup(item)) {
//    //            int i = checkedListBox_Series.Items.IndexOf(item);
//    //            //ChartSeriesVisible(item, checkedListBox_Series.GetItemChecked(i), ChartListboxChecked(" <show μ> "), e.NewValue == CheckState.Checked);
//    //            ChartSeriesVisible_KChart(item, checkedListBox_Series.GetItemChecked(i), ChartListboxChecked(" <show μ> "), e.NewValue == CheckState.Checked);
//    //        }
//    //    }
//    //    //if (e.NewValue == CheckState.Unchecked && !ChartListboxChecked(" <show μ> ")) ChartListboxSet(" <show μ> ", true); // does not work
//    //} else {
//    //    //ChartSeriesVisible(legend, e.NewValue == CheckState.Checked, ChartListboxChecked(" <show μ> "), ChartListboxChecked(" <show σ> "));
//    //    ChartSeriesVisible_KChart(legend, e.NewValue == CheckState.Checked, ChartListboxChecked(" <show μ> "), ChartListboxChecked(" <show σ> "));
//    //}
//    //// refit the chart to the existing visible data
//    ////chart1.ChartAreas[0].RecalculateAxesScale();
//}

//private static bool ChartListboxGroup(string str) {
//    return ChartListboxAll(str) || ChartListboxShowMu(str) || ChartListboxShowSigma(str);
//}
//private static bool ChartListboxAxes(string str) {
//    return str == " <Axes> ";
//}
//private static bool ChartListboxNoGrid(string str) {
//    return str == " <NoGrid> ";
//}
//private static bool ChartListboxAll(string str) {
//    return str == " <ALL species> ";
//}
//private static bool ChartListboxMu(string str) {
//    return str.Contains("μ") && !str.Contains("σ");
//}
//private static bool ChartListboxShowMu(string str) {
//    return str == " <show μ> ";
//}
//private static bool ChartListboxSigma(string str) {
//    return str.Contains("σ");
//}
//private static bool ChartListboxShowSigma(string str) {
//    return str == " <show σ> ";
//}

//public void ChartSnap(bool toClipboard, bool toDisk) {
//    try {
//        if (toDisk) {
//            //== Save a .emf file to Application.StartupPath directory
//            chart1.SaveImage(outputDirectory + "\\chart.emfplus.emf", ChartImageFormat.EmfPlus); // InkScape cannot read EmfPlus at all
//            chart1.SaveImage(outputDirectory + "\\chart.emf", ChartImageFormat.Emf);             // Emf (and InkScape) does not deal well with shaded areas
//        }
//    } catch {
//        new ModalPopUp(panel_ModalPopUp).PopUp("Could not write to directory: " + outputDirectory, "(Change it in Settings.)", () => { }, null);
//    }

//    try {
//        if (toClipboard) {
//            //== Save a .emf file to the Clipboard
//            using (MemoryStream stream = new MemoryStream()) {
//                this.chart1.SaveImage(stream, ChartImageFormat.EmfPlus); // can paste EmfPlus into Powerpoint
//                // this.chart1.SaveImage(stream, ChartImageFormat.Emf);  // Emf does not deal well with shaded areas
//                stream.Seek(0, SeekOrigin.Begin);
//                Metafile metafile = new Metafile(stream);
//                //Clipboard.SetDataObject(metafile, true); // this should work but apparently does not paste correctly to Windows applications
//                ClipboardMetafileHelper.PutEnhMetafileOnClipboard(this.Handle, metafile);
//            }
//        }
//    } catch {
//        new ModalPopUp(panel_ModalPopUp).PopUp("Could not write to clipboard", "", () => { }, null);
//    }
//}

//public void ChartData() {
//    var path = outputDirectory + "\\ChartData.csv";
//    try {
//        string csvContent = "";
//        var theSeries = chart1.Series.ToArray();
//        for (int s = theSeries.Length-1; s >= 0; s--) {
//            var series = theSeries[s];
//            if (series.Enabled) {
//                string seriesName = series.Name;
//                int pointCount = series.Points.Count;
//                for (int p = 0; p < pointCount; p++) {
//                    DataPoint point = series.Points[p];
//                    string yValuesCSV = String.Empty;
//                    int count = point.YValues.Length;
//                    for (int i = 0; i < count; i++) {
//                        yValuesCSV += point.YValues[i];
//                        if (i != count - 1)
//                            yValuesCSV += ",";
//                    }
//                    var csvLine = seriesName + "," + point.XValue + "," + yValuesCSV;
//                    csvContent += csvLine + Environment.NewLine;
//                }
//            }
//        }
//        System.IO.StreamWriter file = new System.IO.StreamWriter(path);
//        file.WriteLine(csvContent);
//        file.Close();
//        new ModalPopUp(panel_ModalPopUp).PopUp("Chart data written to this file: " + path, "(Change directory in Settings.)", () => { }, null);
//    } catch {
//        new ModalPopUp(panel_ModalPopUp).PopUp("Could not write chart data to this file: " + path, "(Change directory in Settings.)", () => { }, null);
//    }
//}

//public override void ChartData() {
//    // because of sandboxing, we must use the official Save Dialog to automatically create an exception and allow writing the file?
//    var dlg = new NSSavePanel ();
//    dlg.Title = "Save CSV File";
//    dlg.AllowedFileTypes = new string[] { "csv" };
//    dlg.Directory = GUI_Mac.modelsDirectory;
//    if (dlg.RunModal () == 1) {
//        var path = "";
//        try {
//            path = dlg.Url.Path;
//            File.WriteAllText(path, NSChartView.theChart.ToCSV(), System.Text.Encoding.Unicode);
//        } catch {
//            var alert = new NSAlert () {
//                AlertStyle = NSAlertStyle.Critical,
//                MessageText = "Could not write this file:",
//                InformativeText = path
//            };
//            alert.RunModal ();
//        }
//    }
//}

//private void chart1_Click(object sender, EventArgs e)
//{
////
//}

//private void chart1_SizeChanged(object sender, EventArgs e)
//{
//    //this.panel_Splash.Size = this.chart1.Size;
//}



