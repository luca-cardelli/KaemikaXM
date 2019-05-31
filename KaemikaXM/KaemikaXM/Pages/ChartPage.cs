using System;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Kaemika;

namespace KaemikaXM.Pages
{
    public class ChartPageLandscape : ContentPage {

        private Microcharts.ChartView chartView;

        public ChartPageLandscape() {
            chartView = new Microcharts.ChartView() {
                Chart = new Microcharts.Chart("", ""),
                BackgroundColor = Color.White,
            };
            Content = chartView;
        }

        public void SetChart(Microcharts.Chart chart) {
            chartView.Chart = chart;
        }
 
        protected override void OnAppearing() {
            base.OnAppearing();
            (Gui.gui as GUI_Xamarin).ChartUpdateLandscape();
        }

       // Device rotation handling

        private double width = 0;
        private double height = 0;

        protected override void OnSizeAllocated(double width, double height) {
            base.OnSizeAllocated(width, height); //must be called
            if (this.width != width || this.height != height) {
                this.width = width;
                this.height = height;
                if (this.height > this.width) App.PortraitOrientation();
            }
        }
    }

    public class ChartPage : KaemikaPage {

        private string title = "";
        private Microcharts.ChartView chartView;
        private ModelInfo currentModelInfo;
        public Picker noisePicker;
        public ImageButton stopButton;
        private CollectionView legendView;
        private CollectionView parameterView;
        public ImageButton startButton;
        private ToolbarItem solverRK547MButton;
        private ToolbarItem solverGearBDFButton;
        private Grid mainGrid;
        private StackLayout stackView;

        private ToolbarItem SolverRK547MButton() {
            return new ToolbarItem("RK547M", "icons8refresh96solver1", () => {
                if (Exec.IsExecuting()) return;
                solverRK547MButton.IsEnabled = false;
                GUI_Xamarin.currentSolver = "RK547M";
                MainTabbedPage.theModelEntryPage.StartAction(forkWorker: true, switchToChart: false, switchToOutput: false, autoContinue: false);
                solverGearBDFButton.IsEnabled = true;
            });
        }

        private ToolbarItem SolverGearBDFButton() {
            return new ToolbarItem("GearBDF", "icons8refresh96solver2", () => {
                if (Exec.IsExecuting()) return;
                solverGearBDFButton.IsEnabled = false;
                GUI_Xamarin.currentSolver = "GearBDF";
                MainTabbedPage.theModelEntryPage.StartAction(forkWorker: true, switchToChart: false, switchToOutput: false, autoContinue: false);
                solverRK547MButton.IsEnabled = true;
            });
        }

        public ImageButton StopButton() {
            ImageButton button = new ImageButton() {
                Source = "icons8stop40.png",
                HeightRequest = MainTabbedPage.buttonHeightRequest,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = MainTabbedPage.secondBarColor,
            };
            button.Clicked += async (object sender, EventArgs e) => {
                Exec.EndingExecution();
            };
            return button;
        }

        public ChartPage() {
            Title = "Chart";
            Icon = "tab_feed.png";

            chartView = new Microcharts.ChartView() {
                Chart = new Microcharts.Chart("", ""),
                HeightRequest = 300,
                BackgroundColor = Color.White,
            };

            solverRK547MButton = SolverRK547MButton();
            ToolbarItems.Add(solverRK547MButton);
            solverRK547MButton.IsEnabled = false;
            solverGearBDFButton = SolverGearBDFButton();
            ToolbarItems.Add(solverGearBDFButton);
            solverGearBDFButton.IsEnabled = true;

            stopButton = StopButton();
            noisePicker = MainTabbedPage.theModelEntryPage.NoisePicker();
            startButton = MainTabbedPage.theModelEntryPage.StartButton(switchToChart:false, switchToOutput:false);

            int bottomBarPadding = 4;
            Grid bottomBar = new Grid { RowSpacing = 0, Padding = bottomBarPadding };
            bottomBar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.BackgroundColor = MainTabbedPage.secondBarColor;

            bottomBar.Children.Add(stopButton, 0, 0);
            bottomBar.Children.Add(noisePicker, 1, 0);
            bottomBar.Children.Add(startButton, 2, 0);

            mainGrid = new Grid { ColumnSpacing = 0 };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(MainTabbedPage.buttonHeightRequest + 2 * bottomBarPadding) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            legendView = LegendView();
            parameterView = ParameterView();

            ScrollView inspectionView = new ScrollView();
            stackView = new StackLayout();
            inspectionView.Content = stackView;
            stackView.Children.Add(legendView);
            stackView.Children.Add(parameterView);

            mainGrid.Children.Add(chartView, 0, 0);
            mainGrid.Children.Add(inspectionView, 0, 1);
            mainGrid.Children.Add(bottomBar, 0, 2);

            Content = mainGrid;
        }

        public void SetChart(Microcharts.Chart chart, ModelInfo modelInfo) {
            chartView.Chart = chart;
        }

        public void SetModel(ModelInfo modelInfo) {
            this.title = (modelInfo == null) ? "" : modelInfo.title;
            Title = this.title;
            currentModelInfo = modelInfo;
        }

        // ======== LEGEND ========= //

        const int LegendItemHeight = 20;

        public void SetLegend(Microcharts.Series[] legend) {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                var legendList = new ObservableCollection<LegendItem>();
                for (int i = legend.Length - 1; i >= 0; i--)
                    legendList.Add(new LegendItem {
                        Name = legend[i].name,
                        Color = SkiaSharp.Views.Forms.Extensions.ToFormsColor(legend[i].color),
                        Width = (legend[i].visible) ? 50 : 6,
                        Height =
                            (legend[i].lineStyle == Microcharts.LineStyle.Thick) ? 4  // show a wide bar for thick plot lines
                          : (legend[i].lineMode == Microcharts.LineMode.Line) ? 1     // show a smaller bar for think plot lines
                          : LegendItemHeight,                                         // show a full rectangle for Range areas
                    });
                legendView.ItemsSource = legendList;
                MainTabbedPage.theChartPage.stackView.Children[0].HeightRequest = 40 + LegendItemHeight * (legend.Length + 1) / 2;
            });
        }

        public class LegendItem {
            public string Name { get; set; }
            public Color Color { get; set; }
            public int Height { get; set; }
            public int Width { get; set; }
        }

        public CollectionView LegendView () {
            CollectionView collectionView = new CollectionView() {
                //ItemsLayout = ListItemsLayout.VerticalList,  // can also be set to a vertical grid
                ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical), 
                SelectionMode = SelectionMode.Single,          
            };
            // collectionView.SetBinding(ItemsView.ItemsSourceProperty, "Monkeys"); // a binding for the ItemSource, but we give keep regenerating it

            collectionView.ItemsSource = new ObservableCollection<LegendItem>();  // CollectionView contains LegendItems with bindings set in ItemTemplate

            collectionView.ItemTemplate = new DataTemplate(() => {                // CollectionView contains LegendItems with bindings set in ItemTemplate
                Grid grid = new Grid { Padding = 2 };
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(LegendItemHeight) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                BoxView box = new BoxView { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center }; // needed, otherwise the default .Fill option ignores HeightRequest
                box.SetBinding(BoxView.ColorProperty, "Color"); // property of LegendItem
                box.SetBinding(BoxView.HeightRequestProperty, "Height"); // property of LegendItem
                box.SetBinding(BoxView.WidthRequestProperty, "Width"); // property of LegendItem

                Label nameLabel = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold };
                nameLabel.SetBinding(Label.TextProperty, "Name"); // property of LegendItem

                grid.Children.Add(box, 0, 0);
                grid.Children.Add(nameLabel, 1, 0);

                return grid;
            });

            collectionView.SelectionChanged += (object sender, SelectionChangedEventArgs args) => {
                LegendItem item = collectionView.SelectedItem as LegendItem;
                if (item != null) {
                    (Gui.gui as GUI_Xamarin).InvertVisible(item.Name);
                    (Gui.gui as GUI_Xamarin).VisibilityRemember();
                    Gui.gui.ChartUpdate();
                    Gui.gui.LegendUpdate();
                    collectionView.SelectedItem = null; // avoid some visible flashing of the selection
                }
            };

            return collectionView;
        }

        // ======== PARAMETERS ========= //

        const int ParameterItemHeight = 40;

        private static Dictionary<string, ParameterInfo> parameterInfoDict = new Dictionary<string, ParameterInfo>(); // persistent information
        private static Dictionary<string, ParameterState> parameterStateDict = new Dictionary<string, ParameterState>();

        // clear the parameterStateDict at the beginning of every execution, but we keep the parametersInfoDict forever

        public void ParametersClear() {
            parameterStateDict = new Dictionary<string, ParameterState>();
        }

        public class ParameterState {
            public string parameter;
            private double value; // normalized to [0..1] so we never change the default min/max for slider
            public ParameterState(string parameter, ParameterInfo info) {  // NormalizeAndSetValue from info.draw
                this.parameter = parameter;
                this.value = NormalizeValue(info);   
            }
            public double UnnormalizeAndGetValue(ParameterInfo info) {     // return [info.rangeMin..info.rangeMax]
                return UnnormalizeValue(this.value, info);
            }
            public double GetNormalizedValue() { // [0..1]
                return this.value;
            }
            public void SetNormalizedValue(double value, ParameterInfo info) {  // [0..1]
                if (info.distribution == "bernoulli") { this.value = (value < 0.5) ? 0.0 : 1.0; }
                else this.value = value;
            }
            private double NormalizeValue(ParameterInfo info) {
                return (info.range == 0.0) ? 0.5 : (info.drawn - info.rangeMin) / info.range;
            }
            private double UnnormalizeValue(double value, ParameterInfo info) {
                return info.rangeMin + value * info.range;
            }
        }
 
        // ask the gui if this parameter is locked

        public double ParameterOracle(string parameter) { // returns NAN if oracle not available
            if (parameterInfoDict.ContainsKey(parameter) && parameterInfoDict[parameter].locked)
                // parameter does not exist yet in parameterExistsDict but will exist at the end of the run, and it will be locked
                return parameterInfoDict[parameter].drawn;
            return double.NaN;
        }

        // reflect the parameter state into the gui

        public void ParametersUpdate() {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                RefreshParameters();
                MainTabbedPage.theChartPage.stackView.Children[1].HeightRequest = 20 + ParameterItemHeight * parameterInfoDict.Count;
                
            });
        }
        private void RefreshParameters() {
            var parameterList = new ObservableCollection<ParameterBinding>();
            foreach (var kvp in parameterStateDict) {
                ParameterInfo info = parameterInfoDict[kvp.Key];
                ParameterState state = parameterStateDict[kvp.Key];
                ParameterBinding parameterBinding = 
                    new ParameterBinding {
                        Parameter = info.parameter,
                        Format = info.ParameterLabel(true),
                        Value = state.GetNormalizedValue(), // [0..1]
                        Locked = info.locked,
                    };
                parameterList.Add(parameterBinding);
            }
            parameterView.ItemsSource = parameterList;
        }

        public void AddParameter(string parameter, double drawn, string distribution, double[] arguments) {
            if (!parameterInfoDict.ContainsKey(parameter)) {
                parameterInfoDict[parameter] = new ParameterInfo(parameter, drawn, distribution, arguments);
                parameterStateDict[parameter] = new ParameterState(parameter, parameterInfoDict[parameter]);
            }
            parameterStateDict[parameter] = new ParameterState(parameter, parameterInfoDict[parameter]); // use the old value, not the one from drawn
            if (parameterInfoDict.ContainsKey(parameter) && parameterInfoDict[parameter].locked) return; // do not change the old value if locked
            ParameterInfo info = new ParameterInfo(parameter, drawn, distribution, arguments);           // use the new value, from drawn
            ParameterState state = new ParameterState(parameter, info);                                  // update the value
            parameterInfoDict[parameter] = info;
            parameterStateDict[parameter] = state;
        }

        // bind the parameter info to the gui via SetBinding/BindingContext, ugh!

        public class ParameterBinding {
            public string Parameter { get; set; } // do not rename: bound property, ugh!
            public string Format { get; set; } // do not rename: bound property, ugh!
            public double Value { get; set; } // do not rename: bound property, ugh!
            public bool Locked { get; set; } // do not rename: bound property, ugh!
        }

        public CollectionView ParameterView () {
            CollectionView collectionView = new CollectionView() {
                ItemsLayout = new GridItemsLayout(1, ItemsLayoutOrientation.Vertical),
                SelectionMode = SelectionMode.None
            };

            collectionView.ItemsSource = new ObservableCollection<ParameterBinding>();  // CollectionView contains ParameterBindings with bindings set in ItemTemplate, ugh!

            collectionView.ItemTemplate = new DataTemplate(() => {                   // CollectionView contains ParameterBindings with bindings set in ItemTemplate, ugh!
                Grid grid = new Grid { Padding = 2 };
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ParameterItemHeight) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 150 });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Slider slider = new Slider();
                slider.MaximumTrackColor = Color.Blue;
                slider.SetBinding(Slider.ValueProperty, "Value"); // property of ParameterBinding, ugh!
                //slider.ValueChanged += (object source, ValueChangedEventArgs e) => {
                //    if (slider.BindingContext == null) return;
                //    ParameterBinding parameterBinding = (ParameterBinding)slider.BindingContext;
                //    ParameterInfo parameterInfo = parameterInfoDict[parameterBinding.Parameter];
                //    Slider sl = source as Slider;
                //    //parameterInfo.drawn = (source as Slider).Value;
                //    //parameterBinding.Format = parameterInfo.ParameterLabel();
                //    //SetParameters(); // don't call RefreshParameters because we may be in a worker thread
                //};
                slider.DragCompleted += (object source, EventArgs e) => {
                    if (slider.BindingContext == null) return;
                    ParameterBinding parameterBinding = (ParameterBinding)slider.BindingContext; // the implicit binding context, ugh!
                    ParameterInfo info = parameterInfoDict[parameterBinding.Parameter];
                    ParameterState state = parameterStateDict[parameterBinding.Parameter];
                    state.SetNormalizedValue((source as Slider).Value, info);
                    info.drawn = state.UnnormalizeAndGetValue(info);
                    parameterBinding.Format = info.ParameterLabel(true);
                    RefreshParameters(); // otherwise formatLabel does not update even though it has a data binding, ugh!
                };

                Switch switcher = new Switch();
                switcher.SetBinding(Switch.IsToggledProperty, "Locked"); // property of ParameterBinding, ugh!
                switcher.Toggled += (object source, ToggledEventArgs e) => {
                    if (switcher.BindingContext == null) return; // ugh!
                    ParameterBinding parameterBinding = (ParameterBinding)switcher.BindingContext; // the implicit binding context, ugh!
                    ParameterInfo parameterInfo = parameterInfoDict[parameterBinding.Parameter];
                    parameterInfo.locked = (source as Switch).IsToggled;
                };

                Label formatLabel = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold };
                formatLabel.SetBinding(Label.TextProperty, "Format"); // property of ParameterBinding, ugh!

                grid.Children.Add(slider, 0, 0);
                grid.Children.Add(switcher, 1, 0);
                grid.Children.Add(formatLabel, 2, 0);

                return grid;
            });

            return collectionView;
        }
        // ======== On Switched To ========= //

        public override void OnSwitchedTo() {
            MainTabbedPage.OnAnySwitchedTo(this);
            MainTabbedPage.theModelEntryPage.SyncNoisePicker(noisePicker);
            if (!Exec.IsExecuting() && // we could be waiting on a continuation! StartAction would switch us right back to this page even if it does not start a thread!
                currentModelInfo != MainTabbedPage.theModelEntryPage.modelInfo) // forkWorker: we can compute the chart concurrently
                MainTabbedPage.theModelEntryPage.StartAction(forkWorker: true, switchToChart: false, switchToOutput: false, autoContinue: false);
            Gui.gui.ChartUpdate();
        }

    }
}
