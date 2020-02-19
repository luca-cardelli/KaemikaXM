using System;
using System.Linq;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using UIKit;

[assembly: ResolutionGroupName("XFormsTouch")]
[assembly: ExportEffect(typeof(XFormsTouch.iOS.TouchEffectIOS), "TouchEffect")]

namespace XFormsTouch.iOS
{
    public class TouchEffectIOS : PlatformEffect
    {
        UIView view;
        TouchRecognizer touchRecognizer;

        protected override void OnAttached()
        {
            // Get the iOS UIView corresponding to the Element that the effect is attached to
            view = Control == null ? Container : Control;

            // Get access to the TouchEffect class in the .NET Standard library
            TouchEffect effect = (TouchEffect)Element.Effects.FirstOrDefault(e => e is TouchEffect);

            if (effect != null && view != null)
            {
                // Create a TouchRecognizer for this UIView
                touchRecognizer = new TouchRecognizer(Element, view, effect);
                view.UserInteractionEnabled = true;
                view.AddGestureRecognizer(touchRecognizer);
            }
        }

        protected override void OnDetached()
        {
            if (touchRecognizer != null)
            {
                // Clean up the TouchRecognizer object
                touchRecognizer.Detach();

                // Remove the TouchRecognizer from the UIView
                view.RemoveGestureRecognizer(touchRecognizer);
            }
        }
    }
}