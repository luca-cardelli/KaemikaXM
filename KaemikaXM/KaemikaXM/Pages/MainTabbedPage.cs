using System;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
//using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Kaemika;

namespace KaemikaXM.Pages {

    public interface ICustomTextEdit {
        Xamarin.Forms.View AsView();
        string GetText();
        void SetText(string text);
        void InsertText(string text);
        void SelectAll();
        void SetFocus();
        void ShowInputMethod(); // pop up the keyboard
        void HideInputMethod(); // pop down the keyboard
        void SetSelection(int start, int end);
        void SetSelectionLineChar(int line, int chr, int length); // line >=0, ch >=0
        float GetFontSize();
        void SetFontSize(float size);
        void SetEditable(bool editable);
        bool IsEditable();
        void OnTextChanged(TextChangedDelegate del);
        void OnFocusChange(FocusChangeDelegate del);
    }
    public delegate void TextChangedDelegate(ICustomTextEdit textEdit);
    public delegate void FocusChangeDelegate(ICustomTextEdit textEdit);

    public abstract class KaemikaPage : ContentPage { // the children of the main tabbed page
        public abstract void OnSwitchedTo(); // called by hand since OnAppearing is flakey
    }

    public class MainTabbedPage : Xamarin.Forms.TabbedPage {

        public static Color barColor = Color.FromHex("6200ED"); // Electric Indigo: "6F00FF" // standard blue: "2195F3"; https://www.color-hex.com/
        public static Color secondBarColor = Color.FromHex("E8F2FC"); // formerly "61D5ff"; 
        public static Color pickerColor = Color.FromHex("E0F2FC");
        public static int buttonHeightRequest = 40;

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
            this.On<Xamarin.Forms.PlatformConfiguration.Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);
            BarBackgroundColor = barColor;
            BarTextColor = Color.White;
            UnselectedTabColor = Color.White;
            SelectedTabColor = Color.White;
            this.On<Xamarin.Forms.PlatformConfiguration.Android>().DisableSwipePaging();  //disables swiping between tabbed pages

            theDocListPage = new DocListPage();
            theModelListPage = new ModelListPage();
            theModelEntryPage = new ModelEntryPage();
            theOutputPage = new OutputPage();
            theChartPage = new ChartPage();

            theDocListPageNavigation = new NavigationPage(theDocListPage) { Title = "Tutorial", IconImageSource = "icons8usermanual100.png", BarBackgroundColor = barColor, BarTextColor = Color.White }; 
            theModelListPageNavigation = new NavigationPage(theModelListPage) { Title = "Files", IconImageSource = "icons8openedfolder96.png", BarBackgroundColor = barColor, BarTextColor = Color.White }; 
            theModelEntryPageNavigation = new NavigationPage(theModelEntryPage) { Title = "Edit", IconImageSource = "icons8mindmap96.png", BarBackgroundColor = barColor, BarTextColor = Color.White }; 
            theOutputPageNavigation = new NavigationPage(theOutputPage) { Title = "Output", IconImageSource = "icons8truefalse100.png", BarBackgroundColor = barColor, BarTextColor = Color.White }; 
            theChartPageNavigation = new NavigationPage(theChartPage) { Title = "Chart", IconImageSource = "icons8combochart48.png", BarBackgroundColor = barColor, BarTextColor = Color.White };  // DO NOT use "icons8combochart96.png", it will not scale

            // To change tab order, just shuffle these Add calls around.
            // with more than 5 tabs the app will just crash on load
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

        public static void Executing(bool executing) {
            if (executing) {
                theOutputPageNavigation.IconImageSource = "icons8refresh96.png"; // this is actually a size 40 icon in iOS resources: otherwise it does not scale
                theChartPageNavigation.IconImageSource = "icons8refresh96.png"; // this is actually a size 40 icon in iOS resources: otherwise it does not scale
                // we need to use size 40x40 icons or they get stuck at wrong size after changing icon:
                theModelEntryPage.deviceButton.Source = "icons8device40disabled.png";
                theChartPage.deviceButton.Source = "icons8device40disabled.png";
                theModelEntryPage.startButton.Source = "icons8play40disabled.png";
                theChartPage.startButton.Source = "icons8play40disabled.png"; 
                theChartPage.stopButton.Source = "icons8stop40.png"; 
            }
            else {
                theOutputPageNavigation.IconImageSource = "icons8truefalse100.png";
                theChartPageNavigation.IconImageSource = "icons8combochart48.png"; // DO NOT use icons8combochart96, it will not scale
                // we need to use size 40x40 icons or they get stuck at wrong size after changing icon:
                theModelEntryPage.deviceButton.Source = ProtocolDevice.Exists() ? "icons8device40on.png" : "icons8device40off.png";
                theChartPage.deviceButton.Source = ProtocolDevice.Exists() ? "icons8device40on.png" : "icons8device40off.png";
                theModelEntryPage.startButton.Source = "icons8play40.png";
                theChartPage.startButton.Source = "icons8play40.png"; 
                theChartPage.stopButton.Source = "icons8stop40disabled.png";
            }
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

        private static string onAnySwitchedTo = ""; // debug
        public static void OnAnySwitchedTo(KaemikaPage toPage) {
            onAnySwitchedTo = toPage.ToString();
        }

        // Device rotation handling

        private double width = 0;
        private double height = 0;

        protected override void OnSizeAllocated(double width, double height) {
            base.OnSizeAllocated(width, height); //must be called
            if ((width > 0 && this.width != width) || (height > 0 && this.height != height)) {
                this.width = width;
                this.height = height;
                // // Disabled
                // if (this.width > this.height) { if (Gui.platform != "iOS") App.LandscapeOrientation(); } // iOS does not handle well return from rotation
                // // else App.theApp.MainPage.ForceLayout(); // does not help, also check that theApp is not null
            }
        }

    }
}
