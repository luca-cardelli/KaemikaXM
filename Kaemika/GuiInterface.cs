using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Kaemika {

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
        SKPaint LinePaint(float strokeWidth, SKColor color, SKStrokeCap cap = SKStrokeCap.Butt);
        SKRect MeasureText(string text, SKPaint paint);
    }

    public interface Painter : Colorer {
        // Painter implementations hold a private canvas on which to draw
        void Clear(SKColor background);
        void DrawLine(List<SKPoint> points, SKPaint paint);
        void DrawPolygon(List<SKPoint> points, SKPaint paint);
        void DrawSpline(List<SKPoint> points, SKPaint paint); 
        void DrawRect(SKRect rect, SKPaint paint);
        void DrawRoundRect(SKRect rect, float corner, SKPaint paint);
        void DrawCircle(SKPoint p, float radius, SKPaint paint);
        void DrawText(string text, SKPoint point, SKPaint paint);
    }

    public class PlatformTexter : Texter {
        //kaemikaFont = new Font("Matura MT Script Capitals", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

        //### Android/iOS: GraphLayout does not use a Colorer and does not seem to set a font at all in its text paints
        //### Android/iOS: for the legend, ChartPage.DataTemplate does not set a font for the Label used to display text
        //### Android/iOS: otherwise these font families may be used only on chart axes
        public /*interface Texter*/ string fontFamily {
            // NO LONGER USED only fixedFontFamily is used, to remove variability of Unicode support
            // (watch out for GetFont, which has a fixedFont boolean parameter, but only some UI buttons are non-fixedWidth)
            get {
                return
                    (Gui.platform == Platform.macOS) ? "Helvetica" :               // CGColorer on macOS defers to SKColorer for the fonts
                    (Gui.platform == Platform.Windows) ? "Lucida Sans Unicode" :   // "Lucida Sans Unicode" is missing the variant marker "ˬ"
                    (Gui.platform == Platform.iOS) ? "Helvetica" :                 // Maybe it is never used and default fonts are used
                    (Gui.platform == Platform.Android) ? "Helvetica" :             // Maybe it is never used and default fonts are used
                    "Helvetica";
            }
        }
        public /*interface Texter*/ string fixedFontFamily {
            get {
                return
                    (Gui.platform == Platform.macOS) ? "Menlo" :                // CGColorer on macOS defers to SKColorer for the fonts
                    (Gui.platform == Platform.Windows) ? "Consolas" :           // "Consolas" has  variant marker "ˬ"; "Lucida Sans Typewriter": unicode math symbols are too small; "Courier New" is too ugly
                    (Gui.platform == Platform.iOS) ? "Menlo" :                  // Used by DisEditText. Menlo on iOS seems to lack "ˬ", but only in the Score species names (!?!)
                    (Gui.platform == Platform.Android) ? "DroidSansMono.ttf" :  // Used by DisEditText; this need to be placed in assets. Other option: "CutiveMono-Regular.ttf"
                    "Courier";
            }
        }
    }

    // ====  PLATFORM-NEUTRAL GUI INTERFACE =====

    public class Gui {
        public static string KaemikaVersion = "1.0.24";

        public static Platform platform = Platform.NONE;

        public static void Log(string s) {
            KGui.gui.GuiOutputAppendText(s + System.Environment.NewLine);
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
        void MicrofluidicsVisible(bool on);
        void MicrofluidicsOn();
        void MicrofluidicsOff();
        void IncrementFont(float pointSize);
        void Save();
        void Load();
        void SetDirectory();
        void PrivacyPolicyToClipboard();
        void SplashOff();
        void SavePreferences();
        void SetSnapshotSize(); // set standard size for snapshots
    }

}
