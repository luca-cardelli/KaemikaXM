using System;
using Android.Content;
using Android.Widget;
using Android.Text;
//using Android.Views; // used, but name clashes
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using KaemikaXM.Pages;

////https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/custom-renderer/view

[assembly: ExportRenderer(typeof(KaemikaXM.Droid.CustomTextEditView), typeof(KaemikaXM.Droid.CustomTextEditRenderer))]
namespace KaemikaXM.Droid {

    public class CustomTextEditView : View, ICustomTextEdit {
        private EditText editText; // set by CustomTextEditRenderer when it creates the EditText control
        private string text;       // cache text content between deallocation/reallocation
        private bool editable;     // cache editable state in case it is set while editText is null
        public const float defaultFontSize = 14; // Dip
        private float fontSize = defaultFontSize; // cache fontSize state as well

        public void SetEditText(EditText newEditText) {
            this.editText = newEditText;
            SetText(this.text);
            SetEditable(this.editable);
            SetFontSize(this.fontSize);
        }
        public void ClearEditText() {
            this.text = editText.Text; // save the last text before deallocation
            this.editText = null;
        }
        public string GetText() {
            if (editText == null) return "";
            return editText.Text;
        }
        public void SetText(string text) {
            this.text = text;
            if (editText == null) return;
            editText.Text = text;
        }
        public void SelectAll() {
            if (editText == null) return;
            editText.SelectAll();
        }
        public void SetSelection(int start, int end) {
            if (editText == null) return;
            editText.SetSelection(start, end);
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
                editText.FocusableInTouchMode = true;
                editText.Focusable = true;
            } else {
                editText.Focusable = false;
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
        public DisEditText(Context context, CustomTextEditView view) : base(context) {
            this.view = view;
            // properly set up the EditText:
            this.Gravity = Android.Views.GravityFlags.Top; //first line at top of area, not center
            this.SetPadding(20, 0, 0, 50); // bottom margin needed to stay above the red line
            this.SetTextSize(Android.Util.ComplexUnitType.Dip, CustomTextEditView.defaultFontSize);
        }
        protected override void Dispose(bool disposing)  {
            this.view.ClearEditText();
            base.Dispose(disposing);
        }
    }

    public class CustomTextEditRenderer : ViewRenderer<CustomTextEditView, EditText>  {

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
                view.SetEditText(editText); // export editText so we can use it from CustomTextEditView methods
                SetNativeControl(editText);

                //###
                //scrollView = new Android.Widget.ScrollView(Context);
                ////nestedScrollView = new Android.Support.V4.Widget.NestedScrollView(Context);
                //scrollView.AddView(editText);
                //SetNativeControl(scrollView);
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