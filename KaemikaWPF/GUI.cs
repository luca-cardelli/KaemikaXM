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
    public partial class GUI : Form
    {
        public  Font kaemikaFont;
        public  Font textFont;
        public  Font chartFont;

        /* GUI INITIALIZATION */

        public GUI() {
            InitializeComponent();
            txtInput = richTextBox;
            kaemikaFont = new Font("Matura MT Script Capitals", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            textFont = new Font("Lucida Sans Unicode", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            chartFont = new Font("Lucida Sans Unicode", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        }

        private void frmMain_Load(object sender, EventArgs e) {
            //this.AutoScaleMode = AutoScaleMode.None;
            //this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            if (this.Width > Screen.PrimaryScreen.Bounds.Size.Width) {
                // keep the right column fixed at original width
                this.tableLayoutPanel_Columns.ColumnStyles[0].SizeType = SizeType.Absolute;
                this.tableLayoutPanel_Columns.ColumnStyles[0].Width = (2 * Screen.PrimaryScreen.Bounds.Size.Width) / 5;
                this.tableLayoutPanel_Columns.ColumnStyles[1].SizeType = SizeType.Absolute;
                this.tableLayoutPanel_Columns.ColumnStyles[1].Width = (3 * Screen.PrimaryScreen.Bounds.Size.Width) / 5;
            }
            this.Width = Math.Min(this.Width, Screen.PrimaryScreen.Bounds.Size.Width);
            this.Height = Math.Min(this.Height, Screen.PrimaryScreen.Bounds.Size.Height);
            this.CenterToScreen();
            this.comboBox_Examples.SelectedIndex = 2;
            this.comboBox_Export.SelectedIndex = 0;
            this.comboBox_Solvers.SelectedIndex = 0;
            this.chart1.MouseWheel += chart1_MouseWheel; // for zooming
            this.chart1.ChartAreas[0].AxisX.ScrollBar.Enabled = false; // not really useful when zooming
            this.chart1.ChartAreas[0].AxisY.ScrollBar.Enabled = false; // not really useful when zooming
            this.chart1.MouseDown += chart1_MouseDown; // for scrolling
            this.chart1.MouseMove += chart1_MouseMove; // for scrolling
            this.chart1.MouseUp += chart1_MouseUp; // for scrolling
            this.chart1.DoubleClick += chart1_DoubleClick; // for resetting zoom/scroll
            flowLayoutPanel_Parameters.Visible = false;
            flowLayoutPanel_Parameters.BringToFront();
            checkedListBox_Series.Visible = false;
            checkedListBox_Series.BringToFront();
            RestoreInput();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e) {
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

        //public void InputSetErrorSelection(int lineNumber, int columnNumber, int length) {
        //    int lineBeg = this.richTextBox.GetFirstCharIndexFromLine(lineNumber);
        //    int lineEnd = this.richTextBox.GetFirstCharIndexFromLine(lineNumber+1);
        //    if (lineBeg < 0) lineBeg = this.richTextBox.Text.Length; // lineNumber is too big
        //    if (lineEnd < 0) lineEnd = this.richTextBox.Text.Length;
        //    this.richTextBox.HideSelection = false; // keep selection highlight on loss of focus
        //    this.richTextBox.Select(lineBeg, lineEnd - lineBeg);
        //}

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

        public void StopEnable(bool b) {
            this.btnParse.Enabled = !b;
            this.btnConstruct.Enabled = !b;
            this.btnScope.Enabled = !b;
            this.btnEval.Enabled = !b;
            this.comboBox_Export.Enabled = !b;
            this.checkBox_LNA.Enabled = !b;
            this.radioButton_LNA_SD.Enabled = (!b) && this.checkBox_LNA.Checked;
            this.radioButton_LNA_Var.Enabled = (!b) && this.checkBox_LNA.Checked;
            this.radioButton_LNA_SDRange.Enabled = (!b) && this.checkBox_LNA.Checked;
            this.radioButton_LNA_VarRange.Enabled = (!b) && this.checkBox_LNA.Checked;
            this.radioButton_LNA_Fano.Enabled = (!b) && this.checkBox_LNA.Checked;
            this.radioButton_LNA_CV.Enabled = (!b) && this.checkBox_LNA.Checked;

            this.btnStop.Enabled = b;
            if (b) this.btnStop.Focus(); else this.btnEval.Focus();

            btnStop.BackColor = (btnStop.Enabled) ? Color.Tomato : Color.Gainsboro;
            btnEval.BackColor = (btnEval.Enabled) ? Color.LightSalmon : Color.Gainsboro;
        }
        //public bool StopEnabled() {
        //    return btnStop.Enabled;
        //}

        public void ContinueEnable(bool b) {
            this.button_Continue.Enabled = b;
            button_Continue.BackColor = (button_Continue.Enabled) ? Color.Yellow : Color.Gainsboro;
        }

        public bool ContinueEnabled() {
            return button_Continue.Enabled;
        }

        public bool ScopeVariants() {
            return checkBox_ScopeVariants.Checked;
        }
        public bool RemapVariants() {
            return checkBox_RemapVariants.Checked;
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
            ChartSetGrid();
            ChartSetAxes();
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
            if (checkBox_LNA.Checked && radioButton_LNA_SD.Checked) return Noise.Sigma;
            if (checkBox_LNA.Checked && radioButton_LNA_Var.Checked) return Noise.SigmaSq;
            if (checkBox_LNA.Checked && radioButton_LNA_SDRange.Checked) return Noise.SigmaRange;
            if (checkBox_LNA.Checked && radioButton_LNA_VarRange.Checked) return Noise.SigmaSqRange;
            if (checkBox_LNA.Checked && radioButton_LNA_Fano.Checked) return Noise.Fano;
            if (checkBox_LNA.Checked && radioButton_LNA_CV.Checked) return Noise.CV;
            return Noise.None;
        }

        public string Solver() {
            return this.comboBox_Solvers.Text;
        }

        private void CheckBox_precomputeLNA_CheckedChanged(object sender, EventArgs e) {
        }

        public bool TraceComputational() {
            return radioButton_TraceComputational.Checked;
        }

        private int Transparency() {
            return Decimal.ToInt32(this.numericUpDown_Transparency.Value);
        }
        private void ChartSetGrid() {
            if (this.ChartGrid()) {
                this.chart1.ChartAreas[0].AxisX.MajorGrid.LineWidth = 1;
                this.chart1.ChartAreas[0].AxisY.MajorGrid.LineWidth = 1;
            } else {
                this.chart1.ChartAreas[0].AxisX.MajorGrid.LineWidth = 0;
                this.chart1.ChartAreas[0].AxisY.MajorGrid.LineWidth = 0;
            }
        }

        private bool ChartGrid() {
            return this.checkBox_ChartGrid.Checked;
        }

        private void ChartSetAxes() {
            if (this.ChartAxes()) {
                chart1.ChartAreas[0].AxisX.Enabled = AxisEnabled.True; 
                chart1.ChartAreas[0].AxisY.Enabled = AxisEnabled.True; 
            } else {
                chart1.ChartAreas[0].AxisX.Enabled = AxisEnabled.False; 
                chart1.ChartAreas[0].AxisY.Enabled = AxisEnabled.False; 
            }
        }

        private bool ChartAxes() {
            return this.checkBox_CharAxes.Checked;
        }
        
        public void ChartListboxClear() {
            ChartListboxRemember();
            checkedListBox_Series.Items.Clear();
            checkedListBox_Series.ColumnWidth = 100;
            if (checkBox_LNA.Checked) {
                CheckedListboxAdd(" <show μ> ");
                CheckedListboxAdd(" <show σ> ");
            }
            CheckedListboxAdd(" <ALL species> ");
        }
        private int CheckedListboxAdd(string s) {
            int checkboxsize = 30;
            int slen = TextRenderer.MeasureText(s, checkedListBox_Series.Font).Width + checkboxsize;
            // need to consider size of checkboxes: 30 will work up to 300% DPI.
            // N.B. needs to start the app at that DPI to see the real effect at that DPI (not just switching DPI with the app open)
            checkedListBox_Series.ColumnWidth = Math.Max(checkedListBox_Series.ColumnWidth, slen);
            int index = checkedListBox_Series.Items.Add(s);
            checkedListBox_Series.SetItemChecked(index, true);
            return index;
        }

        private static Dictionary<string, bool> chartListboxRemember = 
            new Dictionary<string, bool>();
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
                flowLayoutPanel_Parameters.Width = width;
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
            ChartSetAxes();
        }

        private void checkBox_ChartGrid_CheckedChanged(object sender, EventArgs e)
        {
            ChartSetGrid();
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

        private void numericUpDown_Transparency_ValueChanged(object sender, EventArgs e) {
            foreach (Series series in chart1.Series) {
                if (series.ChartType == SeriesChartType.Range)
                   series.Color = Color.FromArgb(this.Transparency(), series.Color);
            }
            chart1.Update();
        }


        private void CheckBoxButton_Parameters_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxButton_Parameters.Checked) {
                int buttonLocationX = tableLayoutPanel_Columns.Location.X + tableLayoutPanel_RightColumn.Location.X + panel_Controls.Location.X + checkBoxButton_Parameters.Location.X;
                int buttonLocationY = tableLayoutPanel_Columns.Location.Y + tableLayoutPanel_RightColumn.Location.Y + panel_Controls.Location.Y + checkBoxButton_Parameters.Location.Y;
                flowLayoutPanel_Parameters.Location = new Point(
                    buttonLocationX - flowLayoutPanel_Parameters.Size.Width + checkBoxButton_Parameters.Size.Width,
                    buttonLocationY - flowLayoutPanel_Parameters.Size.Height
                );
                flowLayoutPanel_Parameters.Visible = true;
            } else {
                flowLayoutPanel_Parameters.Visible = false;
                ParametersClear();
            }
        }

        private void checkBoxButton_EditChart_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxButton_EditChart.Checked) {
                int buttonLocationX = tableLayoutPanel_Columns.Location.X + tableLayoutPanel_RightColumn.Location.X + panel_Controls.Location.X + checkBoxButton_EditChart.Location.X;
                int buttonLocationY = tableLayoutPanel_Columns.Location.Y + tableLayoutPanel_RightColumn.Location.Y + panel_Controls.Location.Y + checkBoxButton_EditChart.Location.Y;
                checkedListBox_Series.Location = new Point(
                    buttonLocationX - checkedListBox_Series.Size.Width + checkBoxButton_EditChart.Size.Width,
                    buttonLocationY - checkedListBox_Series.Size.Height
                );
                checkedListBox_Series.Visible = true;
            } else {
                checkedListBox_Series.Visible = false;
                ChartListboxForget();
            }
        }
        private void checkedListBox_Series_SelectedIndexChanged(object sender, EventArgs e) {
        }
        private void checkedListBox_Series_ItemCheck(object sender, ItemCheckEventArgs e)
        { // the checkbox change happens AFTER this event, so we must infer what changed from 'e'.
            string legend = checkedListBox_Series.Items[e.Index].ToString();
            if (ChartListboxAll(legend)) {
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

        private void button_ChartSnap_Click(object sender, EventArgs e) {
            try {
                //== Save a .Bmp to the Clipboard
                //using (MemoryStream ms = new MemoryStream()) {
                //    this.chart1.SaveImage(ms, ChartImageFormat.Bmp);
                //    Bitmap bm = new Bitmap(ms);
                //    Clipboard.SetImage(bm);
                //}

                //== Save a .emf file to Application.StartupPath directory
                //chart1.SaveImage(Application.StartupPath + "\\chart.emf", ChartImageFormat.EmfPlus); // InkScape cannot read EmfPlus
                chart1.SaveImage(Application.StartupPath + "\\chart.emf", ChartImageFormat.Emf);

                //== Save a .emf file to the Clipboard
                using (MemoryStream stream = new MemoryStream()) {
                    this.chart1.SaveImage(stream, ChartImageFormat.EmfPlus);
                    // this.chart1.SaveImage(stream, ChartImageFormat.Emf);  // Emf does not deal well with shaded areas
                    stream.Seek(0, SeekOrigin.Begin);
                    Metafile metafile = new Metafile(stream);
                    //Clipboard.SetDataObject(metafile, true); // this should work but apparently does not paste correctly to Windows applications
                    ClipboardMetafileHelper.PutEnhMetafileOnClipboard(this.Handle, metafile);
                }
            } catch { }
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

        private void btnParse_Click(object sender, EventArgs e) { Exec.Execute_Starter(forkWorker: true, doParse: true); }
        private void btnConstruct_Click(object sender, EventArgs e) { Exec.Execute_Starter(forkWorker: true, doAST: true); }
        private void btnScope_Click(object sender, EventArgs e) { Exec.Execute_Starter(forkWorker: true, doScope: true); }
        private void btnEval_Click(object sender, EventArgs e) { Exec.Execute_Starter(forkWorker: true); }

        private void comboBox_Export_SelectedIndexChanged(object sender, EventArgs e) {
                 if (comboBox_Export.Text == "Chemical Trace") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ChemicalTrace); }
            else if (comboBox_Export.Text == "Computational Trace") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ComputationalTrace); }
            else if (comboBox_Export.Text == "Reaction Graph") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ReactionGraph); }
            else if (comboBox_Export.Text == "Reaction Complex Graph") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ComplexGraph); }
            else if (comboBox_Export.Text == "Protocol Step Graph") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ProtocolGraph); }
            else if (comboBox_Export.Text == "Protocol State Graph") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.PDMPGraph); }
            else if (comboBox_Export.Text == "System Reactions") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.PDMPreactions); }
            else if (comboBox_Export.Text == "System Equations") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.PDMPequations); }

            else if (comboBox_Export.Text == "CRN (LBS silverlight)") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.MSRC_LBS); }
            else if (comboBox_Export.Text == "CRN (LBS html5)") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.MSRC_CRN); }
            else if (comboBox_Export.Text == "ODE (Oscill8)") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.ODE); }
            else if (comboBox_Export.Text == "Protocol") { Gui.gui.OutputSetText(""); Exec.Execute_Exporter(false, ExportAs.Protocol); }

            //else if (comboBox_Export.Text == "PDMP GraphViz") Exec.Execute_Exporter(false, ExportAs.PDMP_GraphViz);
            //else if (comboBox_Export.Text == "PDMP Parallel") Exec.Execute_Exporter(false, ExportAs.PDMP_Parallel);
            //else if (comboBox_Export.Text == "PDMP Parallel GraphViz") Exec.Execute_Exporter(false, ExportAs.PDMP_Parallel_GraphViz);

            else if (comboBox_Export.Text == "Last simulation state") {
                Gui.gui.OutputSetText("");
                string s = Exec.lastReport + Environment.NewLine + Exec.lastState + Environment.NewLine;
                this.OutputAppendText(s);
                try { Clipboard.SetText(s); } catch (ArgumentException) { };
            } else { }
            this.comboBox_Export.SelectedIndex = 0;
        }

             /* OTHERS */

        private void btnStop_Click(object sender, EventArgs e)
        {
            Exec.EndingExecution(); // signals that we should stop
        }

        private void button_Continue_Click(object sender, EventArgs e)
        {
            Protocol.continueExecution = true;
        }

        private void checkBox_ScopeVariants_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_ScopeVariants.Checked) {
                checkBox_RemapVariants.Enabled = true;
            } else {
                checkBox_RemapVariants.Enabled = false;
                checkBox_RemapVariants.Checked = false;
            }
        }

        private void checkBox_RemapVariants_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void button_Source_Copy_Click(object sender, EventArgs e)
        {
            try { Clipboard.SetText(InputGetText()); } catch (ArgumentException) { };
        }

        private void button_Source_Paste_Click(object sender, EventArgs e)
        {
            InputSetText(Clipboard.GetText());
        }

        private void button_Target_Copy_Click(object sender, EventArgs e)
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

        private void comboBox_Sub_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_Sub.Text.Count() > 0) {
                InputInsertText(comboBox_Sub.Text);
            }
        }
        private void comboBox_Sup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_Sup.Text.Count() > 0) {
                InputInsertText(comboBox_Sup.Text);
            }
        }
        private void comboBox_Math_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_Math.Text.Count() > 0) {
                InputInsertText(comboBox_Math.Text);
            }
        }

        private void comboBox_Examples_SelectedIndexChanged(object sender, EventArgs e)
        {
            // https://stackoverflow.com/questions/433171/how-to-embed-a-text-file-in-a-net-assembly

            // Basic group
            if (comboBox_Examples.Text == "   Start Here") {
                InputSetText(Properties.Resources.StartHere);
            } else if (comboBox_Examples.Text == "   RingOscillator") {
                InputSetText(Properties.Resources.RingOscillator);
            } else if (comboBox_Examples.Text == "   Reactions") {
                InputSetText(Properties.Resources.Reactions);
            } else if (comboBox_Examples.Text == "   Enzyme Kinetics") {
                InputSetText(Properties.Resources.EnzymeKinetics);
            } else if (comboBox_Examples.Text == "   Approximate Majority") {
                InputSetText(Properties.Resources.ApproximateMajority);
            } else if (comboBox_Examples.Text == "   2AM Oscillator") {
                InputSetText(Properties.Resources._2AM_Oscillator);
            } else if (comboBox_Examples.Text == "   Transporters") {
                InputSetText(Properties.Resources.Transporters);

            // Differential signals group
            } else if (comboBox_Examples.Text == "   Sine Wave") {
                InputSetText(Properties.Resources.SineWave);
            } else if (comboBox_Examples.Text == "   Square Wave") {
                InputSetText(Properties.Resources.SquareWave);
            } else if (comboBox_Examples.Text == "   High Pass Filter") {
                InputSetText(Properties.Resources.HighPassFilter);
            } else if (comboBox_Examples.Text == "   Lorenz Attractor") {
                InputSetText(Properties.Resources.LorenzAttractor);
            } else if (comboBox_Examples.Text == "   Derivative1") {
                InputSetText(Properties.Resources.Derivative1);
            } else if (comboBox_Examples.Text == "   Derivative2") {
                InputSetText(Properties.Resources.Derivative2);

            // PID Controller group
            } else if (comboBox_Examples.Text == "   PosTestSignal Sine") {
                InputSetText(Properties.Resources.PosTestSignal_Sine);
            } else if (comboBox_Examples.Text == "   PosTestSignal Step") {
                InputSetText(Properties.Resources.PosTestSignal_Step);
            } else if (comboBox_Examples.Text == "   TestSignal Sine") {
                InputSetText(Properties.Resources.TestSignal_Sine);
            } else if (comboBox_Examples.Text == "   TestSignal Step") {
                InputSetText(Properties.Resources.TestSignal_Step);
            } else if (comboBox_Examples.Text == "   Proportional Block") {
                InputSetText(Properties.Resources.Proportional_Block);
            } else if (comboBox_Examples.Text == "   Integral Block") {
                InputSetText(Properties.Resources.Integral_Block);
            } else if (comboBox_Examples.Text == "   Derivative Block") {
                InputSetText(Properties.Resources.Derivative_Block);
            } else if (comboBox_Examples.Text == "   Addition Block") {
                InputSetText(Properties.Resources.Addition_Block);
            } else if (comboBox_Examples.Text == "   Subtraction Block") {
                InputSetText(Properties.Resources.Subtraction_Block);
            } else if (comboBox_Examples.Text == "   DualRailConverter Block") {
                InputSetText(Properties.Resources.DualRailConverter_Block);
            } else if (comboBox_Examples.Text == "   PIDController Block") {
                InputSetText(Properties.Resources.PIDController_Block);
            } else if (comboBox_Examples.Text == "   PIDController") {
                InputSetText(Properties.Resources.PIDController);
            } else if (comboBox_Examples.Text == "   PIDController Optimization") {
                InputSetText(Properties.Resources.PIDController_Optimization);

            // Samples group
            } else if (comboBox_Examples.Text == "   Samples") {
                InputSetText(Properties.Resources.Samples);
            } else if (comboBox_Examples.Text == "   Molar Mass") {
                InputSetText(Properties.Resources.MolarMass);
            } else if (comboBox_Examples.Text == "   Mix and Split") {
                InputSetText(Properties.Resources.MixAndSplit);
            } else if (comboBox_Examples.Text == "   PBS") {
                InputSetText(Properties.Resources.PBS);
            } else if (comboBox_Examples.Text == "   Serial Dilution") {
                InputSetText(Properties.Resources.SerialDilution);

            // Documentation Group
            } else if (comboBox_Examples.Text == "   GOLD Grammar") {
                InputSetText(Properties.Resources.KaemikaGrammar);
            } else if (comboBox_Examples.Text == "   Builtin Operators") {
                InputSetText(Properties.Resources.BuiltinFunctions);
            } else if (comboBox_Examples.Text == "   Flows") {
                InputSetText(Properties.Resources.Flows);
            } else if (comboBox_Examples.Text == "   Functions") {
                InputSetText(Properties.Resources.Functions);
            } else {
            }
            this.comboBox_Examples.SelectedIndex = 0;
        }

        private void checkBox_LNA_CheckedChanged(object sender, EventArgs e)
        {
            radioButton_LNA_SDRange.Enabled = checkBox_LNA.Checked;
            radioButton_LNA_VarRange.Enabled = checkBox_LNA.Checked;
            radioButton_LNA_SD.Enabled = checkBox_LNA.Checked;
            radioButton_LNA_Var.Enabled = checkBox_LNA.Checked;
            radioButton_LNA_CV.Enabled = checkBox_LNA.Checked;
            radioButton_LNA_Fano.Enabled = checkBox_LNA.Checked;
        }

        private void radioButton_LNA_SD_CheckedChanged(object sender, EventArgs e)
        {
        }
        private void radioButton_LNA_Var_CheckedChanged(object sender, EventArgs e)
        {
        }
        private void comboBox_Solvers_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
        private void chart1_Click(object sender, EventArgs e)
        {
        }

        // MSAGL Viewer

        private void GViewer1_Load(object sender, EventArgs e)
        {

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
