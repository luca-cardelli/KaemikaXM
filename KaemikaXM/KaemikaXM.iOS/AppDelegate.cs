using Foundation;
using UIKit;
using System.IO;
using Kaemika;

namespace KaemikaXM.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {

            global::Xamarin.Forms.Forms.SetFlags("Shell_Experimental", "Visual_Experimental", "CollectionView_Experimental", "FastRenderers_Experimental");
            global::Xamarin.Forms.Forms.Init();

            CustomTextEditorDelegate neutralTextEditor =
               () => {
                   GUI_Xamarin.NeutralTextEditView editor = new GUI_Xamarin.NeutralTextEditView();
                   Xamarin.Forms.View view = editor.AsView();
                   view.HorizontalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand;
                   view.VerticalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand;
                   return editor;
                   //return new GUI_Xamarin.NeutralTextEditView {
                   //    HorizontalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand,
                   //    VerticalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand,
                   //};
               };


            CustomTextEditorDelegate customTextEditor =
                () => {
                    return new CustomTextEditView {
                        HorizontalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand,
                        VerticalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand,
                    };
            };

            // How to add a LaunchScreen - SplashScreen
            // https://docs.microsoft.com/en-us/xamarin/ios/app-fundamentals/images-icons/launch-screens?tabs=windows#migrating-to-launch-screen-storyboards

            UINavigationBar.Appearance.TintColor = UIColor.White; // affect the color of the bitmaps in the top toolbar
            Gui.platform = Kaemika.Platform.iOS;
            LoadApplication(new App(customTextEditor));  // or neutralTextEditor

            return base.FinishedLaunching(app, options);
        }
    }
}
