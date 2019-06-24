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
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            // put kaemikaCGT.cgt in the Assets folder of the .Android subproject, and set its build action to AndroidAsset
            // we need to do the asset reading here from inside the .Android subproject to get access to the Android.Content namespace
            Stream stream = Assets.Open("kaemikaCGT.cgt");
            MemoryStream cgtStream = new MemoryStream(); 
            stream.CopyTo(cgtStream);       // copy Stream to a MemoryStream because CGTReader will try to ask for the length of Stream, and a plain Stream does not suport that
            cgtStream.Position = 0;         // after CopyTo the memorystream position is at the end, so reset it

            var groups = new List<KaemikaXM.Pages.ModelInfoGroup>();

            var group1 = new KaemikaXM.Pages.ModelInfoGroup("Basic Models");
            foreach (string a in new List<string> { "StartHere", "RingOscillator", "Reactions", "EnzymeKinetics", "ApproximateMajority", "2AM Oscillator", "Transporters" }) AddAsset(group1, a);
            groups.Add(group1);

            var group2 = new KaemikaXM.Pages.ModelInfoGroup("Differential Signals");
            foreach (string a in new List<string> { "SineWave", "SquareWave", "HighPassFilter", "LorenzAttractor", "Derivative1", "Derivative2" }) AddAsset(group2, a);
            groups.Add(group2);

            var group5 = new KaemikaXM.Pages.ModelInfoGroup("PID Controller");
            foreach (string a in new List<string> { "PosTestSignal Sine", "PosTestSignal Step", "TestSignal Sine", "TestSignal Step", "Proportional Block", "Integral Block", "Derivative Block", "Addition Block", "Subtraction Block", "DualRailConverter Block", "PIDController Block", "PIDController", "PIDController Optimization" }) AddAsset(group5, a);
            groups.Add(group5);

            var group3 = new KaemikaXM.Pages.ModelInfoGroup("Protocols");
            foreach (string a in new List<string> { "Samples", "MolarMass", "MixAndSplit", "PBS", "SerialDilution" }) AddAsset(group3, a);
            groups.Add(group3);

            var group4 = new KaemikaXM.Pages.ModelInfoGroup("Documentation");
            foreach (string a in new List<string> { "KaemikaGrammar", "BuiltinFunctions", "Flows", "Functions" }) AddAsset(group4, a, executable: false);
            groups.Add(group4);

            // allow the higher level of the package hierarchy to access the device-dependent functionality
            // without building dependencies on the device packagages
            GUI_Xamarin.customTextEditor = 
                () => {
                    return new CustomTextEditView {
                        HorizontalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand,
                        VerticalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand,
                    };
            };

            global::Xamarin.Forms.Forms.SetFlags("Shell_Experimental", "Visual_Experimental", "CollectionView_Experimental", "FastRenderers_Experimental");
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            LoadApplication(new App(cgtStream, groups));
        }

        private void AddAsset(KaemikaXM.Pages.ModelInfoGroup group, string assetname, bool executable = true) {
            try {
                group.Add(new KaemikaXM.Pages.ModelInfo {
                    filename = "",
                    title = assetname,
                    text = new StreamReader(Assets.Open(assetname + ".txt")).ReadToEnd(),
                    date = DateTime.Now,
                    executable = executable,
                });
            } catch { }
        }

        public string ReadAsset(string asset)  {
            StreamReader sr = new StreamReader(Assets.Open("AboutAssets"+".txt"));
            return sr.ReadToEnd();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)  {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}