using System;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;

namespace KaemikaXM.Pages {

    public interface ICustomTextEdit {
        string GetText();
        void SetText(string text);
        void InsertText(string text);
        void SelectAll();
        void SetFocus();
        void SetSelection(int start, int end);
        void SetSelectionLineChar(int line, int chr, int length); // line >=0, ch >=0
        float GetFontSize();
        void SetFontSize(float size);
        void SetEditable(bool editable);
        void OnTextChanged(TextChangedDelegate del);
        void OnFocusChange(FocusChangeDelegate del);
    }
    public delegate void TextChangedDelegate(ICustomTextEdit textEdit);
    public delegate void FocusChangeDelegate(ICustomTextEdit textEdit);

    public abstract class KaemikaPage : ContentPage { // the children of the main tabbed page
        public abstract void OnSwitchedTo(); // called by hand since OnAppearing is flakey
    }

    public class MainTabbedPage : Xamarin.Forms.TabbedPage {

        public static MainTabbedPage theMainTabbedPage;
        public static ChartPageLandscape theChartPageLandscape = new ChartPageLandscape();      // this page is never pushed, there is only one

        public static DocListPage theDocListPage;                           // this page is never pushed, there is only one
        public static ModelListPage theModelListPage;                     // this page is never pushed, there is only one
        public static ModelEntryPage theModelEntryPage;                  // this page is never pushed, there is only one
        public static OutputPage theOutputPage;                              // this page is never pushed, there is only one
        public static ChartPage theChartPage;                                 // this page is never pushed, there is only one

        public static NavigationPage theDocListPageNavigation;
        public static NavigationPage theModelListPageNavigation;
        public static NavigationPage theModelEntryPageNavigation;
        public static NavigationPage theOutputPageNavigation;
        public static NavigationPage theChartPageNavigation;

        public MainTabbedPage() {
            var specific = this.On<Xamarin.Forms.PlatformConfiguration.Android>();
            specific.SetToolbarPlacement(ToolbarPlacement.Bottom);
            BarBackgroundColor = Color.FromHex("2195F3");
            BarTextColor = Color.White;
            specific.SetBarItemColor(Color.FromHex("66FFFFFF"));
            specific.SetBarSelectedItemColor(Color.White);
            //specific.DisableSmoothScroll(); //??
            //specific.DisableSwipePaging();  //disables swiping between tabbed pages

            theDocListPage = new DocListPage();
            theModelListPage = new ModelListPage();
            theModelEntryPage = new ModelEntryPage();
            theOutputPage = new OutputPage();
            theChartPage = new ChartPage();

            theDocListPageNavigation = new NavigationPage(theDocListPage) { Title = "Tutorial", Icon = "icons8usermanual100.png" };
            theModelListPageNavigation = new NavigationPage(theModelListPage) { Title = "Networks", Icon = "icons8openedfolder96.png" };
            theModelEntryPageNavigation = new NavigationPage(theModelEntryPage) { Title = "Network", Icon = "icons8mindmap96.png" };
            theOutputPageNavigation = new NavigationPage(theOutputPage) { Title = "Output", Icon = "icons8truefalse100.png" };
            theChartPageNavigation = new NavigationPage(theChartPage) { Title = "Chart", Icon = "icons8combochart48.png" };

            // To change tab order, just shuffle these Add calls around.
            Children.Add(theDocListPageNavigation);
            Children.Add(theModelListPageNavigation);
            Children.Add(theModelEntryPageNavigation);
            Children.Add(theOutputPageNavigation);
            Children.Add(theChartPageNavigation);
        }

        public static EventHandler currentPageChangedDelegate = (object sender, EventArgs e) => {  // add to instance to respond to event CurrentPageChanged
            if ((sender as MainTabbedPage).CurrentPage != null)
                (((sender as MainTabbedPage)
                .CurrentPage as NavigationPage)
                .CurrentPage as KaemikaPage)
                .OnSwitchedTo();
        };

        public static void SwitchToTab(NavigationPage page) {
            foreach (NavigationPage child in theMainTabbedPage.Children) {
                if (child == page) {
                    (page.CurrentPage as KaemikaPage).OnSwitchedTo();
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                        theMainTabbedPage.CurrentPage = page;
                    });
                    return;
                }
            }
        }

        private static string onAnySwitchedTo = ""; // debug
        public static void OnAnySwitchedTo(KaemikaPage toPage) {
            onAnySwitchedTo = toPage.ToString();
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
