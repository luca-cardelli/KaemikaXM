using System;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;

namespace KaemikaXM.Pages {

    public interface ICustomTextEdit {
        string GetText();
        void SetText(string text);
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

        public static DocListPage theDocListPage = new DocListPage();                           // this page is never pushed, there is only one
        public static ModelListPage theModelListPage = new ModelListPage();                     // this page is never pushed, there is only one
        public static ModelEntryPage theModelEntryPage = new ModelEntryPage();                  // this page is never pushed, there is only one
        public static OutputPage theOutputPage = new OutputPage();                              // this page is never pushed, there is only one
        public static ChartPage theChartPage = new ChartPage();                                 // this page is never pushed, there is only one

        public static NavigationPage theDocListPageNavigation = new NavigationPage(theDocListPage) { Title = "Tutorial", Icon = "icons8usermanual100.png" };
        public static NavigationPage theModelListPageNavigation = new NavigationPage(theModelListPage) { Title = "Networks", Icon = "icons8openedfolder96.png" };
        public static NavigationPage theModelEntryPageNavigation = new NavigationPage(theModelEntryPage) { Title = "Network", Icon = "icons8mindmap96.png" };
        public static NavigationPage theOutputPageNavigation = new NavigationPage(theOutputPage) { Title = "Output", Icon = "icons8truefalse100.png" };
        public static NavigationPage theChartPageNavigation = new NavigationPage(theChartPage) { Title = "Chart", Icon = "icons8combochart48.png" };

        public MainTabbedPage() {
            var specific = this.On<Xamarin.Forms.PlatformConfiguration.Android>();
            specific.SetToolbarPlacement(ToolbarPlacement.Bottom);
            BarBackgroundColor = Color.FromHex("2195F3");
            BarTextColor = Color.White;
            specific.SetBarItemColor(Color.FromHex("66FFFFFF"));
            specific.SetBarSelectedItemColor(Color.White);
            //specific.DisableSmoothScroll(); //??
            //specific.DisableSwipePaging();  //disables swiping between tabbed pages

            // To change tab order, just shuffle these Add calls around.
            Children.Add(theDocListPageNavigation);
            Children.Add(theModelListPageNavigation);
            Children.Add(theModelEntryPageNavigation);
            Children.Add(theOutputPageNavigation);
            Children.Add(theChartPageNavigation);
        }

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
