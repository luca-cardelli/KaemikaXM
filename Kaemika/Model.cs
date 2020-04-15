using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using KaemikaAssets;

namespace Kaemika
{
    // uses ModelInfo to store data
    // uses ModelInfoGroup, a list of ModelInfo for grouped lists

    public class ModelInfo {
        public string title { get; set; }        // keep this as a Property for Xamarin Binding
        public string datestring { get; set; }    // keep this as a Poperty for Xamarin Binding

        public string filename; // this will be a randomly generated filename that is never shown
        public string text; // make sure to initialize to "", not null
        public bool modified;
        public bool executable;
        public DateTime date;
        public ModelInfo() {
            title = "Untitled";
            text = "";
            modified = false;
            executable = true;
            filename = "";
            date = DateTime.Now;
            datestring = DateTime.Now.ToString();
        }
        public ModelInfo(string sample) {
            title = "Untitled";
            text = sample;
            modified = false;
            executable = true;
            filename = "";
            date = DateTime.Now;
            datestring = DateTime.Now.ToString();
        }
        public ModelInfo Copy() {
            ModelInfo copy = new ModelInfo();
            copy.title = this.title + " (copy)";
            copy.text = this.text;
            return copy;
        }
    }

    public class ModelInfoGroup : ObservableCollection<ModelInfo> {
        public string GroupHeading { get; private set; }
        public ModelInfoGroup(string groupHeading) {
            GroupHeading = groupHeading;
        }
    }

    public static class Tutorial {
        private static List<ModelInfoGroup> groups = null;
        public static List<ModelInfoGroup> Groups () {
            if (groups == null) groups = Setup();
            return groups;
        }
        private static void AddAsset(ModelInfoGroup group, string assetname, bool executable = true) {
            try {
                group.Add(new ModelInfo {
                    filename = "",
                    title = assetname,
                    //text = new StreamReader(Assets.Open(assetname + ".txt")).ReadToEnd(), // old Android assets
                    text = SharedAssets.TextAsset(assetname + ".txt"),
                    date = DateTime.Now,
                    executable = executable,
                });
            } catch { }
        }
        //public string ReadAsset(string asset)  {
        //    StreamReader sr = new StreamReader(Assets.Open("AboutAssets"+".txt"));
        //    return sr.ReadToEnd();
        //}

        private static List<ModelInfoGroup> Setup() {
            var groups = new List<ModelInfoGroup>();

            var group1 = new ModelInfoGroup("Basic Models");
            foreach (string a in new List<string> { "StartHere", "LotkaVolterra", "Predatorial", "RingOscillator", "Reactions", "EnzymeKinetics", "ApproximateMajority", "2AM Oscillator", "Transporters" }) AddAsset(group1, a);
            groups.Add(group1);

            var group3 = new ModelInfoGroup("Protocols");
            foreach (string a in new List<string> { "Samples", "Droplets", "MixAndSplit", "PBS", "SerialDilution" }) AddAsset(group3, a);
            groups.Add(group3);

            var group2a = new ModelInfoGroup("Arithmetic");
            foreach (string a in new List<string> { "A01 Copy", "A02 Addition", "A03 CopyAndAdd", "A04 Multiplication", "A05 Division", "B01 DifferentialSignals", "B02 DifferentialAddition", "B03 DifferentialSubtraction", "B04 DifferentialAbstractions" }) AddAsset(group2a, a);
            groups.Add(group2a);

            var group2 = new ModelInfoGroup("Differential Signals");
            foreach (string a in new List<string> { "SineWave", "SquareWave", "HighPassFilter", "LorenzAttractor", "Derivative1", "Derivative2" }) AddAsset(group2, a);
            groups.Add(group2);

            var group5 = new ModelInfoGroup("PID Controller");
            foreach (string a in new List<string> { "PosTestSignal Sine", "PosTestSignal Step", "TestSignal Sine", "TestSignal Step", "Proportional Block", "Integral Block", "Derivative Block", "Addition Block", "Subtraction Block", "DualRailConverter Block", "PIDController Block", "PIDController", "PIDController Optimization" }) AddAsset(group5, a);
            groups.Add(group5);

            var group4 = new ModelInfoGroup("Documentation");
            foreach (string a in new List<string> { "KaemikaGrammar", "BuiltinFunctions", "Flows", "Functions" }) AddAsset(group4, a, executable: false);
            groups.Add(group4);

            return groups;
        }
    }

}
