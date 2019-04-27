using System;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;

namespace KaemikaXM.Pages {
    public class MainTabbedPage : Xamarin.Forms.TabbedPage {

        public static MainTabbedPage theMainTabbedPage;
        public static DocListPage theDocListPage = new DocListPage();                           // this page is never pushed, there is only one
        public static ModelListPage theModelListPage = new ModelListPage();                     // this page is never pushed, there is only one
        public static ModelEntryPage theModelEntryPage = new ModelEntryPage();                  // this page is never pushed, there is only one
        public static OutputPage theOutputPage = new OutputPage();                              // this page is never pushed, there is only one
        public static ChartPage theChartPage = new ChartPage();                                 // this page is never pushed, there is only one
        public static ChartPageLandscape theChartPageLandscape = new ChartPageLandscape();      // this page is never pushed, there is only one

        public static NavigationPage theModelListPageNavigation;

        public MainTabbedPage() {
            var specific = this.On<Xamarin.Forms.PlatformConfiguration.Android>();
            specific.SetToolbarPlacement(ToolbarPlacement.Bottom);
            BarBackgroundColor = Color.FromHex("2195F3");
            BarTextColor = Color.White;
            specific.SetBarItemColor(Color.FromHex("66FFFFFF"));
            specific.SetBarSelectedItemColor(Color.White);

            // To change tab order, just shuffle these Add calls around.
            Children.Add(new NavigationPage(theDocListPage) { Title = "Tutorial", Icon = "icons8usermanual100.png" });
            theModelListPageNavigation = new NavigationPage(theModelListPage) { Title = "Networks", Icon = "icons8openedfolder96.png" }; Children.Add(theModelListPageNavigation);
            Children.Add(new NavigationPage(theModelEntryPage) { Title = "Network", Icon = "icons8mindmap96.png" });
            Children.Add(new NavigationPage(theOutputPage) { Title = "Output", Icon = "icons8truefalse100.png" });
            Children.Add(new NavigationPage(theChartPage) { Title = "Chart", Icon = "icons8combochart48.png" });
        }

        public void SwitchToTab(string title) {
            foreach (Page child in theMainTabbedPage.Children) {
                if (child.Title == title) {
                    theMainTabbedPage.CurrentPage = child;
                    if (title == "My Networks" || title == "Networks") theModelListPage.RegenerateList(); // OnAppearing() seems to miss
                    return;
                }
            }
        }

        
        // Device rotation handling

        private double width = 0;
        private double height = 0;

        protected override void OnSizeAllocated(double width, double height) {
            base.OnSizeAllocated(width, height); //must be called
            if (this.width != width || this.height != height) {
                this.width = width;
                this.height = height;
                
                if (this.width > this.height) App.LandscapeOrientation();
            }
        }

    }
}
