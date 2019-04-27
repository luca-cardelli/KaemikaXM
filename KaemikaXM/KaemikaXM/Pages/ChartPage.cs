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

    public class ChartPage : ContentPage {

        private string title = "";
        private Microcharts.ChartView chartView;
        public Picker noisePicker;
        private ImageButton stopButton;
        private CollectionView legendView;
        public ImageButton startButton;

        public ImageButton StopButton() {
            ImageButton button = new ImageButton() {
                Source = "icons8stop40.png",
                HeightRequest = 40,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = Color.FromHex("9999FF"),
            };
            button.Clicked += async (object sender, EventArgs e) => {
                Gui.gui.StopEnable(false); // signals that we should stop
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

            stopButton = StopButton();
            noisePicker = MainTabbedPage.theModelEntryPage.NoisePicker();
            startButton = MainTabbedPage.theModelEntryPage.StartButton();

            int bottomBarPadding = 4;
            Grid bottomBar = new Grid { RowSpacing = 0, Padding = bottomBarPadding };
            bottomBar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.BackgroundColor = Color.FromHex("9999FF");

            bottomBar.Children.Add(stopButton, 0, 0);
            bottomBar.Children.Add(noisePicker, 1, 0);
            bottomBar.Children.Add(startButton, 2, 0);

            Grid grid = new Grid { ColumnSpacing = 0 };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(startButton.HeightRequest + 2 * bottomBarPadding) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            legendView = LegendView();

            grid.Children.Add(chartView, 0, 0);
            grid.Children.Add(legendView, 0, 1);
            grid.Children.Add(bottomBar, 0, 2);

            Content = grid;
        }

        public void SetChart(Microcharts.Chart chart) {
            chartView.Chart = chart;
        }

        public void SetTitle(string title) { 
            this.title = title;
            MainTabbedPage.theChartPage.Title = this.title;
        }

        public void SetLegend(List<Microcharts.Series> legend) {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                var legendList = new ObservableCollection<LegendItem>();
                for (int i = legend.Count - 1; i >= 0; i--)
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
            });
        }

        const int LegendItemHeight = 20;

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

            collectionView.ItemTemplate = new DataTemplate(() => {
                Grid grid = new Grid { Padding = 2 };
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(LegendItemHeight) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                BoxView box = new BoxView { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center }; // needed, otherwise the default .Fill option ignores HeightRequest
                box.SetBinding(BoxView.ColorProperty, "Color");
                box.SetBinding(BoxView.HeightRequestProperty, "Height");
                box.SetBinding(BoxView.WidthRequestProperty, "Width");

                Label nameLabel = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold };
                nameLabel.SetBinding(Label.TextProperty, "Name");

                grid.Children.Add(box, 0, 0);
                grid.Children.Add(nameLabel, 1, 0);

                return grid;
            });

            collectionView.ItemsSource = new ObservableCollection<LegendItem>();
            collectionView.SelectionChanged += (object sender, SelectionChangedEventArgs args) => {
                LegendItem item = collectionView.SelectedItem as LegendItem;
                if (item != null) {
                    MainTabbedPage.theChartPage.chartView.Chart.InvertVisible(item.Name);
                    (Gui.gui as GUI_Xamarin).VisibilityRemember();
                    Gui.gui.ChartUpdate();
                    Gui.gui.LegendUpdate();
                    collectionView.SelectedItem = null; // avoid some visble flashing of the selection
                }
            };

            return collectionView;
        }

        protected override void OnAppearing() {
            base.OnAppearing();
            SetTitle(this.title);
            MainTabbedPage.theModelEntryPage.SyncNoisePicker(noisePicker);
            Gui.gui.ChartUpdate();
        }

    }
}
