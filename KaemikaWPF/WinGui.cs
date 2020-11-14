using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using SkiaSharp;
using SkiaSharp.Views.Desktop; // for Extensions.ToBitmap method
using Kaemika;
using KaemikaAssets;

namespace KaemikaWPF {
    // This all runs in the gui thread: external-thread calls should be made through GuiInterface.

    public partial class WinGui : Form, KGuiControl {  // Form must be the first class in the file !!!!

        public static WinGui winGui;    // "The" GUI

        private PlatformTexter texter;
        private static Dictionary<float, Font> fonts;
        private static Dictionary<float, Font> fontsFixed;

        /* GUI INITIALIZATION */

        public WinControls winControls;  // platform-specific gui controls 

        // Constructor, invoked from App
        public WinGui() {
            InitializeComponent();
            winGui = this;
            Gui.platform = Kaemika.Platform.Windows;
        }

        public Font GetFont(float pointSize, bool fixedWidth) {
            if (fixedWidth) {
                if (!fontsFixed.ContainsKey(pointSize)) fontsFixed[pointSize] = new Font(this.texter.fixedFontFamily, pointSize, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                return fontsFixed[pointSize];
            } else {
                if (!fonts.ContainsKey(pointSize)) fonts[pointSize] = new Font(this.texter.fontFamily, pointSize, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                return fonts[pointSize];
            }
        }

        public void SetSnapshotSize() {
            // CLICK ON TITLE BAR and preass Alt-PrScr for window snapshot
            this.Width = 1366 + 14; // for 1366 snapshot
            this.Height = 768 + 7;  // for 768 snapshot
        }

        // ON LOAD

        private void GuiToWin_Load(object sender, EventArgs e) {
            this.texter = new PlatformTexter();
            fonts = new Dictionary<float, Font>();
            fontsFixed = new Dictionary<float, Font>();

            // Register this Gui for platform-independent access via interface KGuiControl
            KGui.Register(this);

            // Register this Gui for platform-independent access via interface KControls
            winControls = new WinControls();             // set up platform-specific gui controls 
            KGui.Register(new KControls(winControls));   // bind actions to them (non-platform specific) and register them throug KGui
            winControls.RestorePreferences();            //needs kControls initialized

            txtInput.MouseClick += (object ms, MouseEventArgs me) => { KGui.kControls.CloseOpenMenu(); };
            txtOutput.MouseClick += (object ms, MouseEventArgs me) => { KGui.kControls.CloseOpenMenu(); };
            panel1.MouseClick += (object ms, MouseEventArgs me) => { KGui.kControls.CloseOpenMenu(); };
            panel2.MouseClick += (object ms, MouseEventArgs me) => { KGui.kControls.CloseOpenMenu(); };
            panel_Splash.MouseClick += (object ms, MouseEventArgs me) => { KGui.kControls.CloseOpenMenu(); };

            this.KeyPreview = true; // used to detect shift key down

            //AutoScaleMode and AutoScaleDimensions are set in the GUI editor
            this.Width = Math.Min(this.Width, Screen.PrimaryScreen.Bounds.Size.Width);
            this.Height = Math.Min(this.Height, Screen.PrimaryScreen.Bounds.Size.Height);
            this.CenterToScreen();

            this.BackColor = WinControls.cMainButtonDeselected;
            this.panel1.BackColor = WinControls.cMainButtonDeselected;
            this.panel2.BackColor = WinControls.cMainButtonDeselected;
            this.splitContainer_Columns.BackColor = WinControls.cMainButtonDeselected;

            // Splash screen

            this.panel_Splash.Location = this.panel_KChart.Location;
            this.panel_Splash.Size = this.panel_KChart.Size;
            this.panel_Splash.BringToFront();
            this.panel_Splash.Visible = true;

            // Text

            WinControls.SetTextFont(10, true);

            // Device

            this.panel_Microfluidics.Controls.Add(new DeviceSKControl());
            this.panel_Microfluidics.BackColor = Extensions.ToDrawingColor(KDeviceHandler.deviceBackColor);

            // KChart

            this.panel_KChart.Controls.Add(new KChartSKControl());
            this.panel_KChart.BackColor = Color.White;

            // KScore

            this.panel_KScore.Controls.Add(new KScoreSKControl());
            this.panel_KScore.BackColor = Color.White;

            // Saved state

            GuiRestoreInput();
        }

        private void GuiToWin_FormClosing(object sender, FormClosingEventArgs e) {
            KGui.gui.GuiSaveInput();
        }

        // ====  KGuiControl INTERFACE =====

        public /* Interface KGuiControl */ void GuiInputSetEditable(bool editable) {
            if (!this.InvokeRequired) {
                if (editable) {
                    this.txtInput.ReadOnly = false;
                    this.txtInput.WordWrap = false;
                    this.txtInput.ScrollBars = ScrollBars.Both;
                } else {
                    this.txtInput.ReadOnly = true;
                    this.txtInput.WordWrap = true;
                    this.txtInput.ScrollBars = ScrollBars.Vertical;
                }
            } else this.Invoke((Action)delegate { GuiInputSetEditable(editable); });
        }

        public /* Interface KGuiControl */ string GuiInputGetText() {
            if (!this.InvokeRequired) {
                return this.txtInput.Text;
            } else return (string)this.Invoke((Func<string>) delegate { return GuiInputGetText(); }); 
        }

        public /* Interface KGuiControl */ void GuiInputSetText(string text) {
            if (!this.InvokeRequired) {
                this.txtInput.Text = text;
            } else this.Invoke((Action) delegate { GuiInputSetText(text); });
        }

        public /* Interface KGuiControl */ void GuiInputInsertText(string text) {
            if (!this.InvokeRequired) {
                this.txtInput.SelectedText = text;
            } else this.Invoke((Action) delegate { GuiInputInsertText(text); });
        }

        public /* Interface KGuiControl */ void GuiInputSetErrorSelection(int lineNumber, int columnNumber, int length, string failCategory, string failMessage) {
            if (!this.InvokeRequired) {
                GuiOutputAppendText(failCategory + ": " + failMessage);
                if (lineNumber >= 0 && columnNumber >= 0) SetSelectionLineChar(lineNumber, columnNumber, length);
                MessageBox.Show(failMessage, failCategory, MessageBoxButtons.OK, MessageBoxIcon.Error);
            } else this.Invoke((Action) delegate { GuiInputSetErrorSelection(lineNumber, columnNumber, length, failCategory, failMessage); });
        }

        public void SetSelectionLineChar(int line, int chr, int tokenlength) {
            if (line < 0 || chr < 0) return;
            string text = this.txtInput.Text;
            int i = 0;
            while (i < text.Length && line > 0) {
                if (text[i] == '\n') line--;
                i++;
            }
            if (i < text.Length && text[i] == '\r') i++;
            int linestart = i;
            while (i < text.Length && chr > 0) { chr--; i++; }
            int tokenstart = i;
            this.txtInput.HideSelection = false; // keep selection highlight on loss of focus
            this.txtInput.Select(tokenstart, tokenlength);
        }

        public /* Interface KGuiControl */  void GuiOutputTextShow() {
            if (!this.InvokeRequired) {
                txtOutput.Visible = true;
            } else this.Invoke((Action) delegate { GuiOutputTextShow(); });
        }

        public /* Interface KGuiControl */ void GuiOutputTextHide() {
            if (!this.InvokeRequired) {
                txtOutput.Visible = false;
            } else this.Invoke((Action) delegate { GuiOutputTextHide(); });
        }

        public /* Interface KGuiControl */ void GuiDeviceUpdate() {
            if (!DeviceSKControl.Exists()) return;
            if (!this.InvokeRequired) {
                if (!DeviceSKControl.IsVisible()) { //### or WinControls.IsMicrofluidicsVisible() ????
                    WinGui.winGui.winControls.onOffDeviceView.Selected(!WinGui.winGui.winControls.onOffDeviceView.IsSelected()); 
                    return; 
                }
                DeviceSKControl.SetSize(WinGui.winGui.panel_Microfluidics.Size);
                DeviceSKControl.InvalidateAndUpdate();
            } else this.Invoke((Action) delegate { GuiDeviceUpdate(); });
        }

        public /* Interface KGuiControl */ void GuiDeviceShow() { }
        public /* Interface KGuiControl */ void GuiDeviceHide() { }

        public /* Interface KGuiControl */ void GuiSaveInput() {
            try {
                string path = WinControls.CreateKaemikaDataDirectory() + "\\save.txt";
                File.WriteAllText(path, this.GuiInputGetText());
            } catch (Exception) { }
        }

        public /* Interface KGuiControl */ void GuiRestoreInput() {
            try {
                string path = WinControls.CreateKaemikaDataDirectory() + "\\save.txt";
                if (File.Exists(path)) {
                    this.GuiInputSetText(File.ReadAllText(path));
                } else {
                    this.GuiInputSetText(SharedAssets.TextAsset("StartHere.txt"));
                }
            } catch (Exception) { }
        }

        private static int visiblePosition = 0;

        public /* Interface KGuiControl */ void GuiOutputSetText(string text, bool savePosition) {
            if (!this.InvokeRequired) {
                if (savePosition)
                    visiblePosition = txtOutput.GetCharIndexFromPosition(new Point(3, 3));
                int visPos = visiblePosition;
                txtOutput.Text = text;
            } else this.Invoke((Action) delegate { GuiOutputSetText(text, savePosition); });
        }

        public /* Interface KGuiControl */ string GuiOutputGetText() {
            if (!this.InvokeRequired) {
                return this.txtOutput.Text;
            } else return (string)this.Invoke((Func<string>) delegate { return GuiOutputGetText(); }); 
        }

        public /* Interface KGuiControl */ void GuiOutputAppendText(string text) {
            if (!this.InvokeRequired) {
                txtOutput.AppendText(text);
                txtOutput.SelectionStart = visiblePosition;
                txtOutput.SelectionLength = 0;
                txtOutput.ScrollToCaret();
            } else this.Invoke((Action) delegate { GuiOutputAppendText(text); });
        }

        public /* Interface KGuiControl */ void GuiBeginningExecution() {
            if (!this.InvokeRequired) {
                KGui.kControls.Executing(true);
            } else this.Invoke((Action) delegate { GuiBeginningExecution(); });
        }

        public /* Interface KGuiControl */ void GuiEndingExecution() {
            if (!this.InvokeRequired) {
                KGui.kControls.Executing(false);
            } else this.Invoke((Action) delegate { GuiEndingExecution(); });
        }

        public /* Interface KGuiControl */ void GuiContinueEnable(bool b) {
            if (!this.InvokeRequired) {
                KGui.kControls.ContinueEnable(b);
            } else this.Invoke((Action) delegate { GuiContinueEnable(b); });
        }

        public /* Interface KGuiControl */ bool GuiContinueEnabled() {
            return false;
        }

        public /* Interface KGuiControl */ void GuiChartUpdate() {
            if (!this.InvokeRequired) {
                KChartHandler.VisibilityRestore(); // this is needed to hide the series in the chart
                KChartSKControl.SetSize(panel_KChart.Size);
                KChartSKControl.InvalidateAndUpdate();
            } else this.Invoke((Action) delegate { GuiChartUpdate(); });
        }

        public /* Interface KGuiControl */ void GuiLegendUpdate() {
            if (!this.InvokeRequired) {
                KChartHandler.VisibilityRestore(); // this is needed to hide the series in the legend
                KGui.kControls.SetLegend();
            } else this.Invoke((Action) delegate { GuiLegendUpdate(); });
        }

        public /* Interface KGuiControl */ void GuiClipboardSetText(string text) {
            Clipboard.SetText(text);
        }

        // SCORES - Interface between the KScoreSKControl control and its enclosing panel_KScore

        public Size ScoreSize() {
            return panel_KScore.Size;
        }

        public void ScoreHide() {
            panel_KScore.Visible = false;
        }

        public void ScoreShow() {
            panel_KScore.Visible = true;
        }

        private void panel_KScore_SizeChanged(object sender, EventArgs e) {
            KScoreSKControl.SetSize(this.panel_KScore.Size);
        }

        // PARAMETERS

        public /* Interface KGuiControl */ void GuiParametersUpdate() {
            if (!this.InvokeRequired) {
                KGui.kControls.ParametersUpdate();
            } else this.Invoke((Action) delegate { GuiParametersUpdate(); });
        }

        // EXPORT
      
        public /* Interface KGuiControl */ void GuiModelToSBML() {
            if (!this.InvokeRequired) {
                string sbml = "";
                try { sbml = Export.SBML(); }
                catch (Error ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK); return;  }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Something happened", MessageBoxButtons.OK); return;  }

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                saveFileDialog.InitialDirectory = WinControls.modelsDirectory;
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = false;
                if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                    try {
                        File.WriteAllText(saveFileDialog.FileName, sbml, System.Text.Encoding.UTF8); // use UTF8, not Unicode for SBML!
                    } catch {
                        MessageBox.Show(saveFileDialog.FileName, "Could not write this file:", MessageBoxButtons.OK);
                    }
                }
            } else this.Invoke((Action) delegate { GuiModelToSBML(); });
        }

        public /* Interface KGuiControl */ void GuiChartSnap() {
            if (!this.InvokeRequired) {
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

                Size chartSize = WinGui.winGui.panel_KChart.Size;
                Export.Snap(GenColorer, GenPainter, new SKSize(chartSize.Width, chartSize.Height));
                try { DoPaste(theBitmap); } catch { }
            } else this.Invoke((Action) delegate { GuiChartSnap(); });
        }

        public /* Interface KGuiControl */ void GuiChartSnapToSvg() {
            if (!this.InvokeRequired) {
                string svg = Export.SnapToSVG(GuiChartSize());

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "SVG files (*.svg)|*.svg|All files (*.*)|*.*";
                saveFileDialog.InitialDirectory = WinControls.modelsDirectory;
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = false;
                if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                    try {
                        File.WriteAllText(saveFileDialog.FileName, svg, System.Text.Encoding.Unicode);
                    } catch {
                        MessageBox.Show(saveFileDialog.FileName, "Could not write this file:", MessageBoxButtons.OK);
                    }
                }
            } else this.Invoke((Action) delegate { GuiChartSnapToSvg(); });
        }

        public /* Interface KGuiControl */ SKSize GuiChartSize() {
            if (!this.InvokeRequired) {
                Size chartSize = WinGui.winGui.panel_KChart.Size;
                return new SKSize(chartSize.Width, chartSize.Height);
            } else return (SKSize)this.Invoke((Func<SKSize>) delegate { return GuiChartSize(); }); 
        }

        public /* Interface KGuiControl */ void GuiChartData() {
            if (!this.InvokeRequired) {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                saveFileDialog.InitialDirectory = WinControls.modelsDirectory;
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = false;
                if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                    try {
                        File.WriteAllText(saveFileDialog.FileName, KChartHandler.ToCSV(), System.Text.Encoding.Unicode);
                    } catch {
                        MessageBox.Show(saveFileDialog.FileName, "Could not write this file:", MessageBoxButtons.OK);
                    }
                }
            } else this.Invoke((Action) delegate { GuiChartData(); });
        }

        public /* Interface KGuiControl */ void GuiOutputCopy() {
            try { Clipboard.SetText(KGui.gui.GuiOutputGetText()); } catch (ArgumentException) { };
        }

        public /* Interface KGuiControl */ void GuiOutputClear() {
            GuiOutputSetText("", savePosition:true);
        }

        public /* Interface KGuiControl */ void GuiProcessOutput() {
            Exec.currentOutputAction.action();
        }

        public /* Interface KGuiControl */ void GuiProcessGraph(string graphFamily) {
            GuiOutputSetText(Export.ProcessGraph(graphFamily), savePosition: false);
        }

        /* SIZE CHANGED */

        private void panel_Microfluidics_SizeChanged(object sender, EventArgs e) {
            DeviceSKControl.SetSize(this.panel_Microfluidics.Size);
        }

        private void panel_KChart_SizeChanged(object sender, EventArgs e) {
            KChartSKControl.SetSize(this.panel_KChart.Size);
        }

        // SHIFT KEY DOWN

        //public static bool ShiftKeyDown() {
        //    //https://stackoverflow.com/questions/570577/detect-shift-key-is-pressed-without-using-events-in-windows-forms
        //    // True if shift is the only modifier key down, but another non-modifier key may be down too:
        //    return Control.ModifierKeys == Keys.Shift;
        //}

        ////Make sure in your form initialization you add: ====>>>>   this.KeyPreview = true
        private bool shiftKeyDown = false;
        public bool IsShiftDown() {
            return shiftKeyDown;
        }
        private void GuiToWin_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.ShiftKey) { shiftKeyDown = true; KChartSKControl.OnShiftKeyDown(); }
        }
        private void GuiToWin_KeyUp(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.ShiftKey) { shiftKeyDown = false; KChartSKControl.OnShiftKeyUp(); }
        }

        // SPLIT CONTAINERS

        private void splitContainer_Columns_SplitterMoved(object sender, SplitterEventArgs e) {
            //this.splitContainer_Columns.BackColor = darkPurple;  // in case it is dragged to the edge and would be hard to see
        }

        private void splitContainer_Rows_SplitterMoved(object sender, SplitterEventArgs e) {
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





