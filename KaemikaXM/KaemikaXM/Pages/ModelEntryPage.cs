using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.Collections.Generic;
using Kaemika;

namespace KaemikaXM.Pages {

    public class ModelEntryPage : KaemikaPage {

        public ModelInfo modelInfo;
        public View editor; // is a CustomTextEditView and implements ICustomTextEdit
        public ToolbarItem editItem;
        public ToolbarItem pasteAllItem;
        public ToolbarItem copyAllItem;
        public Picker noisePicker;
        public Picker subPicker;
        public Picker supPicker;
        public Noise noisePickerSelection = Noise.None;
        public ImageButton startButton;

        public ToolbarItem EditItem()  {
            return
                new ToolbarItem("Edit", "icons8pencil96", async () => {
                    SetModel(modelInfo.Copy(), editable: true);
                });
        }
        public ToolbarItem PasteAllItem() {
            return
                new ToolbarItem("PasteAll", "icons8import96", async () => {
                    if (Clipboard.HasText) {
                        string text = await Clipboard.GetTextAsync();
                        MainTabbedPage.theModelEntryPage.SetText(text);
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
                ProtocolActuator.continueExecution = true; 
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

        public void SetStartButtonToContinue() {
            Device.BeginInvokeOnMainThread(() => {
                // we need to use size 40x40 icons or they get stuck at wrong size after changing icon
                MainTabbedPage.theModelEntryPage.startButton.Source = "icons8pauseplay40.png";
                MainTabbedPage.theChartPage.startButton.Source = "icons8pauseplay40.png";
            });         
        }

        public void SetContinueButtonToStart() {
            Device.BeginInvokeOnMainThread(() => {
                // we need to use size 40x40 icons or they get stuck at wrong size after changing icon
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
        public Picker SubPicker() {
            Picker charPicker = new Picker {
                Title = "Sub", TitleColor = MainTabbedPage.barColor,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = MainTabbedPage.pickerColor,
                FontSize = 9,
                TextColor = Color.Black,
            };
            foreach (string s in subscripts) charPicker.Items.Add(s);
            charPicker.Unfocused += async (object sender, FocusEventArgs e) => {
                if (charPicker.SelectedItem != null)
                    (editor as ICustomTextEdit).InsertText(charPicker.SelectedItem as string);
                charPicker.SelectedItem = null;
            };
            return charPicker;
        }
        public Picker SupPicker() {
            Picker charPicker = new Picker {
                Title = "Sup", TitleColor = MainTabbedPage.barColor,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = MainTabbedPage.pickerColor,
                FontSize = 9,
                TextColor = Color.Black,
            };
            foreach (string s in superscripts) charPicker.Items.Add(s);
            charPicker.Unfocused += async (object sender, FocusEventArgs e) => {
                if (charPicker.SelectedItem != null)
                    (editor as ICustomTextEdit).InsertText(charPicker.SelectedItem as string);
                charPicker.SelectedItem = null;
            };
            return charPicker;
        }
        public Grid CharPickers(Picker subPicker, Picker supPicker, Picker noisePicker) {
            Grid charPickers = new Grid { RowSpacing = 0, Margin = 0 };
            charPickers.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            charPickers.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            charPickers.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            charPickers.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            charPickers.Children.Add(subPicker, 0, 0);
            charPickers.Children.Add(noisePicker, 1, 0);
            charPickers.Children.Add(supPicker, 2, 0);
            return charPickers;
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
            Icon = "tab_feed.png";

            //ToolbarItems.Add(DeleteItem());

            editItem = EditItem();
            ToolbarItems.Add(editItem);

            pasteAllItem = PasteAllItem();
            pasteAllItem.IsEnabled = false;
            ToolbarItems.Add(pasteAllItem);

            copyAllItem = CopyAllItem();
            ToolbarItems.Add(copyAllItem);

            editor = Kaemika.GUI_Xamarin.customTextEditor();

            (editor as ICustomTextEdit).OnTextChanged(
                async(ICustomTextEdit textEdit) => {
                    if (!modelInfo.modified) {
                        modelInfo.modified = true;
                        ResetCurrentModels();
                    }
                });

            (editor as ICustomTextEdit).OnFocusChange(
                async (ICustomTextEdit textEdit) => { if (modelInfo.modified) SaveEditor(); });

            noisePicker = NoisePicker();
            subPicker = SubPicker(); subPicker.IsVisible = false;
            supPicker = SupPicker(); supPicker.IsVisible = false;
            startButton = StartButton(switchToChart: true, switchToOutput: false);
            stepper = TextSizeStepper(editor as ICustomTextEdit);

            int bottomBarPadding = 4;
            Grid bottomBar = new Grid { RowSpacing = 0, Padding = bottomBarPadding };
            bottomBar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.BackgroundColor = MainTabbedPage.secondBarColor;

            bottomBar.Children.Add(stepper, 0, 0);
            //bottomBar.Children.Add(noisePicker, 1, 0);
            bottomBar.Children.Add(CharPickers(subPicker, supPicker, noisePicker), 1, 0);
            bottomBar.Children.Add(startButton, 2, 0);

            Grid grid = new Grid { ColumnSpacing = 0 };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(startButton.HeightRequest+2*bottomBarPadding) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(editor, 0, 0);
            grid.Children.Add(bottomBar, 0, 1);

            Content = grid;
        }

        public void SetModel(ModelInfo info, bool editable) {
            modelInfo = info;
            Title = modelInfo.title;
            SetText(modelInfo.text);
            (editor as ICustomTextEdit).SetEditable(editable);
            editItem.IsEnabled = !editable;
            pasteAllItem.IsEnabled = editable;
            subPicker.IsVisible = editable;
            supPicker.IsVisible = editable;
        }

        public string GetText() {
            return (editor as ICustomTextEdit).GetText();
        }

        public void SetText(string text) {
            (editor as ICustomTextEdit).SetText(text);
            if (!modelInfo.modified) {
                modelInfo.modified = true;
                ResetCurrentModels();
            }
        }

        public void InsertText(string text) {
            (editor as ICustomTextEdit).InsertText(text);
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
