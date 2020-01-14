using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using System.IO;
using System.Collections.Generic;
using Kaemika;

namespace KaemikaXM.Droid {

    // This label determines the application name, overriding the manifest and the project properties sheet
    [Activity(Label = "Kaemika", Icon = "@mipmap/icon", Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)] // MainLauncher = true, replaced by SplashScreen

    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {

        protected override void OnCreate(Bundle savedInstanceState) {
            // These refer to Android axml files in the Resources>layout directory of this package
            // they help set the style (deep purple) for the application tabbar and toolbar at the bottom and top
            TabLayoutResource = Resource.Layout.Tabbar; 
            ToolbarResource = Resource.Layout.Toolbar; 

            base.OnCreate(savedInstanceState);

            global::Xamarin.Forms.Forms.SetFlags("Shell_Experimental", "Visual_Experimental", "CollectionView_Experimental", "FastRenderers_Experimental");
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            // How to open files from the Assets directory of KaemikaXM.Android:
            // Android.Content.Res.AssetManager Android.Content.ContentWrapper.Assets(...)
            // Assets.Open("xxx");

            CustomTextEditorDelegate customTextEditor =
                () => {
                    return new CustomTextEditView {
                        HorizontalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand,
                        VerticalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand,
                    };
            };

            Gui.platform = Kaemika.Platform.Android;
            LoadApplication(new App(customTextEditor));
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)  {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}