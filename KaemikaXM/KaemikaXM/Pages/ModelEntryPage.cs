using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Essentials;
using Kaemika;

namespace KaemikaXM.Pages {

    public class ModelEntryPage : KaemikaPage {

        public ModelInfo modelInfo;
        public View editor; // is a CustomTextEditView and implements ICustomTextEdit
        public ToolbarItem editItem;
        public ToolbarItem pasteAllItem;
        public ToolbarItem copyAllItem;
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

        public const string secondBarColor = "61D5ff"; // standard blue is "2195F3"; https://www.color-hex.com/

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
                HeightRequest = 40,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = Color.FromHex(secondBarColor),
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
                HeightRequest = 40,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = Color.FromHex(secondBarColor),
            };
            button.Clicked += async (object sender, EventArgs e) => {
                float size = editor.GetFontSize();
                if (size > 6) editor.SetFontSize(size - 1);
            };
            return button;
        }
            
        public ImageButton StartButton() {
            ImageButton button = new ImageButton() {
                Source = "icons8play40.png",
                HeightRequest = 40,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = Color.FromHex(secondBarColor),
            };
            button.Clicked += async (object sender, EventArgs e) => {
                if (Gui.gui.StopEnabled() && !Gui.gui.ContinueEnabled()) return; // we are already running a simulation, don't start a concurrent one
                if (Gui.gui.ContinueEnabled()) {
                    ProtocolActuator.continueExecution = true; // make start button work as continue button
                    MainTabbedPage.SwitchToTab(MainTabbedPage.theChartPageNavigation);
                } else { // do a start
                    MainTabbedPage.theOutputPage.SetTitle(modelInfo.title);
                    MainTabbedPage.theChartPage.SetTitle(modelInfo.title);
                    Exec.Execute_Starter(forkWorker: true, doEval: true); // This is where it all happens
                    MainTabbedPage.SwitchToTab(MainTabbedPage.theChartPageNavigation);
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
                BackgroundColor = Color.FromHex(secondBarColor),
                FontSize = 14,
            };
            foreach (string s in ProtocolActuator.noiseString) noisePicker.Items.Add(s);
            noisePicker.Unfocused += async (object sender, FocusEventArgs e) => {
                noisePickerSelectedItem = noisePicker.SelectedItem;
                noisePickerSelection = ProtocolActuator.NoiseOfString(noisePicker.SelectedItem as string);
            };
            return noisePicker;
        }

        public Grid stepper;
        public Grid TextSizeStepper(ICustomTextEdit editor) {
            Grid stepper = new Grid { RowSpacing = 0, Margin = 0 };
            stepper.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            stepper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            stepper.BackgroundColor = Color.FromHex(secondBarColor);
            stepper.Children.Add(TextDn(editor), 1, 0);
            stepper.Children.Add(TextUp(editor), 2, 0);
            return stepper;
        }

        public void SyncNoisePicker(Picker noisePicker) {
            noisePicker.SelectedItem = noisePickerSelectedItem;
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
                async(ICustomTextEdit textEdit) => { modelInfo.modified = true; });

            (editor as ICustomTextEdit).OnFocusChange(
                async (ICustomTextEdit textEdit) => { if (modelInfo.modified) SaveEditor(); });

            noisePicker = NoisePicker();
            startButton = StartButton();
            stepper = TextSizeStepper(editor as ICustomTextEdit);

            int bottomBarPadding = 4;
            Grid bottomBar = new Grid { RowSpacing = 0, Padding = bottomBarPadding };
            bottomBar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.BackgroundColor = Color.FromHex(secondBarColor);

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

        public void SetModel(ModelInfo info, bool editable) {
            modelInfo = info;
            Title = modelInfo.title;
            SetText(modelInfo.text);
            (editor as ICustomTextEdit).SetEditable(editable);
            editItem.IsEnabled = !editable;
            pasteAllItem.IsEnabled = editable;
        }

        public string GetText() {
            return (editor as ICustomTextEdit).GetText();
        }

        public void SetText(string text) {
            (editor as ICustomTextEdit).SetText(text);
            modelInfo.modified = true;
        }

        public void InsertText(string text) {
            //### for now we just append it
            SetText(GetText() + text);
        }

        public void SaveEditor() {
            modelInfo.text = GetText();
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

        public override void OnSwitchedTo() {
            SyncNoisePicker(noisePicker);
        }

        protected override void OnAppearing() {
            base.OnAppearing();
            OnSwitchedTo();
        }

        public async void ErrorMessage(string msg) {
            var page = new ContentPage();
            await page.DisplayAlert("Title", msg, "Accept", "Cancel");
        }

    }
}
