using Xamarin.Forms;
using Xamarin.Essentials;

namespace KaemikaXM.Pages {
    public class OutputPage : ContentPage {

        private string title = "";
        private View editor; // is a CustomTextEditView and implements ICustomTextEdit

        public OutputPage() {
            Icon = "icons8truefalse100.png";

            editor = Kaemika.GUI_Xamarin.customTextEditor();
            (editor as ICustomTextEdit).SetEditable(false);

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
            return (editor as ICustomTextEdit).GetText();
        }

        public void SetText(string text) {
            (editor as ICustomTextEdit).SetText(text);
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
