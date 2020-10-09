using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using SkiaSharp;
using SkiaSharp.Views.Desktop; // for Extensions.ToBitmap method
using Kaemika;

namespace KaemikaWPF {

    public class WinControls : GuiControls {

        // User Directory

        public static string modelsDirectory = string.Empty;
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

        // Colors

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

        // Controls

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
        public WinControls() {
            modelsDirectory = Environment.GetFolderPath(defaultUserDataDirectoryPath);
            onOffStop = MenuButton(WinGui.winGui.btnStop);
            onOffEval = MenuButton(WinGui.winGui.btnEval);
            onOffDevice = MenuButton(WinGui.winGui.button_Device);
            onOffDeviceView = MenuButton(WinGui.winGui.button_FlipMicrofluidics);
            onOffFontSizePlus = MenuButton(WinGui.winGui.button_FontSizePlus);
            onOffFontSizeMinus = MenuButton(WinGui.winGui.button_FontSizeMinus);
            onOffSave = MenuButton(WinGui.winGui.button_Source_Copy);
            onOffLoad = MenuButton(WinGui.winGui.button_Source_Paste);
            menuTutorial = new WinFlyoutMenu(WinGui.winGui.button_Tutorial, WinGui.winGui.tableLayoutPanel_Tutorial, WinGui.winGui.panel1,FlyoutAttachment.RightTop, 9.0F, false, new Padding(4),
                cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
            menuNoise = new WinFlyoutMenu(WinGui.winGui.button_Noise, WinGui.winGui.tableLayoutPanel_Noise, WinGui.winGui.panel2, FlyoutAttachment.LeftDown, 10.0F, false, new Padding(1),
                cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
            menuOutput = new WinFlyoutMenu(WinGui.winGui.button_Output, WinGui.winGui.tableLayoutPanel_Output, WinGui.winGui.panel2, FlyoutAttachment.LeftDown, 12.0F, false, new Padding(4),
                cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
            menuExport = new WinFlyoutMenu(WinGui.winGui.button_Export, WinGui.winGui.tableLayoutPanel_Export, WinGui.winGui.panel1, FlyoutAttachment.RightDown, 11.0F, false, new Padding(4),
                cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
            menuMath = new WinFlyoutMenu(WinGui.winGui.button_Math, WinGui.winGui.tableLayoutPanel_Math, WinGui.winGui.panel1, FlyoutAttachment.RightDown, 12.0F, false, new Padding(1),
                cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
            menuLegend = new WinFlyoutMenu(WinGui.winGui.button_EditChart, WinGui.winGui.tableLayoutPanel_Legend, WinGui.winGui.panel2, FlyoutAttachment.TextOutputRight, 9.0F, false, new Padding(8,0,8,0),
                cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cPanelButtonText, cPanelButtonDeselected, cPanelButtonSelected);
            menuParameters = new WinFlyoutMenu(WinGui.winGui.button_Parameters, WinGui.winGui.tableLayoutPanel_Parameters, WinGui.winGui.panel1, FlyoutAttachment.TextOutputLeft, 9.0F, false, new Padding(4),
                cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cPanelButtonText, cPanelButtonDeselected, cPanelButtonSelected);
            menuSettings = new WinFlyoutMenu(WinGui.winGui.button_Settings, WinGui.winGui.tableLayoutPanel_Settings, WinGui.winGui.panel2, FlyoutAttachment.LeftUp, 12.0F, false, new Padding(4),
                cMainButtonText, cMainButtonDeselected, cMainButtonSelected, cMenuButtonText, cMenuButtonDeselected, cMenuButtonSelected);
        }
        private KButton MenuButton(Button button) {
            return new WinButton(AutoSizeButton(button), cMainButtonText, cMainButtonDeselected, cMainButtonSelected);
        }
        public bool IsShiftDown() {
            return WinGui.winGui.IsShiftDown();
        }
        public bool IsMicrofluidicsVisible() { 
            return WinGui.winGui.panel_Microfluidics.Visible; 
        }
        public void MicrofluidicsVisible(bool on) {
            WinGui.winGui.panel_Microfluidics.Visible = on;
        }
        public void MicrofluidicsOn() {
            DeviceSKControl.SetSize(WinGui.winGui.panel_Microfluidics.Size);
            WinGui.winGui.panel_Microfluidics.BringToFront();
            WinGui.winGui.panel_Microfluidics.Visible = true;
        }
        public void MicrofluidicsOff() {
            WinGui.winGui.panel_Microfluidics.Visible = false;
        }
        public void IncrementFont(float pointSize) {
            SetTextFont(WinGui.winGui.txtInput.Font.Size + pointSize, true);
        }
        public void PrivacyPolicyToClipboard() {
            Clipboard.SetText("http://lucacardelli.name/Artifacts/Kaemika/KaemikaUWP/privacy_policy.html");
        }
        public void SplashOff() {
            WinGui.winGui.panel_Splash.Visible = false;
        }
        public void Save() {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog()) {
                saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.InitialDirectory = modelsDirectory;
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = false;
                if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                    try {
                        File.WriteAllText(saveFileDialog.FileName, KGui.gui.GuiInputGetText(), System.Text.Encoding.Unicode);
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
                        KGui.gui.GuiInputSetText(File.ReadAllText(openFileDialog.FileName, System.Text.Encoding.Unicode));
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
                    SavePreferences();
                    MessageBox.Show(folderPath, "Directory set to:", MessageBoxButtons.OK);
                }
            }
        }
        public void SavePreferences() {
            try {
                string path2 = CreateKaemikaDataDirectory() + "\\modelsdir.txt";
                File.WriteAllText(path2, modelsDirectory);
            } catch (Exception) { }
            try {
                string path2 = CreateKaemikaDataDirectory() + "\\outputaction.txt";
                File.WriteAllText(path2, Exec.currentOutputAction.name);
            } catch (Exception) { }
        }
        public void RestorePreferences() {
            try {
                string path2 = CreateKaemikaDataDirectory() + "\\modelsdir.txt";
                if (File.Exists(path2)) { modelsDirectory = File.ReadAllText(path2); }
            } catch (Exception) { }
            try {
                string path2 = CreateKaemikaDataDirectory() + "\\outputaction.txt";
                KGui.kControls.SetOutputSelection(File.Exists(path2) ? File.ReadAllText(path2) : "");
            } catch (Exception) { KGui.kControls.SetOutputSelection(""); } // set to default
        }
        public static Button AutoSizeButton(Button button) {
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.UseVisualStyleBackColor = false;
            button.Padding = new Padding(0);
            button.Margin = new Padding(0);
            return button;
        }
        public static void SetTextFont(float size, bool fixedWidth) {
            if (size >= 6) {
                Font font = WinGui.winGui.GetFont(size, fixedWidth);
                WinGui.winGui.txtInput.Font = font;
                WinGui.winGui.txtOutput.Font = font;
            }
        }
        public void SetSnapshotSize() {
            WinGui.winGui.SetSnapshotSize();
        }
    }
    
    // ====== Controls ======

    public class WinControl : KControl {
        public Control control;
    }

    // ====  COMMON ONOFF BUTTONS =====

    public class WinButton : WinControl, KButton {
        public Button button;
        private Color textColor;
        private Color backgroundColor;
        private Color selectedColor;
        private Color saveMouseOverBackColor;
        private Color saveMouseDownBackColor;
        private string toolTip;
        public WinButton(Button button, Color textColor, Color backgroundColor, Color selectedColor) {
            this.control = button;
            this.button = button;
            this.button.ForeColor = textColor;
            this.button.BackColor = backgroundColor;
            this.textColor = textColor;
            this.backgroundColor = backgroundColor;
            this.selectedColor = selectedColor;
            this.saveMouseOverBackColor = button.FlatAppearance.MouseOverBackColor;
            this.saveMouseDownBackColor = button.FlatAppearance.MouseDownBackColor;
            this.toolTip = "";
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
            int height = button.Font.Height; const int width = 50; const int padding = 0; const int frame = 3; const int left = 0;
            int thickness = (series.lineStyle == KLineStyle.Thick) ? 3 : (series.lineMode == KLineMode.Line) ? 1 : (height - 2*frame - 1);
            int framedH = thickness + 2 * frame; int framedY = (height - 2 * padding - framedH) / 2;
            int framedW = width - 2 * padding; int framedX = left + padding;
            using (SKBitmap skBitmap = new SKBitmap(left + width, height))
            using (SKCanvas skCanvas = new SKCanvas(skBitmap))
            using (SKPaint back = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(backgroundColor.R, backgroundColor.G, backgroundColor.B, backgroundColor.A) })
            using (SKPaint white = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(255, 255, 255, 255) })
            using (SKPaint color = new SKPaint { Style = SKPaintStyle.Fill, Color = series.color }) {
                skCanvas.DrawRect(new SKRect(0, 0, left + width, height), back);
                if (series.visible) {
                    skCanvas.DrawRect(new SKRect(framedX, framedY, framedX + framedW, framedY + framedH), white);
                    skCanvas.DrawRect(new SKRect(framedX + frame, framedY + frame, framedX + framedW - frame, framedY + frame + thickness), color);
                }
                sdImage = skBitmap.ToBitmap();
            }
            this.button.Image = sdImage;
            this.button.AutoSize = false;
            this.button.Size = this.button.Image.Size;
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
        public void SetToolTip(string toolTip) {
            this.toolTip = toolTip;
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
        public void OnClick(EventHandler handler) {
            this.button.Click += handler;
        }
    }

    public class WinSlider : WinControl, KSlider {
        public TrackBar trackbar;
        public WinSlider(TrackBar trackbar) {
            this.control = trackbar;
            this.trackbar = trackbar;
            this.trackbar.TickStyle = TickStyle.None;
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

    public class WinNumerical : WinControl, KNumerical {
        public TextBox numerical;
        public double lo;
        public double hi;
        public WinNumerical(TextBox numerical) {
            this.control = numerical;
            this.numerical = numerical;
            this.control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.lo = double.MinValue;
            this.hi = double.MaxValue;
            this.numerical.Text = "NaN";
        }
        public void SetBounds(double min, double max) {
            lo = min;
            hi = max;
        }
        public void SetValue(double value) {
            value = Math.Min(value, hi);
            value = Math.Max(value, lo);
            this.numerical.Text = value.ToString();
        }
        public double GetValue() {
            try { return double.Parse(this.numerical.Text); } catch { return double.NaN; }
        }
        public void OnClick(EventHandler handler) {
            this.numerical.TextChanged += handler;
        }
    }

    public enum FlyoutAttachment { RightDown, LeftDown, RightUp, LeftUp, RightTop, LeftTop, TextOutputLeft, TextOutputRight };

    public class WinFlyoutMenu : WinButton, KFlyoutMenu {
        private Panel buttonBar;
        private TableLayoutPanel menu;
        private Dictionary<string, KButton> namedControls;
        private FlyoutAttachment attachment;
        public bool autoClose { get; set; }
        private float pointSize;
        private bool fixedWidth;
        private Color cMenuButtonText;
        private Color cMenuButtonDeselected;
        private Color cMenuButtonSelected;
        public KButton selectedItem { get; set; }
        public WinFlyoutMenu(Button button, TableLayoutPanel menu, Panel buttonBar, FlyoutAttachment attachment, float pointSize, bool fixedWidth, Padding padding,
                    Color cMainButtonText, Color cMainButtonDeselected, Color cMainButtonSelected, Color cMenuButtonText, Color cMenuButtonDeselected, Color cMenuButtonSelected)
                    : base(button, cMainButtonText, cMainButtonDeselected, cMainButtonSelected) {
            this.menu = menu;
            this.namedControls = new Dictionary<string, KButton>();
            this.menu.Margin = new Padding(0);
            this.menu.Padding = padding;
            this.buttonBar = buttonBar;
            this.autoClose = false;
            this.pointSize = pointSize;
            this.fixedWidth = fixedWidth;
            this.attachment = attachment;

            this.cMenuButtonText = cMenuButtonText;
            this.cMenuButtonDeselected = cMenuButtonDeselected;
            this.cMenuButtonSelected = cMenuButtonSelected;
            menu.BackColor = cMenuButtonDeselected;
            menu.AutoSize = true;
            menu.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            menu.AutoScroll = false; // or it will flow horizontally and add a scrollbar
            menu.Visible = false;
            menu.BringToFront();
        }
        public void ClearMenuItems() {
            this.menu.Controls.Clear();
            this.namedControls = new Dictionary<string, KButton>();
            this.menu.RowStyles.Clear();    // or it makes equal-height rows
            this.menu.ColumnStyles.Clear(); // or it makes equal-width columns
            this.menu.RowCount = 0;
            this.menu.ColumnCount = 1;
        }
        public void SetSelection(string name) {
            if (!this.namedControls.ContainsKey(name)) return;
            KButton control = this.namedControls[name];
            KControls.ItemSelected(this, control);
        }
        private void AddMenuControl(Control control) { // row++, col=1
            this.menu.Controls.Add(control);
            this.menu.RowCount++;
            this.menu.SetRow(control, this.menu.RowCount - 1);
            this.menu.SetColumn(control, 1);
        }
        public void AddMenuItem(KControl item, string name = null) { // to column 1
            if (name != null && item is KButton asKButton) this.namedControls[name] = asKButton;
            AddMenuControl(((WinControl)item).control);
        }
        public void AddMenuItems(KControl[] items) { // to column 1
            FlowLayoutPanel rowItems = new FlowLayoutPanel();
            rowItems.FlowDirection = FlowDirection.LeftToRight;
            rowItems.AutoSize = true;
            rowItems.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            rowItems.BorderStyle = BorderStyle.None;
            rowItems.Padding = new Padding(0, 0, 0, 0);
            rowItems.Margin = new Padding(0, 0, 0, 0);
            for (int i = 0; i < items.Length; i++)
                rowItems.Controls.Add(((WinControl)items[i]).control);
            AddMenuControl(rowItems);
        }
        public void AddMenuRow(KControl[] items) {
            this.menu.RowCount++;
            this.menu.ColumnCount = Math.Max(this.menu.ColumnCount, items.Length);
            for (int i = 0; i < items.Length; i++) {
                Control control = ((WinControl)items[i]).control;
                this.menu.Controls.Add(control);
                this.menu.SetRow(control, this.menu.RowCount - 1);
                this.menu.SetColumn(control, i);
            }
        }
        public void AddMenuGrid(KControl[,] items) {
            var colNo = items.GetLength(0);
            var rowNo = items.GetLength(1);
            for (int r = 0; r < rowNo; r++) {
                var row = new KControl[colNo];
                for (int c = 0; c < colNo; c++) row[c] = items[c, r];
                AddMenuRow(row);
            }
        }
        public void AddSeparator() { // to 1-column grids, or else try to use SetRowSpan
            var separator = new Label();
            separator.AutoSize = false;
            separator.Text = "";
            separator.BorderStyle = BorderStyle.Fixed3D; // or FixedSingle then can change BackgroundColor
            separator.Height = 2;
            separator.Width = 2;
            separator.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            AddMenuControl(separator);
        }
        public KButton NewMenuSection(int level = 1) {
            var sectionButton = new WinButton(new TightButton(false, false), WinControls.cMenuButtonHotText, this.cMenuButtonDeselected, this.cMenuButtonSelected);
            sectionButton.SetFont(WinGui.winGui.GetFont(this.pointSize - (level - 1), this.fixedWidth));
            return sectionButton;
        }
        public KButton NewMenuItemButton(bool multiline = false) {
            var itemButton = new WinButton(new TightButton(true, multiline), this.cMenuButtonText, this.cMenuButtonDeselected, this.cMenuButtonSelected);
            itemButton.SetFont(WinGui.winGui.GetFont(this.pointSize, this.fixedWidth));
            return itemButton;
        }
        public KSlider NewMenuItemTrackBar() {
            return new WinSlider(new TrackBar());
        }
        public KNumerical NewMenuItemNumerical() {
            return new WinNumerical(new TextBox());
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
            else if (this.attachment == FlyoutAttachment.TextOutputRight) this.menu.Location = new Point(WinGui.winGui.txtOutput.Size.Width - this.menu.Width, 0);
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

    public class TightButton : Button {
        bool mouseEntered;
        string ownText;
        StringFormat ownFormat;
        bool hover;
        bool multiline;
        public TightButton(bool hover, bool multiline) : base() {
            this.hover = hover;
            this.multiline = multiline;
            mouseEntered = false;
            ownText = "";
            this.ownFormat = new StringFormat();
            this.ownFormat.Alignment = StringAlignment.Near;
            this.ownFormat.LineAlignment = StringAlignment.Near;
            WinControls.AutoSizeButton(this); // but autosize only until a Text is assigned, or if multiline
        }
        public override string Text { get { return ownText; } 
            set{
                ownText = value;
                if (Image == null && !multiline) {
                    // Set the tight button size
                    this.AutoSize = false;
                    this.Size = new Size(this.PreferredSize.Width, this.Font.Height);
                }
            } 
        }
        private Color HiLi(Color c) {
            int r = 8;
            if (c.R + c.G + c.B > 382)
                return Color.FromArgb(255, c.R - c.R / r, c.G - c.G / r, c.B - c.B / r);
            else return Color.FromArgb(255, c.R + (255-c.R)/r, c.G + (255 - c.G) / r, c.B + (255 - c.B) /r);
        }
        protected override void OnPaint(PaintEventArgs e) {
            //base.OnPaint(e);  // don't do it
            const int textPadding = 4;
            using (Pen p = new Pen((hover && mouseEntered) ? HiLi(BackColor) : BackColor)) 
                e.Graphics.FillRectangle(p.Brush, ClientRectangle);
            if (Image != null) e.Graphics.DrawImage(Image, new Point(0, 0));
            else using (Pen p = new Pen(ForeColor)) {
                    Rectangle rect = new Rectangle(ClientRectangle.X + textPadding, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
                    e.Graphics.DrawString(ownText, Font, p.Brush, rect, ownFormat);
                }
        }
        protected override void OnMouseEnter(EventArgs e) {
            base.OnMouseEnter(e);
            this.mouseEntered = true; Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            this.mouseEntered = false; Invalidate();
        }
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

