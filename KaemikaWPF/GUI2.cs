using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using Kaemika;

namespace KaemikaWPF
{
    // This all runs in the gui thread: external-thread calls should be made through GUI_Interface.
    public partial class GUI2 : Form
    {
        public static Font kaemikaFont = new Font("Matura MT Script Capitals", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        public static string fontFamily = "Lucida Sans Unicode"; // "Lucida Sans Typewriter" does not handle Unicode well
        public static Font textFont = new Font(fontFamily, 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        public static Font font8 = new Font(fontFamily, 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        public static Font font9 = new Font(fontFamily, 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        public static Font font10 = new Font(fontFamily, 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

        public static Color lighterBlue = Color.FromArgb(255, 51, 153, 255);
        public static Color darkerBlue = Color.FromArgb(255, 0, 122, 204);
        public static Color darkPurple = Color.FromArgb(255, 104, 33, 122);
        public static Color palePurple = Color.FromArgb(255, 251, 239, 255);
        public static Color whiteText = Color.White;
        public static Color hotText = Color.HotPink;
        public static Color buttonGrey = Color.FromArgb(255, 127, 127, 127);

        // ====  MODAL POPUP =====

        public class ModalPopUp {
            Panel panel;
            private Button okButton;
            private EventHandler okHander;
            private Button cancelButton;
            private EventHandler cancelHandler;
            public ModalPopUp(Panel panel) {
                this.panel = panel;
            }

            public void PopUp(string line1, string line2, Action okAction, Action cancelAction) {
                this.panel.Location = new Point(0, 0);
                this.panel.Size = GUI2.ActiveForm.Size;

                ((Label)this.panel.Controls.Find("label_ModalPopUpText", true)[0]).Text = line1;
                ((Label)this.panel.Controls.Find("label_ModalPopUpText2", true)[0]).Text = line2;

                okButton = (Button)this.panel.Controls.Find("button_ModalPopUp_OK", true)[0];
                if (okAction == null) {
                    okHander = null;
                    okButton.Visible = false;
                } else { 
                    okHander = (object o, EventArgs e) => { okAction(); PopDown(); };
                    okButton.Click += okHander;
                    okButton.Visible = true;
                }

                cancelButton = (Button)this.panel.Controls.Find("button_ModalPopUp_Cancel", true)[0];
                if (cancelAction == null) {
                    cancelHandler = null;
                    cancelButton.Visible = false;
                } else { 
                    cancelHandler = (object o, EventArgs e) => { cancelAction(); PopDown(); };
                    cancelButton.Click += cancelHandler;
                    cancelButton.Visible = true;
                }

                this.panel.BringToFront();
                this.panel.Visible = true;
            }

            private void PopDown() {
                okButton.Click -= okHander;
                cancelButton.Click -= cancelHandler;
                this.panel.Visible = false;
            }
        }

        // ====  FLYOUT MENUS =====

        public delegate void Handler<T>(FlyoutMenu flyoutMenu, T data);  // callback for flyout-menu selections with user data

        public class ButtonHandler : Button {     // subclass of Button with HandleData method to handle user data via callback
            public ButtonHandler() : base() { }   // an indirection for ButtonPlus<T> for any T, so we can cast then inside selectionHandler
            public virtual FlyoutMenu FlyoutMenu() { return null; }
            public virtual bool NullHandler() { return true;  }
            public virtual void HandleData() { }
        }
        public class ButtonPlus<T> : ButtonHandler {  // subclass of Button with HandleData method to handle T user data via T callback
            private FlyoutMenu flyoutMenu;
            private T data;
            private Handler<T> handler;
            public ButtonPlus(FlyoutMenu menu, T data, Handler<T> handler) : base() {
                this.flyoutMenu = menu;
                this.data = data;
                this.handler = handler;
            }
            public override FlyoutMenu FlyoutMenu() { return this.flyoutMenu; }
            public override bool NullHandler() { return this.handler == null; }
            public override void HandleData() { this.handler(this.flyoutMenu, this.data); }
        }

        public enum FlyoutLocation { Right, Left, RightTop, LeftTop };

        public static List<FlyoutMenu> allFlyoutMenus = new List<FlyoutMenu> { };

        public class FlyoutMenu {
            public Panel buttonBar;
            public Button button;
            public FlowLayoutPanel menu;
            FlyoutLocation menuLocation;
            public FlyoutMenu(Panel buttonBar, Button button, bool enabled, FlowLayoutPanel menu, bool autoSizeMenu, FlyoutLocation menuLocation) {
                this.buttonBar = buttonBar;
                this.buttonBar.BackColor = darkerBlue;
                this.button = button;
                this.button.BackColor = darkerBlue;
                this.button.Enabled = enabled;
                this.button.Click += this.button_Click;
                this.menu = menu;
                this.menuLocation = menuLocation;
                this.menu.BackColor = darkPurple;
                this.menu.FlowDirection = FlowDirection.TopDown;
                this.menu.AutoSize = autoSizeMenu;
                this.menu.AutoSizeMode = AutoSizeMode.GrowOnly;
                this.menu.Visible = false;
                this.menu.BringToFront();
                allFlyoutMenus.Add(this);
            }

            public void AddMenuItem<T>(ButtonPlus<T> buttonPlus) {
                this.menu.Controls.Add(buttonPlus);
            }
            public void SetMenuSize(Size size) {
                this.menu.Size = size;
            }

            public void Close() {
                this.button.BackColor = darkerBlue;
                this.menu.Visible = false;
            }

            public void Enabled(bool b) {
                if (b) {
                    this.button.Enabled = true;
                } else {
                    this.button.Enabled = false;
                    this.Close();
                }
            }

            public void CloseAllOthers() {
                foreach (FlyoutMenu menu in allFlyoutMenus) if (menu != this) menu.Close();
            }
         
            private void button_Click(object sender, EventArgs e) {
                if (!this.menu.Visible) {
                    this.CloseAllOthers();
                    if (this.menuLocation == FlyoutLocation.Right) this.menu.Location = new Point(this.buttonBar.Location.X + this.buttonBar.Size.Width, this.button.Location.Y);
                    else if (this.menuLocation == FlyoutLocation.Left) this.menu.Location = new Point(this.buttonBar.Location.X - this.menu.Size.Width, this.buttonBar.Location.Y + this.button.Location.Y);
                    else if (this.menuLocation == FlyoutLocation.RightTop) this.menu.Location = new Point(this.buttonBar.Location.X + this.buttonBar.Size.Width, this.buttonBar.Location.Y);
                    else if (this.menuLocation == FlyoutLocation.LeftTop) this.menu.Location = new Point(this.buttonBar.Location.X - this.menu.Size.Width, this.buttonBar.Location.Y);
                    this.button.BackColor = darkPurple;
                    this.menu.BringToFront();
                    this.menu.Visible = true;
                } else {
                    this.button.BackColor = darkerBlue;
                    this.menu.Visible = false;
                }
            }

            private void selectionHandler(object sender, EventArgs e) {
                ButtonHandler buttonHandler = (ButtonHandler)sender;
                if (buttonHandler.NullHandler()) return; // don't even close the menu
                // close the menu:
                buttonHandler.FlyoutMenu().Close();
                // client callback:
                buttonHandler.HandleData(); 
            }

            public ButtonPlus<T> PlainButton<T>(string text, Color backColor, Color textColor, Font font, FlyoutMenu flyoutMenu, T data, Handler<T> onSelection) {
                ButtonPlus<T> button = new ButtonPlus<T>(flyoutMenu, data, onSelection);
                button.BackColor = backColor;
                button.ForeColor = textColor;
                button.AutoSize = true;
                button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.Margin = new Padding(0);
                button.Name = text; // used by onClick
                button.Text = text;
                button.Font = font;
                button.UseVisualStyleBackColor = false;
                button.Click += selectionHandler;
                return button;
            }
            public ButtonPlus<T> ImageButton<T>(string text, Image image, FlyoutMenu flyoutMenu, T data, Handler<T> onSelection) {
                ButtonPlus<T> button = new ButtonPlus<T>(flyoutMenu, data, onSelection);
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.Margin = new Padding(0);
                button.AutoSize = false;
                button.Size = new Size(image.Size.Width+2, image.Size.Height+2); // somehow we need to enlarge the button to show the whole image
                button.Image = image;
                button.Name = text; // used by onClick
                button.Text = "";
                button.UseVisualStyleBackColor = false;
                button.Click += selectionHandler;
                return button;
            }

        }

        // Device Control

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

                ProtocolDevice.Draw(canvas, canvasX, canvasY, canvasWidth, canvasHeight);

            }
        }

        /* GUI INITIALIZATION */

        public GUI2() {
            InitializeComponent();
            this.KeyPreview = true; // needed by OnKeyDown to override Ctrl-V to paste text instead of pictures
            txtInput = richTextBox;
        }

        // Microfluidics

        public static DeviceSKControl deviceControl = null;

        // Flyout Menus

        private void NoSelection<T>(FlyoutMenu flyoutMenu, T data) { }

        // Tutorial Flyout Menu

        FlyoutMenu menu_Tutorial;

        private void SelectTutorial(FlyoutMenu flyoutMenu, ModelInfo info) {
            InputSetText(info.text);
        }

        // Export Flyout Menu

        FlyoutMenu menu_Export;

        private void SelectExport(FlyoutMenu flyoutMenu, ExportAction exportAction) {
            exportAction.action();
        }

        // Noise Flyout Menu

        FlyoutMenu menu_Noise;

        private Noise SelectNoiseSelectedItem = Noise.None;
        private void SelectNoise(FlyoutMenu flyoutMenu, Noise noise){
            Noise oldNoise = SelectNoiseSelectedItem;
            SelectNoiseSelectedItem = noise;
            flyoutMenu.button.Image = ImageOfNoise(noise);
            if (noise != oldNoise) StartAction(forkWorker: true, autoContinue: false);
        }

        private Image ImageOfNoise(Noise noise) {
            if (noise == Noise.None) return global::KaemikaWPF.Properties.Resources.Noise_None_W_48x48;
            if (noise == Noise.SigmaRange) return global::KaemikaWPF.Properties.Resources.Noise_SigmaRange_W_48x48;
            if (noise == Noise.Sigma) return global::KaemikaWPF.Properties.Resources.Noise_Sigma_W_48x48;
            if (noise == Noise.CV) return global::KaemikaWPF.Properties.Resources.Noise_CV_W_48x48;
            if (noise == Noise.SigmaSqRange) return global::KaemikaWPF.Properties.Resources.Noise_SigmaSqRange_W_48x48;
            if (noise == Noise.SigmaSq) return global::KaemikaWPF.Properties.Resources.Noise_SigmaSq_W_48x48;
            if (noise == Noise.Fano) return global::KaemikaWPF.Properties.Resources.Noise_Fano_W_48x48;
            throw new Error("ImageOfNoise");
        }

        // Math Flyout Menu

        FlyoutMenu menu_Math;
        private void SelectMath(FlyoutMenu flyoutMenu, string s) {
            InputInsertText(s);
        }

        // ON LOAD

        private void GUI2_Load(object sender, EventArgs e)
        {
            //this.AutoScaleMode = AutoScaleMode.None;
            //this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.Width = Math.Min(this.Width, Screen.PrimaryScreen.Bounds.Size.Width);
            this.Height = Math.Min(this.Height, Screen.PrimaryScreen.Bounds.Size.Height);
            this.CenterToScreen();

            this.BackColor = darkerBlue;
            this.panel1.BackColor = darkerBlue;
            this.panel2.BackColor = darkerBlue;

            // Splash

            this.panel_Splash.Location = this.chart1.Location;
            this.panel_Splash.Size = this.chart1.Size;
            this.panel_Splash.BringToFront();
            this.panel_Splash.Visible = true;

            // Microfluidics

            this.panel_Microfluidics.Visible = false;
            this.panel_Microfluidics.BackColor = Color.FromArgb(ProtocolDevice.deviceBackColor.Alpha, ProtocolDevice.deviceBackColor.Red, ProtocolDevice.deviceBackColor.Green, ProtocolDevice.deviceBackColor.Blue);
            deviceControl = new DeviceSKControl();
            this.panel_Microfluidics.Controls.Add(deviceControl);
            deviceControl.Location = new Point(0, 0);
            button_FlipMicrofluidics.Visible = false;

            // Tutorial Flyout Menu

            menu_Tutorial = new FlyoutMenu(panel1, button_Tutorial, true, flowLayoutPanel_Tutorial, false, FlyoutLocation.RightTop);
            List<ModelInfoGroup> groups = Tutorial.Groups();
            foreach (ModelInfoGroup group in groups) {
                ButtonPlus<ModelInfoGroup> groupButton = menu_Tutorial.PlainButton<ModelInfoGroup>(group.GroupHeading, darkPurple, hotText, font9, menu_Tutorial, group, null); // null: don't even close the menu if selected
                menu_Tutorial.AddMenuItem(groupButton);
                foreach (ModelInfo info in group) {
                    ButtonPlus<ModelInfo> button = menu_Tutorial.PlainButton<ModelInfo>(info.title, darkPurple, whiteText, font8, menu_Tutorial, info, SelectTutorial);
                    menu_Tutorial.AddMenuItem(button);
                }
            }

            // Noise Flyout Menu

            menu_Noise = new FlyoutMenu(panel2, button_Noise, true, flowLayoutPanel_Noise, false, FlyoutLocation.Left);
            Size flowLayoutPanelSize = new Size(0, 0);
            foreach (Noise noise in Gui.noise) {
                ButtonPlus<Noise> button = menu_Noise.ImageButton(Gui.StringOfNoise(noise), ImageOfNoise(noise), menu_Noise, noise, SelectNoise);
                flowLayoutPanelSize = new Size(Math.Max(flowLayoutPanelSize.Width, button.Size.Width), flowLayoutPanelSize.Height + button.Size.Height);
                menu_Noise.AddMenuItem(button);
            }
            menu_Noise.SetMenuSize(flowLayoutPanelSize);

            // Export Flyout Menu

            menu_Export = new FlyoutMenu(panel2, button_Export, true, flowLayoutPanel_Export, true, FlyoutLocation.LeftTop);
            foreach (ExportAction export in Exec.exportActionsList()) {
                ButtonPlus<ExportAction> button = menu_Export.PlainButton(export.name, darkPurple, whiteText, font8, menu_Export, export, SelectExport);
                menu_Export.AddMenuItem(button);
            }

            // Math Flyout Menu
            List<string> symbols = new List<string> { 
                "∂", "μ", "σ", "±", "ʃ", "√", "∑", "∏", "\'",                                                  // Symbols
                "⁺", "⁻", "⁼", "⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹", "⁽", "⁾",                     // Superscript
                "_", "₊", "₋", "₌", "₀", "₁", "₂", "₃", "₄", "₅", "₆", "₇", "₈", "₉", "₍", "₎"};               // Subscript
            menu_Math = new FlyoutMenu(panel1, button_Math, true, flowLayoutPanel_Math, false, FlyoutLocation.Right);
            foreach (string symbol in symbols) {
                ButtonPlus<string> button = menu_Math.PlainButton(symbol, darkPurple, whiteText, font10, menu_Math, symbol, SelectMath);
                menu_Math.AddMenuItem(button);
            }

            // Stop Button

            this.btnStop.Visible = false; 
            this.btnStop.Enabled = false;

            // Settings Panel

            panel_Settings.BackColor = darkPurple;
            panel_Settings.Visible = false;

            button_RK547M.BackColor = darkerBlue;
            button_GearBDF.BackColor = buttonGrey;
            button_PrecomputeLNA.BackColor = buttonGrey;
            button_TraceChemical.BackColor = darkerBlue;
            button_TraceComputational.BackColor = buttonGrey;
            textBox_OutputDirectory.Text = Application.StartupPath;

            // Modal PopUp

            panel_ModalPopUp.BackColor = lighterBlue;
            panel_ModalPopUp.Visible = false;
            button_ModalPopUp_OK.BackColor = darkPurple;
            button_ModalPopUp_Cancel.BackColor = darkerBlue;

            // Chart

            this.chart1.SuppressExceptions = true;
            this.chart1.MouseWheel += chart1_MouseWheel; // for zooming
            this.chart1.ChartAreas[0].AxisX.ScrollBar.Enabled = false; // not really useful when zooming
            this.chart1.ChartAreas[0].AxisY.ScrollBar.Enabled = false; // not really useful when zooming
            this.chart1.MouseDown += chart1_MouseDown; // for scrolling
            this.chart1.MouseMove += chart1_MouseMove; // for scrolling
            this.chart1.MouseUp += chart1_MouseUp; // for scrolling
            this.chart1.DoubleClick += chart1_DoubleClick; // for resetting zoom/scroll

            flowLayoutPanel_Parameters.BackColor = palePurple;
            flowLayoutPanel_Parameters.Visible = false;
            flowLayoutPanel_Parameters.BringToFront();
            checkedListBox_Series.BackColor = palePurple;
            checkedListBox_Series.Visible = false;
            checkedListBox_Series.BringToFront();
            RestoreInput();
        }

        private void GUI2_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.SaveInput();
        }

        public RichTextBox txtInput;

        public string InputGetText() {
            return this.txtInput.Text;
        }

        public void InputSetText(string text) {
            this.txtInput.Text = text;
        }

        public void InputInsertText(string text) {
            this.txtInput.SelectedText = text;
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
            while (i < text.Length && chr > 0) {chr--; i++; }
            int tokenstart = i;
            this.richTextBox.HideSelection = false; // keep selection highlight on loss of focus
            this.richTextBox.Select(tokenstart, tokenlength);
        }

        public void SaveInput() {
            try {
                string path = Application.StartupPath + "\\save.txt";
                File.WriteAllText(path, this.InputGetText());
            } catch (Exception) { }
        }

        public void RestoreInput() {
            try {
                string path = Application.StartupPath + "\\save.txt";
                if (File.Exists(path)) {
                    this.InputSetText(File.ReadAllText(path));
                }
            } catch (Exception) { }
        }

        public void OutputSetText(string text) {
            txtTarget.Text = text;
            txtTarget.SelectionStart = txtTarget.Text.Length;
            txtTarget.SelectionLength = 0;
            txtTarget.ScrollToCaret();
        }

        public string OutputGetText() {
            return this.txtTarget.Text;
        } 

        public void OutputAppendText(string text) {
            txtTarget.AppendText(text);
            //txtTarget.SelectionStart = txtTarget.Text.Length;
            //txtTarget.SelectionLength = 0;
            //txtTarget.ScrollToCaret();
        }

        public void OutputAppendComputation(string chemicalTrace, string computationalTrace, string graphViz) {
            if (TraceComputational()) OutputAppendText(computationalTrace); else OutputAppendText(chemicalTrace);
        }

        public void Executing(bool executing) {
            if (executing) {
                this.button_Device.Enabled = false;
                this.btnEval.Enabled = false;
                this.btnStop.Enabled = true; this.btnStop.Visible = true; this.btnStop.Focus();
                this.menu_Noise.Enabled(false);
                this.menu_Tutorial.Enabled(false);
                this.menu_Export.Enabled(false);
            } else {
                this.button_Device.Enabled = true;
                this.btnEval.Enabled = true; this.btnEval.Focus();
                this.btnStop.Visible = false; this.btnStop.Enabled = false;
                this.menu_Noise.Enabled(true);
                this.menu_Tutorial.Enabled(true);
                this.menu_Export.Enabled(true);
            }
        }

        private void SetStartButtonToContinue() {
            this.btnEval.Image = global::KaemikaWPF.Properties.Resources.icons8pauseplay40;
            this.btnEval.Enabled = true;
        }
        private void SetContinueButtonToStart() {
            this.btnEval.Image = global::KaemikaWPF.Properties.Resources.icons8play40;
            this.btnEval.Enabled = false;

        }

        private bool continueButtonIsEnabled = false;
        public void ContinueEnable(bool b) {
            continueButtonIsEnabled = b;
            if (continueButtonIsEnabled) SetStartButtonToContinue(); else SetContinueButtonToStart();
        }

        public bool ContinueEnabled() {
            return continueButtonIsEnabled;
        }

        public bool ScopeVariants() {
            return true; // checkBox_ScopeVariants.Checked;
        }
        public bool RemapVariants() {
            return true; // checkBox_RemapVariants.Checked;
        }

        public Series ChartSeriesNamed(string name) {
            if (name == null) return null;
            int index = this.chart1.Series.IndexOf(name);
            if (index >= 0) return this.chart1.Series[index]; else return null;
        }

        public void ChartClear(string title) {
            ChartListboxClear();
            this.chart1.Titles.Clear();
            this.chart1.Series.Clear();
            this.chart1.ChartAreas[0].AxisX.Minimum = 0;
            this.chart1.ChartAreas[0].AxisX.LabelStyle.Format = "G4";
            this.chart1.ChartAreas[0].AxisY.LabelStyle.Format = "G4";
            if ((title != null) && (title != "")) chart1.Titles.Add(title);
            foreach (var legend in this.chart1.Legends) {
                legend.LegendItemOrder = LegendItemOrder.SameAsSeriesOrder;
                // legend.Font = Program.chartFont;
            }
            // we are inserting series in reverse to get Red on top, and this will somehow actually reverse back the legend
            ChartSetNoGrid(ChartListboxRemembered(" <NoGrid> "));
            ChartSetAxes(ChartListboxRemembered(" <Axes> "));
        }

        public void ChartClearData() {
            foreach (Series series in this.chart1.Series) series.Points.Clear();
        }

        public void ChartUpdate() {
            this.chart1.Series.ResumeUpdates();
            //this.chart1.Update();
            this.chart1.Series.SuspendUpdates();
        }

        public Series ChartAddSeries(string legend, Color color, Noise noise) {
            if (!this.chart1.Series.IsUniqueName(legend)) return null;
            Series series = this.chart1.Series.Add(legend);
            if (noise == Noise.None) {
                series.ChartType = SeriesChartType.Line;
                series.Color = color;
                series.BorderWidth = 3; // line width
            } else if (noise == Noise.Sigma || noise == Noise.SigmaSq || noise == Noise.CV || noise == Noise.Fano) {
                series.ChartType = SeriesChartType.Line;
                series.Color = color;
                series.BorderWidth = 1; // line width
            } else if (noise == Noise.SigmaRange || noise == Noise.SigmaSqRange) {
                series.ChartType = SeriesChartType.Range;
                series.Color = Color.FromArgb(this.Transparency(), color);
            } else throw new Error("ChartAddSeries");
            return series;
        }

        private void ChartSeriesVisible(string legend, bool visible, bool showMu, bool showSigma) {
            Series series = ChartSeriesNamed(legend);
            if (series != null) series.Enabled = visible;
            foreach (Noise noise in Gui.noise) {
                string noiseString = Gui.StringOfNoise(noise);
                if (ChartListboxMu(noiseString)) {
                    Series seriesLNA = ChartSeriesNamed(legend + noiseString);
                    if (seriesLNA != null) seriesLNA.Enabled = visible && showMu;
                }
                if (ChartListboxSigma(noiseString)) {
                    Series seriesLNA = ChartSeriesNamed(legend + noiseString);
                    if (seriesLNA != null) seriesLNA.Enabled = visible && showSigma;
                }
            }
        }

        public void ChartAddPoint(Series series, double t, double mean, double variance, Noise noise) {
            if (double.IsNaN(mean) || double.IsNaN(variance)) return;
            if (double.IsInfinity(mean) || double.IsInfinity(variance)) return;
            if (series != null) {
                int i = -1;
                if (noise == Noise.None) i = series.Points.AddXY(t, mean);
                if (noise == Noise.SigmaSq) i = series.Points.AddXY(t, variance);
                if (noise == Noise.Sigma) i = series.Points.AddXY(t, Math.Sqrt(variance));
                if (noise == Noise.CV) i = series.Points.AddXY(t, ((mean == 0.0) ? 0.0 : (Math.Sqrt(variance) / mean)));
                if (noise == Noise.Fano) i = series.Points.AddXY(t, ((mean == 0.0) ? 0.0 : (variance / mean)));
                if (noise == Noise.SigmaSqRange) i = series.Points.AddXY(t, mean - variance, mean + variance);
                if (noise == Noise.SigmaRange) { double sd = Math.Sqrt(variance); i = series.Points.AddXY(t, mean - sd, mean + sd); }
                if (i>=0) series.Points[i].Tag = series.Name; // add tag to show the point's series when hovering on it
            }
        }
        public string ChartAddPointAsString(Series series, double t, double mean, double variance, Noise noise) {
            // do what ChartAddPoint does, but return it as a string for exporting/printing data
            if (double.IsNaN(mean) || double.IsNaN(variance)) return "";
            if (double.IsInfinity(mean) || double.IsInfinity(variance)) return "";
            string s = "";
            if (series != null) {
                s += series.Name + "=";
                if (noise == Noise.None) s += mean.ToString();
                if (noise == Noise.SigmaSq) s += variance.ToString();
                if (noise == Noise.Sigma) s += Math.Sqrt(variance);
                if (noise == Noise.CV) s += ((mean == 0.0) ? 0.0 : (Math.Sqrt(variance) / mean)).ToString();
                if (noise == Noise.Fano) s += ((mean == 0.0) ? 0.0 : (variance / mean)).ToString();
                if (noise == Noise.SigmaSqRange) s += mean.ToString() + "±" + variance.ToString();
                if (noise == Noise.SigmaRange) { double sd = Math.Sqrt(variance); s += mean.ToString() + "±" + sd.ToString(); }
            }
            return s;
        }

        
        public Noise NoiseSeries() {
            return SelectNoiseSelectedItem;
        }

        private static string solver = "RK547M";
        public string Solver() {
            return solver;
        }

        private void button_RK547M_Click(object sender, EventArgs e) {
            button_RK547M.BackColor = darkerBlue;
            button_GearBDF.BackColor = buttonGrey;
            solver = "RK547M";
        }

        private void button_GearBDF_Click(object sender, EventArgs e) {
            button_RK547M.BackColor = buttonGrey;
            button_GearBDF.BackColor = darkerBlue;
            solver = "GearBDF";
        }

        public bool precomputeLNA = false;
        private void button_PrecomputeLNA_Click(object sender, EventArgs e) {
            precomputeLNA = !precomputeLNA;
            if (precomputeLNA) button_PrecomputeLNA.BackColor = darkerBlue;
            else button_PrecomputeLNA.BackColor = buttonGrey;
        }

        private bool traceComputational = false;
        public bool TraceComputational() {
            return traceComputational;
        }

        private void button_TraceChemical_Click(object sender, EventArgs e){
            button_TraceChemical.BackColor = darkerBlue;
            button_TraceComputational.BackColor = buttonGrey;
            traceComputational = false;
        }

        private void button_TraceComputational_Click(object sender, EventArgs e){
            button_TraceComputational.BackColor = darkerBlue;
            button_TraceChemical.BackColor = buttonGrey;
            traceComputational = true;
        }

        private int Transparency() {
            return 32;
        }
        private void ChartSetNoGrid(bool checkd) {
            if (checkd) {
                this.chart1.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
                this.chart1.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;
            } else {
                this.chart1.ChartAreas[0].AxisX.MajorGrid.LineWidth = 1;
                this.chart1.ChartAreas[0].AxisY.MajorGrid.LineWidth = 1;
            }
        }

        private void ChartSetAxes(bool checkd) {
            if (checkd) {
                chart1.ChartAreas[0].AxisX.Enabled = AxisEnabled.True; 
                chart1.ChartAreas[0].AxisY.Enabled = AxisEnabled.True; 
            } else {
                chart1.ChartAreas[0].AxisX.Enabled = AxisEnabled.False; 
                chart1.ChartAreas[0].AxisY.Enabled = AxisEnabled.False; 
            }
        }
        
        public void ChartListboxClear() {
            ChartListboxRemember();
            checkedListBox_Series.Items.Clear();
            checkedListBox_Series.ColumnWidth = 100;
            CheckedListboxAdd(" <Axes> ", ChartListboxRemembered(" <Axes> "));
            CheckedListboxAdd(" <NoGrid> ", ChartListboxRemembered(" <NoGrid> "));
            if (NoiseSeries() != Noise.None) {
                CheckedListboxAdd(" <show μ> ", ChartListboxRemembered(" <show μ> "));
                CheckedListboxAdd(" <show σ> ", ChartListboxRemembered(" <show σ> "));
            }
            CheckedListboxAdd(" <ALL species> ");
            ChartListboxRestore(); // reflect those series just added
        }
        private int CheckedListboxAdd(string s, bool checkd = true) {
            int checkboxsize = 30;
            int slen = TextRenderer.MeasureText(s, checkedListBox_Series.Font).Width + checkboxsize;
            // need to consider size of checkboxes: 30 will work up to 300% DPI.
            // N.B. needs to start the app at that DPI to see the real effect at that DPI (not just switching DPI with the app open)
            checkedListBox_Series.ColumnWidth = Math.Max(checkedListBox_Series.ColumnWidth, slen);
            int index = checkedListBox_Series.Items.Add(s);
            checkedListBox_Series.SetItemChecked(index, checkd);
            return index;
        }

        private static Dictionary<string, bool> chartListboxRemember = 
            new Dictionary<string, bool>();
        private bool ChartListboxRemembered(string item) {
            if (chartListboxRemember.ContainsKey(item)) return chartListboxRemember[item]; else return true;
        }
        private void ChartListboxRemember() {
            foreach (var item in checkedListBox_Series.Items)
                if (!ChartListboxAll(item.ToString())) chartListboxRemember[item.ToString()] = false;
            foreach (var item in checkedListBox_Series.CheckedItems)
                if (!ChartListboxAll(item.ToString())) chartListboxRemember[item.ToString()] = true;
        }
        public void ChartListboxRestore() {
            foreach (var keyPair in chartListboxRemember) {
                int i = checkedListBox_Series.Items.IndexOf(keyPair.Key);
                if (i >= 0) checkedListBox_Series.SetItemChecked(i, keyPair.Value);
            }
        }
        private void ChartListboxForget() {
            chartListboxRemember = new Dictionary<string, bool>();
            int i = checkedListBox_Series.Items.IndexOf(" <ALL species> ");
            if (i >= 0) {
                checkedListBox_Series.SetItemChecked(i, false);
                checkedListBox_Series.SetItemChecked(i, true);
            }
            i = checkedListBox_Series.Items.IndexOf(" <show μ> ");
            if (i >= 0) {
                checkedListBox_Series.SetItemChecked(i, false);
                checkedListBox_Series.SetItemChecked(i, true);
            }
            i = checkedListBox_Series.Items.IndexOf(" <show σ> ");
            if (i >= 0) {
                checkedListBox_Series.SetItemChecked(i, false);
                checkedListBox_Series.SetItemChecked(i, true);
            }
            i = checkedListBox_Series.Items.IndexOf(" <Axes> ");
            if (i >= 0) {
                checkedListBox_Series.SetItemChecked(i, false);
                checkedListBox_Series.SetItemChecked(i, true);
            }
            i = checkedListBox_Series.Items.IndexOf(" <NoGrid> ");
            if (i >= 0) {
                checkedListBox_Series.SetItemChecked(i, false);
                checkedListBox_Series.SetItemChecked(i, true);
            }
        }
        private bool ChartListboxChecked(string legend) {
            int i = checkedListBox_Series.Items.IndexOf(legend);
            if (i >= 0) return checkedListBox_Series.GetItemChecked(i);
            else return false;
        }
        private void ChartListboxSet(string legend, bool state) {
            int i = checkedListBox_Series.Items.IndexOf(legend);
            if (i >= 0) checkedListBox_Series.SetItemChecked(i, state);
        }
        public void ChartListboxAddSeries(string legend) {
            CheckedListboxAdd(legend);
            ChartListboxRestore();
        }

        // ======== PARAMETERS ========= //

        private static Dictionary<string, ParameterInfo> parameterInfoDict = new Dictionary<string, ParameterInfo>();
        private static Dictionary<string, ParameterState> parameterStateDict = new Dictionary<string, ParameterState>();

        // clear all the parameter info when we close the parameters gui panel, otherwise we remember all this info across executions

        public void ParametersClear() {
            parameterInfoDict = new Dictionary<string, ParameterInfo>();
            parameterStateDict = new Dictionary<string, ParameterState>();
            flowLayoutPanel_Parameters.Controls.Clear();
        }

        public class ParameterState {
            public string parameter;
            public CheckBox checkBox;
            public TrackBar trackBar;
            public int rangeSteps;
            public bool programmaticChange; // i.e. value change not from GUI
            public ParameterState (string parameter, CheckBox checkBox, TrackBar trackBar, int rangeSteps) {
                this.parameter = parameter;
                this.checkBox = checkBox;
                this.trackBar = trackBar;
                this.rangeSteps = rangeSteps;
                this.programmaticChange = false;
            }
        }

        // ask the gui if this parameter is locked

        public double ParameterOracle(string parameter) {
            if (!parameterInfoDict.ContainsKey(parameter)) return double.NaN;
            if (parameterStateDict[parameter].checkBox.Checked) return (double)parameterInfoDict[parameter].drawn;
            return double.NaN;
        }

        // reflect the parameter state into the gui

        public void AddParameter(string parameter, double drawn, string distribution, double[] arguments) {
            const int width = 300;
            if (!parameterInfoDict.ContainsKey(parameter)) {
                CheckBox newCheckBox = new CheckBox();
                newCheckBox.Width = width - 30; // space for scrollbar
                newCheckBox.Margin = new Padding(10, 0, 0, 0);
                newCheckBox.CheckedChanged += (object source, EventArgs e) => {
                    ParameterInfo paramInfo = parameterInfoDict[parameter];
                    ParameterState paramState = parameterStateDict[parameter];
                    paramInfo.locked = paramState.checkBox.Checked;
                };
                TrackBar newTrackBar = new TrackBar(); newTrackBar.Width = width - 30;
                newTrackBar.ValueChanged += (object source, EventArgs e) => {
                    ParameterInfo paramInfo = parameterInfoDict[parameter];
                    ParameterState paramState = parameterStateDict[parameter];
                    if (!paramState.programmaticChange) paramInfo.drawn = paramInfo.rangeMin + ((source as TrackBar).Value/((double)paramState.rangeSteps)) * paramInfo.range;
                    paramState.programmaticChange = false;
                    paramState.checkBox.Text = paramInfo.ParameterLabel(false);
                };
                parameterStateDict[parameter] = new ParameterState(parameter, newCheckBox, newTrackBar, (distribution == "bernoulli") ? 1 : 100);
                flowLayoutPanel_Parameters.Controls.Add(newCheckBox); 
                flowLayoutPanel_Parameters.Controls.Add(newTrackBar);
                int oldWidth = flowLayoutPanel_Parameters.Width;
                Point oldLocation = flowLayoutPanel_Parameters.Location;
                flowLayoutPanel_Parameters.Width = width;
                flowLayoutPanel_Parameters.Location = new Point(oldLocation.X + oldWidth - width, oldLocation.Y); // stick to the right when changing size

            }
            parameterInfoDict[parameter] = new ParameterInfo(parameter, drawn, distribution, arguments);
            ParameterInfo info = parameterInfoDict[parameter];
            ParameterState state = parameterStateDict[parameter];
            if (!info.locked) {
                state.trackBar.Minimum = 0;
                state.trackBar.Maximum = state.rangeSteps;
                state.programmaticChange = true;
                state.trackBar.Value = (info.range == 0.0) ? (state.rangeSteps/2) : (int)(state.rangeSteps * (info.drawn - info.rangeMin) / info.range);
                state.checkBox.Text = info.ParameterLabel(false);
            }
        }

        /* GUI EVENTS */

        /* CHART */

        // System.Windows.Forms.DataVisualization.Charting 
        // https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.datavisualization.charting?view=netframework-4.7.2
        // https://www.dotnetperls.com/chart

        private void checkBox_CharAxes_CheckedChanged(object sender, EventArgs e)
        {
            //ChartSetAxes();
        }

        private void checkBox_ChartGrid_CheckedChanged(object sender, EventArgs e)
        {
            //ChartSetGrid();
        }

        private void chart1_DoubleClick(object sender, EventArgs e) {
            try {
                ((Chart)sender).ChartAreas[0].AxisX.ScaleView.ZoomReset();
                ((Chart)sender).ChartAreas[0].AxisY.ScaleView.ZoomReset();
                mouseToolTip.RemoveAll();
            } catch { }
        }
        private bool dragging = false;
        private Point mouseDown;
        private Point mouseMove;
        private double mouseDownViewMininumX;
        private double mouseDownViewMininumY;
        private ToolTip mouseToolTip = new ToolTip();
        private void chart1_MouseDown(object sender, MouseEventArgs e) {
            mouseDown = e.Location;
            mouseDownViewMininumX = ((Chart)sender).ChartAreas[0].AxisX.ScaleView.ViewMinimum;
            mouseDownViewMininumY = ((Chart)sender).ChartAreas[0].AxisY.ScaleView.ViewMinimum;
            dragging = true;
        }
        private bool Zoomed(Chart chart) {
            var xAxis = chart.ChartAreas[0].AxisX;
            var yAxis = chart.ChartAreas[0].AxisY;
            return chart.Series.Count > 0 && chart.Series[0].Points.Count > 0 &&
                   (xAxis.ScaleView.ViewMinimum != xAxis.Minimum || xAxis.ScaleView.ViewMaximum != xAxis.Maximum
                   || yAxis.ScaleView.ViewMinimum != yAxis.Minimum || yAxis.ScaleView.ViewMaximum != yAxis.Maximum);
        }
        private void chart1_MouseMove(object sender, MouseEventArgs e) {
            if (e.Location == mouseMove) return;
            try {
                var chart = (Chart)sender;
                var xAxis = chart.ChartAreas[0].AxisX;
                var yAxis = chart.ChartAreas[0].AxisY;
                if (dragging) {
                    var dX = xAxis.PixelPositionToValue(e.Location.X) - xAxis.PixelPositionToValue(mouseDown.X);
                    var dY = yAxis.PixelPositionToValue(e.Location.Y) - yAxis.PixelPositionToValue(mouseDown.Y);
                    xAxis.ScaleView.Scroll(mouseDownViewMininumX - dX);
                    yAxis.ScaleView.Scroll(mouseDownViewMininumY - dY);
                } else {
                    //+ " (X=" + xAxis.PixelPositionToValue(e.Location.X).ToString("G4") + ", Y=" + yAxis.PixelPositionToValue(e.Location.Y).ToString("G4") + ")"
                    mouseToolTip.RemoveAll();
                    string pointTag = "";
                    var hits = chart.HitTest(e.X, e.Y, false, ChartElementType.DataPoint);
                    foreach(HitTestResult hit in hits) {
                        if (hit.Object is DataPoint){
                            DataPoint point = (DataPoint)hit.Object;
                            if (point.YValues.Count() == 1)
                                pointTag += point.Tag + "  [x = " + point.XValue.ToString("G4") + ", y = " + point.YValues[0].ToString("G4") + "]";
                            else if (point.YValues.Count() == 2) // YValues[0] is mean - (variance or s.d.), YValues[1] is mean + (variance or s.d.)
                                pointTag += point.Tag + "  [x = " + point.XValue.ToString("G4") + ", y = " + ((point.YValues[0] + point.YValues[1]) / 2).ToString("G4") + "±" + ((point.YValues[1] - point.YValues[0]) / 2).ToString("G4") + "]";
                            pointTag += Environment.NewLine;
                        }
                    }
                    if (pointTag != "") pointTag = pointTag.Substring(0, pointTag.Length - 1); // remove last newLine
                    if (pointTag == "" && Zoomed(chart)) pointTag = "Zoomed!";
                    mouseToolTip.Show(pointTag, chart1, e.Location.X + 32, e.Location.Y - 15);
                }
            } catch { }
            mouseMove = e.Location;
        }
        private void chart1_MouseUp(object sender, MouseEventArgs e) {
            dragging = false;
        }

        private void chart1_MouseWheel(object sender, MouseEventArgs e) {
            var chart = (Chart)sender;
            var xAxis = chart.ChartAreas[0].AxisX;
            var yAxis = chart.ChartAreas[0].AxisY;
            var xMin = xAxis.ScaleView.ViewMinimum;
            var xMax = xAxis.ScaleView.ViewMaximum;
            var yMin = yAxis.ScaleView.ViewMinimum;
            var yMax = yAxis.ScaleView.ViewMaximum;
            const double scale = 0.2;
            try {
                var posX = xAxis.PixelPositionToValue(e.Location.X);
                var posY = yAxis.PixelPositionToValue(e.Location.Y);
                if (posX < xMin || posX > xMax || posY < yMin || posY > yMax) return;
                if (e.Delta < 0) { // Scrolled down: zoom out around mouse position              
                    var posXStart = xMin - (posX - xMin) * scale;
                    var posXFinish = xMax + (xMax - posX) * scale;
                    var posYStart = yMin - (posY - yMin) * scale;
                    var posYFinish = yMax + (yMax - posY) * scale;
                    if (posXStart < xAxis.Minimum) posXStart = xAxis.Minimum;
                    if (posXFinish > xAxis.Maximum) posXFinish = xAxis.Maximum;
                    if (posYStart < yAxis.Minimum) posYStart = yAxis.Minimum;
                    if (posYFinish > yAxis.Maximum) posYFinish = yAxis.Maximum;
                    xAxis.ScaleView.Zoom(posXStart, posXFinish);
                    yAxis.ScaleView.Zoom(posYStart, posYFinish);
                    if (posXStart == xAxis.Minimum && posXFinish == xAxis.Maximum) xAxis.ScaleView.ZoomReset();
                    if (posYStart == yAxis.Minimum && posYFinish == yAxis.Maximum) yAxis.ScaleView.ZoomReset();
                } else if (e.Delta > 0) { // Scrolled up: zoom in around mouse position
                    var posXStart = xMin + (posX - xMin) * scale;
                    var posXFinish = xMax - (xMax - posX) * scale;
                    var posYStart = yMin + (posY - yMin) * scale;
                    var posYFinish = yMax - (yMax - posY) * scale;
                    xAxis.ScaleView.Zoom(posXStart, posXFinish);
                    yAxis.ScaleView.Zoom(posYStart, posYFinish);
                }
                if (!Zoomed(chart)) mouseToolTip.RemoveAll();
            }
            catch { }            
        }

        private void button_Parameters_Click(object sender, EventArgs e) {
            if (!flowLayoutPanel_Parameters.Visible) {
                int buttonLocationX = panel2.Location.X + button_Parameters.Location.X + 2;
                int buttonLocationY = panel2.Location.Y + button_Parameters.Location.Y;
                flowLayoutPanel_Parameters.Location = new Point(
                    buttonLocationX - flowLayoutPanel_Parameters.Size.Width,
                    buttonLocationY
                );
                button_Parameters.BackColor = darkPurple;
                flowLayoutPanel_Parameters.BringToFront();
                flowLayoutPanel_Parameters.Visible = true;
            } else {
                flowLayoutPanel_Parameters.Visible = false;
                button_Parameters.BackColor = darkerBlue;
            }
        }

        private void button_EditChart_Click(object sender, EventArgs e) {
            if (!checkedListBox_Series.Visible) {
                int buttonLocationX = panel2.Location.X + button_EditChart.Location.X + 2;
                int buttonLocationY = panel2.Location.Y + button_EditChart.Location.Y;
                checkedListBox_Series.Location = new Point(
                    buttonLocationX - checkedListBox_Series.Size.Width,
                    buttonLocationY
                );
                button_EditChart.BackColor = darkPurple;
                checkedListBox_Series.BringToFront();
                checkedListBox_Series.Visible = true;
            } else {
                checkedListBox_Series.Visible = false;
                button_EditChart.BackColor = darkerBlue;
                ChartListboxForget();
            }
        }

        private void button_Settings_Click(object sender, EventArgs e) {
            if (!panel_Settings.Visible) {
                int buttonLocationX = panel2.Location.X + button_Settings.Location.X + 2;
                int buttonLocationY = panel2.Location.Y + button_Settings.Location.Y + button_Settings.Size.Height;
                panel_Settings.Location = new Point(
                    buttonLocationX - panel_Settings.Size.Width,
                    buttonLocationY - panel_Settings.Size.Height
                );
                button_Settings.BackColor = darkPurple;
                panel_Settings.BringToFront();
                panel_Settings.Visible = true;
            } else {
                panel_Settings.Visible = false;
                button_Settings.BackColor = darkerBlue;
            }
        }

        private void checkedListBox_Series_SelectedIndexChanged(object sender, EventArgs e) {
        }
        private void checkedListBox_Series_ItemCheck(object sender, ItemCheckEventArgs e)
        { // the checkbox change happens AFTER this event, so we must infer what changed from 'e'.
            string legend = checkedListBox_Series.Items[e.Index].ToString();
            if (ChartListboxAxes(legend)) {
                ChartSetAxes(e.NewValue == CheckState.Checked);
            } else if (ChartListboxNoGrid(legend)) {
                ChartSetNoGrid(e.NewValue == CheckState.Checked);
            } else if (ChartListboxAll(legend)) {
                List<string> items = new List<string>(); // copy enumerator because it will change while iterating
                foreach (var item in checkedListBox_Series.Items) items.Add(item.ToString());
                foreach (var item in items) {
                    if (!ChartListboxGroup(item)) {
                        int i = checkedListBox_Series.Items.IndexOf(item);
                        checkedListBox_Series.SetItemChecked(i, e.NewValue == CheckState.Checked);
                    }
                }
            } else if (ChartListboxShowMu(legend)) {
                List<string> items = new List<string>(); // copy enumerator because it will change while iterating
                foreach (var item in checkedListBox_Series.Items) items.Add(item.ToString());
                foreach (var item in items) {
                    if (!ChartListboxGroup(item)) {
                        int i = checkedListBox_Series.Items.IndexOf(item);
                        ChartSeriesVisible(item, checkedListBox_Series.GetItemChecked(i), e.NewValue == CheckState.Checked, ChartListboxChecked(" <show σ> "));
                    }
                }
                //if (e.NewValue == CheckState.Unchecked && !ChartListboxChecked(" <show σ> ")) ChartListboxSet(" <show σ> ", true); // does not work
            } else if (ChartListboxShowSigma(legend)) {
                List<string> items = new List<string>(); // copy enumerator because it will change while iterating
                foreach (var item in checkedListBox_Series.Items) items.Add(item.ToString());
                foreach (var item in items) {
                    if (!ChartListboxGroup(item)) {
                        int i = checkedListBox_Series.Items.IndexOf(item);
                        ChartSeriesVisible(item, checkedListBox_Series.GetItemChecked(i), ChartListboxChecked(" <show μ> "), e.NewValue == CheckState.Checked);
                    }
                }
                //if (e.NewValue == CheckState.Unchecked && !ChartListboxChecked(" <show μ> ")) ChartListboxSet(" <show μ> ", true); // does not work
            } else ChartSeriesVisible(legend, e.NewValue == CheckState.Checked, ChartListboxChecked(" <show μ> "), ChartListboxChecked(" <show σ> "));
            // refit the chart to the existing visible data
            chart1.ChartAreas[0].RecalculateAxesScale(); 
        }

        private static bool ChartListboxGroup(string str) {
            return ChartListboxAll(str) || ChartListboxShowMu(str) || ChartListboxShowSigma(str);
        }
        private static bool ChartListboxAxes(string str) {
            return str == " <Axes> ";
        }
        private static bool ChartListboxNoGrid(string str) {
            return str == " <NoGrid> ";
        }
        private static bool ChartListboxAll(string str) {
            return str == " <ALL species> ";
        }
        private static bool ChartListboxMu(string str) {
            return str.Contains("μ") && !str.Contains("σ");
        }
        private static bool ChartListboxShowMu(string str) {
            return str == " <show μ> ";
        }
        private static bool ChartListboxSigma(string str) {
            return str.Contains("σ");
        }
        private static bool ChartListboxShowSigma(string str) {
            return str == " <show σ> ";
        }

        public void ChartSnap() {
            try {
                //== Save a .Bmp to the Clipboard
                //using (MemoryStream ms = new MemoryStream()) {
                //    this.chart1.SaveImage(ms, ChartImageFormat.Bmp);
                //    Bitmap bm = new Bitmap(ms);
                //    Clipboard.SetImage(bm);
                //}

                //== Save a .emf file to Application.StartupPath directory
                chart1.SaveImage(textBox_OutputDirectory.Text + "\\chart.emfplus.emf", ChartImageFormat.EmfPlus); // InkScape cannot read EmfPlus at all
                chart1.SaveImage(textBox_OutputDirectory.Text + "\\chart.emf", ChartImageFormat.Emf);             // Emf (and InkScape) does not deal well with shaded areas

                //== Save a .emf file to the Clipboard
                using (MemoryStream stream = new MemoryStream()) {
                    this.chart1.SaveImage(stream, ChartImageFormat.EmfPlus); // can paste EmfPlus into Powerpoint
                    // this.chart1.SaveImage(stream, ChartImageFormat.Emf);  // Emf does not deal well with shaded areas
                    stream.Seek(0, SeekOrigin.Begin);
                    Metafile metafile = new Metafile(stream);
                    //Clipboard.SetDataObject(metafile, true); // this should work but apparently does not paste correctly to Windows applications
                    ClipboardMetafileHelper.PutEnhMetafileOnClipboard(this.Handle, metafile);
                }
            } catch {
                new ModalPopUp(panel_ModalPopUp).PopUp("Could not write to directory: " + textBox_OutputDirectory.Text, "(Change it in Settings.)", () => { }, null);
            }
        }

        public void ChartData() {
            try {
                string csvContent = "";
                foreach(var series in chart1.Series) {
                     if (series.Enabled) {
                         string seriesName = series.Name;
                         int pointCount = series.Points.Count;
                         for(int p = 0; p < pointCount; p++) {
                             DataPoint point = series.Points[p];
                             string yValuesCSV = String.Empty;
                             int count = point.YValues.Length;
                             for(int i = 0; i < count ; i++) {
                                  yValuesCSV += point.YValues[i];
                                  if(i != count-1)
                                  yValuesCSV += ",";
                             }
                             var csvLine = seriesName + "," + point.XValue + "," + yValuesCSV;
                             csvContent += csvLine + Environment.NewLine;
                         }
                     }
                }
                System.IO.StreamWriter file = new System.IO.StreamWriter(textBox_OutputDirectory.Text + "\\ChartData.csv");
                file.WriteLine(csvContent);
                file.Close();
            } catch {
                new ModalPopUp(panel_ModalPopUp).PopUp("Could not write to directory: " + textBox_OutputDirectory.Text, "(Change it in Settings.)", () => { }, null);
            }
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

        /* EXECUTE */

            
        public void StartAction(bool forkWorker, bool autoContinue = false) {
            if (Exec.IsExecuting() && !Gui.gui.ContinueEnabled()) return; // we are already running a simulation, don't start a concurrent one
            if (Exec.IsExecuting() && Gui.gui.ContinueEnabled()) { // we are already running a simulation; make start button work as continue button
                Protocol.continueExecution = true; 
            } else { // do a start
                Exec.Execute_Starter(forkWorker, autoContinue: autoContinue); // This is where it all happens
            }
        }

        //private void btnParse_Click(object sender, EventArgs e) { Exec.Execute_Starter(forkWorker: true, doParse: true); }
        //private void btnConstruct_Click(object sender, EventArgs e) { Exec.Execute_Starter(forkWorker: true, doAST: true); }
        //private void btnScope_Click(object sender, EventArgs e) { Exec.Execute_Starter(forkWorker: true, doScope: true); }
        private void btnEval_Click(object sender, EventArgs e) {
            // if (!modelInfo.executable) return;
            this.panel_Splash.Visible = false;
            StartAction(forkWorker: true, autoContinue: false);
        }

        /* OTHERS */

        private void btnStop_Click(object sender, EventArgs e) {
            Exec.EndingExecution(); // signals that we should stop
        }

        private void button_Continue_Click(object sender, EventArgs e) {
            Protocol.continueExecution = true;
        }

        //private void checkBox_ScopeVariants_CheckedChanged(object sender, EventArgs e)
        //{
        //    if (checkBox_ScopeVariants.Checked) {
        //        checkBox_RemapVariants.Enabled = true;
        //    } else {
        //        checkBox_RemapVariants.Enabled = false;
        //        checkBox_RemapVariants.Checked = false;
        //    }
        //}

        //private void checkBox_RemapVariants_CheckedChanged(object sender, EventArgs e)
        //{
        //}

        private void button_Source_Copy_Click(object sender, EventArgs e)
        {
            try { Clipboard.SetText(InputGetText()); } catch (ArgumentException) { };
        }

        private void button_Source_Paste_Click(object sender, EventArgs e)
        {
            new ModalPopUp(panel_ModalPopUp).PopUp("Do you really want to paste the clipboard", "and replace all source text?", () => { InputSetText(Clipboard.GetText()); }, () => { });
        }

        public void OutputCopy()
        {
            try { Clipboard.SetText(OutputGetText()); } catch (ArgumentException) { };
        }

        private void checkBox_FullTrace_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void radioButton_TraceChemical_CheckedChanged(object sender, EventArgs e)
        {
            Gui.gui.OutputSetText("");
            Exec.Execute_Exporter(false, ExportAs.ChemicalTrace);
        }

        private void radioButton_TraceComputational_CheckedChanged(object sender, EventArgs e)
        {
            Gui.gui.OutputSetText("");
            Exec.Execute_Exporter(false, ExportAs.ComputationalTrace);
        }

        private void chart1_Click(object sender, EventArgs e)
        {
        }

        private void chart1_SizeChanged(object sender, EventArgs e)
        {
            this.panel_Splash.Size = this.chart1.Size;
        }

        private void panel_Microfluidics_SizeChanged(object sender, EventArgs e)
        {
            deviceControl.Size = this.panel_Microfluidics.Size;
        }

        private void button_Device_Click(object sender, EventArgs e) {
            if (!ProtocolDevice.Exists()) {
                ProtocolDevice.Start(30, 100);
                deviceControl.Size = this.panel_Microfluidics.Size;
                panel_Microfluidics.BringToFront();
                panel_Microfluidics.Visible = true;
                button_Device.Image = global::KaemikaWPF.Properties.Resources.icons8device_ON_48x48;
                button_FlipMicrofluidics.Visible = true;
                button_FlipMicrofluidics.BackColor = darkPurple;
            } else {
                if (!Exec.IsExecuting()) {
                    button_FlipMicrofluidics.Visible = false;
                    panel_Microfluidics.Visible = false;
                    button_Device.Image = global::KaemikaWPF.Properties.Resources.icons8device_OFF_48x48;
                    ProtocolDevice.Stop();
                }
            }
        }

        private void button_FlipMicrofluidics_Click(object sender, EventArgs e) {
            if (panel_Microfluidics.Visible) {
                panel_Microfluidics.Visible = false;
                button_FlipMicrofluidics.BackColor = darkerBlue;
            } else {
                deviceControl.Size = this.panel_Microfluidics.Size;
                panel_Microfluidics.BringToFront();
                panel_Microfluidics.Visible = true;
                button_FlipMicrofluidics.BackColor = darkPurple;
            }
        }

        private void button_FontSizePlus_Click(object sender, EventArgs e) {
            Font font = new Font(fontFamily, txtInput.Font.Size + 1, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            txtInput.Font = font;
            txtTarget.Font = font;
        }

        private void button_FontSizeMinus_Click(object sender, EventArgs e) {
            Font font = new Font(fontFamily, txtInput.Font.Size - 1, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            txtInput.Font = font;
            txtTarget.Font = font;
        }

        // Overrides system Ctrl-V, which would paste a picture instead of text from clipboard
        // no need to add this method as an event handler in the form
        //https://stackoverflow.com/questions/173593/how-to-catch-control-v-in-c-sharp-app
        //Make sure in your form initialization you add: ====>>>>   this.KeyPreview = true
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control) {
                // MessageBox.Show("Hello world"); // test: works only if mainForm.KeyPreview = true
                txtInput.Paste(DataFormats.GetFormat(DataFormats.Text)); // assign clipboard to RichTextBox.SelectedText
                e.Handled = true; // prevents the system from doing another Ctrl-V after this action
            }
            base.OnKeyDown(e);
        }
    }
}
