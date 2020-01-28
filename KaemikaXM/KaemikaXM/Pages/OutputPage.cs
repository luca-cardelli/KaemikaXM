using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Essentials;
using Kaemika;
using QuickGraph;
using GraphSharp;

namespace KaemikaXM.Pages {

    public enum OutputKind {Text, Graph};

    public class OutputAction {
        public string name;
        public OutputKind kind;       // action outputs to either textOutput or grapOutput sub-page
        public System.Action action;  // this may be invoked from work thread
        public OutputAction(OutputPage page, string name, OutputKind kind, ExportAs export) {
            this.name = name;
            this.kind = kind;
            this.action = async () => { page.SwitchAndExecute(kind, export); };
        }
    }

    public class OutputPage : KaemikaPage {

        private string title = "";
        private ModelInfo currentModelInfo;
        private Grid grid;
        private AbsoluteLayout overlapping;
        private ICustomTextEdit editor;
        private View plot;     // is a GraphLayoutView
        private View backdrop; // because the editor stops short of filling the view
        public ToolbarItem textOutputButton;
        public ToolbarItem graphOutputButton;
        private Picker outputPicker;

        public OutputAction currentOutputAction;
        private OutputAction currentTextOutputAction;
        private OutputAction currentGraphOutputAction;

        private Dictionary<string, OutputAction> outputActions;
        private List<OutputAction> outputActionsList() {
            return new List<OutputAction>() {
                new OutputAction(this, "Reaction Network", OutputKind.Text, ExportAs.CRN),
                new OutputAction(this, "Chemical Trace", OutputKind.Text, ExportAs.ChemicalTrace),
                new OutputAction(this, "Computational Trace", OutputKind.Text, ExportAs.ComputationalTrace),
                new OutputAction(this, "Reaction Graph", OutputKind.Graph, ExportAs.ReactionGraph),
                new OutputAction(this, "Reaction Complex Graph", OutputKind.Graph, ExportAs.ComplexGraph),
                new OutputAction(this, "Protocol Step Graph", OutputKind.Graph, ExportAs.ProtocolGraph),
                new OutputAction(this, "Protocol State Graph", OutputKind.Graph, ExportAs.PDMPGraph),
                new OutputAction(this, "System Reactions", OutputKind.Text, ExportAs.PDMPreactions),
                new OutputAction(this, "System Equations", OutputKind.Text, ExportAs.PDMPequations),
                new OutputAction(this, "System Stoichiometry", OutputKind.Text, ExportAs.PDMPstoichiometry),
                new OutputAction(this, "Scalable Vector Graphics", OutputKind.Text, ExportAs.SVG),
                //new OutputAction(this, "Protocol State Graph (Par.)", OutputKind.Graph, ExportAs.PDMPGraph_Parallel),
            };
        }

        public void SwitchAndExecute(OutputKind kind, ExportAs export) { // this may be invoked from work thread, so execute it on main thread
            if (kind == OutputKind.Text) {
                Device.BeginInvokeOnMainThread(async () => {
                    textOutputButton.IsEnabled = false;
                    overlapping.RaiseChild(backdrop);
                    overlapping.RaiseChild(editor.AsView());
                    graphOutputButton.IsEnabled = true;
                });
            } else {
                Device.BeginInvokeOnMainThread(async () => {
                    graphOutputButton.IsEnabled = false;
                    overlapping.RaiseChild(backdrop);
                    overlapping.RaiseChild(plot);
                    textOutputButton.IsEnabled = true;
                });
            }
            Gui.toGui.OutputSetText("");
            Exec.Execute_Exporter(true, export);
        }

        private ToolbarItem TextOuputButton() {
            return new ToolbarItem("TextOutput", "icons8text", () => {
                outputPicker.SelectedItem = currentTextOutputAction.name; // triggers a Picker.SelectedIndexChanged event
            });
        }

        private ToolbarItem GraphOutputButton() {
            return new ToolbarItem("GraphOutput", "icons8activedirectoryfilled100", () => {
                outputPicker.SelectedItem = currentGraphOutputAction.name; // triggers a Picker.SelectedIndexChanged event
            });
        }

        public Picker OutputPicker() {
            Picker outputPicker = new Picker {
                Title = "Output and Export", TitleColor = MainTabbedPage.barColor,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                BackgroundColor = MainTabbedPage.pickerColor,
                FontSize = 14,
            };

            currentTextOutputAction = outputActions["Reaction Network"];
            currentGraphOutputAction = outputActions["Protocol Step Graph"];
            currentOutputAction = currentTextOutputAction;

            foreach (var kvp in outputActions) outputPicker.Items.Add(kvp.Key);
            outputPicker.SelectedItem = currentOutputAction.name;

            outputPicker.SelectedIndexChanged += async (object sender, System.EventArgs e) => {
                currentOutputAction = outputActions[outputPicker.SelectedItem as string];
                if (currentOutputAction.kind == OutputKind.Text) currentTextOutputAction = currentOutputAction;
                if (currentOutputAction.kind == OutputKind.Graph) currentGraphOutputAction = currentOutputAction;
                currentOutputAction.action();
            };
            return outputPicker;
        }

        public void SetTraceComputational() {
            string selectedItem = "Computational Trace";
            outputPicker.SelectedItem = selectedItem;
            //// that should trigger the rest:
            //currentOutputAction = outputActions[selectedItem];
            //currentTextOutputAction = currentOutputAction;
        }

        public OutputPage() {
            Title = "Output";
            IconImageSource = "icons8truefalse100.png";

            outputActions = new Dictionary<string, OutputAction>();
            foreach (OutputAction outputAction in outputActionsList())
                outputActions[outputAction.name] = outputAction;

            // in iOS>Resource the images of the TitleBar buttons must be size 40, otherwise they will scale but still take the horizontal space of the original

            textOutputButton = TextOuputButton();
            graphOutputButton = GraphOutputButton();

            ToolbarItems.Add(graphOutputButton);
            graphOutputButton.IsEnabled = true;
            ToolbarItems.Add(textOutputButton);
            textOutputButton.IsEnabled = false;
            ToolbarItems.Add(
                new ToolbarItem("CopyAll", "icons8export96", async () => {
                    string text = "";
                    if (currentOutputAction.kind == OutputKind.Text) {
                        text = editor.GetText();
                    }
                    if (currentOutputAction.kind == OutputKind.Graph) {
                        var layout = (plot as GraphLayoutView).GraphLayout;
                        if (layout == null) return;
                        var graph = layout.GRAPH;
                        if (graph == null) return;
                        text = Export.GraphViz(graph);
                    }
                    if (text != "") await Clipboard.SetTextAsync(text);
                }));

            editor = Kaemika.XamarinToGui.TextEditor();
            editor.SetEditable(false);

            plot = new GraphLayoutView() {
                GraphLayout = null,
                BackgroundColor = Color.White,
            };

            var stepper = MainTabbedPage.theModelEntryPage.TextSizeStepper(editor);
            var startButton = MainTabbedPage.theModelEntryPage.StartButton(switchToChart: false, switchToOutput: false); // just needed to get its HightRequest
            outputPicker = OutputPicker();

            int bottomBarPadding = 4;
            Grid bottomBar = new Grid { RowSpacing = 0, Padding = bottomBarPadding };
            bottomBar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.BackgroundColor = MainTabbedPage.secondBarColor; 

            bottomBar.Children.Add(stepper, 0, 0);
            bottomBar.Children.Add(outputPicker, 1, 0);
            Grid.SetColumnSpan(outputPicker, 2);

            grid = new Grid { ColumnSpacing = 0 };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(startButton.HeightRequest + 2 * bottomBarPadding) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            backdrop = new Label { Text = "", BackgroundColor = Color.White };

            overlapping = new AbsoluteLayout();
            AbsoluteLayout.SetLayoutBounds(plot, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(plot, AbsoluteLayoutFlags.All);
            AbsoluteLayout.SetLayoutBounds(backdrop, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(backdrop, AbsoluteLayoutFlags.All);
            AbsoluteLayout.SetLayoutBounds(editor.AsView(), new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(editor.AsView(), AbsoluteLayoutFlags.All);
            overlapping.Children.Add(plot);
            overlapping.Children.Add(backdrop);
            overlapping.Children.Add(editor.AsView());

            grid.Children.Add(overlapping, 0, 0);
            grid.Children.Add(bottomBar, 0, 1);

            Content = grid;
        }

        public string GetText() {
            return editor.GetText();
        }

        public void SetText(string text) {
            editor.SetText(text);
        }

        public void AppendText(string text) {
            SetText(GetText() + text);
        }

        public void SetModel(ModelInfo modelInfo) {
            this.title = (modelInfo == null) ? "" : modelInfo.title;
            Title = this.title;
            currentModelInfo = modelInfo;
        }

        // called when there is a graphCache[graphFamily] item to process
        // called only through currentOutputAction or the output actions menu
        public void ProcessGraph(string graphFamily) {
            var execution = Exec.lastExecution; // atomically copy it
            if (execution == null) return; // something's wrong
            lock (Exec.exporterMutex) { (plot as GraphLayoutView).GraphLayout = GraphLayout.MessageGraph("...","... working ...","..."); }
            Dictionary<string, object> layoutCache = execution.layoutCache; // Dictionary<string, GraphLayout>
            GraphLayout layout;
            if (layoutCache.ContainsKey(graphFamily)) {
                layout = (GraphLayout)layoutCache[graphFamily];
            } else {
                var graph = execution.graphCache[graphFamily];
                if (graph.VertexCount == 0 || graph.EdgeCount == 0) // GraphSharp crashes in these cases
                    layout = GraphLayout.MessageGraph("...","...","... empty .");
                else try { layout = new GraphLayout("Graph Layout", graph); }
                     catch { layout = GraphLayout.MessageGraph("...","...","... failed !!!"); }
            }
            execution.layoutCache[graphFamily] = layout;
            lock (Exec.exporterMutex) { (plot as GraphLayoutView).GraphLayout = layout; } // This property assignment triggers redrawing the graph
        }

        public void OutputClear() { // external call, wait until we switch to this page
            if (MainTabbedPage.theMainTabbedPage.CurrentPage == MainTabbedPage.theOutputPageNavigation) {
                SetText("");
                lock (Exec.exporterMutex) { (plot as GraphLayoutView).GraphLayout = GraphLayout.MessageGraph("Preparing ...", "...", "..."); }
            }
        }
        public void ProcessOutput() { // external call, wait until we switch to this page
            if (MainTabbedPage.theMainTabbedPage.CurrentPage == MainTabbedPage.theOutputPageNavigation)
                currentOutputAction.action();
        }

        public override void OnSwitchedTo() {
            MainTabbedPage.OnAnySwitchedTo(this);
            if (currentModelInfo != MainTabbedPage.theModelEntryPage.modelInfo) {
                OutputClear();
                MainTabbedPage.theModelEntryPage.StartAction(forkWorker: true, switchToChart: false, switchToOutput: false, autoContinue: true);
            } else currentOutputAction.action();
        }

    }
}
