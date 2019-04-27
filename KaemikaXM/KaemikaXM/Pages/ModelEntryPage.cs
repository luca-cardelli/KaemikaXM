using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Essentials;
using Kaemika;

namespace KaemikaXM.Pages {

    public class ModelEntryPage : ContentPage {

        public ModelInfo modelInfo;
        private Editor editor;
        private int editorFontSize = 12;
        public Picker noisePicker;
        public object noisePickerSelectedItem = ProtocolActuator.noiseString[0];
        public Noise noisePickerSelection = Noise.None;
        public ImageButton startButton;

        //public ToolbarItem DeleteItem() {
        //    return
        //        new ToolbarItem("Delete", "icons8trash.png", async () => {
        //            if (File.Exists(modelInfo.filename)) {
        //                File.Delete(modelInfo.filename);
        //                MainTabbedPage.theModelEntryPage.SetModel(new ModelInfo());
        //            }
        //            MainTabbedPage.theMainTabbedPage.SwitchToTab("Networks");
        //        });
        //}

        public Button TextUp() {
            Button button = new Button {
                Text = "A",
                FontSize = 10,
                BorderWidth = 0,
                BorderColor = Color.FromHex("9999FF"),
                BackgroundColor = Color.BlanchedAlmond,
                CornerRadius = 6,
            };
            button.Clicked += async (object sender, EventArgs e) => { if (editor.FontSize < 128) editor.FontSize++; };
            return button;
        }
        public Button TextDn() {
            Button button = new Button {
                Text = "A",
                FontSize = 6,
                BorderWidth = 0,
                BorderColor = Color.FromHex("9999FF"),
                BackgroundColor = Color.BlanchedAlmond,
                CornerRadius = 6,
            };
            button.Clicked += async (object sender, EventArgs e) => { if (editor.FontSize > 6) editor.FontSize--; };
            return button;
        }
            
        public ImageButton StartButton() {
            ImageButton button = new ImageButton() {
                Source = "icons8play40.png",
                HeightRequest = 40,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = Color.FromHex("9999FF"),
            };
            button.Clicked += async (object sender, EventArgs e) => {
                if (Gui.gui.StopEnabled() && !Gui.gui.ContinueEnabled()) return; // we are already running a simulation, don't start a concurrent one
                if (Gui.gui.ContinueEnabled()) {
                    ProtocolActuator.continueExecution = true; // make start button work as continue button
                    MainTabbedPage.theMainTabbedPage.SwitchToTab("Chart");
                } else { // do a start
                    MainTabbedPage.theOutputPage.SetTitle(modelInfo.title);
                    MainTabbedPage.theChartPage.SetTitle(modelInfo.title);
                    Exec.Execute_Starter(forkWorker: true, doEval: true); // This is where it all happens
                    MainTabbedPage.theMainTabbedPage.SwitchToTab("Chart");
                }
            };
            return button;
        }

        public void SetStartButtonToContinue() {
            Device.BeginInvokeOnMainThread(() => {
                startButton.Source = "icons8pauseplay40.png";
                MainTabbedPage.theChartPage.startButton.Source = "icons8pauseplay40.png";
            });         
        }

        public void SetStartButtonToStart() {
            Device.BeginInvokeOnMainThread(() => {
                startButton.Source = "icons8play40.png";
                MainTabbedPage.theChartPage.startButton.Source = "icons8play40.png";
            });
        }

        public Picker NoisePicker() {
            Picker noisePicker = new Picker {
                Title = "Noise",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = Color.FromHex("9999FF"),
                FontSize = 14,
            };
            foreach (string s in ProtocolActuator.noiseString) noisePicker.Items.Add(s);
            noisePicker.Unfocused += async (object sender, FocusEventArgs e) => {
                noisePickerSelectedItem = noisePicker.SelectedItem;
                noisePickerSelection = ProtocolActuator.NoiseOfString(noisePicker.SelectedItem as string);
            };
            return noisePicker;
        }

        public void SyncNoisePicker(Picker noisePicker) {
            noisePicker.SelectedItem = noisePickerSelectedItem;
        }

        public ModelEntryPage() {

            modelInfo = new ModelInfo();
            Title = modelInfo.title;
            Icon = "tab_feed.png";

            //ToolbarItems.Add(DeleteItem());
            ToolbarItems.Add(
                    new ToolbarItem("PasteAll", "icons8import96", async () => {
                        if (Clipboard.HasText) {
                            string text = await Clipboard.GetTextAsync();
                            MainTabbedPage.theModelEntryPage.SetText(text);
                        }
                    }));
            ToolbarItems.Add(
                    new ToolbarItem("CopyAll", "icons8export96", async () => {
                        string text = MainTabbedPage.theModelEntryPage.GetText();
                        if (text != "") await Clipboard.SetTextAsync(text);
                    }));

            editor = new Editor() {
                Text = modelInfo.text,
                AutoSize = EditorAutoSizeOption.Disabled,   //AutoSize = EditorAutoSizeOption.TextChanges,
                FontSize = editorFontSize,
                IsSpellCheckEnabled = false,
                IsTextPredictionEnabled = false,
                Margin = 8,
            };

            editor.TextChanged += async (object sender, TextChangedEventArgs e) => {
                if (e.NewTextValue != e.OldTextValue) modelInfo.modified = true;
            };
            editor.Completed += async (object sender, EventArgs e) => {
                if (modelInfo.modified) SaveEditor();
            };

            noisePicker = NoisePicker();
            startButton = StartButton();

            Grid stepper = new Grid { RowSpacing = 0 , Margin = 0};
            stepper.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            stepper.BackgroundColor = Color.FromHex("9999FF");

            stepper.Children.Add(TextDn(), 1, 0);
            stepper.Children.Add(TextUp(), 2, 0);

            int bottomBarPadding = 4;
            Grid bottomBar = new Grid { RowSpacing = 0, Padding = bottomBarPadding };
            bottomBar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.BackgroundColor = Color.FromHex("9999FF");

            bottomBar.Children.Add(stepper, 0, 0);
            bottomBar.Children.Add(noisePicker, 1, 0);
            bottomBar.Children.Add(startButton, 2, 0);

            Grid grid = new Grid { ColumnSpacing = 0 };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(startButton.HeightRequest+2*bottomBarPadding) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(editor, 0, 0);
            grid.Children.Add(bottomBar, 0, 1);

            Content = grid;
        }

        public void SetModel(ModelInfo info) {
            modelInfo = info;
            Load();
        }

        public string GetText() {
            return editor.Text;
        }

        public void SetText(string text) {
            editor.Text = text;
            modelInfo.modified = true;
        }

        public void InsertText(string text) {
            //### for now we just append it
            SetText(GetText() + text);
        }

        public void Load() {
            Title = modelInfo.title;
            editor.Text = (modelInfo.text.Length > 0) ? '\a' + modelInfo.text : ""; //communicate that this is brand new non-empty page
        }

        public void SaveEditor() {
            modelInfo.text = editor.Text;
            if (string.IsNullOrWhiteSpace(modelInfo.filename)) SaveFresh();
            else Overwrite();
        }

        public void SaveFresh() {
            modelInfo.filename = Path.Combine(App.FolderPath, $"{Path.GetRandomFileName()}" + App.modelExtension);
            File.WriteAllText(modelInfo.filename, modelInfo.title + Environment.NewLine + modelInfo.text);
        }

        public void Overwrite() {
            File.WriteAllText(modelInfo.filename, modelInfo.title + Environment.NewLine + modelInfo.text);
        }

        protected override void OnAppearing() {
            base.OnAppearing();
            SyncNoisePicker(noisePicker);
        }

        public async void ErrorMessage(string msg) {
            var page = new ContentPage();
            await page.DisplayAlert("Title", msg, "Accept", "Cancel");
        }

    }
}
