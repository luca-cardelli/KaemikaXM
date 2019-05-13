using Xamarin.Forms;
using Xamarin.Essentials;
using QuickGraph;
using GraphSharp;
using Kaemika;

namespace KaemikaXM.Pages {

    public class GraphLayoutPage : KaemikaPage {

        private string title = "";
        private View editor; // is a CustomTextEditView and implements ICustomTextEdit
        private View plot;   // is a GraphLayoutView

        public GraphLayoutPage() {
            Icon = "icons8activedirectoryfilled100.png";

            ToolbarItems.Add(
                new ToolbarItem("CopyAll", "icons8export96", async () => {
                    string text = MainTabbedPage.theGraphLayoutPage.GetText();
                    if (text != "") await Clipboard.SetTextAsync(text);
                }));

            editor = Kaemika.GUI_Xamarin.customTextEditor();
            (editor as ICustomTextEdit).SetEditable(false);

            plot = new GraphLayoutView() {
                GraphLayout = null,
                HeightRequest = 300, //####
                BackgroundColor = Color.White,
            };

            var stepper = MainTabbedPage.theModelEntryPage.TextSizeStepper(editor as ICustomTextEdit);
            var startButton = MainTabbedPage.theModelEntryPage.StartButton(); // just needed to the its HightRequest

            int bottomBarPadding = 4;
            Grid bottomBar = new Grid { RowSpacing = 0, Padding = bottomBarPadding };
            bottomBar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomBar.BackgroundColor = Color.FromHex(ModelEntryPage.secondBarColor);

            bottomBar.Children.Add(stepper, 0, 0);

            Grid grid = new Grid { ColumnSpacing = 0 };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(startButton.HeightRequest + 2 * bottomBarPadding) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(plot, 0, 0);
            grid.Children.Add(editor, 0, 1);
            grid.Children.Add(bottomBar, 0, 2);

            Content = grid;
        }

        public void SetTitle(string title) { 
            this.title = title;
            MainTabbedPage.theGraphLayoutPage.Title = this.title;
        }

        public string GetText() {
            return (editor as ICustomTextEdit).GetText(); ;
        }

        public void SetText(string text) {
            (editor as ICustomTextEdit).SetText(text);
        }

        public void SetGraph(AdjacencyGraph<Vertex, Kaemika.Edge<Vertex>> graph) {
            (plot as GraphLayoutView).GraphLayout = new GraphLayout("Graph Layout", graph); // This property assignment should trigger redrawing the graph
        }

        public override void OnSwitchedTo() {
            MainTabbedPage.OnAnySwitchedTo(this);
            SetTitle(this.title);
            Gui.gui.GraphUpdate();
        }

    }
}
