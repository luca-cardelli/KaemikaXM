using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Kaemika;

namespace KaemikaWPF
{
    public class GUI_Windows : GuiInterface {
        public GUI form;
        public GUI_Windows(GUI form) {
            this.form = form;
        }

        delegate void VoidArgVoidReturnDelegate();
        delegate void BoolArgVoidReturnDelegate(bool b);
        delegate void BoolBoolArgVoidReturnDelegate(bool b1, bool b2);
        delegate void StringArgVoidReturnDelegate(string s);
        delegate void StringDoubleStringDoubleArrayArgVoidReturnDelegate(string parameter, double drawn, string distribution, double[] arguments);
        delegate double StringArgDoubleReturnDelegate(string parameter);
        delegate void StringStringArgVoidReturnDelegate(string s1, string s2);
        delegate void StringStringStringArgVoidReturnDelegate(string s1, string s2, string s3);
        delegate void IntIntArgVoidReturnDelegate(int i, int j);
        delegate void IntIntIntStringArgVoidReturnDelegate(int i, int j, int n, string s);
        delegate void IntDoubleDoubleArgVoidReturn(int i, double x, double y);
        delegate int VoidArgIntReturnDelegate();
        delegate Noise VoidArgNoiseReturnDelegate();
        delegate void SeriesDoubleDoubleArgVoidReturn(Series series, double x, double y);
        delegate void SeriesDoubleDoubleDoubleArgVoidReturn(Series series, double x, double y1, double y2);
        delegate void SeriesDoubleDoubleDoubleNoiseArgVoidReturn(Series series, double x, double y, double v, Noise n);
        delegate void StringDoubleDoubleDoubleNoiseArgVoidReturn(string seriesName, double x, double y, double v, Noise n);
        delegate bool VoidArgBoolReturnDelegate();
        delegate bool StringArgBoolReturnDelegate(string s);
        delegate void StringBoolArgVoidReturnDelegate(string s, bool b);
        delegate string VoidArgStringReturnDelegate();
        delegate Series StringColorNoiseArgSeriesReturnDelegate(string legend, Color color, Noise noise);
        delegate string StringColorNoiseArgStringReturnDelegate(string legend, Color color, Noise noise);

        public override string InputGetText() {
            if (form.txtInput.InvokeRequired) {
                VoidArgStringReturnDelegate d = new VoidArgStringReturnDelegate(InputGetText);
                return (string)form.Invoke(d, new object[] {});
            } else {
                return form.InputGetText();
            }
        }

        public override void InputSetText(string text) {
            if (form.txtInput.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(InputSetText);
                form.Invoke(d, new object[] { text });
            } else {
                form.InputSetText(text);
            }
        }

        public override void InputInsertText(string text) {
            if (form.txtInput.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(InputInsertText);
                form.Invoke(d, new object[] { text });
            } else {
                form.InputInsertText(text);
            }
        }
        public override void InputSetErrorSelection(int lineNumber, int columnNumber, int length, string failMessage) {
            if (form.txtInput.InvokeRequired) {
                IntIntIntStringArgVoidReturnDelegate d = new IntIntIntStringArgVoidReturnDelegate(InputSetErrorSelection);
                form.Invoke(d, new object[] { lineNumber, columnNumber, length, failMessage });
            } else {
                OutputAppendText(failMessage);
                if (lineNumber >=0 && columnNumber>=0) form.SetSelectionLineChar(lineNumber, columnNumber, length);
            }
        }
     
        public override void SaveInput() {
            if (form.txtInput.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(SaveInput);
                form.Invoke(d, new object[] { });
            } else {
                form.SaveInput();
            }
        }

        public override void RestoreInput() {
            if (form.txtInput.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(RestoreInput);
                form.Invoke(d, new object[] { });
            } else {
                form.RestoreInput();
            }
        }

        public override void OutputClear(string text) {
            OutputSetText("");
            //### also clear any graph
        }

        public override void ProcessGraph(string graphFamily) {
            OutputSetText(Export.ProcessGraph(graphFamily));
        }
       
        public override void OutputSetText(string text) {
            if (form.txtTarget.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(OutputSetText);
                form.Invoke(d, new object[] { text });
            } else {
                form.OutputSetText(text);
            }
        }

        public override string OutputGetText() {
            if (form.txtTarget.InvokeRequired) {
                VoidArgStringReturnDelegate d = new VoidArgStringReturnDelegate(OutputGetText);
                return (string)form.Invoke(d, new object[] {});
            } else {
                return form.OutputGetText();
            }
        } 

        public override void OutputAppendText(string text) {
            if (form.txtTarget.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(OutputAppendText);
                form.Invoke(d, new object[] { text });
            } else {
                form.OutputAppendText(text);
            }
        }

        public override void ChartClear(string title) {
            if (form.chart1.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(ChartClear);
                form.Invoke(d, new object[] { title });
            } else {
                form.ChartClear(title);
            }
        }

        public override void ChartClearData() {
            if (form.chart1.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartClearData);
                form.Invoke(d, new object[] { });
            } else {
                form.ChartClearData();
            }
        }

        public override void ChartUpdate() {
            if (form.chart1.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartUpdate);
                form.Invoke(d, new object[] { });
            } else {
                form.ChartUpdate();
            }
        }

        public override void LegendUpdate() {
            // it's part of ChartUpdate
        }

        public override string ChartAddSeries(string legend, Color color, Noise noise) {
            if (form.chart1.InvokeRequired) {
                StringColorNoiseArgStringReturnDelegate d = new StringColorNoiseArgStringReturnDelegate(ChartAddSeries);
                return (string)form.Invoke(d, new object[] { legend, color, noise });
            } else {
                Series series = form.ChartAddSeries(legend, color, noise);
                if (series != null) return series.Name; else return null; 
            }
        }
        public override void ChartAddPoint(string seriesName, double t, double mean, double variance, Noise noise) {
            if (form.chart1.InvokeRequired) {
                StringDoubleDoubleDoubleNoiseArgVoidReturn d = new StringDoubleDoubleDoubleNoiseArgVoidReturn(ChartAddPoint);
                form.Invoke(d, new object[] { seriesName, t, mean, variance, noise });
            } else {
                Series series = form.ChartSeriesNamed(seriesName);
                if (series != null) form.ChartAddPoint(series, t, mean, variance, noise);
                else throw new Error("ChartAddPoint series null");
            }
        }

        public override string ChartAddPointAsString(string seriesName, double t, double mean, double variance, Noise noise) {
            Series series = form.ChartSeriesNamed(seriesName);
            if (series != null) return form.ChartAddPointAsString(series, t, mean, variance, noise);
            else throw new Error("ChartAddPoint series null");
        }

        public override Noise NoiseSeries() {
            if (form.checkBox_LNA.InvokeRequired) {
                VoidArgNoiseReturnDelegate d = new VoidArgNoiseReturnDelegate(NoiseSeries);
                return (Noise)form.Invoke(d, new object[] {});
            } else {
                return form.NoiseSeries();
            }
        }

        public override bool ScopeVariants() {
            if (form.checkBox_ScopeVariants.InvokeRequired) {
                VoidArgBoolReturnDelegate d = new VoidArgBoolReturnDelegate(ScopeVariants);
                return (bool)form.Invoke(d, new object[] {});
            } else {
                return form.ScopeVariants();
            }
        }
        public override bool RemapVariants() {
            if (form.checkBox_RemapVariants.InvokeRequired) {
                VoidArgBoolReturnDelegate d = new VoidArgBoolReturnDelegate(RemapVariants);
                return (bool)form.Invoke(d, new object[] {});
            } else {
                return form.RemapVariants();
            }
        }
        
        public override void BeginningExecution() {
            if (form.btnStop.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(BeginningExecution);
                form.Invoke(d, new object[] { });
            } else {
                form.StopEnable(true);
            }
        }
        
        public override void EndingExecution() {
            if (form.btnStop.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(EndingExecution);
                form.Invoke(d, new object[] { });
            } else {
                form.StopEnable(false);
            }
        }

        public override void ContinueEnable(bool b) {
            if (form.btnStop.InvokeRequired) {
                BoolArgVoidReturnDelegate d = new BoolArgVoidReturnDelegate(ContinueEnable);
                form.Invoke(d, new object[] { b });
            } else {
                form.ContinueEnable(b);
            }
        }

        public override bool ContinueEnabled() {
            if (form.button_Continue.InvokeRequired) {
                VoidArgBoolReturnDelegate d = new VoidArgBoolReturnDelegate(ContinueEnabled);
                return (bool)form.Invoke(d, new object[] {});
            } else {
                return form.ContinueEnabled();
            }
        }

        public override string Solver() {
            if (form.comboBox_Solvers.InvokeRequired) {
                VoidArgStringReturnDelegate d = new VoidArgStringReturnDelegate(Solver);
                return (string)form.Invoke(d, new object[] {});
            } else {
                return form.Solver();
            }
        }
        public override bool PrecomputeLNA() {
            if (form.checkBox_precomputeLNA.InvokeRequired) {
                VoidArgBoolReturnDelegate d = new VoidArgBoolReturnDelegate(PrecomputeLNA);
                return (bool)form.Invoke(d, new object[] {});
            } else {
                return form.checkBox_precomputeLNA.Checked;
            }
        }

        public void ChartListboxClear() {
            if (form.checkedListBox_Series.InvokeRequired){
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartListboxClear);
                form.Invoke(d, new object[] { });
            } else {
                form.ChartListboxClear();
            }
        }

        public void ChartListboxRestore() {
            if (form.checkedListBox_Series.InvokeRequired){
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartListboxRestore);
                form.Invoke(d, new object[] { });
            } else {
                form.ChartListboxRestore();
            }
        }
 
        public override void ChartListboxAddSeries(string legend) {
            if (form.checkedListBox_Series.InvokeRequired){
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(ChartListboxAddSeries);
                form.Invoke(d, new object[] { legend });
            } else {
                form.ChartListboxAddSeries(legend);
            }
        }

        public override void ParametersClear() {
        }

        public override void ParametersUpdate() {
        }

        public override void AddParameter(string parameter, double drawn, string distribution, double[] arguments) {
            if (form.flowLayoutPanel_Parameters.InvokeRequired){
                StringDoubleStringDoubleArrayArgVoidReturnDelegate d = new StringDoubleStringDoubleArrayArgVoidReturnDelegate(AddParameter);
                form.Invoke(d, new object[] { parameter, drawn, distribution, arguments });
            } else {
                form.AddParameter(parameter, drawn, distribution, arguments);
            }
        }

        public override double ParameterOracle(string parameter) {
            if (form.flowLayoutPanel_Parameters.InvokeRequired){
                StringArgDoubleReturnDelegate d = new StringArgDoubleReturnDelegate(ParameterOracle);
                return (double)form.Invoke(d, new object[] { parameter });
            } else {
                return form.ParameterOracle(parameter);
            }

        }

        public override void ClipboardSetText(string text) {
            Clipboard.SetText(text);
        }

        public override void ProcessOutput() {
            if (form.TraceComputational()) Exec.Execute_Exporter(false, ExportAs.ComputationalTrace);
            else Exec.Execute_Exporter(false, ExportAs.ChemicalTrace);
        }

    }
}
