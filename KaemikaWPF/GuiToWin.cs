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

    public partial class GuiToWin : Form {  // Form must be the first class in the file !!!!

        private PlatformTexter texter;
        private static Dictionary<float, Font> fonts;
        private static Dictionary<float, Font> fontsFixed;

        /* GUI INITIALIZATION */

        public WinControls winControls;  // set up platform-specific gui controls 
        public KControls kControls;      // bind actions to them (non-platform specific)

        // Constructor, invoked from App
        public GuiToWin() {
            InitializeComponent();

            this.texter = new PlatformTexter();
            fonts = new Dictionary<float, Font>();
            fontsFixed = new Dictionary<float, Font>();

            txtInput.MouseClick += (object sender, MouseEventArgs e) => { App.guiToWin.kControls.CloseOpenMenu(); };
            txtOutput.MouseClick += (object sender, MouseEventArgs e) => { App.guiToWin.kControls.CloseOpenMenu(); };
            panel1.MouseClick += (object sender, MouseEventArgs e) => { App.guiToWin.kControls.CloseOpenMenu(); };
            panel2.MouseClick += (object sender, MouseEventArgs e) => { App.guiToWin.kControls.CloseOpenMenu(); };
            panel_Splash.MouseClick += (object sender, MouseEventArgs e) => { App.guiToWin.kControls.CloseOpenMenu(); };
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

        // ON LOAD

        private void GuiToWin_Load(object sender, EventArgs e) {
            this.KeyPreview = true; // needed by OnKeyDown to override Ctrl-V to paste text instead of pictures in RichTextBox control

            //AutoScaleMode and AutoScaleDimensions are set in the GUI editor
            this.Width = Math.Min(this.Width, Screen.PrimaryScreen.Bounds.Size.Width);
            this.Height = Math.Min(this.Height, Screen.PrimaryScreen.Bounds.Size.Height);
            this.CenterToScreen();

            this.BackColor = WinControls.cMainButtonDeselected;
            this.panel1.BackColor = WinControls.cMainButtonDeselected;
            this.panel2.BackColor = WinControls.cMainButtonDeselected;
            this.splitContainer_Columns.BackColor = WinControls.cMainButtonDeselected;

            // Splash

            this.panel_Splash.Location = this.panel_KChart.Location;
            this.panel_Splash.Size = this.panel_KChart.Size;
            this.panel_Splash.BringToFront();
            this.panel_Splash.Visible = true;

            // Controls

            winControls = new WinControls();             // set up platform-specific gui controls 
            kControls = new KControls(winControls);      // bind actions to them (non-platform specific)

            // Text

            WinControls.SetTextFont(10, true);

            // Device

            DeviceSKControl.deviceControl = new DeviceSKControl();
            this.panel_Microfluidics.Controls.Add(DeviceSKControl.deviceControl);
            DeviceSKControl.deviceControl.Location = new Point(0, 0);
            this.panel_Microfluidics.BackColor = Color.FromArgb(ProtocolDevice.deviceBackColor.Alpha, ProtocolDevice.deviceBackColor.Red, ProtocolDevice.deviceBackColor.Green, ProtocolDevice.deviceBackColor.Blue);

            // KChart

            KChartSKControl.chartControl = new KChartSKControl();
            this.panel_KChart.Controls.Add(KChartSKControl.chartControl);
            KChartSKControl.chartControl.Location = new Point(0, 0);
            this.panel_KChart.BackColor = Color.White;

            // Saved state

            RestoreInput();
        }

        private void GuiToWin_FormClosing(object sender, FormClosingEventArgs e) {
            this.SaveInput();
        }

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

        public void SaveInput() {
            try {
                string path = WinControls.CreateKaemikaDataDirectory() + "\\save.txt";
                File.WriteAllText(path, this.InputGetText());
            } catch (Exception) { }
        }

        public void RestoreInput() {
            try {
                string path = WinControls.CreateKaemikaDataDirectory() + "\\save.txt";
                if (File.Exists(path)) {
                    this.InputSetText(File.ReadAllText(path));
                } else {
                    this.InputSetText(SharedAssets.TextAsset("StartHere.txt"));
                }
            } catch (Exception) { }
        }

        private static int visiblePosition = 0;
        public void OutputSetText(string text) {
            if (txtOutput.Text != "" && text == "") 
                visiblePosition = txtOutput.GetCharIndexFromPosition(new Point(3, 3));
            txtOutput.Text = text;
            //txtTarget.SelectionStart = txtTarget.Text.Length;
            //txtTarget.SelectionLength = 0;
            //txtTarget.ScrollToCaret();
        }

        public string OutputGetText() {
            return this.txtOutput.Text;
        }

        public void OutputAppendText(string text) {
            txtOutput.AppendText(text);
            //txtOutput.SelectionStart = 0;
            //txtOutput.SelectionLength = 0;
            txtOutput.SelectionStart = visiblePosition;
            txtOutput.SelectionLength = 0;
            txtOutput.ScrollToCaret();
            //txtTarget.SelectionStart = txtTarget.Text.Length;
            //txtTarget.SelectionLength = 0;
            //txtTarget.ScrollToCaret();
        }

        public void ChartUpdate() {
            KChartHandler.VisibilityRestore(); // this is needed to hide the series in the chart
            KChartSKControl.chartControl.Size = panel_KChart.Size;
            KChartSKControl.chartControl.Invalidate();
            KChartSKControl.chartControl.Update();
        }

        public void LegendUpdate() {
            KChartHandler.VisibilityRestore(); // this is needed to hide the series in the legend
            App.guiToWin.kControls.SetLegend();
        }

        //public class LineButton : Button {
        //    private int thickness;
        //    private Color color;
        //    public LineButton(int thickness, Color color) : base() {
        //        this.thickness = thickness;
        //        this.color = color;
        //    }
        //    protected override void OnPaint(PaintEventArgs pevent) {
        //        //base.OnPaint(pevent); 
        //        if (Text == "---") {
        //            using (Pen p = new Pen(Color.White)) {
        //                pevent.Graphics.FillRectangle(p.Brush, ClientRectangle);
        //            }
        //            using (Pen p = new Pen(this.color)) {
        //                int leftRightMargin = 4;
        //                pevent.Graphics.FillRectangle(p.Brush, 
        //                    new Rectangle(
        //                        ClientRectangle.X + leftRightMargin, 
        //                        ClientRectangle.Y+(ClientRectangle.Height - thickness)/2, 
        //                        ClientRectangle.Width - 2*leftRightMargin, 
        //                        thickness));
        //            }
        //        } else {
        //            using (Pen p = new Pen(BackColor)) {
        //                pevent.Graphics.FillRectangle(p.Brush, ClientRectangle);
        //            }
        //        }
        //    }
        //}

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


        // ======== PARAMETERS ========= //

        public void ParametersUpdate() {
            kControls.ParametersUpdate();
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
        //private bool dragging = false;
        //private Point mouseDown;
        //private Point mouseMove;
        //private double mouseDownViewMininumX;
        //private double mouseDownViewMininumY;

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

            Size chartSize = App.guiToWin.panel_KChart.Size;
            KChartHandler.Snap(GenColorer, GenPainter, new SKSize(chartSize.Width, chartSize.Height));
            try { DoPaste(theBitmap); } catch { }

        }

        public void ChartSnapToSvg() {
            string svg = KChartHandler.SnapToSVG(ChartSize());

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
        }

        public SKSize ChartSize() {
            Size chartSize = App.guiToWin.panel_KChart.Size;
            return new SKSize(chartSize.Width, chartSize.Height);
        }

        public void ChartData() {
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
        }

        ////https://social.msdn.microsoft.com/Forums/windows/en-US/4cced4a8-6e66-40f6-8710-deb99d962b91/clipboard-and-metafiles-compatibility?forum=winforms
        //public class ClipboardMetafileHelper {
        //    [DllImport("user32.dll")]
        //    static extern bool OpenClipboard(IntPtr hWndNewOwner);
        //    [DllImport("user32.dll")]
        //    static extern bool EmptyClipboard();
        //    [DllImport("user32.dll")]
        //    static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        //    [DllImport("user32.dll")]
        //    static extern bool CloseClipboard();
        //    [DllImport("gdi32.dll")]
        //    static extern IntPtr CopyEnhMetaFile(IntPtr hemfSrc, IntPtr hNULL);
        //    [DllImport("gdi32.dll")]
        //    static extern bool DeleteEnhMetaFile(IntPtr hemf);
        //    // Metafile mf is set to a state that is not valid inside this function.
        //    static public bool PutEnhMetafileOnClipboard(IntPtr hWnd, Metafile mf) {
        //        bool bResult = false;
        //        IntPtr hEMF1;
        //        IntPtr hEMF2;
        //        hEMF1 = mf.GetHenhmetafile();
        //        if (!hEMF1.Equals(new IntPtr(0))) {
        //            hEMF2 = CopyEnhMetaFile(hEMF1, new IntPtr(0));
        //            if (!hEMF2.Equals(new IntPtr(0))) {
        //                if (OpenClipboard(hWnd)) {
        //                    if (EmptyClipboard()) {
        //                        IntPtr hRes = SetClipboardData(14 /*CF_ENHMETAFILE*/, hEMF2);
        //                        bResult = hRes.Equals(hEMF2);
        //                        CloseClipboard();
        //                    }
        //                }
        //            }
        //            DeleteEnhMetaFile(hEMF1);
        //        }
        //        return bResult;
        //    }
        //}

        /* OTHERS */

        public void OutputCopy() {
            try { Clipboard.SetText(OutputGetText()); } catch (ArgumentException) { };
        }

        private void panel_Microfluidics_SizeChanged(object sender, EventArgs e) {
            DeviceSKControl.deviceControl.Size = this.panel_Microfluidics.Size;
        }

        private void panel_KChart_SizeChanged(object sender, EventArgs e) {
            KChartSKControl.chartControl.Size = this.panel_KChart.Size;
        }

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





