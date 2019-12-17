using Foundation;
using UIKit;
using KaemikaXM.Pages;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

//Some black magic that resize the window when the keyboard shows/hides
//https://forums.xamarin.com/discussion/30336/popup-keyboard-hides-parts-of-the-ui-rather-than-resizing
[assembly: ExportRenderer(typeof(ModelEntryPage), typeof(KaemikaXM.iOS.IosPageRenderer))]

namespace KaemikaXM.iOS
{
    public class IosPageRenderer : PageRenderer
    {
        NSObject observerHideKeyboard;
        NSObject observerShowKeyboard;

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated); 

            observerHideKeyboard = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, OnHideNotification);
            observerShowKeyboard = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, OnShowNotification);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated); 

            NSNotificationCenter.DefaultCenter.RemoveObserver(observerHideKeyboard);
            NSNotificationCenter.DefaultCenter.RemoveObserver(observerShowKeyboard);
        }

        void OnShowNotification(NSNotification notification) {
            if (!IsViewLoaded) return;
            MainTabbedPage.theModelEntryPage.KeyboardIsUp();
            var frameBegin = UIKeyboard.FrameBeginFromNotification(notification);
            var frameEnd = UIKeyboard.FrameEndFromNotification(notification);
            var bounds = Element.Bounds;
            var cover = 2 * MainTabbedPage.buttonHeightRequest + 20; // Height of the two bottom bars plus some random spacing
            var newBounds = new Rectangle(bounds.Left, bounds.Top, bounds.Width,
                bounds.Height - frameBegin.Top + frameEnd.Top
                + cover );
            Element.Layout(newBounds);
        }

        void OnHideNotification(NSNotification notification) {
            if (!IsViewLoaded) return;
            MainTabbedPage.theModelEntryPage.KeyboardIsDown();
            var frameBegin = UIKeyboard.FrameBeginFromNotification(notification);
            var frameEnd = UIKeyboard.FrameEndFromNotification(notification);
            var bounds = Element.Bounds;
            var cover = 2 * MainTabbedPage.buttonHeightRequest + 20; // Height of the two bottom bars plus some random spacing
            var newBounds = new Rectangle(bounds.Left, bounds.Top, bounds.Width,
                bounds.Height - frameBegin.Top + frameEnd.Top
                - cover );
            Element.Layout(newBounds);
        }
    }
}