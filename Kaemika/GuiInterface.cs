using System;
using System.Drawing;

namespace Kaemika
{
    public enum Noise : int { None, SigmaRange, Sigma, CV, SigmaSqRange, SigmaSq, Fano }

    public class Error : Exception {
        public Error(string message) : base(message) { }
    }

    public class Gui {
        public static GuiInterface gui; // hold "the" gui here
        public static void Log(string s) {
            Gui.gui.OutputAppendText(s + System.Environment.NewLine);
        }
    }

    public abstract class GuiInterface {
        public abstract string InputGetText();
        public abstract void InputSetText(string text);
        public abstract void InputInsertText(string text);
        public abstract void InputSetErrorSelection(int lineNumber, int columnNumber, string failMessage);
        public abstract void OutputSetText(string text);
        public abstract string OutputGetText();
        public abstract void OutputAppendText(string text);
        public abstract void ChartClear(string title);
        public abstract void ChartUpdate();
        public abstract void LegendUpdate();
        public abstract string ChartAddSeries(string legend, Color color, Noise noise);
        // returns the same string as 'legend' if it could add the series, and null if not, e.g. if the series was a duplicate name
        public abstract void ChartAddPoint(string seriesName, double t, double mean, double variance, Noise noise);
        public abstract string ChartAddPointAsString(string seriesName, double t, double mean, double variance, Noise noise);
        public abstract Noise NoiseSeries();
        public abstract bool ScopeVariants();
        public abstract bool RemapVariants();
        public abstract void SaveInput();
        public abstract void RestoreInput();
        public abstract void StopEnable(bool b);
        public abstract bool StopEnabled();
        public abstract void ContinueEnable(bool b);
        public abstract bool ContinueEnabled();
        public abstract bool TraceComputational();
        public abstract string Solver();
        public abstract void ChartListboxAddSeries(string name);
        public abstract void ClipboardSetText(string text);
   }
}
