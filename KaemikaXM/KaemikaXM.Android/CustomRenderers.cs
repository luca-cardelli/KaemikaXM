using Android.Content;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

//[assembly: ExportRenderer(typeof(Entry), typeof(KaemikaXM.Droid.MyEntryRenderer))]

//===================================
// This was used for the Editor in v1.0
[assembly: ExportRenderer(typeof(Editor), typeof(KaemikaXM.Droid.MyEditorRenderer))]
//=====================================

//[assembly: ExportEffect(typeof(KaemikaXM.Droid.EntrySelectEffect), nameof(KaemikaXM.Droid.EntrySelectEffect))]

namespace KaemikaXM.Droid {

    //// this works with the above assembly declarations
    //public class MyEntryRenderer : EntryRenderer {
    //    public MyEntryRenderer(Context context) : base(context) {
    //        AutoPackage = false;
    //    }
    //    protected override void OnElementChanged(ElementChangedEventArgs<Entry> e) {
    //        base.OnElementChanged(e);
    //        if (e.OldElement == null) {
    //            var nativeEditText = (global::Android.Widget.EditText)Control;
    //            nativeEditText.SetSelectAllOnFocus(true); // select all text on focus
    //        }
    //    }
    //}

    //// this works with the above assembly declarations
    //public class MyEditorRenderer : EditorRenderer {
    //    public MyEditorRenderer(Context context) : base(context) {
    //        AutoPackage = false;
    //    }
    //    protected override void OnElementChanged(ElementChangedEventArgs<Editor> e) {
    //        base.OnElementChanged(e);
    //        if (e.OldElement == null) {
    //            var nativeEditText = (global::Android.Widget.EditText)Control;
    //            nativeEditText.SetSelectAllOnFocus(true); // select all text on focus
    //        }
    //    }
    //}

        //===================================
        // This was used for the Editor in v1.0
        //=====================================
    public class MyEditorRenderer : EditorRenderer {
        public MyEditorRenderer(Context context) : base(context) {
            AutoPackage = false;
        }
        // protect all event handlers by try-catch because all these UI resource can get deallocated when switching out of the app and back again

        private static int LineFromPos(string s, int pos) {
            int line = 1;
            for (int i = 0; i < pos; i++) {
                if (i == s.Length) return -1;
                if (s[i] == '\n') line++;
            }
            return line;
        }

        private static int PosFromLine(string s, int line) {
            int pos = 0;
            while (line > 1) {
                if (pos == s.Length) return -1;
                if (s[pos] == '\n') line--;
                pos++;
            }
            return pos;
        }

        private static EditText storedNativeEditText = null;
        public static void SetSelection(int lineNumber, int columnNumber) {
            string text = storedNativeEditText.Text;
            int start = PosFromLine(text, lineNumber);
            if (start < 0) start = text.Length-1; // lineNumber is too big
            int stop = start + columnNumber;
            if (stop >= text.Length) stop = text.Length-1; // columnNumber is too big
            try { storedNativeEditText.SetSelection(start, stop); } catch { } 
        }
        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.Editor> e) {
            base.OnElementChanged(e);
            if (e.OldElement == null) { // a new Editor "Element" has been assigned to the nativeExitText "Control"
                var nativeEditText = Control as EditText;
                var editor = e.NewElement as Editor; // or?: var editor = Element as Editor

                System.EventHandler<TouchEventArgs> handler = null;
                handler = (object sender, TouchEventArgs args) => {
                    try { 
                        storedNativeEditText = nativeEditText; // keep refreshing it with the most recent one
                        nativeEditText.SetSelection(0, 0);  // will scroll text to the top
                        nativeEditText.Touch -= handler;    // remove handler to reenable regular touch handling
                    } catch { }
                };

                editor.TextChanged += (object sender, TextChangedEventArgs args) => {
                    try {
                        if (editor.Text.Length > 0 && editor.Text[0] == '\a') { //understand that this is a brand new non-empty page page
                            editor.Text = editor.Text.Substring(1, editor.Text.Length - 1);
                            nativeEditText.Touch += handler; // to handle the first touch
                        }
                    }
                    catch { }
                };

            }
        }
    }

    // some code to set up platform effects instead of renderers, but still only react to events and cannot be called?

    //public class EntrySelectEffect : PlatformEffect {

    //    public void SelectText(int in_Index, int in_Length) {
    //        EditText textBox = Control as EditText;
    //        Entry entry = Element as Entry;
    //        if (textBox == null || entry == null)
    //            return;
    //        textBox.SetSelection(in_Index, in_Length);
    //    }

    //    public void SelectText(string in_SelectText) {
    //        EditText textBox = Control as EditText;
    //        Entry entry = Element as Entry;
    //        if (textBox == null || entry == null)
    //            return;
    //        int index = entry.Text.IndexOf(in_SelectText);
    //        int length = in_SelectText.Length;
    //        textBox.SetSelection(index, length);
    //    }

    //    protected override void OnAttached() {
    //    }

    //    protected override void OnDetached() {
    //    }

    //    protected override void OnElementPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args) {
    //        base.OnElementPropertyChanged(args);
    //        // e.g. on changes of IsFocused property
    //        SelectText(0, 20);
    //    }

    //}

    //public class testEffects {
    //    public void f() {
    //        var editor = new Editor();
    //        editor.Effects.Add(new EntrySelectEffect());
    //    }        
    //}

}


// https://forums.xamarin.com/discussion/135174/how-to-keep-copy-paste-and-scroll-for-editor-inside-scrollview-xamarin-forms-for-android

// https://forums.xamarin.com/discussion/66779/what-is-all-this-new-old-and-element-stuff-inside-a-custom-renderer
//The idea is that the renderer has some properties that you can use and/or control.

//Element is the virtual control that's being rendered in the renderer, e.g. Button, Entry, Frame etc.

//Control is the platform implementation of that control, e.g.UIButton/Button, etc.

//Control is supposed to be created only once. Usually, the base class will take care of this for you, but if you are rendering a completely custom control, you may have to do it yourself. If the Control allocates memory at all you will need to release it yourself in the Dispose override.

//After that, you are supposed to update its properties to match those of the virtual control.If you look at the source for the Xamarin.Forms renderers, you can often see a suite of methods called Updatexxx where xxx is the property being updated. Most renderers call these update methods from the OnElementPropertyChanged event handler, but you can also use them inside OnElementChanged to initialise the Control to your satisfaction when the first binding occurs.

//Element changes depending on the binding of the virtual control.If you're familiar with INotifyPropertyChanged and its cousin INotifyPropertyChanging the OldElement and NewElement will start to make sense. OldElement is the Element that used to be bound to the renderer; NewElement is the Element that is now bound. You can use these two properties to make sure that you unhook event handlers from the old Element before hooking them up with the new Element as the binding changes.

//There are certain scenarios in which OldElement and NewElement are null.

//OldElement is null when the first binding occurs (fairly obviously).

//NewElement is null when the renderer is being deallocated (also fairly obviously).

//It's up to you to do the right thing in each scenario.

//Hope that clarifies a bit.

//This is explained some more in https://developer.xamarin.com/guides/xamarin-forms/custom-renderer/view/.