using System;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using KaemikaXM.Pages;
using System.Threading.Tasks;
using Kaemika;

//https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/custom-renderer/view

[assembly: ExportRenderer(typeof(KaemikaXM.iOS.CustomTextEditView), typeof(KaemikaXM.iOS.CustomTextEditRenderer))]
namespace KaemikaXM.iOS
{
    public class CustomTextEditView : View, ICustomTextEdit {
        private DisEditText editText; // set by CustomTextEditRenderer when it creates the EditText control
        private string text;       // cache text content between deallocation/reallocation
        private bool editable;     // cache editable state in case it is set while editText is null
        public const float defaultFontSize = 12; // Dip
        private float fontSize = defaultFontSize; // cache fontSize state as well

        public View AsView() {
            return this;
        }

        // https://forums.xamarin.com/discussion/24218/is-there-a-way-to-determine-if-you-are-on-the-main-thread

        public class Ack {};
        public static Ack ack = new Ack();

        public static Task<T> BeginInvokeOnMainThreadAsync<T>(Func<T> a) {   
            var tcs = new TaskCompletionSource<T>();
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                try {
                    var result = a();
                    tcs.SetResult(result);
                } catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        public void SetEditText(DisEditText newEditText) {
            this.editText = newEditText;
            SetText(this.text);
            SetEditable(this.editable);
            SetFontSize(this.fontSize);
            // this.editText.SetTextColor(Android.Graphics.Color.Rgb(98, 00, 237)); // "6200ED"
        }
        public void ClearEditText() {
            if (NSThread.IsMain) {
                this.text = editText.Text; // save the last text before deallocation
                this.editText = null;
            } else { _ = BeginInvokeOnMainThreadAsync(() => { ClearEditText(); return ack; }).Result; }
        }
        public string GetText() {
            if (editText == null) return "";
            if (NSThread.IsMain) {
                return editText.Text;
            } else return BeginInvokeOnMainThreadAsync(() => { return GetText(); }).Result;
        }
        public void SetText(string text) {
            if (NSThread.IsMain) {
                this.text = text;
                if (editText == null) return;
                editText.Text = text;
                SetSelection(0, 0);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { SetText(text); return ack; }).Result; }
        }
        public void InsertText(string insertion) {
            if (editText == null) return;
            if (NSThread.IsMain) {
                (int start, int end) = GetSelection();
                text = editText.Text;
                text = text.Substring(0, start) + insertion + text.Substring(end, text.Length - end);
                editText.Text = text;
                SetSelection(start + insertion.Length, start + insertion.Length);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { InsertText(insertion); return ack; }).Result; }
        }
        public void SetFocus() {
            if (editText == null) return;
            if (NSThread.IsMain) {
                editText.BecomeFirstResponder();
            } else { _ = BeginInvokeOnMainThreadAsync(() => { SetFocus(); return ack; }).Result; }
        }
        public void ShowInputMethod() {
            if (editText == null) return;
            if (NSThread.IsMain) {
                editText.BecomeFirstResponder(); // this will trigger MainTabbedPage.theModelEntryPage.KeyboardIsUp() from CustomPageRenderer
            } else { _ = BeginInvokeOnMainThreadAsync(() => { ShowInputMethod(); return ack; }).Result; }
        }
        public void HideInputMethod() {
            if (editText == null) return;
            if (NSThread.IsMain) {
                editText.ResignFirstResponder(); // this will trigger MainTabbedPage.theModelEntryPage.KeyboardIsDown() from CustomPageRenderer
            } else { _ = BeginInvokeOnMainThreadAsync(() => { HideInputMethod(); return ack; }).Result; }
        }
        //https://riptutorial.com/ios/example/13625/getting-and-setting-the-cursor-postition
        //https://stackoverflow.com/questions/26307436/uitextview-selectall-method-not-working-as-expected
        public void SelectAll() {
            if (editText == null) return;
            if (NSThread.IsMain) {
                editText.SelectAll(editText);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { SelectAll(); return ack; }).Result; }
        }
        public (int, int) GetSelection() {
            if (editText == null) { return (0, 0); }
            if (NSThread.IsMain) {
                UITextRange selectedRange = editText.SelectedTextRange;
                if (selectedRange == null) return (0, 0);
                else {
                    int start = (int)editText.GetOffsetFromPosition(editText.BeginningOfDocument, selectedRange.Start);
                    int end = (int)editText.GetOffsetFromPosition(editText.BeginningOfDocument, selectedRange.End);
                    return (start, end);
                }
            } else return BeginInvokeOnMainThreadAsync(() => { return GetSelection(); }).Result;
        }
        public void SetSelection(int start, int end) {
            if (editText == null) return;
            if (NSThread.IsMain) {
                start = Math.Max(start, 0);
                end = Math.Min(end, editText.Text.Length - 1);
                if (end < start) end = start;
                var startPosition = editText.GetPosition(editText.BeginningOfDocument, start);
                var endPosition = editText.GetPosition(editText.BeginningOfDocument, end);
                if (startPosition != null && endPosition != null) {
                    editText.SelectedTextRange = editText.GetTextRange(startPosition, endPosition);
                    editText.ScrollRangeToVisible(editText.SelectedRange);
                }
            } else { _ = BeginInvokeOnMainThreadAsync(() => { SetSelection(start, end); return ack; }).Result; }
        }
        public void SetSelectionLineChar(int line, int chr, int tokenlength) {
            if (editText == null) return;
            if (NSThread.IsMain) {
                if (line < 0 || chr < 0) return;
                string text = GetText();
                int i = 0;
                while (i < text.Length && line > 0) {
                    if (text[i] == '\n') line--;
                    i++;
                }
                if (i < text.Length && text[i] == '\r') i++;
                int linestart = i;
                while (i < text.Length && chr > 0) {chr--; i++; }
                int tokenstart = i;
                SetSelection(tokenstart, tokenstart + tokenlength);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { SetSelectionLineChar(line, chr, tokenlength); return ack; }).Result; }
        }
        public float GetFontSize() {
            return this.fontSize;
        }
        public void SetFontSize(float size) {
            this.fontSize = size;
            if (editText == null || editText.Font == null) return;
            if (NSThread.IsMain) {
                editText.Font = editText.Font.WithSize(size);
            } else { _ = BeginInvokeOnMainThreadAsync(() => { SetFontSize(size); return ack; }).Result; }
        }
        public void SetEditable(bool editable) {
            this.editable = editable;
            if (editText == null) return;
            if (NSThread.IsMain) {
                editText.Editable = editable;
            } else { _ = BeginInvokeOnMainThreadAsync(() => { SetEditable(editable); return ack; }).Result; }
        }
        public bool IsEditable() {
            return this.editable;
        }

        public EventHandler textChangedDelegate = null;
        public void OnTextChanged(TextChangedDelegate del) {
            textChangedDelegate = (sender, e) => del(this);
        }
        public EventHandler focusChangeDelegate = null;
        public void OnFocusChange(FocusChangeDelegate del) {
            focusChangeDelegate = (sender, e) => del(this);
        }
    }

    // Subclass to handle the disposing of EditText e.g. when the app is suspended
    public class DisEditText : UITextView {
        private CustomTextEditView view; // store the view that we need to clean up on dispose
        // private static Typeface typeface = null;
        public DisEditText(CustomTextEditView view) : base() {
            this.view = view;
            this.Font = UIFont.FromName(new PlatformTexter().fixedFontFamily, CustomTextEditView.defaultFontSize);
        }
        protected override void Dispose(bool disposing)  {
            this.view.ClearEditText();
            base.Dispose(disposing);
        }
    }

    public class CustomTextEditRenderer : ViewRenderer<CustomTextEditView, UITextView>  {

        private DisEditText editText;

        public CustomTextEditRenderer() : base() { }

        protected override void OnElementChanged(ElementChangedEventArgs<CustomTextEditView> e) {
            base.OnElementChanged(e);

            //Element is the virtual control that's being rendered in the renderer, e.g. Button, Entry, Frame etc.   Element : CustomTextEditView
            //Control is the platform implementation of that control, e.g.UIButton/Button, etc.                      Control : UITextView

            if (Control == null && Element != null) {
                CustomTextEditView view = Element as CustomTextEditView;
                editText = new DisEditText(view);
                view.SetEditText(editText); // export editText so we can use it from CustomTextEditView methods
                SetNativeControl(editText);
            }
            if (e.OldElement != null) {
                // Unsubscribe events
                CustomTextEditView view = e.OldElement as CustomTextEditView;
                if (view.textChangedDelegate != null) editText.Changed -= view.textChangedDelegate;
                if (view.focusChangeDelegate != null) editText.Ended -= view.focusChangeDelegate;
            }
            if (e.NewElement != null) {
                // Subscribe events
                CustomTextEditView view = e.NewElement as CustomTextEditView;
                if (view.textChangedDelegate != null) editText.Changed += view.textChangedDelegate;
                if (view.focusChangeDelegate != null) editText.Ended += view.focusChangeDelegate;
            }
        }

    }

}