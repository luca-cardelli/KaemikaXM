using System;
using System.Collections.Generic;
using SkiaSharp;
using KaemikaAssets;

namespace Kaemika
{
    public enum Platform { Windows, macOS, Android, iOS, NONE }

    public enum Noise { None = 0, SigmaRange = 1, Sigma = 2, CV = 3, SigmaSqRange = 4, SigmaSq = 5, Fano = 6 }

    public class Error : Exception {
        public Error(string message) : base(message) { }
    }
    public class ConstantEvaluation : Exception {
        public ConstantEvaluation(string message) : base(message) { }
    }
    public class ExecutionEnded : Exception {
        public ExecutionEnded(string message) : base(message) { }
    }
    public class Reject : Exception {
        public Reject() : base("Reject") { }
    }

    // ====  PLATFORM-NEUTRAL GRAPHICS =====

    public interface Texter {
        string fontFamily { get; }
        string fixedFontFamily { get; }
    }

    public interface Colorer : Texter {
        // Colorer implementations hold fonts and paints but do not require a canvas
        SKTypeface font { get; }
        SKTypeface fixedFont { get; }
        SKPaint TextPaint(SKTypeface typeface, float textSize, SKColor color);
        SKPaint FillPaint(SKColor color);
        SKPaint LinePaint(float strokeWidth, SKColor color);
        SKRect MeasureText(string text, SKPaint paint);
    }

    public interface Painter : Colorer {
        // Painter implementations hold a private canvas on which to draw
        void Clear(SKColor background);
        void DrawRect(SKRect rect, SKPaint paint);
        void DrawRoundRect(SKRect rect, float padding, SKPaint paint);
        void DrawCircle(SKPoint p, float radius, SKPaint paint);
        void DrawText(string text, SKPoint point, SKPaint paint);
    }

    public class PlatformTexter : Texter {
        //kaemikaFont = new Font("Matura MT Script Capitals", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

        //### Android/iOS: GraphLayout does not use a Colorer and does not seem to set a font at all in its text paints
        //### Android/iOS: for the legend, ChartPage.DataTemplate does not set a font for the Label used to display text
        //### Android/iOS: otherwise these font families may be used only on chart axes
        public /*interface Texter*/ string fontFamily {
            get {
                return
                    (Gui.platform == Platform.macOS) ? "Helvetica" :               // CGColorer on macOS defers to SKColorer for the fonts
                    (Gui.platform == Platform.Windows) ? "Lucida Sans Unicode" :
                    (Gui.platform == Platform.iOS) ? "Helvetica" :                 // Maybe it is never used and default fonts are used
                    (Gui.platform == Platform.Android) ? "Helvetica" :             // Maybe it is never used and default fonts are used
                    "Helvetica";
            }
        }
        public /*interface Texter*/ string fixedFontFamily {
            get {
                return
                    (Gui.platform == Platform.macOS) ? "Menlo" :                // CGColorer on macOS defers to SKColorer for the fonts
                    (Gui.platform == Platform.Windows) ? "Consolas" :           // "Lucida Sans Typewriter": unicode math symbols are too small; "Courier New" is too ugly
                    (Gui.platform == Platform.iOS) ? "Menlo" :                  // Used by DisEditText.
                    (Gui.platform == Platform.Android) ? "DroidSansMono.ttf" :  // Used by DisEditText; this need to be placed in assets. Other option: "CutiveMono-Regular.ttf"
                    "Courier";
            }
        }
    }

    // ====  PLATFORM-NEUTRAL GUI INTERFACE =====

    public class Gui {
        public static Platform platform = Platform.NONE;
        public static ToGui toGui;                 // calls from execution thread to platform Gui
        // public static GuiControls guiControls;  // platform controls and callbacks; only Win and Mac have this, not iOS/Android

        public static void Log(string s) {
            Gui.toGui.OutputAppendText(s + System.Environment.NewLine);
            if (Exec.lastExecution != null) Exec.lastExecution.netlist.Emit(new CommentEntry(s));
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
            if (double.IsNaN(value)) return "NaN";
            if (value == 0.0) return value.ToString(numberFormat) + spacer + baseUnit;
            else if (Math.Abs(value) * 1e12 < 1)  return (value * 1e15) .ToString(numberFormat) + spacer + "f" + baseUnit;
            else if (Math.Abs(value) * 1e9  < 1)  return (value * 1e12) .ToString(numberFormat) + spacer + "p" + baseUnit;
            else if (Math.Abs(value) * 1e6  < 1)  return (value * 1e9)  .ToString(numberFormat) + spacer + "n" + baseUnit;
            else if (Math.Abs(value) * 1e3  < 1)  return (value * 1e6)  .ToString(numberFormat) + spacer + "μ" + baseUnit;
            else if (Math.Abs(value)        < 1)  return (value * 1e3)  .ToString(numberFormat) + spacer + "m" + baseUnit;
            else if (Math.Abs(value) * 1e-3 < 1)  return (value       ) .ToString(numberFormat) + spacer       + baseUnit;
            else if (Math.Abs(value) * 1e-6 < 1)  return (value * 1e-3) .ToString(numberFormat) + spacer + "k" + baseUnit;
            else if (Math.Abs(value) * 1e-9 < 1)  return (value * 1e-6) .ToString(numberFormat) + spacer + "M" + baseUnit;
            else if (Math.Abs(value) * 1e-12 < 1) return (value * 1e-9) .ToString(numberFormat) + spacer + "G" + baseUnit;
            else if (Math.Abs(value) * 1e-15 < 1) return (value * 1e-12).ToString(numberFormat) + spacer + "T" + baseUnit;
            else                                  return (value * 1e-15).ToString(numberFormat) + spacer + "P" + baseUnit;
        }
        public static string FormatUnit(float value, string spacer, string baseUnit, string numberFormat) {
            return FormatUnit((double)value, spacer, baseUnit, numberFormat);
        }
    }

    // Calls from (typically) the Execution thread that need to run in (typically) the Gui thread
    // This is used by Mac, Win and XM(iOS/Android)

    public abstract class ToGui { // this could be an interface
        public abstract string InputGetText();
        public abstract void InputSetText(string text);
        public abstract void InputInsertText(string text);
        public abstract void InputSetErrorSelection(int lineNumber, int columnNumber, int length, string failMessage);
        public abstract void OutputSetText(string text);
        public abstract string OutputGetText();
        public abstract void OutputAppendText(string text);

        public abstract void BeginningExecution();   // signals that execution is starting
        public abstract void EndingExecution();     // signals that execution has ended (run to end, or stopped)
        public abstract void ContinueEnable(bool b);
        public abstract bool ContinueEnabled();
        public abstract void SetTraceComputational();

        public abstract void ChartUpdate();
        public abstract void LegendUpdate();
        public abstract void ParametersUpdate();

        public abstract void OutputClear(string title);
        public abstract void ProcessOutput();
        public abstract void ProcessGraph(string graphFamily);  // deliver execution output in graph form

        public abstract void DeviceShow();
        public abstract void DeviceHide();
        public abstract void DeviceUpdate();

        public abstract void SaveInput();
        public abstract void RestoreInput();
        public abstract void ClipboardSetText(string text);
        public abstract void ChartSnap();
        public abstract void ChartSnapToSvg();
        public abstract SKSize ChartSize();
        public abstract void ChartData();
        public abstract void OutputCopy();
    }

    // Platform-dependent controls to which we attach Gui-thread-run callbacks
    // This is used only by Win and Mac, that have a common interface logic

    public interface GuiControls {
        KButton onOffStop { get; }
        KButton onOffEval { get; }
        KButton onOffDevice { get; }
        KButton onOffDeviceView { get; }
        KButton onOffFontSizePlus { get; }
        KButton onOffFontSizeMinus { get; }
        KButton onOffSave { get; }
        KButton onOffLoad { get; }
        KFlyoutMenu menuTutorial { get; }
        KFlyoutMenu menuNoise { get; }
        KFlyoutMenu menuOutput { get; }
        KFlyoutMenu menuExport { get; }
        KFlyoutMenu menuLegend { get; }
        KFlyoutMenu menuParameters { get; }
        KFlyoutMenu menuMath { get; }
        KFlyoutMenu menuSettings { get; }
        bool IsShiftDown();
        bool IsMicrofluidicsVisible();
        void MicrosfluidicsVisible(bool on);
        void MicrofluidicsOn();
        void MicrofluidicsOff();
        void IncrementFont(float pointSize);
        void Save();
        void Load();
        void SetDirectory();
        void PrivacyPolicyToClipboard();
        void SplashOff();
    }

}
