using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.Collections.Generic;
using Kaemika;

namespace KaemikaXM.Pages {

    public class ModelEntryPage : KaemikaPage {

        public ModelInfo modelInfo;
        public ICustomTextEdit editor;
        public ToolbarItem editItem;
        public ToolbarItem pasteAllItem;
        public ToolbarItem copyAllItem;
        public Grid topBar;
        public Grid bottomBar;
        public Picker noisePicker;
        public Picker subPicker;
        public Picker supPicker;
        public Picker mathPicker;
        public Button spamSpecies;
        public Button spamAt;
        public Button spamNumber;
        public Button spamEq;
        public Button spamPlus;
        public Button spamArrow;
        public Button spamBiArrow;
        public Button spamSharp;
        public Button spamCatal;
        public Button spamBra;
        public Button spamKet;
        //public Button spamComma;
        public Button spamReport;
        public Button spamEquil;
        public Noise noisePickerSelection = Noise.None;
        public ImageButton deviceButton;
        public ImageButton startButton;

        public ToolbarItem EditItem()  {
            return
                new ToolbarItem("Edit", "icons8pencil96", async () => {
                    if (editor.IsEditable()) {
                        if (isKeyboardUp)
                            editor.HideInputMethod();
                        else
                            editor.ShowInputMethod();
                    } else {
                        SetModel(modelInfo.Copy(), editable: true);
                        editor.ShowInputMethod();
                    }
                });
        }
        public ToolbarItem PasteAllItem() {
            return
                new ToolbarItem("PasteAll", "icons8import96", async () => {
                    if (Clipboard.HasText) {
                        string text = await Clipboard.GetTextAsync();
                        MainTabbedPage.theModelEntryPage.SetText(text);
                        SaveEditor(); // otherwise it would not be saved becauese there is no focus change
                    }
                });
        }
        public ToolbarItem CopyAllItem() {
            return
                new ToolbarItem("CopyAll", "icons8export96", async () => {
                    string text = MainTabbedPage.theModelEntryPage.GetText();
                    if (text != "") await Clipboard.SetTextAsync(text);
                });
        }

        public ImageButton TextUp(ICustomTextEdit editor) {
            ImageButton button = new ImageButton() {
                Source = "icons8BigA40.png",
                HeightRequest = MainTabbedPage.buttonHeightRequest,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = MainTabbedPage.secondBarColor,
            };
            button.Clicked += async (object sender, EventArgs e) => {
                float size = editor.GetFontSize();
                if (size < 128) editor.SetFontSize(size+1);
            };
            return button;
        }
        public ImageButton TextDn(ICustomTextEdit editor) {
            ImageButton button = new ImageButton() {
                Source = "icons8SmallA40.png",
                HeightRequest = MainTabbedPage.buttonHeightRequest,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = MainTabbedPage.secondBarColor,
            };
            button.Clicked += async (object sender, EventArgs e) => {
                float size = editor.GetFontSize();
                if (size > 6) editor.SetFontSize(size - 1);
            };
            return button;
        }

        public void StartAction(bool forkWorker, bool switchToChart, bool switchToOutput, bool autoContinue = false) {
            if (Exec.IsExecuting() && !Gui.gui.ContinueEnabled()) return; // we are already running a simulation, don't start a concurrent one
            if (Exec.IsExecuting() && Gui.gui.ContinueEnabled()) { // we are already running a simulation; make start button work as continue button
                Protocol.continueExecution = true; 
                MainTabbedPage.SwitchToTab(MainTabbedPage.theChartPageNavigation);
            } else { // do a start
                MainTabbedPage.theOutputPage.SetModel(modelInfo);
                MainTabbedPage.theChartPage.SetModel(modelInfo);
                Exec.Execute_Starter(forkWorker, autoContinue: autoContinue); // This is where it all happens
                if (switchToChart) MainTabbedPage.SwitchToTab(MainTabbedPage.theChartPageNavigation);
                else if (switchToOutput) MainTabbedPage.SwitchToTab(MainTabbedPage.theOutputPageNavigation);
            }
        }

        private void ResetCurrentModels() { // so outputs and charts get recomputed after models are edited
            MainTabbedPage.theOutputPage.SetModel(null);
            MainTabbedPage.theChartPage.SetModel(null);
        }

        public ImageButton StartButton(bool switchToChart, bool switchToOutput) {
            ImageButton button = new ImageButton() {
                Source = "icons8play40.png",
                HeightRequest = MainTabbedPage.buttonHeightRequest,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = MainTabbedPage.secondBarColor,
            };
            button.Clicked += async (object sender, EventArgs e) => {
                if (!modelInfo.executable) return;
                StartAction(forkWorker: true, switchToChart, switchToOutput, autoContinue: false);
            };
            return button;
        }

        public ImageButton DeviceButton() {
            ImageButton button = new ImageButton() {
                Source = "icons8device40off.png",
                HeightRequest = MainTabbedPage.buttonHeightRequest,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = MainTabbedPage.secondBarColor,
            };
            button.Clicked += async (object sender, EventArgs e) => {
                if (!modelInfo.executable) return;
                if (Exec.IsExecuting()) return; // pretend button is in disabled state
                if (ProtocolDevice.Exists()) {
                    deviceButton.Source = "icons8device40off.png";
                    MainTabbedPage.theChartPage.deviceButton.Source = "icons8device40off.png";
                    MainTabbedPage.theChartPage.SwitchToPlotView();
                    ProtocolDevice.Stop();
                } else {
                    ProtocolDevice.Start(35, 200);
                    MainTabbedPage.theChartPage.SwitchToDeviceView();
                    deviceButton.Source = "icons8device40on.png";
                    MainTabbedPage.theChartPage.deviceButton.Source = "icons8device40on.png";
                }
            };
            return button;
        }

        public void SetStartButtonToContinue() {
            Device.BeginInvokeOnMainThread(() => {
                // we need to use size 40x40 icons or they get stuck at wrong size after changing icon
                MainTabbedPage.theModelEntryPage.deviceButton.Source = "icons8device40disabled.png";
                MainTabbedPage.theChartPage.deviceButton.Source = "icons8device40disabled.png";
                MainTabbedPage.theModelEntryPage.startButton.Source = "icons8pauseplay40.png";
                MainTabbedPage.theChartPage.startButton.Source = "icons8pauseplay40.png";
            });         
        }

        public void SetContinueButtonToStart() {
            Device.BeginInvokeOnMainThread(() => {
                // we need to use size 40x40 icons or they get stuck at wrong size after changing icon
                MainTabbedPage.theModelEntryPage.deviceButton.Source = "icons8device40disabled.png";
                MainTabbedPage.theChartPage.deviceButton.Source = "icons8device40disabled.png";
                MainTabbedPage.theModelEntryPage.startButton.Source = "icons8play40disabled.png"; // disabled because we are running a continutation
                MainTabbedPage.theChartPage.startButton.Source = "icons8play40disabled.png"; // disabled because we are running a continutation
            });
        }

        public Picker NoisePicker() {
            Picker noisePicker = new Picker {
                Title = "Noise", TitleColor = MainTabbedPage.barColor,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = MainTabbedPage.pickerColor,
                FontSize = 14,  
               
            };
            foreach (Noise s in Gui.noise) noisePicker.Items.Add(Gui.StringOfNoise(s));
            noisePicker.Unfocused += async (object sender, FocusEventArgs e) => {
                Noise oldSelection = noisePickerSelection;
                noisePickerSelection = Gui.NoiseOfString(noisePicker.SelectedItem as string);
                if (noisePickerSelection != oldSelection)
                    MainTabbedPage.theModelEntryPage.StartAction(forkWorker: true, switchToChart: true, switchToOutput: false, autoContinue: true);
            };
            return noisePicker;
        }

        // https://www.c-sharpcorner.com/article/xamarin-forms-mvvm-how-to-set-icon-titlecolor-borderstyle-for-picker-using-c/

        string[] subscripts = new string[] { "_", "₊", "₋", "₌", "₀", "₁", "₂", "₃", "₄", "₅", "₆", "₇", "₈", "₉", "₍", "₎"};
        string[] superscripts = new string[] { "\'", "⁺", "⁻", "⁼", "⁰", "¹", "²", "³", "⁴", "⁵", "⁶", "⁷", "⁸", "⁹", "⁽", "⁾"};
        string[] math = new string[] { "(", ")", "-", "*", "/", "^", ">", "<", ">=", "<=", "<>", "∂", "μ", "pi", "e", "time", "var", "cov", "poisson", "gauss", "true", "false", "not", "and", "or", "abs", "arccos", "arcsin", "arctan", "arctan2", "ceiling", "cos", "cosh", "exp", "floor", "int", "log", "max", "min", "pos", "sign", "sin", "sinh", "sqrt", "tan", "tanh" };
        public Picker SymbolPicker(string title, int fontSize, string[] items) {
            Picker symbolPicker = new Picker {
                Title = title, TitleColor = MainTabbedPage.barColor,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = MainTabbedPage.pickerColor,
                FontSize = fontSize,
                TextColor = Color.Black,
            };
            foreach (string s in items) symbolPicker.Items.Add(s);
            symbolPicker.Unfocused += async (object sender, FocusEventArgs e) => {
                if (symbolPicker.SelectedItem != null) {
                    editor.InsertText(symbolPicker.SelectedItem as string);
                    editor.SetFocus(); //otherwise focus remains on picker, even if it is now closed, and typing reactivates picker
                }
                symbolPicker.SelectedItem = null;
            };
            return symbolPicker;
        }
        public Button BtnInsertText(ICustomTextEdit editor, string title, int fontSize, string str) {
            Button button = new Button() { Margin = 0, BorderWidth = 0, Padding = 0, 
                Text = title,
                HeightRequest = MainTabbedPage.buttonHeightRequest,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = MainTabbedPage.secondBarColor,
                FontSize = fontSize,
            };
            button.Clicked += async (object sender, EventArgs e) => {
                editor.InsertText(str);
                if (Gui.gui.Platform() != "iOS") editor.ShowInputMethod();
            };
            return button;
        }

        public Grid stepper;
        public Grid TextSizeStepper(ICustomTextEdit editor) {
            Grid stepper = new Grid { RowSpacing = 0, Margin = 0 };
            stepper.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            stepper.BackgroundColor = MainTabbedPage.secondBarColor;
            stepper.Children.Add(TextDn(editor), 1, 0);
            stepper.Children.Add(TextUp(editor), 2, 0);
            return stepper;
        }

        public void SyncNoisePicker(Picker noisePicker) {
            noisePicker.SelectedItem = Gui.StringOfNoise(noisePickerSelection);
        }

        public ModelEntryPage() {

            modelInfo = new ModelInfo();
            Title = modelInfo.title;
            IconImageSource = "icons8mindmap96.png";

            // in iOS>Resource the images of the TitleBar buttons must be size 40, otherwise they will scale but still take the horizontal space of the original

            //ToolbarItems.Add(DeleteItem());

            editItem = EditItem();
            ToolbarItems.Add(editItem);

            pasteAllItem = PasteAllItem();
            pasteAllItem.IsEnabled = false;
            ToolbarItems.Add(pasteAllItem);

            copyAllItem = CopyAllItem();
            ToolbarItems.Add(copyAllItem);

            editor = Kaemika.GUI_Xamarin.TextEditor();

            editor.OnTextChanged(
                async(ICustomTextEdit textEdit) => {
                    if (!modelInfo.modified) {
                        modelInfo.modified = true;
                        ResetCurrentModels();
                    }
                });

            editor.OnFocusChange(
                async (ICustomTextEdit textEdit) => { 
                    if (modelInfo.modified) SaveEditor(); 
                });

            noisePicker = NoisePicker();
            subPicker = SymbolPicker("Sub", 7, subscripts); 
            supPicker = SymbolPicker("Sup", 7, superscripts);
            mathPicker = SymbolPicker(" ∑ ", 12, math);
            spamSpecies = BtnInsertText(editor, "species", 8, "species "); 
            spamAt = BtnInsertText(editor, "@", 12, " @ "); 
            spamNumber = BtnInsertText(editor, "number", 8, "number "); 
            spamEq = BtnInsertText(editor, "=", 12, " = "); 
            spamPlus = BtnInsertText(editor, "+", 12, " + "); 
            spamArrow = BtnInsertText(editor, "->", 12, " -> "); 
            spamBiArrow = BtnInsertText(editor, "<->", 12, " <-> "); 
            spamSharp = BtnInsertText(editor, "#", 12, "#"); 
            spamCatal = BtnInsertText(editor, ">>", 12, " >> "); 
            spamBra = BtnInsertText(editor, "{", 12, "{"); 
            spamKet = BtnInsertText(editor, "}", 12, "}"); 
            spamReport = BtnInsertText(editor, "report", 8, "report ");
            //spamComma = BtnInsertText(editor, ",", 12, ", ");
            spamEquil = BtnInsertText(editor, "equilib.", 8, "equilibrate for ");
            startButton = StartButton(switchToChart: true, switchToOutput: false);
            deviceButton = DeviceButton();
            stepper = TextSizeStepper(editor);

            int topBarPadding = 0;
            topBar = new Grid { RowSpacing = 0, ColumnSpacing = 0,  Padding = topBarPadding };
            topBar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.BackgroundColor = MainTabbedPage.secondBarColor;
            topBar.IsVisible = false;

            topBar.Children.Add(spamSpecies, 0, 0);
            topBar.Children.Add(spamAt, 1, 0);
            topBar.Children.Add(spamNumber, 2, 0);
            topBar.Children.Add(spamEq, 3, 0);
            topBar.Children.Add(spamPlus, 4, 0);
            topBar.Children.Add(spamArrow, 5, 0);
            topBar.Children.Add(spamBiArrow, 6, 0);
            topBar.Children.Add(spamSharp, 7, 0);
            topBar.Children.Add(spamCatal, 8, 0);
            topBar.Children.Add(spamBra, 9, 0);
            topBar.Children.Add(spamKet, 10, 0);
            topBar.Children.Add(spamReport, 11, 0);
            topBar.Children.Add(spamEquil, 12, 0);
            topBar.Children.Add(subPicker, 13, 0);
            topBar.Children.Add(supPicker, 14, 0);
            topBar.Children.Add(mathPicker, 15, 0);
            //topBar.Children.Add(spamComma, xx, 0);

            int bottomBarPadding = 4;
            bottomBar = new Grid { RowSpacing = 0, Padding = bottomBarPadding };
            bottomBar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // stepper
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // noisePicker
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // deviceButton
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // startButton
            bottomBar.BackgroundColor = MainTabbedPage.secondBarColor;

            bottomBar.Children.Add(stepper, 0, 0);
            bottomBar.Children.Add(deviceButton, 1, 0);
            bottomBar.Children.Add(noisePicker, 2, 0);
            bottomBar.Children.Add(startButton, 3, 0);

            Grid grid = new Grid { ColumnSpacing = 0 };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(spamSpecies.HeightRequest+2*topBarPadding) });  // top bar
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });                           // editor
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(startButton.HeightRequest+2*bottomBarPadding) });   // bottom bar
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(topBar, 0, 0);
            grid.Children.Add(editor.AsView(), 0, 1);
            grid.Children.Add(bottomBar, 0, 2);

            Content = grid;
        }

        public void SetModel(ModelInfo info, bool editable) {
            modelInfo = info;
            Title = modelInfo.title;
            SetText(modelInfo.text);
            editor.SetEditable(editable);
            if (editable) editItem.IconImageSource = "HideKeyboard";
            else editItem.IconImageSource = "icons8pencil96";
            // editItem.IsEnabled = !editable;
            pasteAllItem.IsEnabled = editable;
            topBar.IsVisible = editable;
        }

        private bool isKeyboardUp = false;

        public void KeyboardIsUp() {
            isKeyboardUp = true;
            editItem.IconImageSource = "HideKeyboard";
        }

        public void KeyboardIsDown() {
            isKeyboardUp = false;
            editItem.IconImageSource = "ShowKeyboard";
        }

        public string GetText() {
            string text = editor.GetText();
            if (text == null) text = "";
            return text;
        }

        public void SetText(string text) {
            editor.SetText(text);
            if (!modelInfo.modified) {
                modelInfo.modified = true;
                ResetCurrentModels();
            }
        }

        public void InsertText(string text) {
            editor.InsertText(text);
        }

        public void SaveEditor() {
            modelInfo.text = GetText();
            if (string.IsNullOrWhiteSpace(modelInfo.filename)) SaveFresh();
            else Overwrite();
            modelInfo.modified = false;
        }

        public void SaveFresh() {
            modelInfo.filename = Path.Combine(App.FolderPath, $"{Path.GetRandomFileName()}" + App.modelExtension);
            File.WriteAllText(modelInfo.filename, modelInfo.title + Environment.NewLine + modelInfo.text);
        }

        public void Overwrite() {
            File.WriteAllText(modelInfo.filename, modelInfo.title + Environment.NewLine + modelInfo.text);
        }
      
        private static Dictionary<string, Dictionary<string, bool>> visibilityCache =
            new Dictionary<string, Dictionary<string, bool>>();

        public Dictionary<string,bool> Visibility() {
            string theModel = modelInfo.title;
            if (!visibilityCache.ContainsKey(theModel)) visibilityCache[theModel] = new Dictionary<string, bool>();
            return visibilityCache[theModel];
        }

        public override void OnSwitchedTo() {
            MainTabbedPage.OnAnySwitchedTo(this);
            SyncNoisePicker(noisePicker);
        }

        public async void ErrorMessage(string msg) {
            var page = new ContentPage();
            await page.DisplayAlert("Title", msg, "Accept", "Cancel");
        }

    }
}
