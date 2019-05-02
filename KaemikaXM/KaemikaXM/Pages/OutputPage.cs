using Xamarin.Forms;
using Xamarin.Essentials;

namespace KaemikaXM.Pages {

    public class OutputPage : KaemikaPage {

        private string title = "";
        private View editor; // is a CustomTextEditView and implements ICustomTextEdit
        private ToolbarItem traceComputationalButton;
        private ToolbarItem traceChemicalButton;
        private ToolbarItem graphVizButton;
        public string outputPickerSelection;

        private ToolbarItem TraceComputationalButton() {
            return
                new ToolbarItem("Computational", "icons8text", async () => {
                    SetExport(Export.ComputationalTrace);
                    traceComputationalButton.IsEnabled = false;
                    traceChemicalButton.IsEnabled = true;
                    graphVizButton.IsEnabled = true;
                });
        }

        private ToolbarItem TraceChemicalButton() {
            return
                new ToolbarItem("Chemical", "icons8lesstext", async () => {
                    SetExport(Export.ChemicalTrace);
                    traceComputationalButton.IsEnabled = true;
                    traceChemicalButton.IsEnabled = false;
                    graphVizButton.IsEnabled = true;
                });
        }

        private ToolbarItem GraphVizButton() {
            return
                new ToolbarItem("GraphViz", "icons8activedirectoryfilled100", async () => {
                    SetExport(Export.GraphViz);
                    traceComputationalButton.IsEnabled = true;
                    traceChemicalButton.IsEnabled = true;
                    graphVizButton.IsEnabled = false;
                });
        }

        //public Picker OutputPicker() {
        //    Picker outputPicker = new Picker {
        //        Title = "Output and Export",
        //        HorizontalOptions = LayoutOptions.CenterAndExpand,
        //        BackgroundColor = Color.FromHex(ModelEntryPage.secondBarColor),
        //        FontSize = 14,
        //    };
        //    outputPicker.Items.Add("Chemical");
        //    outputPicker.Items.Add("Computational");
        //    outputPicker.Items.Add("GraphViz");
        //    outputPickerSelection = "Chemical";
        //    outputPicker.SelectedItem = outputPickerSelection;
        //    outputPicker.Unfocused += async (object sender, FocusEventArgs e) => {
        //        outputPickerSelection = outputPicker.SelectedItem as string;
        //        if (outputPickerSelection == "Chemical") SetExport(Export.ChemicalTrace);
        //        if (outputPickerSelection == "Computational") SetExport(Export.ComputationalTrace);
        //        if (outputPickerSelection == "GraphViz") SetExport(Export.GraphViz); ;
        //    };
        //    return outputPicker;
        //}

        public OutputPage() {
            Icon = "icons8truefalse100.png";

            traceComputationalButton = TraceComputationalButton();
            traceChemicalButton = TraceChemicalButton();
            graphVizButton = GraphVizButton();

            ToolbarItems.Add(graphVizButton);
            graphVizButton.IsEnabled = true;
            ToolbarItems.Add(traceChemicalButton);
            traceChemicalButton.IsEnabled = false;
            ToolbarItems.Add(traceComputationalButton);
            traceComputationalButton.IsEnabled = true;
            ToolbarItems.Add(
                new ToolbarItem("CopyAll", "icons8export96", async () => {
                    string text = MainTabbedPage.theOutputPage.GetText();
                    if (text != "") await Clipboard.SetTextAsync(text);
                }));

            editor = Kaemika.GUI_Xamarin.customTextEditor();
            (editor as ICustomTextEdit).SetEditable(false);

            var stepper = MainTabbedPage.theModelEntryPage.TextSizeStepper(editor as ICustomTextEdit);
            var startButton = MainTabbedPage.theModelEntryPage.StartButton(); // just needed to the its HightRequest
            //var outputPicker = OutputPicker();

            //var layout = new AbsoluteLayout();
            //AbsoluteLayout.SetLayoutBounds(editor, new Rectangle(0, 0, 1, 1));
            //AbsoluteLayout.SetLayoutFlags(editor, AbsoluteLayoutFlags.All);
            //layout.Children.Add(editor);

            int bottomBarPadding = 4;
            Grid bottomBar = new Grid { RowSpacing = 0, Padding = bottomBarPadding };
            bottomBar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.BackgroundColor = Color.FromHex(ModelEntryPage.secondBarColor);

            bottomBar.Children.Add(stepper, 0, 0);
            //bottomBar.Children.Add(outputPicker, 1, 0);
            //Grid.SetColumnSpan(outputPicker, 2);
            // bottomBar.Children.Add(noisePicker, 1, 0);
            // bottomBar.Children.Add(startButton, 2, 0);

            Grid grid = new Grid { ColumnSpacing = 0 };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(startButton.HeightRequest + 2 * bottomBarPadding) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(editor, 0, 0);
            grid.Children.Add(bottomBar, 0, 1);

            Content = grid;
        }

        public enum Export { ChemicalTrace, ComputationalTrace, GraphViz };
        private Export export = Export.ChemicalTrace;
        private string chemicalOutput = "";
        private string computationalOutput = "";
        private string graphVizOutput = "";

        public void SetExport(Export export) {
            this.export = export;
            ShowExport();
        }

        public void ShowExport() {
            if (export == Export.ChemicalTrace) (editor as ICustomTextEdit).SetText(chemicalOutput);
            if (export == Export.ComputationalTrace) (editor as ICustomTextEdit).SetText(computationalOutput);
            if (export == Export.GraphViz) (editor as ICustomTextEdit).SetText(graphVizOutput);
        }

        public string GetText() {
            if (export == Export.ChemicalTrace) return chemicalOutput;
            if (export == Export.ComputationalTrace) return computationalOutput;
            if (export == Export.GraphViz) return graphVizOutput;
            return "";
        }

        public void SetText(string text) {
            chemicalOutput = text;
            computationalOutput = text;
            graphVizOutput = "";
            ShowExport();
        }

        public void AppendText(string text) {
            chemicalOutput = chemicalOutput + text;
            computationalOutput = computationalOutput + text;
            // graphVizOutput unchanged
            ShowExport();
        }

        public void AppendTraceComputational(string chemicalTrace, string computationalTrace, string graphViz) {
            chemicalOutput = chemicalOutput + chemicalTrace;
            computationalOutput = computationalOutput + computationalTrace;
            graphVizOutput = graphVizOutput + graphViz;
            ShowExport();
        }

        public void SetTitle(string title) { 
            this.title = title;
            MainTabbedPage.theOutputPage.Title = this.title;
        }

        public override void OnSwitchedTo() {
            MainTabbedPage.OnAnySwitchedTo(this);
            SetTitle(this.title);
        }

    }
}
