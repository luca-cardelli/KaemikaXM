using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Kaemika;

namespace KaemikaWPF
{
    public class WinToGui : ToGui {

        public WinToGui() {
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

        public override void DeviceUpdate() {
            if (GuiToWin.deviceControl == null) return;
            if (!GuiToWin.deviceControl.Visible) return;
            if (GuiToWin.deviceControl.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(DeviceUpdate);
                GuiToWin.deviceControl.Invoke(d, new object[] { });
            } else {
                GuiToWin.deviceControl.Size = App.fromGui.panel_Microfluidics.Size;
                GuiToWin.deviceControl.Invalidate();
                GuiToWin.deviceControl.Update();
            }
        }
        public override void DeviceShow() { }
        public override void DeviceHide() { }

        public override string InputGetText() {
            if (App.fromGui.richTextBox.InvokeRequired) {
                VoidArgStringReturnDelegate d = new VoidArgStringReturnDelegate(InputGetText);
                return (string)App.fromGui.Invoke(d, new object[] {});
            } else {
                return App.fromGui.InputGetText();
            }
        }

        public override void InputSetText(string text) {
            if (App.fromGui.richTextBox.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(InputSetText);
                App.fromGui.Invoke(d, new object[] { text });
            } else {
                App.fromGui.InputSetText(text);
            }
        }

        public override void InputInsertText(string text) {
            if (App.fromGui.richTextBox.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(InputInsertText);
                App.fromGui.Invoke(d, new object[] { text });
            } else {
                App.fromGui.InputInsertText(text);
            }
        }
        public override void InputSetErrorSelection(int lineNumber, int columnNumber, int length, string failMessage) {
            if (App.fromGui.richTextBox.InvokeRequired) {
                IntIntIntStringArgVoidReturnDelegate d = new IntIntIntStringArgVoidReturnDelegate(InputSetErrorSelection);
                App.fromGui.Invoke(d, new object[] { lineNumber, columnNumber, length, failMessage });
            } else {
                OutputAppendText(failMessage);
                if (lineNumber >=0 && columnNumber>=0) App.fromGui.SetSelectionLineChar(lineNumber, columnNumber, length);
            }
        }
     
        public override void SaveInput() {
            if (App.fromGui.richTextBox.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(SaveInput);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.SaveInput();
            }
        }

        public override void RestoreInput() {
            if (App.fromGui.richTextBox.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(RestoreInput);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.RestoreInput();
            }
        }

        public override void OutputClear(string text) {
            OutputSetText("");
            // also clear any graph
        }

        public override void ProcessGraph(string graphFamily) {
            OutputSetText(Export.ProcessGraph(graphFamily));
        }
       
        public override void OutputSetText(string text) {
            if (App.fromGui.txtTarget.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(OutputSetText);
                App.fromGui.Invoke(d, new object[] { text });
            } else {
                App.fromGui.OutputSetText(text);
            }
        }

        public override string OutputGetText() {
            if (App.fromGui.txtTarget.InvokeRequired) {
                VoidArgStringReturnDelegate d = new VoidArgStringReturnDelegate(OutputGetText);
                return (string)App.fromGui.Invoke(d, new object[] {});
            } else {
                return App.fromGui.OutputGetText();
            }
        } 

        public override void OutputAppendText(string text) {
            if (App.fromGui.txtTarget.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(OutputAppendText);
                App.fromGui.Invoke(d, new object[] { text });
            } else {
                App.fromGui.OutputAppendText(text);
            }
        }

        public override void OutputCopy() {
            if (App.fromGui.txtTarget.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(OutputCopy);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.OutputCopy();
            }
        }

        // CHARTS

        public override void ChartUpdate() {
            if (App.fromGui.panel_KChart.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartUpdate);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.ChartUpdate();
            }
        }

        public override void ChartSnap() {
            if (App.fromGui.panel_KChart.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartSnap);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.ChartSnap();
            }
        }
        
        public override void ChartSnapToSvg() {
            if (App.fromGui.panel_KChart.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartSnapToSvg);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.ChartSnapToSvg();
            }
        }

        public override void ChartData() {
            if (App.fromGui.panel_KChart.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartData);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.ChartData();
            }
        }

        public override void LegendUpdate() {
            if (App.fromGui.panel_KChart.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(LegendUpdate);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.LegendUpdate();
            }
        }

        public override void BeginningExecution() {
            if (App.fromGui.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(BeginningExecution);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.clickerHandler.Executing(true);
            }
        }
        
        public override void EndingExecution() {
            if (App.fromGui.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(EndingExecution);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.clickerHandler.Executing(false);
            }
        }

        public override void ContinueEnable(bool b) {
            if (App.fromGui.InvokeRequired) {
                BoolArgVoidReturnDelegate d = new BoolArgVoidReturnDelegate(ContinueEnable);
                App.fromGui.Invoke(d, new object[] { b });
            } else {
                App.fromGui.clickerHandler.ContinueEnable(b);
            }
        }

        public override bool ContinueEnabled() {
            return false;
        }

        public override void ParametersClear() {
            if (App.fromGui.flowLayoutPanel_Parameters.InvokeRequired){
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ParametersClear);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.ParametersClear();
            }
        }

        public override void ParametersUpdate() {
            if (App.fromGui.flowLayoutPanel_Parameters.InvokeRequired){
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ParametersUpdate);
                App.fromGui.Invoke(d, new object[] { });
            } else {
                App.fromGui.ParametersUpdate();
            }
        }

        public override void AddParameter(string parameter, double drawn, string distribution, double[] arguments) {
            if (App.fromGui.flowLayoutPanel_Parameters.InvokeRequired){
                StringDoubleStringDoubleArrayArgVoidReturnDelegate d = new StringDoubleStringDoubleArrayArgVoidReturnDelegate(AddParameter);
                App.fromGui.Invoke(d, new object[] { parameter, drawn, distribution, arguments });
            } else {
                App.fromGui.AddParameter(parameter, drawn, distribution, arguments);
            }
        }

        public override double ParameterOracle(string parameter) {
            if (App.fromGui.flowLayoutPanel_Parameters.InvokeRequired){
                StringArgDoubleReturnDelegate d = new StringArgDoubleReturnDelegate(ParameterOracle);
                return (double)App.fromGui.Invoke(d, new object[] { parameter });
            } else {
                return App.fromGui.ParameterOracle(parameter);
            }
        }

        public override void ClipboardSetText(string text) {
            Clipboard.SetText(text);
        }

        public override void ProcessOutput() {
            Exec.currentOutputAction.action();
        }


    }
}
