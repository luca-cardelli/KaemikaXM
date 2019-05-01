using System;
using Android.Content;
using Android.Widget;
using Android.Text;
using Android.Graphics;
//using Android.Views; // used, but name clashes
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using KaemikaXM.Pages;

////https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/custom-renderer/view

[assembly: ExportRenderer(typeof(KaemikaXM.Droid.CustomTextEditView), typeof(KaemikaXM.Droid.CustomTextEditRenderer))]
namespace KaemikaXM.Droid {

    public class CustomTextEditView : View, ICustomTextEdit {
        private Android.Widget.ScrollView scrollView; //
        private EditText editText; // set by CustomTextEditRenderer when it creates the EditText control
        private string text;       // cache text content between deallocation/reallocation
        private bool editable;     // cache editable state in case it is set while editText is null
        public const float defaultFontSize = 12; // Dip
        private float fontSize = defaultFontSize; // cache fontSize state as well

        public void SetEditText(Android.Widget.ScrollView scrollView, EditText newEditText) {
            this.scrollView = scrollView;
            this.editText = newEditText;
            SetText(this.text);
            SetEditable(this.editable);
            SetFontSize(this.fontSize);
        }
        public void ClearEditText() {
            this.text = editText.Text; // save the last text before deallocation
            this.editText = null;
            this.scrollView = null;
        }
        public string GetText() {
            if (editText == null) return "";
            return editText.Text;
        }
        public void SetText(string text) {
            this.text = text;
            if (scrollView == null || editText == null) return;
            editText.Text = text;
            scrollView.ScrollTo(0, 0);
        }
        public void SetFocus() {
            if (editText == null) return;
            editText.RequestFocus();
        }
        public void SelectAll() {
            if (editText == null) return;
            editText.SelectAll();
        }
        public void SetSelection(int start, int end) {
            if (editText == null) return;
            start = Math.Max(start, 0);
            end = Math.Min(end, editText.Text.Length - 1);
            if (end < start) end = start;
            editText.SetSelection(start, end);
        }
        public void SetSelectionLineChar(int line, int chr, int tokenlength) {
            if (editText == null) return;
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
            //SetSelection(linestart, tokenstart);
            SetSelection(tokenstart, tokenstart + tokenlength);
            //SetSelection(tokenstart, text.Length - 1);
        }
        public float GetFontSize() {
            return this.fontSize;
        }
        public void SetFontSize(float size) {
            this.fontSize = size;
            if (editText == null) return;
            editText.SetTextSize(Android.Util.ComplexUnitType.Dip, size);
        }
        public void SetEditable(bool editable) {
            this.editable = editable;
            if (editText == null) return;
            if (editable) {
                editText.LongClickable = true;
                editText.Focusable = true;
                editText.FocusableInTouchMode = true; // need to reenable this as well as Focusable
            } else {
                editText.Focusable = false; // disable answering to clicks
                editText.LongClickable = false; // disable longclicks (edit popup menu) as well
            }
        }
        public EventHandler<AfterTextChangedEventArgs> textChangedDelegate = null;
        public void OnTextChanged(TextChangedDelegate del) {
            textChangedDelegate = (sender, e) => del(this);
        }
        public EventHandler<Android.Views.View.FocusChangeEventArgs> focusChangeDelegate = null;
        public void OnFocusChange(FocusChangeDelegate del) {
            focusChangeDelegate = (sender, e) => del(this);
        }
    }

    // Subclass to handle the disposing of EditText e.g. when the app is suspended
    public class DisEditText : EditText {
        private CustomTextEditView view; // store the view that we need to clean up on dispose
        private static Typeface typeface = null;
        public DisEditText(Context context, CustomTextEditView view) : base(context) {
            this.view = view;
            // properly set up the EditText:
            this.Gravity = Android.Views.GravityFlags.Top; //first line at top of area, not center
            this.SetPadding(20, 0, 0, 50); // bottom margin needed to stay above the red line
            this.SetTextSize(Android.Util.ComplexUnitType.Dip, CustomTextEditView.defaultFontSize);
            if (typeface == null) typeface = Typeface.CreateFromAsset(Context.Assets, "DroidSansMono.ttf"); // "CutiveMono-Regular.ttf"
            this.SetTypeface(typeface, TypefaceStyle.Bold);
        }
        protected override void Dispose(bool disposing)  {
            this.view.ClearEditText();
            base.Dispose(disposing);
        }
    }

    public class CustomTextEditRenderer : ViewRenderer<CustomTextEditView, Android.Widget.ScrollView>  {

        private Android.Widget.EditText editText;
        private Android.Widget.ScrollView scrollView;

        public CustomTextEditRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<CustomTextEditView> e) {
            base.OnElementChanged(e);

            //Element is the virtual control that's being rendered in the renderer, e.g. Button, Entry, Frame etc.   Element : CustomTextEditView
            //Control is the platform implementation of that control, e.g.UIButton/Button, etc.                      Control : EditText

            if (Control == null) {
                CustomTextEditView view = Element as CustomTextEditView;
                editText = new DisEditText(Context, view);
                scrollView = new Android.Widget.ScrollView(Context);
                scrollView.AddView(editText);
                view.SetEditText(scrollView, editText); // export editText so we can use it from CustomTextEditView methods
                SetNativeControl(scrollView);
            }
            if (e.OldElement != null) {
                // Unsubscribe events
                CustomTextEditView view = e.OldElement as CustomTextEditView;
                if (view.textChangedDelegate != null) editText.AfterTextChanged -= view.textChangedDelegate;
                if (view.focusChangeDelegate != null) editText.FocusChange -= view.focusChangeDelegate;
            }
            if (e.NewElement != null) {
                // Subscribe events
                CustomTextEditView view = e.NewElement as CustomTextEditView;
                if (view.textChangedDelegate != null) editText.AfterTextChanged += view.textChangedDelegate;
                if (view.focusChangeDelegate != null) editText.FocusChange += view.focusChangeDelegate;
            }
        }

    }
}