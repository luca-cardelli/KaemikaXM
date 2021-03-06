﻿using System;
using System.Collections.Generic;
using SkiaSharp;
using KaemikaAssets;

namespace Kaemika {

    // ====  PLATFORM-GUI REGISTRATION =====

    public abstract class KGui { // Provides platform independent access:     KGui.gui : KGuiControl (Win,Mac,XM),  KGui.kControls : KControls (Win,Mac)
        public static KGuiControl gui { get; private set; }
        public static void Register(KGuiControl platformGui)  { gui = platformGui; }
        public static KControls kControls { get; private set; }
        public static void Register(KControls platformControls) { kControls = platformControls; }
    }

    public interface KGuiControl { // the main gui Form of each platform implements this interface and registers itself on loading
        string GuiInputGetText();
        void GuiInputSetText(string text);
        void GuiInputInsertText(string text);
        void GuiInputSetErrorSelection(int lineNumber, int columnNumber, int length, string failCategory, string failMessage);
        void GuiInputSetEditable(bool editable);

        void GuiOutputTextShow();
        void GuiOutputTextHide();
        void GuiOutputSetText(string text, bool savePosition = false);
        string GuiOutputGetText();
        void GuiOutputAppendText(string text);

        void GuiDeviceShow();
        void GuiDeviceHide();
        void GuiDeviceUpdate();

        void GuiBeginningExecution();   // signals that execution is starting
        void GuiEndingExecution();     // signals that execution has ended (run to end, or stopped)
        void GuiContinueEnable(bool b);
        bool GuiContinueEnabled();

        void GuiSaveInput();
        void GuiRestoreInput();

        void GuiClipboardSetText(string text);
        void GuiChartSnap();
        void GuiChartSnapToSvg();
        void GuiModelToSBML();
        SKSize GuiChartSize();
        void GuiChartData();
        void GuiOutputCopy();

        void GuiOutputClear();
        void GuiProcessOutput();
        void GuiProcessGraph(string graphFamily);  // deliver execution output in graph form

        void GuiChartUpdate();
        void GuiLegendUpdate();
        void GuiParametersUpdate();

        void GuiScoreUpdate();
    }

    // ====  COMMON WIN/MAC BUTTONS =====

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
        void OnClick(EventHandler handler);
    }

    public interface KSlider : KControl {
        void SetBounds(int min, int max);
        void SetValue(int value);
        int GetValue();
        void OnClick(EventHandler handler);
    }
    
    public interface KNumerical : KControl {
        void SetBounds(double min, double max);
        void SetValue(double value);
        double GetValue();
        void OnClick(EventHandler handler);
    }

    // ====  COMMON WIN/MAC FLYOUT MENUS =====

    public interface KFlyoutMenu : KButton {
        bool autoClose { get; set; }
        KButton selectedItem { get; set; }
        void ClearMenuItems();
        void AddMenuItem(KControl item, string name = null); // a single item; the name can be used by SetSelection
        void AddMenuItems(KControl[] items); // a row of items as a single item (not spread in a grid)
        void AddMenuRow(KControl[] items);   // a row of items spread in a grid
        void AddMenuGrid(KControl[,] items); // a full grid of items
        void AddSeparator();
        KButton NewMenuSection(int level = 1);
        KButton NewMenuItemButton(bool multiline = false);
        KSlider NewMenuItemTrackBar();
        KNumerical NewMenuItemNumerical();
        void SetSelection(string name);
        bool IsOpen();
        void Open();
        void Close();
        void Invalidate();
    }

    // ====  COMMON WIN/MAC CONTROLS =====

    public class KControls {
        private GuiControls guiControls;
        private KFlyoutMenu currentlyOpenMenu;

        public KControls(GuiControls guiControls) {
            this.guiControls = guiControls;
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

        public static void ItemSelected(KFlyoutMenu menu, KButton menuItem) {
            if (menu.selectedItem != null) menu.selectedItem.Selected(false);
            menu.selectedItem = menuItem;
            menuItem.Selected(true);
        }
        private void ItemClicked(KFlyoutMenu menu, KButton menuItem, bool setSelection) {
            if (setSelection) ItemSelected(menu, menuItem);
            menu.Close();
            if (menu.autoClose) currentlyOpenMenu = null;
        }

        public void CloseOpenMenu() {
            if (currentlyOpenMenu != null) currentlyOpenMenu.Close();
        }

        private void NullHandler(object sender, EventArgs e) { }

        // Start button

        private void StartButton() {
            guiControls.onOffEval.SetImage("icons8play40");
            guiControls.onOffEval.OnClick(
                (object sender, EventArgs e) => {
                    if (currentModelInfo!=null && !currentModelInfo.executable) return;
                    CloseOpenMenu();
                    StartAction(forkWorker: true, autoContinue: guiControls.IsShiftDown());
                });
            guiControls.onOffEval.Visible(true);
            guiControls.onOffEval.Enabled(true);
        }

        private void StartAction(bool forkWorker, bool autoContinue = false) {
            guiControls.SplashOff();
            if (Exec.IsExecuting() && !ContinueEnabled()) return; // we are already running a simulation, don't start a concurrent one
            if (Exec.IsExecuting() && ContinueEnabled()) { // we are already running a simulation; make start button work as continue button
                Protocol.continueExecution = true;
                if (Exec.lastExecution != null) Exec.lastExecution.netlist.autoContinue = autoContinue;
            } else { // do a start
                Exec.Execute_Starter(forkWorker, autoContinue: autoContinue); // This is where it all happens
            }
        }

        private void SetStartButtonToContinue() {
            guiControls.onOffEval.SetImage("icons8pauseplay40");
            guiControls.onOffEval.Enabled(true);
        }
        private void SetContinueButtonToStart() {
            guiControls.onOffEval.SetImage("icons8play40");
            guiControls.onOffEval.Enabled(false);

        }
        private static bool continueButtonIsEnabled = false;
        public void ContinueEnable(bool b) {
            continueButtonIsEnabled = b;
            if (continueButtonIsEnabled) SetStartButtonToContinue(); else SetContinueButtonToStart();
        }
        public static bool ContinueEnabled() {
            return continueButtonIsEnabled;
        }

        public static bool IsSimulating() {
            return Exec.IsExecuting() && !ContinueEnabled();
        }

        public void Executing(bool executing) {
            if (executing) {
                guiControls.onOffDevice.Enabled(false);
                guiControls.onOffEval.Enabled(false);
                guiControls.onOffStop.Enabled(true); guiControls.onOffStop.Visible(true);
                guiControls.menuNoise.Enabled(false);
                guiControls.menuTutorial.Enabled(false);
                guiControls.menuExport.Enabled(false);
                guiControls.menuOutput.Enabled(false);
            } else {
                guiControls.onOffDevice.Enabled(true);
                guiControls.onOffEval.Enabled(true); 
                guiControls.onOffStop.Visible(false); guiControls.onOffStop.Enabled(false);
                guiControls.menuLegend.Visible(true);
                guiControls.menuNoise.Enabled(true);
                guiControls.menuTutorial.Enabled(true);
                guiControls.menuExport.Visible(true); guiControls.menuExport.Enabled(true);
                guiControls.menuOutput.Enabled(true);
            }
        }

        // Stop button

        private void StopButton() {
            guiControls.onOffStop.SetImage("icons8stop40");
            guiControls.onOffStop.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    Exec.EndingExecution(); // signals that we should stop
                });
            guiControls.onOffStop.Visible(false);
            guiControls.onOffStop.Enabled(false);
        }

        // Save button

        private void SaveButton() {
            guiControls.onOffSave.SetImage("FileSave_48x48");
            guiControls.onOffSave.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    guiControls.onOffSave.Selected(true);
                    guiControls.Save();
                    guiControls.onOffSave.Selected(false);
                });
            guiControls.onOffSave.Visible(true);
            guiControls.onOffSave.Enabled(true);
        }

        // Load button

        private void LoadButton() {
            guiControls.onOffLoad.SetImage("FileLoad_48x48");
            guiControls.onOffLoad.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    currentModelInfo = null;
                    KGui.gui.GuiInputSetEditable(true);
                    guiControls.onOffLoad.Selected(true);
                    guiControls.Load();
                    guiControls.onOffLoad.Selected(false);
                });
            guiControls.onOffLoad.Visible(true);
            guiControls.onOffLoad.Enabled(true);
        }

        // Device button

        private void DeviceButton() {
            guiControls.onOffDevice.SetImage("icons8device_OFF_48x48");
            guiControls.onOffDevice.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    if (!KDeviceHandler.Exists()) {
                        KDeviceHandler.Start(30, 100);
                        guiControls.MicrofluidicsOn();
                        guiControls.onOffDevice.SetImage("icons8device_ON_48x48");
                        guiControls.onOffDeviceView.Visible(true);
                        guiControls.onOffDeviceView.Selected(true);
                    } else {
                        if (!Exec.IsExecuting()) {
                            guiControls.onOffDeviceView.Visible(false);
                            guiControls.MicrofluidicsOff();
                            guiControls.onOffDevice.SetImage("icons8device_OFF_48x48");
                            KDeviceHandler.Stop();
                        }
                    }
                });
            guiControls.onOffDevice.Visible(true);
            guiControls.onOffDevice.Enabled(true);
        }

        // Device View button

        private void DeviceViewButton() {
            guiControls.onOffDeviceView.SetImage("deviceBorder_W_48x48");
            guiControls.onOffDeviceView.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    if (guiControls.IsMicrofluidicsVisible()) {
                        guiControls.MicrofluidicsVisible(false);
                        guiControls.onOffDeviceView.Selected(false);
                    } else {
                        guiControls.MicrofluidicsOn();
                        guiControls.onOffDeviceView.Selected(true);
                    }
                });
            guiControls.onOffDeviceView.Visible(false);
            guiControls.onOffDeviceView.Enabled(true);
        }

        // Font Size plus button

        private void FontSizePlusButton() {
            guiControls.onOffFontSizePlus.SetImage("FontSizePlus_W_48x48");
            guiControls.onOffFontSizePlus.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    guiControls.IncrementFont(1);
                });
            guiControls.onOffFontSizePlus.Visible(true);
            guiControls.onOffFontSizePlus.Enabled(true);
        }

        // Font Size minus button

        private void FontSizeMinusButton() {
            guiControls.onOffFontSizeMinus.SetImage("FontSizeMinus_W_48x48");
            guiControls.onOffFontSizeMinus.OnClick(
                (object sender, EventArgs e) => {
                    CloseOpenMenu();
                    guiControls.IncrementFont(-1);
                });
            guiControls.onOffFontSizeMinus.Visible(true);
            guiControls.onOffFontSizeMinus.Enabled(true);
        }

        // Tutorial menu

        private void TutorialMenu() {
            guiControls.menuTutorial.SetImage("icons8text_48x48");
            guiControls.menuTutorial.OnClick((object s, EventArgs e) => { MenuClicked(guiControls.menuTutorial); });
            guiControls.menuTutorial.autoClose = true;
            guiControls.menuTutorial.ClearMenuItems();
            List<ModelInfoGroup> groups = Tutorial.Groups();
            const int tutorialColumns = 2; const int tutorialRows = 26;
            var tutorialItems = new KButton[tutorialColumns, tutorialRows];
            int it = 0; int jt = 0;
            foreach (ModelInfoGroup group in groups) {
                KButton menuSection = guiControls.menuTutorial.NewMenuSection(); menuSection.SetText(group.GroupHeading);
                if (jt >= tutorialRows) {jt = 0; it++;} if (it < tutorialColumns) { tutorialItems[it,jt] = menuSection; } jt++;
                foreach (ModelInfo info in group) {
                    ModelInfo menuSelection = info;
                    KButton menuItem = guiControls.menuTutorial.NewMenuItemButton();
                    menuItem.SetText(menuSelection.title);
                    menuItem.OnClick((object s, EventArgs e) => {
                        ItemClicked(guiControls.menuTutorial, menuItem, false);               // handle the selection graphical feedback
                        SelectTutorial(guiControls.menuTutorial, menuItem, menuSelection);    // handle storing the menuSelection value
                    });
                    if (jt >= tutorialRows) {jt = 0; it++;} if (it < tutorialColumns) { tutorialItems[it,jt] = menuItem; } jt++;
                }
            }
            guiControls.menuTutorial.AddMenuGrid(tutorialItems);
            guiControls.menuTutorial.Visible(true);
            guiControls.menuTutorial.Enabled(true);
        }

        private static ModelInfo currentModelInfo;
        private void SelectTutorial(KFlyoutMenu menu, KButton menuItem, ModelInfo menuSelection) {
            currentModelInfo = menuSelection;
            KGui.gui.GuiInputSetEditable(true);
            KGui.gui.GuiInputSetText(menuSelection.text);
            KGui.gui.GuiInputSetEditable(menuSelection.executable);
        }

        // Export menu

        private void ExportMenu() {
            guiControls.menuExport.SetImage("icons8_share_384_W_48x48");
            guiControls.menuExport.OnClick((object s, EventArgs e) => { MenuClicked(guiControls.menuExport); });
            guiControls.menuExport.autoClose = true;
            guiControls.menuExport.ClearMenuItems();
            KButton headExportItem = guiControls.menuExport.NewMenuSection(-1); headExportItem.SetText("Share");
            guiControls.menuExport.AddMenuItem(headExportItem);
            guiControls.menuExport.AddSeparator();
            foreach (ExportAction export in Exec.exportActionsList()) {
                ExportAction menuSelection = export;
                KButton menuItem = guiControls.menuExport.NewMenuItemButton();
                menuItem.SetText(export.name);
                menuItem.OnClick((object s, EventArgs e) => { 
                    ItemClicked(guiControls.menuExport, menuItem, false);           // handle the selection graphical feedback
                    SelectExport(guiControls.menuExport, menuItem, menuSelection);  // handle storing the menuSelection value
                });
                guiControls.menuExport.AddMenuItem(menuItem);
            }
            guiControls.menuExport.Visible(false);
            guiControls.menuExport.Enabled(true);
        }

        private void SelectExport(KFlyoutMenu menu, KButton menuItem, ExportAction menuSelection) {
            guiControls.menuExport.Enabled(false);
            guiControls.menuExport.Selected(true);
            try
            { 
                menuSelection.action(); 
            } finally {
                guiControls.menuExport.Selected(false);
                guiControls.menuExport.Enabled(true);
            }
        }

        // Output menu

        private void OutputMenu() {
            guiControls.menuOutput.SetImage("Computation_48x48");
            guiControls.menuOutput.OnClick((object s, EventArgs e) => { MenuClicked(guiControls.menuOutput); });
            guiControls.menuOutput.autoClose = true;
            guiControls.menuOutput.ClearMenuItems();
            KButton headOutputItem = guiControls.menuOutput.NewMenuSection(-1); headOutputItem.SetText("Computed Output");
            guiControls.menuOutput.AddMenuItem(headOutputItem);
            guiControls.menuOutput.AddSeparator();
            foreach (ExportAction output in Exec.outputActionsList()) {
                ExportAction menuSelection = output;
                KButton menuItem = guiControls.menuOutput.NewMenuItemButton();
                menuItem.SetText(output.name);
                menuItem.OnClick((object s, EventArgs e) => { 
                    ItemClicked(guiControls.menuOutput, menuItem, true);           // handle the selection graphical feedback
                    ExecOutputAction(menuSelection);                               // handle storing the menuSelection value
                });
                guiControls.menuOutput.AddMenuItem(menuItem, name: output.name);
            }
            guiControls.menuOutput.Visible(true);
            guiControls.menuOutput.Enabled(true);
        }

        private void ExecOutputAction(ExportAction menuSelection) {
            Exec.currentOutputAction = menuSelection; 
            Exec.currentOutputAction.action();
            guiControls.SavePreferences();
        }
        public void SetOutputSelection(string name) {
            ExportAction action = Exec.OutputActionNamed(name);
            guiControls.menuOutput.SetSelection(action.name); // just calls back ItemSelected(menu,menuItem) after recovering the menuItem from the menu by name
            Exec.currentOutputAction = action;
        }

        // Math menu

        private void MathMenu() {
            guiControls.menuMath.SetImage("icons8_keyboard_96_W_48x48");
            guiControls.menuMath.OnClick((object s, EventArgs e) => { MenuClicked(guiControls.menuMath); });
            guiControls.menuMath.autoClose = true;
            guiControls.menuMath.ClearMenuItems();
            const int mathColumns = 5; const int mathRows = 8;
            var mathItems = new KButton[mathColumns, mathRows];
            int im = 0; int jm = 0;
            foreach (string symbol in SharedAssets.symbols) {
                string menuSelection = symbol;
                KButton menuItem = guiControls.menuMath.NewMenuItemButton();
                menuItem.SetText(symbol);
                menuItem.OnClick((object s, EventArgs e) => { 
                    ItemClicked(guiControls.menuMath, menuItem, false);           // handle the selection graphical feedback
                    SelectMath(guiControls.menuMath, menuItem, menuSelection);  // handle storing the menuSelection value
                });
                if (jm >= mathRows) {jm = 0; im++;} if (im < mathColumns) { mathItems[im,jm] = menuItem; } jm++;
            }
            guiControls.menuMath.AddMenuGrid(mathItems);
            guiControls.menuMath.Visible(true);
            guiControls.menuMath.Enabled(true);
        }

        private void SelectMath(KFlyoutMenu menu, KButton menuItem, string menuSelection) {
            KGui.gui.GuiInputInsertText(menuSelection);
        }

        // Settings menu

        private static Environment.SpecialFolder defaultUserDataDirectoryPath = Environment.SpecialFolder.MyDocuments;
        private static Environment.SpecialFolder defaultKaemikaDataDirectoryPath = Environment.SpecialFolder.ApplicationData;
        private static string defaultUserDataDirectory = Environment.GetFolderPath(defaultUserDataDirectoryPath);
        private static string defaultKaemikaDataDirectory = Environment.GetFolderPath(defaultKaemikaDataDirectoryPath) + "\\Kaemika";

        public static string scoreStyle = "Bach";
        public static string solver = "RK547M";
        public static bool precomputeLNA = false;
        public static bool packReactions = (Gui.platform == Platform.Android || Gui.platform == Platform.iOS) ? true : false;

        private void SettingsMenu() {
            guiControls.menuSettings.SetImage("icons8_settings_384_W_48x48");
            guiControls.menuSettings.autoClose = true;

            var header = guiControls.menuSettings.NewMenuSection(level: -2); header.SetText("Settings");
            var score = guiControls.menuSettings.NewMenuSection(level: 2); score.SetText("Reaction score");
            var mozart = guiControls.menuSettings.NewMenuItemButton(); mozart.SetText("Mozart"); 
            var bach = guiControls.menuSettings.NewMenuItemButton(); bach.SetText("Bach"); bach.Selected(true);
            // var influence = guiControls.menuSettings.NewMenuItemButton(); influence.SetText("influence"); // now unused in Settings
            // var binpacking = guiControls.menuSettings.NewMenuItemButton(); binpacking.SetText("[pack]"); // now unused in Settings
            mozart.OnClick((object s, EventArgs e) => {
                bach.Selected(false);
                scoreStyle = "Mozart";
                mozart.Selected(true);
                KScoreHandler.ScoreUpdate();
            });
            bach.OnClick((object s, EventArgs e) => {
                mozart.Selected(false);
                scoreStyle = "Bach";
                bach.Selected(true);
                KScoreHandler.ScoreUpdate();
            });
            //influence.OnClick((object s, EventArgs e) => { 
            //    if (influence.IsSelected()) {
            //        KScoreHandler.showInfluences = false;
            //        influence.Selected(false);
            //    } else {
            //        KScoreHandler.showInfluences = true;
            //        influence.Selected(true);
            //    }
            //    KScoreHandler.ScoreUpdate();
            //});
            //binpacking.OnClick((object s, EventArgs e) => {
            //    if (binpacking.IsSelected()) {
            //        packReactions = false;
            //        binpacking.Selected(false);
            //    } else {
            //        packReactions = true;
            //        binpacking.Selected(true);
            //    }
            //    KScoreHandler.ScoreUpdate();
            //});
            var solvers = guiControls.menuSettings.NewMenuSection(level: 2); solvers.SetText("ODE Solvers");
            var rk547m = guiControls.menuSettings.NewMenuItemButton(); rk547m.SetText("RK547M"); rk547m.Selected(true);
            var gearBDF = guiControls.menuSettings.NewMenuItemButton(); gearBDF.SetText("GearBDF");
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
            var lna = guiControls.menuSettings.NewMenuSection(level: 2); lna.SetText("LNA");
            var drift = guiControls.menuSettings.NewMenuItemButton(); drift.SetText("Precompute drift");
            drift.OnClick((object s, EventArgs e) => {
                if (drift.IsSelected()) {
                    precomputeLNA = false;
                    drift.Selected(false);
                } else {
                    precomputeLNA = true;
                    drift.Selected(true);
                }
            });
            var directories = guiControls.menuSettings.NewMenuSection(level: 2); directories.SetText("Set directories");
            var forModels = guiControls.menuSettings.NewMenuItemButton(); forModels.SetText("For model files");
            forModels.OnClick((object s, EventArgs e) => {
                guiControls.SetDirectory();
            });
            var privacy = guiControls.menuSettings.NewMenuSection(level: 2); privacy.SetText("Privacy policy");
            var policyURL = guiControls.menuSettings.NewMenuItemButton(); policyURL.SetText("Copy url to clipboard");
            policyURL.OnClick((object s, EventArgs e) => {
                guiControls.PrivacyPolicyToClipboard();
            });
            var version = guiControls.menuSettings.NewMenuSection(level: 4); version.SetText("Version " + Gui.KaemikaVersion); // version.SetText("Version 6.02214076e23");
            version.OnClick((object s, EventArgs e) => { guiControls.SetSnapshotSize(); });

            guiControls.menuSettings.OnClick((object s, EventArgs e) => {
                mozart.Selected(scoreStyle == "Mozart");
                bach.Selected(scoreStyle == "Bach");
                gearBDF.Selected(solver == "GearBDF");
                rk547m.Selected(solver == "RK547M");
                drift.Selected(precomputeLNA);
                MenuClicked(guiControls.menuSettings);
                });

            guiControls.menuSettings.ClearMenuItems();
            guiControls.menuSettings.AddMenuItem(header);
            guiControls.menuSettings.AddSeparator();
            guiControls.menuSettings.AddMenuItem(score);
            guiControls.menuSettings.AddMenuItems(new KButton[2] { mozart, bach });
            //guiControls.menuSettings.AddMenuItems(new KButton[3] { mozart, bach, binpacking });
            //guiControls.menuSettings.AddMenuItems(new KButton[2] { influence, binpacking });
            guiControls.menuSettings.AddSeparator();
            guiControls.menuSettings.AddMenuItem(solvers);
            guiControls.menuSettings.AddMenuItems(new KButton[2] { rk547m, gearBDF });
            guiControls.menuSettings.AddSeparator();
            guiControls.menuSettings.AddMenuItem(lna);
            guiControls.menuSettings.AddMenuItem(drift);
            guiControls.menuSettings.AddSeparator();
            guiControls.menuSettings.AddMenuItem(directories);
            guiControls.menuSettings.AddMenuItem(forModels);
            guiControls.menuSettings.AddSeparator();
            guiControls.menuSettings.AddMenuItem(privacy);
            guiControls.menuSettings.AddMenuItem(policyURL);
            guiControls.menuSettings.AddSeparator();
            guiControls.menuSettings.AddMenuItem(version);

            guiControls.menuSettings.Visible(true);
            guiControls.menuSettings.Enabled(true);
        }

        // Noise menu

        private void NoiseMenu() {
            guiControls.menuNoise.SetImage(ImageOfNoise(Noise.None));
            guiControls.menuNoise.OnClick((object s, EventArgs e) => { MenuClicked(guiControls.menuNoise); });
            guiControls.menuNoise.autoClose = true;
            guiControls.menuNoise.ClearMenuItems();
            KButton headNoiseItem = guiControls.menuNoise.NewMenuSection(); headNoiseItem.SetText("  LNA");
            guiControls.menuNoise.AddMenuItem(headNoiseItem);
            guiControls.menuNoise.AddSeparator();
            foreach (Noise noise in Gui.noise) {
                Noise menuSelection = noise;
                KButton menuItem = guiControls.menuNoise.NewMenuItemButton();
                if (menuSelection == Noise.None) { // initialize default selection
                    ItemSelected(guiControls.menuNoise, menuItem);               // handle the selection graphical feedback
                    InitSelectNoise(guiControls.menuNoise, menuSelection);       // handle storing the menuSelection value
                }
                menuItem.SetImage(ImageOfNoise(noise));
                menuItem.OnClick((object s, EventArgs e) => { 
                    ItemClicked(guiControls.menuNoise, menuItem, true);           // handle the selection graphical feedback
                    SelectNoise(guiControls.menuNoise, menuItem, menuSelection);  // handle storing the menuSelection value
                });
                guiControls.menuNoise.AddMenuItem(menuItem);
            }
            guiControls.menuNoise.Visible(true);
            guiControls.menuNoise.Enabled(true);
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
            guiControls.menuLegend.SetImage("icons8combochart96_W_48x48");
            guiControls.menuLegend.OnClick((object s, EventArgs e) => { MenuClicked(guiControls.menuLegend); });
            guiControls.menuLegend.autoClose = false;
            guiControls.menuLegend.ClearMenuItems();
            guiControls.menuLegend.Visible(false);
            guiControls.menuLegend.Enabled(true);
        }

        public void OnLegendClick(KSeries[] legend, KSeries series) {
            if (guiControls.IsShiftDown()) {
                KChartHandler.ShiftInvertVisible(series.name);
                foreach (var seriesI in legend) seriesI.lineButton.SetLegendImage(seriesI);
            } else {
                KChartHandler.InvertVisible(series.name);
                series.lineButton.SetLegendImage(series); // accessing the shared series data structure!
            }
            KChartHandler.VisibilityRemember();
            KChartHandler.ChartUpdate(null); // cannot provide style: would have to marshall it throug KGui but is ok to update
        }

        public void SetLegend() { // Called by LegendUpdate
            KSeries[] legend = KChartHandler.Legend();
            guiControls.menuLegend.ClearMenuItems();
            for (int i = legend.Length - 1; i >= 0; i--) {
                KSeries series = legend[i]; // captured in the OnClick closure
                series.lineButton = guiControls.menuLegend.NewMenuSection(); series.lineButton.SetLegendImage(series);
                series.lineButton.OnClick((object s, EventArgs e) => { OnLegendClick(legend, series); });
                series.nameButton = guiControls.menuLegend.NewMenuItemButton(); series.nameButton.SetText(series.name);
                series.nameButton.OnClick((object s, EventArgs e) => { OnLegendClick(legend, series); });
                guiControls.menuLegend.AddMenuRow(new KButton[2] { series.lineButton, series.nameButton }); //, pad });
            }
            guiControls.menuLegend.Open();
        }

        public void InvalidateLegend() {
            guiControls.menuLegend.Invalidate();
        }

        // ======== PARAMETERS ========= //

        // Parameters menu (parameters panel)

        private void ParametersMenu() {
            guiControls.menuParameters.SetImage("Parameters_W_48x48");
            guiControls.menuParameters.OnClick((object s, EventArgs e) => { MenuClicked(guiControls.menuParameters); });
            guiControls.menuParameters.autoClose = false;
            guiControls.menuParameters.ClearMenuItems();
            guiControls.menuParameters.Visible(false);
            guiControls.menuParameters.Enabled(true);
        }

        public void ParametersUpdate() {
            lock (parameterLock) {
                RefreshParameters();
            }
            if (parameterStateDict.Count > 0) {
                guiControls.menuParameters.Visible(true);
                guiControls.menuParameters.Open();
            } else {
                guiControls.menuParameters.Close();
                guiControls.menuParameters.Visible(false);
            }
        }

        private void RefreshParameters() {
            // called with already locked parameterLock
            guiControls.menuParameters.ClearMenuItems();
            foreach (var kvp in parameterStateDict) {
                ParameterInfo info = parameterInfoDict[kvp.Key];
                ParameterState state = parameterStateDict[kvp.Key];

                var chkbox = guiControls.menuParameters.NewMenuItemButton(multiline:false);
                chkbox.SetText(info.ParameterLabel());
                chkbox.Selected(info.locked);
                chkbox.OnClick((object s, EventArgs e) => {
                    lock (parameterLock) {
                        if ((!parameterStateDict.ContainsKey(info.parameter)) || (!parameterInfoDict.ContainsKey(info.parameter))) {
                            parameterInfoDict = new Dictionary<string, ParameterInfo>();
                            parameterStateDict = new Dictionary<string, ParameterState>();
                            guiControls.menuParameters.ClearMenuItems();
                            return;
                        }
                        ParameterInfo paramInfo = parameterInfoDict[info.parameter];
                        ParameterState paramState = parameterStateDict[info.parameter];
                        chkbox.Selected(!chkbox.IsSelected());
                        paramInfo.locked = chkbox.IsSelected();
                    }
                });
                var trackbar = guiControls.menuParameters.NewMenuItemNumerical();
                trackbar.SetValue(info.drawn);
                trackbar.OnClick((object source, EventArgs e) => {
                    lock (parameterLock) {
                        if ((!parameterStateDict.ContainsKey(info.parameter)) || (!parameterInfoDict.ContainsKey(info.parameter))) {
                            parameterInfoDict = new Dictionary<string, ParameterInfo>();
                            parameterStateDict = new Dictionary<string, ParameterState>();
                            guiControls.menuParameters.ClearMenuItems();
                            return;
                        }
                        ParameterInfo paramInfo = parameterInfoDict[info.parameter];
                        ParameterState paramState = parameterStateDict[info.parameter];
                        paramInfo.drawn = trackbar.GetValue();
                        chkbox.SetText(paramInfo.ParameterLabel());
                    } });
                guiControls.menuParameters.AddSeparator();
                guiControls.menuParameters.AddMenuItem(chkbox);
                guiControls.menuParameters.AddMenuItem(trackbar);
            }
        }

        public void InvalidateParameters() {
            guiControls.menuParameters.Invalidate();
        }

        // Parameter Logic (GUI-free)

        public static Dictionary<string, ParameterInfo> parameterInfoDict = new Dictionary<string, ParameterInfo>(); // persistent information
        public static Dictionary<string, ParameterState> parameterStateDict = new Dictionary<string, ParameterState>();
        public static object parameterLock = new object(); // protects parameterInfoDict and parameterStateDict

        public class ParameterState {
            public string parameter;
            public double value;
            public ParameterState(string parameter, ParameterInfo info) {
                this.parameter = parameter;
                this.value = info.drawn;
            }
        }

        public static void ParametersClear(Style style) {
            // clear the parameterStateDict at the beginning of every execution, but we keep the parametersInfoDict forever
            if (!style.chartOutput) return;
            lock (parameterLock) {
                parameterStateDict = new Dictionary<string, ParameterState>();
            }
        }

        public static void AddParameter(string parameter, double drawn, DistributionValue distribution, Style style) {
            lock (parameterLock) {
                if (!parameterInfoDict.ContainsKey(parameter)) {
                    parameterInfoDict[parameter] = new ParameterInfo(parameter, drawn, distribution, style);
                    parameterStateDict[parameter] = new ParameterState(parameter, parameterInfoDict[parameter]);
                }
            }
            parameterStateDict[parameter] = new ParameterState(parameter, parameterInfoDict[parameter]); // use the old value, not the one from drawn
            if (parameterInfoDict.ContainsKey(parameter) && parameterInfoDict[parameter].locked) return; // do not change the old value if locked
            ParameterInfo info = new ParameterInfo(parameter, drawn, distribution, style);               // use the new value, from drawn
            ParameterState state = new ParameterState(parameter, info);                                  // update the value
            parameterInfoDict[parameter] = info;
            parameterStateDict[parameter] = state;
        }

        public static double ParameterOracle(string parameter) { // returns NAN if oracle not available
            // ask the gui if this parameter is locked
            lock (parameterLock) {
                if (parameterInfoDict.ContainsKey(parameter) && parameterInfoDict[parameter].locked)
                    // parameter does not exist yet in parameterStateDict but will exist at the end of the run, and it will be locked
                    return parameterInfoDict[parameter].drawn;
                return double.NaN;
            }
        }

    }
}
