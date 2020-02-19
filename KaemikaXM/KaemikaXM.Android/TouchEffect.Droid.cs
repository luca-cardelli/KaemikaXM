using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using Android.Views;
using Xamarin.Forms.Internals;

[assembly: ResolutionGroupName("XFormsTouch")]
[assembly: ExportEffect(typeof(XFormsTouch.Droid.TouchEffectDroid), "TouchEffect")]

namespace XFormsTouch.Droid
{
    public class TouchEffectDroid : PlatformEffect
    {
        Android.Views.View view;
        Element formsElement;
        TouchEffect libTouchEffect;
        bool capture;
        Func<double, double> fromPixels;
        int[] twoIntArray = new int[2];

        static Dictionary<Android.Views.View, TouchEffectDroid> viewDictionary =
            new Dictionary<Android.Views.View, TouchEffectDroid>();

        static Dictionary<int, TouchEffectDroid> idToEffectDictionary =
            new Dictionary<int, TouchEffectDroid>();

        protected override void OnAttached()
        {
            view = Control == null ? Container : Control;

            var touchEffect = (TouchEffect)Element.Effects.FirstOrDefault(e => e is TouchEffect);

            if (touchEffect == null || view == null)
                return;

            // BUG FIXING: not clear this ContainsKey check is needed/useful, but just being paranoid about avoiding exceptions
            if (!viewDictionary.ContainsKey(view)) // hoping for the best
                viewDictionary.Add(view, this);

            formsElement = Element;
            libTouchEffect = touchEffect;

            fromPixels = view.Context.FromPixels;
            view.Touch += OnTouch;
            (Element as Layout<Xamarin.Forms.View>)?.Children.ForEach(v => v.InputTransparent = true);
        }

        protected override void OnDetached()
        {
            try
            {
                if (viewDictionary.ContainsKey(view))
                {
                    viewDictionary.Remove(view);
                    view.Touch -= OnTouch;
                }
            }
            catch (ObjectDisposedException)
            {
                //TODO This Bug is fixed with XForms 3.5 or higher.  
            }
        }

        void OnTouch(object sender, Android.Views.View.TouchEventArgs args)
        {
            // Two object common to all the events
            Android.Views.View senderView = sender as Android.Views.View;
            MotionEvent motionEvent = args.Event;

            // Get the pointer index
            int pointerIndex = motionEvent.ActionIndex;

            // Get the id that identifies a finger over the course of its progress
            int id = motionEvent.GetPointerId(pointerIndex);


            senderView.GetLocationOnScreen(twoIntArray);
            Point screenPointerCoords = new Point(twoIntArray[0] + motionEvent.GetX(pointerIndex),
                                                  twoIntArray[1] + motionEvent.GetY(pointerIndex));


            // Use ActionMasked here rather than Action to reduce the number of possibilities
            switch (args.Event.ActionMasked)
            {
                case MotionEventActions.Down:
                case MotionEventActions.PointerDown:
                    FireEvent(this, id, TouchActionType.Pressed, screenPointerCoords, true);

                    // BUG FIXING: added a check for ContainsKey because id=0 was occasionally added multiple times resulting in an untrappable IllegalArgument exception in external Thread
                    if (!idToEffectDictionary.ContainsKey(id)) // hoping for the best
                        idToEffectDictionary.Add(id, this);

                    capture = libTouchEffect.Capture;
                    break;

                case MotionEventActions.Move:
                    // Multiple Move events are bundled, so handle them in a loop
                    for (pointerIndex = 0; pointerIndex < motionEvent.PointerCount; pointerIndex++)
                    {
                        id = motionEvent.GetPointerId(pointerIndex);

                        if (capture)
                        {
                            senderView.GetLocationOnScreen(twoIntArray);

                            screenPointerCoords = new Point(twoIntArray[0] + motionEvent.GetX(pointerIndex),
                                                            twoIntArray[1] + motionEvent.GetY(pointerIndex));

                            FireEvent(this, id, TouchActionType.Moved, screenPointerCoords, true);
                        }
                        else
                        {
                            CheckForBoundaryHop(id, screenPointerCoords);

                            if (idToEffectDictionary[id] != null)
                            {
                                FireEvent(idToEffectDictionary[id], id, TouchActionType.Moved, screenPointerCoords, true);
                            }
                        }
                    }
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.Pointer1Up:
                    if (capture)
                    {
                        FireEvent(this, id, TouchActionType.Released, screenPointerCoords, false);
                    }
                    else
                    {
                        CheckForBoundaryHop(id, screenPointerCoords);

                        if (idToEffectDictionary[id] != null)
                        {
                            FireEvent(idToEffectDictionary[id], id, TouchActionType.Released, screenPointerCoords, false);
                        }
                    }
                    idToEffectDictionary.Remove(id);
                    break;

                case MotionEventActions.Cancel:
                    if (capture)
                    {
                        FireEvent(this, id, TouchActionType.Cancelled, screenPointerCoords, false);
                    }
                    else
                    {
                        if (idToEffectDictionary[id] != null)
                        {
                            FireEvent(idToEffectDictionary[id], id, TouchActionType.Cancelled, screenPointerCoords, false);
                        }
                    }
                    idToEffectDictionary.Remove(id);
                    break;
            }
        }

        void CheckForBoundaryHop(int id, Point pointerLocation)
        {
            TouchEffectDroid touchEffectHit = null;

            foreach (Android.Views.View view in viewDictionary.Keys)
            {
                // Get the view rectangle
                try
                {
                    view.GetLocationOnScreen(twoIntArray);
                }
                catch // System.ObjectDisposedException: Cannot access a disposed object.
                {
                    continue;
                }
                Rectangle viewRect = new Rectangle(twoIntArray[0], twoIntArray[1], view.Width, view.Height);

                if (viewRect.Contains(pointerLocation))
                {
                    touchEffectHit = viewDictionary[view];
                }
            }

            if (touchEffectHit != idToEffectDictionary[id])
            {
                if (idToEffectDictionary[id] != null)
                {
                    FireEvent(idToEffectDictionary[id], id, TouchActionType.Exited, pointerLocation, true);
                }
                if (touchEffectHit != null)
                {
                    FireEvent(touchEffectHit, id, TouchActionType.Entered, pointerLocation, true);
                }
                idToEffectDictionary[id] = touchEffectHit;
            }
        }

        void FireEvent(TouchEffectDroid touchEffect, int id, TouchActionType actionType, Point pointerLocation, bool isInContact)
        {
            // Get the method to call for firing events
            Action<Element, TouchActionEventArgs> onTouchAction = touchEffect.libTouchEffect.OnTouchAction;

            // BUG FIXING. This is left unchanged, but touch location must be multiplied by display density
            // see KTouch.OnTouchEffectAction

            // Get the location of the pointer within the view
            touchEffect.view.GetLocationOnScreen(twoIntArray);
            double x = pointerLocation.X - twoIntArray[0];
            double y = pointerLocation.Y - twoIntArray[1];
            Point point = new Point(fromPixels(x), fromPixels(y));

            // Call the method
            onTouchAction(touchEffect.formsElement,
                new TouchActionEventArgs(id, actionType, point, isInContact));
        }
    }
}
