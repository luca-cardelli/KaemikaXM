using System;
using System.Drawing;

namespace Kaemika
{
    public enum Noise { None = 0, SigmaRange = 1, Sigma = 2, CV = 3, SigmaSqRange = 4, SigmaSq = 5, Fano = 6 }

    public class Error : Exception {
        public Error(string message) : base(message) { }
    }

    public class Gui {
        public static GuiInterface gui; // hold "the" gui here
        public static void Log(string s) {
            Gui.gui.OutputAppendText(s + System.Environment.NewLine);
        }
        public static Noise[] noise = (Noise[])Enum.GetValues(typeof(Noise));
        private static readonly string[] noiseString = new string[] { " μ", " ±σ", " σ", " σ/μ", " ±σ²", " σ²", " σ²/μ" }; // match order of enum Noise
        private static readonly string[] longNoiseString = new string[] { " μ  (mean)", " ±σ  (μ ± standard deviation)", " σ  (μ and standard deviation)", " σ/μ  (μ and coeff of variation)", " ±σ²  (μ ± variance)", " σ²  (μ and variance)", " σ²/μ  (μ and Fano factor)" }; 
        public static Noise NoiseOfString(string selection) {
            for (int i = 0; i < noise.Length; i++) { if (selection == noiseString[i] || selection == longNoiseString[i]) return noise[i]; }
            return Noise.None; // if selection == null
        }
        public static string StringOfNoise(Noise noise) { return noiseString[(int)noise]; }

        public static string FormatUnit(double value, string spacer, string baseUnit, string numberFormat) {
            if (value == 0.0) return value.ToString(numberFormat) + spacer + baseUnit;
            //else if (Math.Round(value * 1e6) < 1) return (value * 1e9).ToString(numberFormat) + spacer + "n" + baseUnit; // this test avoids producing '1000nM'
            //else if (Math.Round(value * 1e3) < 1) return (value * 1e6).ToString(numberFormat) + spacer + "u" + baseUnit; // this test avoids producing '1000uM'
            //else if (Math.Round(value) < 1) return (value * 1e3).ToString(numberFormat) + spacer + "m" + baseUnit; // this test avoids producing '1000mM'
            else if (Math.Round(Math.Abs(value) * 1e9) < 1)  return (value * 1e12).ToString(numberFormat) + spacer + "p" + baseUnit;
            else if (Math.Round(Math.Abs(value) * 1e6) < 1)  return (value * 1e9).ToString(numberFormat) + spacer + "n" + baseUnit;
            else if (Math.Round(Math.Abs(value) * 1e3) < 1)  return (value * 1e6).ToString(numberFormat) + spacer + "μ" + baseUnit;
            else if (Math.Round(Math.Abs(value)) < 1)        return (value * 1e3).ToString(numberFormat) + spacer + "m" + baseUnit;
            else if (Math.Round(Math.Abs(value) * 1e-3) < 1) return (value).ToString(numberFormat) + spacer + baseUnit;
            else if (Math.Round(Math.Abs(value) * 1e-6) < 1) return (value * 1e-3).ToString(numberFormat) + spacer + "k" + baseUnit;
            else                                             return (value * 1e-6).ToString(numberFormat) + spacer + "M" + baseUnit;
        }
        public static string FormatUnit(float value, string spacer, string baseUnit, string numberFormat) {
            return FormatUnit((double)value, spacer, baseUnit, numberFormat);
        }
    }

    public abstract class GuiInterface {
        public abstract string InputGetText();
        public abstract void InputSetText(string text);
        public abstract void InputInsertText(string text);
        public abstract void InputSetErrorSelection(int lineNumber, int columnNumber, int length, string failMessage);
        public abstract void OutputSetText(string text);
        public abstract string OutputGetText();
        public abstract void OutputAppendText(string text);
        public abstract void ProcessOutput();
        public abstract void ProcessGraph(string graphFamily);  // deliver execution output in graph form
        public abstract void ChartClear(string title);
        public abstract void ChartClearData(); // clear only the data points in the chart
        public abstract void OutputClear(string title);
        public abstract void ParametersClear();
        public abstract void ChartUpdate();
        public abstract void LegendUpdate();
        public abstract string ChartAddSeries(string legend, Color color, Noise noise);
        // returns the same string as 'legend' param if it did add the series, and null if not, e.g. if the series was a duplicate name
        public abstract void ChartAddPoint(string seriesName, double t, double mean, double variance, Noise noise);
        public abstract string ChartAddPointAsString(string seriesName, double t, double mean, double variance, Noise noise);
        public abstract void AddParameter(string parameter, double drawn, string distribution, double[] args);
        public abstract void ParametersUpdate();
        public abstract double ParameterOracle(string parameter); // returns NAN if oracle not available
        public abstract Noise NoiseSeries();
        public abstract bool ScopeVariants();
        public abstract bool RemapVariants();
        public abstract void SaveInput();
        public abstract void RestoreInput();
        public abstract void BeginningExecution();   // signals that execution is starting
        public abstract void EndingExecution();     // signals that execution has ended (run to end, or stopped)
        public abstract void ContinueEnable(bool b);
        public abstract bool ContinueEnabled();
        public abstract string Solver();
        public abstract bool PrecomputeLNA();
        public abstract void ChartListboxAddSeries(string name);
        public abstract void ClipboardSetText(string text);
    }
}
