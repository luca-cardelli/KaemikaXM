using Xamarin.Forms;
using Xamarin.Essentials;

namespace KaemikaXM.Pages {
    public class OutputPage : ContentPage {

        private string title = "";
        private string text;
        private Editor editor;

        public OutputPage() {
            text = "Text output from execution goes here";
            Icon = "icons8truefalse100.png";

            editor = new Editor() {
                Text = text,
                AutoSize = EditorAutoSizeOption.TextChanges,
                FontSize = 10,
                IsSpellCheckEnabled = false,
                IsTextPredictionEnabled = false,
                VerticalOptions = LayoutOptions.FillAndExpand, // needed to make editor scrollable
                Margin = 8,
                IsReadOnly = true, // not yet supported?
            };

            var layout = new AbsoluteLayout();
            AbsoluteLayout.SetLayoutBounds(editor, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(editor, AbsoluteLayoutFlags.All);

            layout.Children.Add(editor);
            Content = layout;

            ToolbarItems.Add(
                new ToolbarItem("CopyAll", "icons8export96", async () => {
                    string text = MainTabbedPage.theOutputPage.GetText();
                    if (text != "") await Clipboard.SetTextAsync(text);
                }));
        }

        public string GetText() {
            return editor.Text;
        }

        public void SetText(string text) {
            editor.Text = text;
        }

        public void SetTitle(string title) { 
            this.title = title;
            MainTabbedPage.theOutputPage.Title = this.title;
        }

        protected override void OnAppearing() {
            base.OnAppearing();
            SetTitle(this.title);
        }
    }
}
