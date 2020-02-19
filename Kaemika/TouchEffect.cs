using Xamarin.Forms;

namespace XFormsTouch
{
    public class TouchEffect : RoutingEffect
    {
        public event TouchActionEventHandler TouchAction;

        public TouchEffect() : base("XFormsTouch.TouchEffect")
        {
        }

        public bool Capture { set; get; } = true;

        public void OnTouchAction(Element element, TouchActionEventArgs args)
        {
            TouchAction?.Invoke(element, args);
        }
    }
}
