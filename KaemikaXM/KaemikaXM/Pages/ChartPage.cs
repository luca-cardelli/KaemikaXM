using System;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Kaemika;
using SkiaSharp;

namespace KaemikaXM.Pages
{
    public class ChartPageLandscape : ContentPage {

        private ChartView chartView;

        public ChartPageLandscape() {
            chartView = new ChartView() {
                Chart = new KChart("", "s", "M"),
                BackgroundColor = Color.White,
            };
            Content = chartView;
        }

        public void SetChart(KChart chart) {
            chartView.Chart = chart;
        }
 
        protected override void OnAppearing() {
            base.OnAppearing();
            //(Gui.gui as GUI_Xamarin).ChartUpdateLandscape();
        }

       // Device rotation handling

        private double width = 0;
        private double height = 0;

        protected override void OnSizeAllocated(double width, double height) {
            base.OnSizeAllocated(width, height); //must be called
            if ((width > 0 && this.width != width) || (height > 0 && this.height != height)) {
                this.width = width;
                this.height = height;
                if (this.height > this.width) App.PortraitOrientation();
                // else App.theApp.MainPage.ForceLayout(); // does not help, also check that theApp is not null
            }
        }
    }

    public class ChartPage : KaemikaPage {

        private string title = "";
        private ModelInfo currentModelInfo;
        public ImageButton startButton;
        public ImageButton deviceButton;
        public ImageButton stopButton;
        private ToolbarItem solverRK547MButton;
        private ToolbarItem solverGearBDFButton;
        private ToolbarItem plotViewButton; // chart plus legend
        private ToolbarItem deviceViewButton; // device
        public Picker noisePicker;

        private Grid grid;                           // grid = overlapping AND bottombar
        private AbsoluteLayout overlappingView;      // overlapping = plot OR device
        private Grid plotView;                       // plot = chart AND scrollInspection
        private ScrollView scrollInspectionView;     // scrollInspection = inspection
        private StackLayout inspectionView;          // inspection = legend AND parameter
        private Grid bottomBar;                      // bottomBar = stop AND noise AND start
        private View backdrop;                       // leaf
        private ChartView chartView;                 // chart = leaf
        private CollectionView legendView;           // legend = leaf
        private CollectionView parameterView;        // parameter = leaf
        public DeviceView deviceView;                // device = leaf

        public SKSize ChartSize() {
            return this.chartView.CanvasSize;
        }

        private ToolbarItem SolverRK547MButton() {
            return new ToolbarItem("RK547M", "icons8refresh96solver1", () => {
                if (Exec.IsExecuting()) return;
                solverRK547MButton.IsEnabled = false;
                KControls.solver = "RK547M";
                MainTabbedPage.theModelEntryPage.StartAction(forkWorker: true, switchToChart: false, switchToOutput: false, autoContinue: false);
                solverGearBDFButton.IsEnabled = true;
            });
        }

        private ToolbarItem SolverGearBDFButton() {
            return new ToolbarItem("GearBDF", "icons8refresh96solver2", () => {
                if (Exec.IsExecuting()) return;
                solverGearBDFButton.IsEnabled = false;
                KControls.solver = "GearBDF";
                MainTabbedPage.theModelEntryPage.StartAction(forkWorker: true, switchToChart: false, switchToOutput: false, autoContinue: false);
                solverRK547MButton.IsEnabled = true;
            });
        }

        public void SwitchToPlotView() {
            Device.BeginInvokeOnMainThread(async () => { // to allow calling this from work thread
                if (plotViewButton.IsEnabled) {
                    plotViewButton.IsEnabled = false;
                    overlappingView.RaiseChild(backdrop);
                    overlappingView.RaiseChild(scrollInspectionView);
                    deviceViewButton.IsEnabled = true;
                }
            });
        }
        private ToolbarItem PlotViewButton() {
            return new ToolbarItem("ChartView", "icons8combochart48white.png", () => { SwitchToPlotView(); });
        }

        public void SwitchToDeviceView() {
            Device.BeginInvokeOnMainThread(async () => { // to allow calling this from work thread
                if (deviceViewButton.IsEnabled) {
                    deviceViewButton.IsEnabled = false;
                    overlappingView.RaiseChild(backdrop);
                    overlappingView.RaiseChild(deviceView);
                    plotViewButton.IsEnabled = true;
                }
            });
        }
        private ToolbarItem DeviceViewButton() {
            return new ToolbarItem("DeviceView", "icons8device40white.png", () => { SwitchToDeviceView(); });
        }

        public ImageButton StopButton() {
            ImageButton button = new ImageButton() {
                Source = "icons8stop40.png",
                HeightRequest = MainTabbedPage.buttonHeightRequest,
                WidthRequest = MainTabbedPage.buttonHeightRequest,
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
            IconImageSource = "icons8combochart48.png";

            // in iOS>Resource the images of the TitleBar buttons must be size 40, otherwise they will scale but still take the horizontal space of the original

            deviceViewButton = DeviceViewButton();
            ToolbarItems.Add(deviceViewButton);
            deviceViewButton.IsEnabled = true;

            plotViewButton = PlotViewButton();
            ToolbarItems.Add(plotViewButton);
            plotViewButton.IsEnabled = false; 
            
            solverRK547MButton = SolverRK547MButton();
            ToolbarItems.Add(solverRK547MButton);
            solverRK547MButton.IsEnabled = false;

            solverGearBDFButton = SolverGearBDFButton();
            ToolbarItems.Add(solverGearBDFButton);
            solverGearBDFButton.IsEnabled = true;

            // bottom bar

            stopButton = StopButton();
            noisePicker = MainTabbedPage.theModelEntryPage.NoisePicker();
            deviceButton = MainTabbedPage.theModelEntryPage.DeviceButton();
            startButton = MainTabbedPage.theModelEntryPage.StartButton(switchToChart:false, switchToOutput:false);

            int bottomBarPadding = 4;
            bottomBar = new Grid { RowSpacing = 0, Padding = bottomBarPadding };
            bottomBar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.BackgroundColor = MainTabbedPage.secondBarColor;

            bottomBar.Children.Add(stopButton, 0, 0);
            bottomBar.Children.Add(deviceButton, 1, 0);
            bottomBar.Children.Add(noisePicker, 2, 0);
            bottomBar.Children.Add(startButton, 3, 0);

            // Setup layout structure

            grid = new Grid { ColumnSpacing = 0 };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });                               // overlappingView
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(startButton.HeightRequest + 2 * bottomBarPadding) });   // bottomBar
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            backdrop = new Label { Text = "", BackgroundColor = Color.White };

            overlappingView = new AbsoluteLayout();

            plotView = new Grid { ColumnSpacing = 0 };
            plotView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });        // chartView
            plotView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });        // overlappingView
            plotView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            scrollInspectionView = new ScrollView();

            inspectionView = new StackLayout();

            chartView = new ChartView() { Chart = new KChart("", "s", "M"), HeightRequest = 300, BackgroundColor = Color.White };
            legendView = LegendView();
            parameterView = ParameterView();
            deviceView = new DeviceView() { }; // Device = device };

            // Fill layout structure

            inspectionView.Children.Add(legendView);
            inspectionView.Children.Add(parameterView);

            scrollInspectionView.Content = inspectionView;

            AbsoluteLayout.SetLayoutBounds(deviceView, new Rectangle(0, 0, 1, 1));  // deviceView
            AbsoluteLayout.SetLayoutFlags(deviceView, AbsoluteLayoutFlags.All);
            AbsoluteLayout.SetLayoutBounds(backdrop, new Rectangle(0, 0, 1, 1));    // backdrop
            AbsoluteLayout.SetLayoutFlags(backdrop, AbsoluteLayoutFlags.All);
            AbsoluteLayout.SetLayoutBounds(plotView, new Rectangle(0, 0, 1, 1));    // scrollInspectionView
            AbsoluteLayout.SetLayoutFlags(plotView, AbsoluteLayoutFlags.All);
            overlappingView.Children.Add(deviceView);
            overlappingView.Children.Add(backdrop);
            overlappingView.Children.Add(scrollInspectionView);

            plotView.Children.Add(chartView, 0, 0);
            plotView.Children.Add(overlappingView, 0, 1);

            grid.Children.Add(plotView, 0, 0);
            grid.Children.Add(bottomBar, 0, 1);

            Content = grid;
        }

        public void InvalidateChart() {
            // this is necessary to invalidate the chartView: we must assign a fresh value to chartView.Chart
            chartView.Chart = KChartHandler.ChartCopy();
        }

        public void SetModel(ModelInfo modelInfo) {
            this.title = (modelInfo == null) ? "" : modelInfo.title;
            Title = this.title;
            currentModelInfo = modelInfo;
        }

        // ======== LEGEND ========= //

        const int LegendFontSize = 12;
        const int LegendItemHeight = 21; // If this value is too small (for the font size?), Label items will flash a gray box for 2 senconds when updated

        public void SetLegend() {
            KSeries[] legend = KChartHandler.Legend();
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                var legendList = new ObservableCollection<LegendItem>();
                for (int i = legend.Length - 1; i >= 0; i--)
                    legendList.Add(new LegendItem {
                        Name = legend[i].name,
                        Color = SkiaSharp.Views.Forms.Extensions.ToFormsColor(legend[i].color),
                        Width = (legend[i].visible) ? 50 : 6,
                        Height =
                            (legend[i].lineStyle == KLineStyle.Thick) ? 4  // show a wide bar for thick plot lines
                          : (legend[i].lineMode == KLineMode.Line) ? 1     // show a smaller bar for think plot lines
                          : LegendItemHeight,                              // show a full rectangle for Range areas
                    });
                legendView.ItemsSource = legendList;
                MainTabbedPage.theChartPage.inspectionView.Children[0].HeightRequest = 40 + LegendItemHeight * (legend.Length + 1) / 2;  // seems redundant?
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
                // grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 50 }); //it is definitely labels that flash a gray box when updated

                BoxView box = new BoxView { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center }; // needed, otherwise the default .Fill option ignores HeightRequest
                box.SetBinding(BoxView.ColorProperty, "Color"); // property of LegendItem
                box.SetBinding(BoxView.HeightRequestProperty, "Height"); // property of LegendItem
                box.SetBinding(BoxView.WidthRequestProperty, "Width"); // property of LegendItem

                Label nameLabel = new Label { FontSize = LegendFontSize, FontAttributes = FontAttributes.Bold };
                nameLabel.SetBinding(Label.TextProperty, "Name"); // property of LegendItem

                grid.Children.Add(box, 0, 0);
                grid.Children.Add(nameLabel, 1, 0);
                // grid.Children.Add(new Label { Text = "xxx" }, 2, 0); //it is definitely labels that flash a gray box when updated

                return grid;
            });

            collectionView.SelectionChanged += (object sender, SelectionChangedEventArgs args) => {
                LegendItem item = collectionView.SelectedItem as LegendItem;
                if (item != null) {
                    KChartHandler.InvertVisible(item.Name);
                    KChartHandler.VisibilityRemember();
                    KChartHandler.ChartUpdate();
                    Gui.toGui.LegendUpdate();
                    collectionView.SelectedItem = null; // avoid some visible flashing of the selection
                }
            };

            return collectionView;
        }

        // ======== PARAMETERS ========= //

        const int ParameterItemHeight = 40;

        public class ParameterBinding {
            // bind the parameter info to the gui via SetBinding/BindingContext, ugh!
            public string Parameter { get; set; } // do not rename: bound property, ugh!
            public string Format { get; set; } // do not rename: bound property, ugh!
            public string Value { get; set; } // do not rename: bound property, ugh! // a double
            public bool Locked { get; set; } // do not rename: bound property, ugh!
        }

        public void ParametersUpdate() {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                lock (KControls.parameterLock) {
                    RefreshParameters();
                }
                MainTabbedPage.theChartPage.inspectionView.Children[1].HeightRequest = 20 + ParameterItemHeight * KControls.parameterInfoDict.Count;               
            });
        }
        private void RefreshParameters() {  
            // call it with already locked parameterLock
            var parameterList = new ObservableCollection<ParameterBinding>();
            foreach (var kvp in KControls.parameterStateDict) {
                ParameterInfo info = KControls.parameterInfoDict[kvp.Key];
                KControls.ParameterState state = KControls.parameterStateDict[kvp.Key];
                ParameterBinding parameterBinding =
                    new ParameterBinding {
                        Parameter = info.parameter,
                        Format = info.ParameterLabel(),
                        Value = state.value.ToString("G4"),
                        Locked = info.locked,
                    };
                parameterList.Add(parameterBinding);
            }
            parameterView.ItemsSource = parameterList;
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
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 75 });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Xamarin.Forms.Entry slider = new Xamarin.Forms.Entry { FontSize = 10, FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center };
                slider.SetBinding(Xamarin.Forms.Entry.TextProperty, "Value"); // property of ParameterBinding, ugh!
                slider.TextChanged += (object source, TextChangedEventArgs e) => { 
                    if (slider.BindingContext == null) return;
                    ParameterBinding parameterBinding = (ParameterBinding)slider.BindingContext; // the implicit binding context, ugh!
                    lock (KControls.parameterLock) {
                        if ((!KControls.parameterStateDict.ContainsKey(parameterBinding.Parameter)) || (!KControls.parameterInfoDict.ContainsKey(parameterBinding.Parameter))) return;
                        ParameterInfo info = KControls.parameterInfoDict[parameterBinding.Parameter];
                        KControls.ParameterState state = KControls.parameterStateDict[parameterBinding.Parameter];
                        try { state.value = double.Parse((source as Xamarin.Forms.Entry).Text); } catch { }
                        info.drawn = state.value;
                        parameterBinding.Format = info.ParameterLabel();
                        //RefreshParameters(); // No longer needed, and it now seems to mess up the rows. OLD: otherwise formatLabel does not update even though it has a data binding, ugh!
                    }
                };

                Switch switcher = new Switch();
                switcher.SetBinding(Switch.IsToggledProperty, "Locked"); // property of ParameterBinding, ugh!
                switcher.Toggled += (object source, ToggledEventArgs e) => {
                    if (switcher.BindingContext == null) return; // ugh!
                    ParameterBinding parameterBinding = (ParameterBinding)switcher.BindingContext; // the implicit binding context, ugh!
                    lock (KControls.parameterLock) {
                        if (!KControls.parameterInfoDict.ContainsKey(parameterBinding.Parameter)) return;
                        ParameterInfo parameterInfo = KControls.parameterInfoDict[parameterBinding.Parameter];
                        parameterInfo.locked = (source as Switch).IsToggled;
                    }
                };

                Label formatLabel = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center };
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
            KChartHandler.ChartUpdate();
        }

    }
}
