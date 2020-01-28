using System;
using Android.Content;
using Android.Widget;
using Android.Text;
using Android.Graphics;
//using Android.Views; // used, but name clashes
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using KaemikaXM.Pages;
using Kaemika;

////https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/custom-renderer/view

[assembly: ExportRenderer(typeof(KaemikaXM.Droid.CustomTextEditView), typeof(KaemikaXM.Droid.CustomTextEditRenderer))]
namespace KaemikaXM.Droid {

    //// get events on views (like editText) changing size:
    //Android.Views.ViewTreeObserver vto = editText.ViewTreeObserver;
    //vto.GlobalLayout += (sender, args) => {
    //    int viewHeight = editText.Height;
    //};

    public class CustomTextEditView : View, ICustomTextEdit {
        private Android.Widget.ScrollView scrollView; //
        private EditText editText; // set by CustomTextEditRenderer when it creates the EditText control
        private string text;       // cache text content between deallocation/reallocation
        private bool editable;     // cache editable state in case it is set while editText is null
        public const float defaultFontSize = 12; // Dip
        private float fontSize = defaultFontSize; // cache fontSize state as well

        public View AsView() {
            return this;
        }

        public void SetEditText(Android.Widget.ScrollView scrollView, EditText newEditText) {
            this.scrollView = scrollView;
            this.editText = newEditText;
            SetText(this.text);
            SetEditable(this.editable);
            SetFontSize(this.fontSize);
            this.editText.SetTextColor(Android.Graphics.Color.Rgb(98, 00, 237)); // "6200ED"
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
        public void InsertText(string insertion) {
            if (editText == null) return;
            GetSelection(out int start, out int end);
            text = editText.Text;
            text = text.Substring(0, start) + insertion + text.Substring(end, text.Length - end);
            editText.Text = text;
            SetSelection(start + insertion.Length, start + insertion.Length);
        }
        public void SetFocus() {
            if (editText == null) return;
            editText.RequestFocus();
        }
        public void ShowInputMethod() {
            if (editText == null) return;
            var imm = (Android.Views.InputMethods.InputMethodManager)(editText.Context.GetSystemService(Context.InputMethodService));
            editText.RequestFocus(); // needed
            imm.ShowSoftInput(editText, Android.Views.InputMethods.ShowFlags.Implicit);
            MainTabbedPage.theModelEntryPage.KeyboardIsUp(); // in the iOS version, this is called automatically by CustomPageRenderer
        }
        public void HideInputMethod() {
            if (editText == null) return;
            var imm = (Android.Views.InputMethods.InputMethodManager)(editText.Context.GetSystemService(Context.InputMethodService));
            editText.RequestFocus(); // needed
            imm.HideSoftInputFromWindow(editText.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.ImplicitOnly);
            MainTabbedPage.theModelEntryPage.KeyboardIsDown(); // in the iOS version, this is called automatically by by CustomPageRenderer
        }
        public void SelectAll() {
            if (editText == null) return;
            editText.SelectAll();
        }
        public void GetSelection(out int start, out int end) {
            if (editText == null) { start = 0; end = 0; return; }
            start = editText.SelectionStart;
            end = editText.SelectionEnd;
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
        public bool IsEditable() {
            return this.editable;
        }
        public EventHandler<AfterTextChangedEventArgs> textChangedDelegate = null;
        public void OnTextChanged(TextChangedDelegate del) {
            textChangedDelegate = (sender, e) => del(this);
        }
        public EventHandler<Android.Views.View.FocusChangeEventArgs> focusChangeDelegate = null;
        public void OnFocusChange(FocusChangeDelegate del) {
            focusChangeDelegate = (sender, e) => del(this);
        }


        //// GLOBAL LAYOUT LISTENER
        //// not useful for keyboard because it does not change the size of the edit window, but a good pattern for something else:
        //// https://stackoverflow.com/questions/4745988/how-do-i-detect-if-software-keyboard-is-visible-on-android-device

        //class GlobalLayoutListener : Java.Lang.Object, Android.Views.ViewTreeObserver.IOnGlobalLayoutListener {
        //    System.Action DoOnGlobalLayout;
        //    public GlobalLayoutListener(System.Action onGlobalLayout) {
        //        this.DoOnGlobalLayout = onGlobalLayout;
        //    }
        //    public void OnGlobalLayout() {
        //        DoOnGlobalLayout();
        //    }
        //}

        //public void SetupListener() {
        //    var contentView = editText;

        //    bool isKeyboardShowing = false;
        //    void onKeyboardVisibilityChanged(bool opened) {
        //        //print("keyboard " + opened);
        //    }

        //    contentView.ViewTreeObserver.AddOnGlobalLayoutListener(new GlobalLayoutListener( () => {

        //            Rect r = new Rect();
        //            contentView.GetWindowVisibleDisplayFrame(r);
        //            int screenHeight = contentView.RootView.Height;

        //            // r.bottom is the position above soft keypad or device button.
        //            // if keypad is shown, the r.bottom is smaller than that before.
        //            int keypadHeight = screenHeight - r.Bottom;

        //            // Log.d(TAG, "keypadHeight = " + keypadHeight);

        //            if (keypadHeight > screenHeight * 0.15) { // 0.15 ratio is perhaps enough to determine keypad height.
        //                // keyboard is opened
        //                if (!isKeyboardShowing) {
        //                    isKeyboardShowing = true;
        //                    onKeyboardVisibilityChanged(true);
        //                }
        //            } else {
        //                // keyboard is closed
        //                if (isKeyboardShowing) {
        //                    isKeyboardShowing = false;
        //                    onKeyboardVisibilityChanged(false);
        //                }
        //            }

        //    }));

        //}

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
            if (typeface == null) typeface = Typeface.CreateFromAsset(Context.Assets, new PlatformTexter().fixedFontFamily);
            this.SetTypeface(typeface, TypefaceStyle.Bold);
            this.SetBackgroundColor(Android.Graphics.Color.White); // by default it is transparent and shows through overlapping views
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

            if (Control == null && Element != null) {
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