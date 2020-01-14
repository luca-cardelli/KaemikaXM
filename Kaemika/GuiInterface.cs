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

    // ====  PLATFORM-NEUTRAL GRAPHICS =====

    public interface Colorer {
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

    // ====  SKIASHARP (SHARED) GRAPHICS =====

    public class SKColorer : Colorer {
        public /*interface Colorer*/ SKTypeface font {
            get {
                var plat = Gui.platform;
                var family =
                    (plat == Platform.macOS) ? "Helvetica" :
                    (plat == Platform.Windows) ? "Lucida Sans Unicode" :
                    //###                   (platform == "iOS") ? "Lucida Sans Unicode" :
                    //###                   (platform == "Android") ? "Lucida Sans Unicode" :  //### for EditText: Typeface.CreateFromAsset(Context.Assets, "?????.ttf"); 
                    "Helvetica";
                return SKTypeface.FromFamilyName(family, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            }
        }
        public /*interface Colorer*/ SKTypeface fixedFont { 
            get {
                var plat = Gui.platform;
                var fixedFamily =
                    (plat == Platform.macOS) ? "Menlo" :
                    (plat == Platform.Windows) ? "Courier New" : // "Lucida Sans Typewriter" unicode math symbols are too small
                                                                 //###                   (platform == "iOS") ? "Lucida Sans Unicode" :
                                                                 //###                   (platform == "Android") ? "Lucida Sans Unicode" :   //### for EditText: Typeface.CreateFromAsset(Context.Assets, "DroidSansMono.ttf");
                   "Courier";
                return SKTypeface.FromFamilyName(fixedFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            }
        }
        public /*interface Colorer*/ SKPaint TextPaint(SKTypeface typeface, float textSize, SKColor color) {
            return new SKPaint { Typeface = typeface, IsStroke = false, Style = SKPaintStyle.Fill, TextSize = textSize, Color = color, IsAntialias = true };
        }
        public /*interface Colorer*/ SKPaint FillPaint(SKColor color) { 
            return new SKPaint { IsStroke = false, Style = SKPaintStyle.Fill, Color = color, IsAntialias = true };
        }
        public /*interface Colorer*/ SKPaint LinePaint(float strokeWidth, SKColor color) {
            return new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth, Color = color, IsAntialias = true };
        }
        public virtual /*interface Colorer*/ SKRect MeasureText(string text, SKPaint paint) {
            if (string.IsNullOrEmpty(text)) return new SKRect(0, 0, 0, 0); // or MeasureText will crash
            var bounds = new SKRect();
            float length = paint.MeasureText(text, ref bounds);
            return bounds;
        }
    }

    public class SKPainter : SKColorer, Painter {
        //### more painting routines are in GraphLayout.cs e.g. for Splines
        protected SKCanvas canvas;
        public SKPainter(SKCanvas canvas) : base() {
            this.canvas = canvas;
        }
        public /*interface Painter*/ void Clear(SKColor background) {
            canvas.Clear(background);
        }
        public /*interface Painter*/ void DrawRect(SKRect rect, SKPaint paint) {
            canvas.DrawRect(rect, paint);
        }
        public /*interface Painter*/ void DrawRoundRect(SKRect rect, float padding, SKPaint paint) {
            canvas.DrawRoundRect(rect, padding, padding, paint);
        }
        public /*interface Painter*/ void DrawCircle(SKPoint p, float radius, SKPaint paint) {
            canvas.DrawCircle(p.X, p.Y, radius, paint);
        }
        public /*interface Painter*/ void DrawText(string text, SKPoint point, SKPaint paint) {
            canvas.DrawText(text, point, paint);
        }
        public /*interface Painter*/ object GetCanvas() {
            return this.canvas;
        }
    }

    // ====  PLATFORM-NEUTRAL GUI INTERFACE =====

    public class Gui {
        public static Platform platform = Platform.NONE;
        public static ToGui gui; // hold "the" gui here  // ######## rename toGui
        //public static FromGui fromGui; // ############# store the clickerHandler here?
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
            if (double.IsNaN(value)) return "NaN";
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

    // Calls from (typically) the Main thread that need to run in (typically) the Gui thread
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

        public abstract void ChartUpdate();
        public abstract void LegendUpdate();

        public abstract void OutputClear(string title);
        public abstract void ProcessOutput();
        public abstract void ProcessGraph(string graphFamily);  // deliver execution output in graph form

        public abstract void ParametersClear();
        public abstract void AddParameter(string parameter, double drawn, string distribution, double[] args);
        public abstract double ParameterOracle(string parameter); // returns NAN if oracle not available
        public abstract void ParametersUpdate();

        public abstract void DeviceShow();
        public abstract void DeviceHide();
        public abstract void DeviceUpdate();

        public abstract void SaveInput();
        public abstract void RestoreInput();
        public abstract void ClipboardSetText(string text);
        public abstract void ChartSnap();
        public abstract void ChartSnapToSvg();
        public abstract void ChartData();
        public abstract void OutputCopy();
    }

    // Calls/callbacks from (typically) the Gui thread
    // And device-dependent controllers to which we attach Gui-thread-run callbacks
    // This is used only by Win and Mac, that have a common interface logic

    public interface FromGui {
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
        bool IsMicrofluidicsVisible();
        void MicrosfluidicsVisible(bool on);
        void MicrofluidicsOn();
        void MicrofluidicsOff();
        void IncrementFont(float pointSize);
        void Save();
        void Load();
        void SetDirectory();
        void SplashOff();
    }

    // ====  COMMON WIN/MAC ONOFF BUTTONS =====

    public interface KControl { }

    public interface KButton : KControl {
        string GetText();
        void SetText(string text);
        void SetImage(string image);
        void SetLegendImage(KSeries series);
        bool IsVisible();
        void Visible(bool b);
        bool IsEnabled();
        void Enabled(bool b);
        bool IsSelected();
        void Selected(bool b);
        void Hover(bool b);
        void OnClick(EventHandler handler);
    }

    public interface KSlider : KControl {
        void SetBounds(int min, int max);
        void SetValue(int value);
        int GetValue();
        void OnClick(EventHandler handler);
    }

    // ====  COMMON WIN/MAC FLYOUT MENUS =====

    public interface KFlyoutMenu : KButton {
        bool autoClose { get; set; }
        KButton selectedItem { get; set; }
        void ClearMenuItems();
        void AddMenuItem(KControl item);     // a single item
        void AddMenuItems(KControl[] items); // a row of items as a single item (not spread in a grid)
        void AddMenuRow(KControl[] items);   // a row of items spread in a grid
        void AddMenuGrid(KControl[,] items); // a full grid of items
        void AddSeparator();
        KButton NewMenuSection(int level = 1);
        KButton NewMenuItemButton();
        KSlider NewMenuItemTrackBar();
        bool IsOpen();
        void Open();
        void Close();
    }

    // ====  COMMON WIN/MAC CLICKER SETUP =====

    public class ClickerHandler {
        private FromGui fromGui;
        private KFlyoutMenu currentlyOpenMenu;

        public ClickerHandler(FromGui fromGui) {
            this.fromGui = fromGui;
            this.currentlyOpenMenu = null;

            StartButton();
            StopButton();
            SaveButton();
            LoadButton();
            DeviceButton();
            DeviceViewButton();
            FontSizePlusButton();
            FontSizeMinusButton();

            TutorialMenu();
            ExportMenu();
            OutputMenu();
            MathMenu();
            SettingsMenu();
            NoiseMenu();
            LegendMenu();
            ParametersMenu();
        }

        private void MenuClicked(KFlyoutMenu menu) {
            if (menu.IsOpen()) { 
                menu.Close();
                if (menu.autoClose) currentlyOpenMenu = null; 
            } else {
                if (menu.autoClose && currentlyOpenMenu != null) currentlyOpenMenu.Close();
                menu.Open();
                if (menu.autoClose) currentlyOpenMenu = menu;
            }
        }

        private void InitItemClicked(KFlyoutMenu menu, KButton menuItem, bool setSelection) {
            if (setSelection) {
                menu.selectedItem = menuItem;
                menuItem.Selected(true);
            }
        }

        private void ItemClicked(KFlyoutMenu menu, KButton menuItem, bool setSelection) {
            if (setSelection) {
                if (menu.selectedItem != null) menu.selectedItem.Selected(false);
                menu.selectedItem = menuItem;
                menuItem.Selected(true);
            }
            menu.Close();
            if (menu.autoClose) currentlyOpenMenu = null;
        }

        public void CloseOpenMenu() {
            if (currentlyOpenMenu != null) currentlyOpenMenu.Close();
        }

        private void NullHandler(object sender, EventArgs e) { }

        // Start button

        private void StartButton() {
            fromGui.onOffEval.SetImage("icons8play40");
            fromGui.onOffEval.OnClick(
                (object sender, EventArgs e) => {
                    // if (!modelInfo.executable) return;
                    CloseOpenMenu();
                    StartAction(forkWorker: true, autoContinue: false);
                });
            fromGui.onOffEval.Visible(true);
            fromGui.onOffEval.Enabled(true);
        }

        private void StartAction(bool forkWorker, bool autoContinue = false) {
            fromGui.SplashOff();
            if (Exec.IsExecuting() && !ContinueEnabled()) return; // we are already running a simulation, don't start a concurrent one
            if (Exec.IsExecuting() && ContinueEnabled()) { // we are already running a simulation; make start button work as continue button
                Protocol.continueExecution = true;
            } else { // do a start
                Exec.Execute_Starter(forkWorker, autoContinue: autoContinue); // This is where it all happens
            }
        }

        private void SetStartButtonToContinue() {
            fromGui.onOffEval.SetImage("icons8pauseplay40");
            fromGui.onOffEval.Enabled(true);
        }
        private void SetContinueButtonToStart() {
            fromGui.onOffEval.SetImage("icons8play40");
            fromGui.onOffEval.Enabled(false);

        }
        private bool continueButtonIsEnabled = false;
        public void ContinueEnable(bool b) {
            continueButtonIsEnabled = b;
            if (continueButtonIsEnabled) SetStartButtonToContinue(); else SetContinueButtonToStart();
        }
        public bool ContinueEnabled() {
            return continueButtonIsEnabled;
        }

        public void Executing(bool executing) {
            if (executing) {
                fromGui.onOffDevice.Enabled(false);
                fromGui.onOffEval.Enabled(false);
                fromGui.onOffStop.Enabled(true); fromGui.onOffStop.Visible(true);
                fromGui.menuNoise.Enabled(false);
                fromGui.menuTutorial.Enabled(false);
                fromGui.menuExport.Enabled(false);
                fromGui.menuOutput.Enabled(false);
            } else {
                fromGui.onOffDevice.Enabled(true);
                fromGui.onOffEval.Enabled(true); 
                fromGui.onOffStop.Visible(false); fromGui.onOffStop.Enabled(false);
                fromGui.menuLegend.Visible(true);
                fromGui.menuNoise.Enabled(true);
                fromGui.menuTutorial.Enabled(true);
                fromGui.menuExport.Visible(true); fromGui.menuExport.Enabled(true);
                fromGui.menuOutput.Enabled(true);
            }
        }

        // Stop button

        private void StopButton() {
            fromGui.onOffStop.SetImage("icons8stop40");
            fromGui.onOffStop.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    Exec.EndingExecution(); // signals that we should stop
                });
            fromGui.onOffStop.Visible(false);
            fromGui.onOffStop.Enabled(false);
        }

        // Save button

        private void SaveButton() {
            fromGui.onOffSave.SetImage("FileSave_48x48");
            fromGui.onOffSave.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    fromGui.onOffSave.Selected(true);
                    fromGui.Save();
                    fromGui.onOffSave.Selected(false);
                });
            fromGui.onOffSave.Visible(true);
            fromGui.onOffSave.Enabled(true);
        }

        // Load button

        private void LoadButton() {
            fromGui.onOffLoad.SetImage("FileLoad_48x48");
            fromGui.onOffLoad.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    fromGui.onOffLoad.Selected(true);
                    fromGui.Load();
                    fromGui.onOffLoad.Selected(false);
                });
            fromGui.onOffLoad.Visible(true);
            fromGui.onOffLoad.Enabled(true);
        }

        // Device button

        private void DeviceButton() {
            fromGui.onOffDevice.SetImage("icons8device_OFF_48x48");
            fromGui.onOffDevice.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    if (!ProtocolDevice.Exists()) {
                        ProtocolDevice.Start(30, 100);
                        fromGui.MicrofluidicsOn();
                        fromGui.onOffDevice.SetImage("icons8device_ON_48x48");
                        fromGui.onOffDeviceView.Visible(true);
                        fromGui.onOffDeviceView.Selected(true);
                    } else {
                        if (!Exec.IsExecuting()) {
                            fromGui.onOffDeviceView.Visible(false);
                            fromGui.MicrofluidicsOff();
                            fromGui.onOffDevice.SetImage("icons8device_OFF_48x48");
                            ProtocolDevice.Stop();
                        }
                    }
                });
            fromGui.onOffDevice.Visible(true);
            fromGui.onOffDevice.Enabled(true);
        }

        // Device View button

        private void DeviceViewButton() {
            fromGui.onOffDeviceView.SetImage("deviceBorder_W_48x48");
            fromGui.onOffDeviceView.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    if (fromGui.IsMicrofluidicsVisible()) {
                        fromGui.MicrosfluidicsVisible(false);
                        fromGui.onOffDeviceView.Selected(false);
                    } else {
                        fromGui.MicrofluidicsOn();
                        fromGui.onOffDeviceView.Selected(true);
                    }
                });
            fromGui.onOffDeviceView.Visible(false);
            fromGui.onOffDeviceView.Enabled(true);
        }

        // Font Size plus button

        private void FontSizePlusButton() {
            fromGui.onOffFontSizePlus.SetImage("FontSizePlus_W_48x48");
            fromGui.onOffFontSizePlus.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    fromGui.IncrementFont(1);
                });
            fromGui.onOffFontSizePlus.Visible(true);
            fromGui.onOffFontSizePlus.Enabled(true);
        }

        // Font Size minus button

        private void FontSizeMinusButton() {
            fromGui.onOffFontSizeMinus.SetImage("FontSizeMinus_W_48x48");
            fromGui.onOffFontSizeMinus.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    fromGui.IncrementFont(-1);
                });
            fromGui.onOffFontSizeMinus.Visible(true);
            fromGui.onOffFontSizeMinus.Enabled(true);
        }

        // Tutorial menu

        private void TutorialMenu() {
            fromGui.menuTutorial.SetImage("icons8text_48x48");
            fromGui.menuTutorial.OnClick((object s, EventArgs e) => { MenuClicked(fromGui.menuTutorial); });
            fromGui.menuTutorial.autoClose = true;
            fromGui.menuTutorial.ClearMenuItems();
            List<ModelInfoGroup> groups = Tutorial.Groups();
            const int tutorialColumns = 2; const int tutorialRows = 26;
            var tutorialItems = new KButton[tutorialColumns, tutorialRows];
            int it = 0; int jt = 0;
            foreach (ModelInfoGroup group in groups) {
                KButton menuSection = fromGui.menuTutorial.NewMenuSection(); menuSection.SetText(group.GroupHeading);
                if (jt >= tutorialRows) {jt = 0; it++;} if (it < tutorialColumns) { tutorialItems[it,jt] = menuSection; } jt++;
                foreach (ModelInfo info in group) {
                    ModelInfo menuSelection = info;
                    KButton menuItem = fromGui.menuTutorial.NewMenuItemButton();
                    menuItem.SetText(menuSelection.title);
                    menuItem.OnClick((object s, EventArgs e) => {
                        ItemClicked(fromGui.menuTutorial, menuItem, false);               // handle the selection graphical feedback
                        SelectTutorial(fromGui.menuTutorial, menuItem, menuSelection);    // handle storing the menuSelection value
                    });
                    if (jt >= tutorialRows) {jt = 0; it++;} if (it < tutorialColumns) { tutorialItems[it,jt] = menuItem; } jt++;
                }
            }
            fromGui.menuTutorial.AddMenuGrid(tutorialItems);
            fromGui.menuTutorial.Visible(true);
            fromGui.menuTutorial.Enabled(true);
        }

        private void SelectTutorial(KFlyoutMenu menu, KButton menuItem, ModelInfo menuSelection) {
            Gui.gui.InputSetText(menuSelection.text);
        }

        // Export menu

        private void ExportMenu() {
            fromGui.menuExport.SetImage("icons8_share_384_W_48x48");
            fromGui.menuExport.OnClick((object s, EventArgs e) => { MenuClicked(fromGui.menuExport); });
            fromGui.menuExport.autoClose = true;
            fromGui.menuExport.ClearMenuItems();
            KButton headExportItem = fromGui.menuExport.NewMenuSection(-1); headExportItem.SetText("Share");
            fromGui.menuExport.AddMenuItem(headExportItem);
            fromGui.menuExport.AddSeparator();
            foreach (ExportAction export in Exec.exportActionsList()) {
                ExportAction menuSelection = export;
                KButton menuItem = fromGui.menuExport.NewMenuItemButton();
                menuItem.SetText(export.name);
                menuItem.OnClick((object s, EventArgs e) => { 
                    ItemClicked(fromGui.menuExport, menuItem, false);           // handle the selection graphical feedback
                    SelectExport(fromGui.menuExport, menuItem, menuSelection);  // handle storing the menuSelection value
                });
                fromGui.menuExport.AddMenuItem(menuItem);
            }
            fromGui.menuExport.Visible(false);
            fromGui.menuExport.Enabled(true);
        }

        private void SelectExport(KFlyoutMenu menu, KButton menuItem, ExportAction menuSelection) {
            menuSelection.action();
        }

        // Output menu

        private void OutputMenu() {
            fromGui.menuOutput.SetImage("Computation_48x48");
            fromGui.menuOutput.OnClick((object s, EventArgs e) => { MenuClicked(fromGui.menuOutput); });
            fromGui.menuOutput.autoClose = true;
            fromGui.menuOutput.ClearMenuItems();
            KButton headOutputItem = fromGui.menuOutput.NewMenuSection(-1); headOutputItem.SetText("Computed Output");
            fromGui.menuOutput.AddMenuItem(headOutputItem);
            fromGui.menuOutput.AddSeparator();
            foreach (ExportAction output in Exec.outputActionsList()) {
                ExportAction menuSelection = output;
                KButton menuItem = fromGui.menuOutput.NewMenuItemButton();
                menuItem.SetText(output.name);
                menuItem.OnClick((object s, EventArgs e) => { 
                    ItemClicked(fromGui.menuOutput, menuItem, true);           // handle the selection graphical feedback
                    SelectOutput(fromGui.menuOutput, menuItem, menuSelection);  // handle storing the menuSelection value
                });
                fromGui.menuOutput.AddMenuItem(menuItem);
                if (menuSelection.name == "Show initial CRN") { // initialize default selection
                    InitItemClicked(fromGui.menuOutput, menuItem, true);      // handle the selection graphical feedback
                    InitSelectOutput(menuSelection);                          // handle storing the menuSelection value
                }
            }
            fromGui.menuOutput.Visible(true);
            fromGui.menuOutput.Enabled(true);
        }

        private void InitSelectOutput(ExportAction outputAction) {
            Exec.currentOutputAction = outputAction;
        }
        private void SelectOutput(KFlyoutMenu menu, KButton menuItem, ExportAction menuSelection) {
            Exec.currentOutputAction = menuSelection; 
            Exec.currentOutputAction.action();
        }

        // Math menu

        private void MathMenu() {
            fromGui.menuMath.SetImage("icons8_keyboard_96_W_48x48");
            fromGui.menuMath.OnClick((object s, EventArgs e) => { MenuClicked(fromGui.menuMath); });
            fromGui.menuMath.autoClose = true;
            fromGui.menuMath.ClearMenuItems();
            const int mathColumns = 5; const int mathRows = 8;
            var mathItems = new KButton[mathColumns, mathRows];
            int im = 0; int jm = 0;
            foreach (string symbol in SharedAssets.symbols) {
                string menuSelection = symbol;
                KButton menuItem = fromGui.menuMath.NewMenuItemButton();
                menuItem.SetText(symbol);
                menuItem.OnClick((object s, EventArgs e) => { 
                    ItemClicked(fromGui.menuMath, menuItem, false);           // handle the selection graphical feedback
                    SelectMath(fromGui.menuMath, menuItem, menuSelection);  // handle storing the menuSelection value
                });
                if (jm >= mathRows) {jm = 0; im++;} if (im < mathColumns) { mathItems[im,jm] = menuItem; } jm++;
            }
            fromGui.menuMath.AddMenuGrid(mathItems);
            fromGui.menuMath.Visible(true);
            fromGui.menuMath.Enabled(true);
        }

        private void SelectMath(KFlyoutMenu menu, KButton menuItem, string menuSelection) {
            Gui.gui.InputInsertText(menuSelection);
        }

        // Settings menu

        private static Environment.SpecialFolder defaultUserDataDirectoryPath = Environment.SpecialFolder.MyDocuments;
        private static Environment.SpecialFolder defaultKaemikaDataDirectoryPath = Environment.SpecialFolder.ApplicationData;
        private static string defaultUserDataDirectory = Environment.GetFolderPath(defaultUserDataDirectoryPath);
        private static string defaultKaemikaDataDirectory = Environment.GetFolderPath(defaultKaemikaDataDirectoryPath) + "\\Kaemika";

        public static string solver = "RK547M";
        public static bool precomputeLNA = false;

        private void SettingsMenu() {
            fromGui.menuSettings.SetImage("icons8_settings_384_W_48x48");
            fromGui.menuSettings.OnClick((object s, EventArgs e) => { MenuClicked(fromGui.menuSettings); });
            fromGui.menuSettings.autoClose = true;

            var header = fromGui.menuSettings.NewMenuSection(level: -2); header.SetText("Settings");
            var solvers = fromGui.menuSettings.NewMenuSection(level: 2); solvers.SetText("ODE Solvers");
            var rk547m = fromGui.menuSettings.NewMenuItemButton(); rk547m.SetText("RK547M"); rk547m.Selected(true);
            var gearBDF = fromGui.menuSettings.NewMenuItemButton(); gearBDF.SetText("GearBDF");
            rk547m.OnClick((object s, EventArgs e) => {
                gearBDF.Selected(false);
                solver = "RK547M";
                rk547m.Selected(true);
            });
            gearBDF.OnClick((object s, EventArgs e) => {
                rk547m.Selected(false);
                solver = "GearBDF";
                gearBDF.Selected(true);
            });
            var lna = fromGui.menuSettings.NewMenuSection(level: 2); lna.SetText("LNA");
            var drift = fromGui.menuSettings.NewMenuItemButton(); drift.SetText("Precompute drift");
            drift.OnClick((object s, EventArgs e) => {
                if (drift.IsSelected()) {
                    precomputeLNA = false;
                    drift.Selected(false);
                } else {
                    precomputeLNA = true;
                    drift.Selected(true);
                }
            });
            var directories = fromGui.menuSettings.NewMenuSection(level: 2); directories.SetText("Set directories");
            var forModels = fromGui.menuSettings.NewMenuItemButton(); forModels.SetText("For model files");
            forModels.OnClick((object s, EventArgs e) => {
                fromGui.SetDirectory();
            });
            var version = fromGui.menuSettings.NewMenuSection(level: 4); version.SetText("Version 6.02214076e23");

            fromGui.menuSettings.ClearMenuItems();
            fromGui.menuSettings.AddMenuItem(header);
            fromGui.menuSettings.AddSeparator();
            fromGui.menuSettings.AddMenuItem(solvers);
            fromGui.menuSettings.AddMenuItems(new KButton[2] { rk547m, gearBDF });
            fromGui.menuSettings.AddSeparator();
            fromGui.menuSettings.AddMenuItem(lna);
            fromGui.menuSettings.AddMenuItem(drift);
            fromGui.menuSettings.AddSeparator();
            fromGui.menuSettings.AddMenuItem(directories);
            fromGui.menuSettings.AddMenuItem(forModels);
            fromGui.menuSettings.AddSeparator();
            fromGui.menuSettings.AddMenuItem(version);

            fromGui.menuSettings.Visible(true);
            fromGui.menuSettings.Enabled(true);
        }

        // Noise menu

        private void NoiseMenu() {
            fromGui.menuNoise.SetImage(ImageOfNoise(Noise.None));
            fromGui.menuNoise.OnClick((object s, EventArgs e) => { MenuClicked(fromGui.menuNoise); });
            fromGui.menuNoise.autoClose = true;
            fromGui.menuNoise.ClearMenuItems();
            KButton headNoiseItem = fromGui.menuNoise.NewMenuSection(); headNoiseItem.SetText("  LNA");
            fromGui.menuNoise.AddMenuItem(headNoiseItem);
            fromGui.menuNoise.AddSeparator();
            foreach (Noise noise in Gui.noise) {
                Noise menuSelection = noise;
                KButton menuItem = fromGui.menuNoise.NewMenuItemButton();
                if (menuSelection == Noise.None) { // initialize default selection
                    InitItemClicked(fromGui.menuNoise, menuItem, true);      // handle the selection graphical feedback
                    InitSelectNoise(fromGui.menuNoise, menuSelection);       // handle storing the menuSelection value
                }
                menuItem.SetImage(ImageOfNoise(noise));
                menuItem.OnClick((object s, EventArgs e) => { 
                    ItemClicked(fromGui.menuNoise, menuItem, true);           // handle the selection graphical feedback
                    SelectNoise(fromGui.menuNoise, menuItem, menuSelection);  // handle storing the menuSelection value
                });
                fromGui.menuNoise.AddMenuItem(menuItem);
            }
            fromGui.menuNoise.Visible(true);
            fromGui.menuNoise.Enabled(true);
        }

        public static Noise SelectNoiseSelectedItem = Noise.None;
        private void InitSelectNoise(KFlyoutMenu menu, Noise newNoise) {
            SelectNoiseSelectedItem = newNoise;
            menu.SetImage(ImageOfNoise(newNoise));
        }
        private void SelectNoise(KFlyoutMenu menu, KButton menuItem, Noise newNoise) {
            Noise oldNoise = SelectNoiseSelectedItem;
            SelectNoiseSelectedItem = newNoise;
            menu.SetImage(ImageOfNoise(newNoise));
            menuItem.Selected(true);
            if (newNoise != oldNoise) StartAction(forkWorker: true, autoContinue: false);
        }

        private string ImageOfNoise(Noise noise) {
            if (noise == Noise.None) return "Noise_None_W_48x48";
            if (noise == Noise.SigmaRange) return "Noise_SigmaRange_W_48x48";
            if (noise == Noise.Sigma) return "Noise_Sigma_W_48x48";
            if (noise == Noise.CV) return "Noise_CV_W_48x48";
            if (noise == Noise.SigmaSqRange) return "Noise_SigmaSqRange_W_48x48";
            if (noise == Noise.SigmaSq) return "Noise_SigmaSq_W_48x48";
            if (noise == Noise.Fano) return "Noise_Fano_W_48x48";
            throw new Error("ImageOfNoise");
        }

        // Legend menu

        private void LegendMenu() {
            fromGui.menuLegend.SetImage("icons8combochart96_W_48x48");
            fromGui.menuLegend.OnClick((object s, EventArgs e) => { MenuClicked(fromGui.menuLegend); });
            fromGui.menuLegend.autoClose = false;
            fromGui.menuLegend.ClearMenuItems();
            fromGui.menuLegend.Visible(false);
            fromGui.menuLegend.Enabled(true);
        }

        private static KSeries[] lastLegend = new KSeries[0];

        public void SetLegend(KSeries[] legend) {
            lastLegend = legend;
            SetLastLegend();
        }

        public void SetLastLegend() {
            KSeries[] legend = lastLegend;
            fromGui.menuLegend.ClearMenuItems();
            for (int i = legend.Length - 1; i >= 0; i--) {
                int row = legend.Length - 1 - i;
                KSeries series = legend[i]; // captured in the closure
                var seriesName = series.name;
                KButton line = fromGui.menuLegend.NewMenuSection(); line.SetLegendImage(series);
                KButton button = fromGui.menuLegend.NewMenuItemButton(); button.SetText(seriesName);
                KButton pad = fromGui.menuLegend.NewMenuSection(); pad.SetText("");
                button.OnClick((object s, EventArgs e) => {
                    KChartHandler.InvertVisible(seriesName);
                    line.SetLegendImage(series); // accessing the shared series data structure!
                    KChartHandler.VisibilityRemember();
                    Gui.gui.ChartUpdate();
                });
                fromGui.menuLegend.AddMenuRow(new KButton[3] { line, button, pad });
            }
            fromGui.menuLegend.Open();
        }

        // Parameters menu

        private void ParametersMenu() {
            fromGui.menuParameters.SetImage("Parameters_W_48x48");
            fromGui.menuParameters.OnClick((object s, EventArgs e) => { MenuClicked(fromGui.menuParameters); });
            fromGui.menuParameters.autoClose = false;
            fromGui.menuParameters.ClearMenuItems();
            fromGui.menuParameters.Visible(false);
            fromGui.menuParameters.Enabled(true);
        }

        public void ParametersUpdate() {
            lock (parameterLock) {
                RefreshParameters();
            }
            if (parameterStateDict.Count > 0) {
                fromGui.menuParameters.Visible(true);
                fromGui.menuParameters.Open();
            } else {
                fromGui.menuParameters.Close();
                fromGui.menuParameters.Visible(false);
            }
        }

        private void RefreshParameters() {
            // called with already locked parameterLock
            fromGui.menuParameters.ClearMenuItems();
            foreach (var kvp in parameterStateDict) {
                ParameterInfo info = parameterInfoDict[kvp.Key];
                ParameterState state = parameterStateDict[kvp.Key];

                var chkbox = fromGui.menuParameters.NewMenuItemButton();
                chkbox.SetText(info.ParameterLabel(true));
                chkbox.Selected(info.locked);
                chkbox.OnClick((object s, EventArgs e) => {
                    lock (parameterLock) {
                        ParameterInfo paramInfo = parameterInfoDict[info.parameter];
                        ParameterState paramState = parameterStateDict[info.parameter];
                        chkbox.Selected(!chkbox.IsSelected());
                        paramInfo.locked = chkbox.IsSelected();
                    }
                });
                var trackbar = fromGui.menuParameters.NewMenuItemTrackBar();
                trackbar.SetBounds(0, state.rangeSteps);
                trackbar.SetValue((info.range == 0.0) ? (state.rangeSteps / 2) : (int)(state.rangeSteps * (info.drawn - info.rangeMin) / info.range));
                trackbar.OnClick((object source, EventArgs e) => {
                    lock (parameterLock) {
                        if ((!parameterStateDict.ContainsKey(info.parameter)) || (!parameterInfoDict.ContainsKey(info.parameter))) return;
                        ParameterInfo paramInfo = parameterInfoDict[info.parameter];
                        ParameterState paramState = parameterStateDict[info.parameter];
                        paramInfo.drawn = paramInfo.rangeMin + trackbar.GetValue() / ((double)paramState.rangeSteps) * paramInfo.range;
                        chkbox.SetText(paramInfo.ParameterLabel(true));
                    } });
                fromGui.menuParameters.AddSeparator();
                fromGui.menuParameters.AddMenuItem(chkbox);
                fromGui.menuParameters.AddMenuItem(trackbar);
            }
        }

        private static Dictionary<string, ParameterInfo> parameterInfoDict = new Dictionary<string, ParameterInfo>(); // persistent information
        private static Dictionary<string, ParameterState> parameterStateDict = new Dictionary<string, ParameterState>();
        private static object parameterLock = new object(); // protects parameterInfoDict and parameterStateDict

        // clear the parameterStateDict at the beginning of every execution, but we keep the parametersInfoDict forever

        public void ParametersClear() {
            lock (parameterLock) {
                parameterStateDict = new Dictionary<string, ParameterState>();
                //clicker.menuParameters.ClearMenuItems();
            }
        }

        public void AddParameter(string parameter, double drawn, string distribution, double[] arguments) {
            lock (parameterLock) {
                if (!parameterInfoDict.ContainsKey(parameter)) {
                    parameterInfoDict[parameter] = new ParameterInfo(parameter, drawn, distribution, arguments);
                    parameterStateDict[parameter] = new ParameterState(parameter, parameterInfoDict[parameter]);
                }
            }
            parameterStateDict[parameter] = new ParameterState(parameter, parameterInfoDict[parameter]); // use the old value, not the one from drawn
            if (parameterInfoDict.ContainsKey(parameter) && parameterInfoDict[parameter].locked) return; // do not change the old value if locked
            ParameterInfo info = new ParameterInfo(parameter, drawn, distribution, arguments);           // use the new value, from drawn
            ParameterState state = new ParameterState(parameter, info);                                  // update the value
            parameterInfoDict[parameter] = info;
            parameterStateDict[parameter] = state;
        }

        public class ParameterState {
            public string parameter;
            public double value;
            public int rangeSteps;
            public ParameterState(string parameter, ParameterInfo info) {
                this.parameter = parameter;
                this.value = info.drawn;
                this.rangeSteps = (info.distribution == "bernoulli") ? 1 : 100;
            }
        }

        // ask the gui if this parameter is locked

        public double ParameterOracle(string parameter) { // returns NAN if oracle not available
            lock (parameterLock) {
                if (parameterInfoDict.ContainsKey(parameter) && parameterInfoDict[parameter].locked)
                    // parameter does not exist yet in parameterStateDict but will exist at the end of the run, and it will be locked
                    return parameterInfoDict[parameter].drawn;
                return double.NaN;
            }
        }

    }
}
