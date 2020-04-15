using System;
using Xamarin.Forms;

namespace XFormsTouch {

    public enum TouchActionType { Entered, Pressed, Moved, Released, Exited, Cancelled }

    public class TouchActionEventArgs : EventArgs {

        public TouchActionEventArgs(long id, TouchActionType type, Point location, bool isInContact) {
            Id = id;
            Type = type;
            Location = location;
            IsInContact = isInContact;
        }

        public long Id { private set; get; }

        public TouchActionType Type { private set; get; }

        public Point Location { private set; get; }

        public bool IsInContact { private set; get; }
    }

    public delegate void TouchActionEventHandler(object sender, TouchActionEventArgs args);

    public class TouchEffect : RoutingEffect {

        public event TouchActionEventHandler TouchAction;

        public TouchEffect() : base("XFormsTouch.TouchEffect") { }

        public bool Capture { set; get; } = true;

        public void OnTouchAction(Element element, TouchActionEventArgs args) {
            TouchAction?.Invoke(element, args);
        }
    }
}
