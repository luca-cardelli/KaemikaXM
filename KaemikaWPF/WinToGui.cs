using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Kaemika;
using SkiaSharp;

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
        delegate SKSize VoidArgSizeReturnDelegate();
        delegate Series StringColorNoiseArgSeriesReturnDelegate(string legend, Color color, Noise noise);
        delegate string StringColorNoiseArgStringReturnDelegate(string legend, Color color, Noise noise);

        public override void DeviceUpdate() {
            if (DeviceSKControl.deviceControl == null) return;
            if (!DeviceSKControl.deviceControl.Visible) return;
            if (DeviceSKControl.deviceControl.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(DeviceUpdate);
                DeviceSKControl.deviceControl.Invoke(d, new object[] { });
            } else {
                DeviceSKControl.deviceControl.Size = App.guiToWin.panel_Microfluidics.Size;
                DeviceSKControl.deviceControl.Invalidate();
                DeviceSKControl.deviceControl.Update();
            }
        }
        public override void DeviceShow() { }
        public override void DeviceHide() { }

        public override string InputGetText() {
            if (App.guiToWin.txtInput.InvokeRequired) {
                VoidArgStringReturnDelegate d = new VoidArgStringReturnDelegate(InputGetText);
                return (string)App.guiToWin.Invoke(d, new object[] {});
            } else {
                return App.guiToWin.InputGetText();
            }
        }

        public override void InputSetText(string text) {
            if (App.guiToWin.txtInput.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(InputSetText);
                App.guiToWin.Invoke(d, new object[] { text });
            } else {
                App.guiToWin.InputSetText(text);
            }
        }

        public override void InputInsertText(string text) {
            if (App.guiToWin.txtInput.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(InputInsertText);
                App.guiToWin.Invoke(d, new object[] { text });
            } else {
                App.guiToWin.InputInsertText(text);
            }
        }
        public override void InputSetErrorSelection(int lineNumber, int columnNumber, int length, string failMessage) {
            if (App.guiToWin.txtInput.InvokeRequired) {
                IntIntIntStringArgVoidReturnDelegate d = new IntIntIntStringArgVoidReturnDelegate(InputSetErrorSelection);
                App.guiToWin.Invoke(d, new object[] { lineNumber, columnNumber, length, failMessage });
            } else {
                OutputAppendText(failMessage);
                if (lineNumber >=0 && columnNumber>=0) App.guiToWin.SetSelectionLineChar(lineNumber, columnNumber, length);
            }
        }
     
        public override void SaveInput() {
            if (App.guiToWin.txtInput.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(SaveInput);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.SaveInput();
            }
        }

        public override void RestoreInput() {
            if (App.guiToWin.txtInput.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(RestoreInput);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.RestoreInput();
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
            if (App.guiToWin.txtOutput.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(OutputSetText);
                App.guiToWin.Invoke(d, new object[] { text });
            } else {
                App.guiToWin.OutputSetText(text);
            }
        }

        public override string OutputGetText() {
            if (App.guiToWin.txtOutput.InvokeRequired) {
                VoidArgStringReturnDelegate d = new VoidArgStringReturnDelegate(OutputGetText);
                return (string)App.guiToWin.Invoke(d, new object[] {});
            } else {
                return App.guiToWin.OutputGetText();
            }
        } 

        public override void OutputAppendText(string text) {
            if (App.guiToWin.txtOutput.InvokeRequired) {
                StringArgVoidReturnDelegate d = new StringArgVoidReturnDelegate(OutputAppendText);
                App.guiToWin.Invoke(d, new object[] { text });
            } else {
                App.guiToWin.OutputAppendText(text);
            }
        }

        public override void OutputCopy() {
            if (App.guiToWin.txtOutput.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(OutputCopy);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.OutputCopy();
            }
        }

        public override void SetTraceComputational() {
            if (App.guiToWin.txtOutput.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(SetTraceComputational);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.kControls.SetTraceComputational();
            }
        }

        // CHARTS

        public override void ChartUpdate() {
            if (App.guiToWin.panel_KChart.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartUpdate);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.ChartUpdate();
            }
        }

        public override void ChartSnap() {
            if (App.guiToWin.panel_KChart.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartSnap);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.ChartSnap();
            }
        }
        
        public override void ChartSnapToSvg() {
            if (App.guiToWin.panel_KChart.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartSnapToSvg);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.ChartSnapToSvg();
            }
        }

        public override SKSize ChartSize() {
            if (App.guiToWin.panel_KChart.InvokeRequired) {
                VoidArgSizeReturnDelegate d = new VoidArgSizeReturnDelegate(ChartSize);
                return (SKSize)App.guiToWin.Invoke(d, new object[] { });
            } else {
                return App.guiToWin.ChartSize();
            }
        }

        public override void ChartData() {
            if (App.guiToWin.panel_KChart.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ChartData);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.ChartData();
            }
        }

        public override void LegendUpdate() {
            if (App.guiToWin.panel_KChart.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(LegendUpdate);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.LegendUpdate();
            }
        }

        public override void BeginningExecution() {
            if (App.guiToWin.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(BeginningExecution);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.kControls.Executing(true);
            }
        }
        
        public override void EndingExecution() {
            if (App.guiToWin.InvokeRequired) {
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(EndingExecution);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.kControls.Executing(false);
            }
        }

        public override void ContinueEnable(bool b) {
            if (App.guiToWin.InvokeRequired) {
                BoolArgVoidReturnDelegate d = new BoolArgVoidReturnDelegate(ContinueEnable);
                App.guiToWin.Invoke(d, new object[] { b });
            } else {
                App.guiToWin.kControls.ContinueEnable(b);
            }
        }

        public override bool ContinueEnabled() {
            return false;
        }

        public override void ParametersUpdate() {
            if (App.guiToWin.InvokeRequired){
                VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(ParametersUpdate);
                App.guiToWin.Invoke(d, new object[] { });
            } else {
                App.guiToWin.ParametersUpdate();
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
