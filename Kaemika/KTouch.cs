using System;
using SkiaSharp;
using XFormsTouch;     // From GitHub package XFormsTouch, adapted from Xamarin samples, contains TouchEffect
//using TouchTracking; // From Xamarin samples, contains TouchEffect, but unlike XFormsTouch it does not work for some reason
using Xamarin.Essentials; // For display density.

// TouchTracking original Xamarin sample for iOS, Android, UWP
// docs: https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/effects/touch-tracking
// code: https://github.com/xamarin/xamarin-forms-samples/tree/master/Effects/TouchTrackingEffect
// incorporating the source code files into my project DOES NOT WORK for some reason

// XFormsTouch, adapted as a library from TouchTracking, but compiled for .NET Frameworks 4.6, and missing UWP
// https://github.com/xamarin/xamarin-forms-samples/tree/master/Effects/TouchTrackingEffect
// Incorporating the GitHub package WORKS, but contains some bugs that need to be fixed in the source code
// Incorporating the source code files into my project WORKS on iOS and Android (unlike TouchTracking).

// So, I incorporated the source files from XFormsTouch (Shared, Android, iOS to the respective packages).

// Then I tried to add the source file "TouchEffects.cs" for UWP from the original TouchTracking.
// It uses "Windows.UI" imports that are not available under WPF, so it must be in the UWP package.
// However it is not clear how to hook up there Xamarin.Forms.Platform.UWP.PlatformEffects (ignored/unrecognized?)
// And in any case the touch interface in WPF and UWP is mapped to mouse actions by default,
// and TouceEffects does not seem to override it. So, no pinch/swipe is available in UWP/WPF,
// but maybe implementing mouse dragging and scrollwheel zooming will work there.

namespace Kaemika {

    // KTouchClient: Inteface to share the Touch gesture recognizer across multiple clients (ScoreView, ChartView, DeviceView, GraphLayoutView)
    // Each client to which the Touch effect is attached must implement KTouchClient by instantiating one KTouchClientData structure
    // The attachment of Touch Effect to a client's "this" is done by this code during its initialization:
    //      TouchEffect touchEffect = new TouchEffect();
    //      touchEffect.TouchAction += KTouchServer.OnTouchEffectAction;
    //      touchEffect.Capture = true;
    //      this.Effects.Add(touchEffect);
    // The common OnTouchEffectAction(sender, e) handler is defined in this file and it immediately does:
    //      KTouchClient kTouchClient = sender as KTouchClient;
    // therefore getting access to the client structures via kTouchClient:KTouchClient

    public interface KTouchClient {
        KTouchClientData data { get; }
    }

    // Interface to the platform-dependent GUIs (KScoreSKControl etc), managed by KScoreHandler
    public interface KTouchable : KControl {
        void OnTouchTapOrMouseMove(Action<SKPoint> action);                 // Hover/Hilight item
        void OnTouchDoubletapOrMouseClick(Action<SKPoint> action);          // Activate Item
        void OnTouchSwipeOrMouseDrag(Action<SKPoint, SKPoint> action);      // Drag Item
        void OnTouchSwipeOrMouseDragEnd(Action<SKPoint, SKPoint> action);   // Drag Item End 
        //void OnTouchTwofingerswipe(Action<SKPoint, SKPoint> action);      // Pan            - automatically handled by platforms
        //void OnTouchTwofingerswipeEnd(Action<SKPoint, SKPoint> action);   // Pan            - automatically handled by platforms
        //void OnTouchPinchOrMouseZoom(Action<float> action);               // Zoom           - automatically handled by platforms    (MouseZoom = scroll wheel)
        //void onTouchTwofingertapOrMouseDoublelick(Action<SKPoint> action);// Pan/Zoom Reset - automatically handled by platforms
        //void OnTouchHoldOrMouseAltclick(Action<SKPoint> action);          // Menu/Info (Altclick = Rightclick or Shiftclick) unimplemented
        // TapandpushOrClickandpress
        // TapandswipeOrClickanddrag
        void DoShow();
        void DoHide();
        void DoInvalidate();
    }

    // variables used by KTouchServer.OnTouchEffectAction, but allocated per-client
    public class KTouchClientData {
        public Action invalidateSurface { get; private set; } // action to  invalidate client surface
        public Action<Swipe> setManualPinchPan { get; private set; } // action to set the client-stored pinchPan transform used by its own drawing
        public Action<SKPoint> onTouchTapOrMouseMove { get; set; } // client registered (via KTouchable) callback
        public Action<SKPoint> onTouchDoubletapOrMouseClick { get; set; } // client registered (via KTouchable) callback
        public Action<SKPoint, SKPoint> onTouchSwipeOrMouseDrag { get; set; } // client registered (via KTouchable) callback
        public Action<SKPoint, SKPoint> onTouchSwipeOrMouseDragEnd { get; set; } // client registered (via KTouchable) callback
        public KTouchServer.Fingers fingers; // private data for KTouchServer
        public Swipe incrementalTranslation; // private data for KTouchServer
        public Swipe incrementalScaling; // private data for KTouchServer
        public Swipe lastPinchPan; // private data for KTouchServer
        public bool dragging; // private data for KTouchServer
        public bool swiping; // private data for KTouchServer
        public bool displayPinchOrigin; // private data for KTouchServer
        public SKPoint pinchOrigin; // private data for KTouchServer

        public KTouchClientData(Action invalidateSurface, Action<Swipe> setManualPinchPan) {
            this.invalidateSurface = invalidateSurface;
            this.setManualPinchPan = setManualPinchPan;

            this.onTouchTapOrMouseMove = null;
            this.onTouchDoubletapOrMouseClick = null;
            this.onTouchSwipeOrMouseDrag = null;
            this.onTouchSwipeOrMouseDragEnd = null;

            this.fingers = new KTouchServer.Fingers();
            this.incrementalTranslation = Swipe.Id();
            this.incrementalScaling = Swipe.Id();
            this.lastPinchPan = Swipe.Id();
            this.dragging = false;
            this.swiping = false;
            this.displayPinchOrigin = false;
            this.pinchOrigin = new SKPoint(0, 0);
        }

        public void DisplayTouchLocation(Painter painter) { // display a dot where the fingers are touching, via the client's painter
            if (this.displayPinchOrigin) painter.DrawCircle(this.pinchOrigin, 10, painter.FillPaint(new SKColor(127, 127, 127, 127)));
        }
    }

    public abstract class KTouchServer {

        public class Finger {
            private static Finger previousTap = null;
            private static Finger lastTap = null;
            public long fingerId;
            public SKPoint initialLocation; // location when this finger was first pressed
            public DateTime initialTime;    // time when this finger was first pressed
            public SKPoint currentLocation; // current location of this finger
            public bool inContact;          // whatever the latest event says about inContact (not used so far)

            public SKPoint Translation() {
                return new SKPoint(this.currentLocation.X - this.initialLocation.X, this.currentLocation.Y - this.initialLocation.Y);
            }
            public float Drag() {
                return Translation().Length;
            }
            public bool Tapped() {
                bool tapped = (Drag() < 10 * DeviceDisplay.MainDisplayInfo.Density) && TimeLib.Precedes(DateTime.Now, initialTime.AddSeconds(0.3));
                if (tapped && lastTap != this) { previousTap = lastTap; lastTap = this; }
                return tapped;
            }
            public bool DoubleTapped() {
                return Tapped() && (lastTap != null) && (previousTap != null) &&
                    SKPoint.Distance(lastTap.initialLocation, previousTap.initialLocation) < 50 * DeviceDisplay.MainDisplayInfo.Density &&
                    TimeLib.Precedes(lastTap.initialTime, previousTap.initialTime.AddSeconds(0.3));
            }
        }
        public class Fingers {
            public Finger fingerOne = null;
            public Finger fingerTwo = null;

            public bool OneContact() { return fingerOne != null && fingerTwo == null;  }
            public bool TwoContacts() { return fingerOne != null && fingerTwo != null; }

            public bool PressedOne(long id, SKPoint location, bool inContact) {
                Finger newFinger = new Finger {
                    fingerId = id,
                    initialLocation = location,
                    initialTime = DateTime.Now,
                    currentLocation = location,
                    inContact = inContact
                };
                if (fingerOne == null) { fingerOne = newFinger; return true; }
                else return false;
            }
            public bool PressedTwo(long id, SKPoint location, bool inContact) {
                Finger newFinger = new Finger {
                    fingerId = id,
                    initialLocation = location,
                    initialTime = DateTime.Now,
                    currentLocation = location,
                    inContact = inContact
                };
                if (fingerOne == null) { return false; }
                else if (fingerTwo == null) { fingerTwo = newFinger; return true; }
                else return false;
            }
            public bool Moved(long id, SKPoint location, bool inContact) {
                if (fingerOne != null && id == fingerOne.fingerId) {
                    fingerOne.currentLocation = location;
                    fingerOne.inContact = inContact;
                    return true;
                } else if (fingerTwo != null && id == fingerTwo.fingerId) {
                    fingerTwo.currentLocation = location;
                    fingerTwo.inContact = inContact;
                    return true;
                } return false;
            }
            public bool ReleasingOne(long id, SKPoint location, bool inContact) {
                if (fingerOne != null && id == fingerOne.fingerId) {
                    fingerOne.currentLocation = location;
                    fingerOne.inContact = inContact;
                    return true;
                } return false;
            }
            public bool ReleasingTwo(long id, SKPoint location, bool inContact) {
                if (fingerTwo != null && id == fingerTwo.fingerId) {
                    fingerTwo.currentLocation = location;
                    fingerTwo.inContact = inContact;
                    return true;
                } return false;
            }
            public bool Released(long id) { // return true if all fingers are released
                if (fingerOne != null) {
                    if (id == fingerOne.fingerId) {
                        fingerOne = null;
                        fingerTwo = null;
                        return true;
                    } else return false;
                } else if (fingerTwo != null) {
                    if (id == fingerTwo.fingerId) {
                        fingerTwo = null;
                        return false;
                    } else return false;
                } else return true;
            }
            public bool Tapped(long id) {
                if (fingerOne != null && id == fingerOne.fingerId) { 
                    return fingerOne.Tapped();
                } else return false;
            }
            public bool DoubleTapped(long id) {
                if (fingerOne != null && id == fingerOne.fingerId) { 
                    return fingerOne.DoubleTapped();
                } else return false;
            }
            public bool TwoFingerTapped(long id) {
                if (fingerOne != null && fingerTwo != null && id == fingerOne.fingerId) { 
                    return fingerOne.Tapped();
                } else return false;
            }

            public SKPoint Center() {
                if (fingerOne == null) return new SKPoint(0,0);
                if (fingerTwo == null) return fingerOne.currentLocation;
                return new SKPoint(
                    (fingerOne.currentLocation.X + fingerTwo.currentLocation.X) / 2.0f,
                    (fingerOne.currentLocation.Y + fingerTwo.currentLocation.Y) / 2.0f);
            }

            public SKPoint Translation() {
                if (fingerOne != null) return fingerOne.Translation();
                else return new SKPoint(0,0);
            }

            public float Scaling() {
                if (fingerOne == null || fingerTwo == null) return 1.0f;
                float initDist = Math.Max(1, SKPoint.Distance(fingerOne.initialLocation, fingerTwo.initialLocation));
                float dist = Math.Max(1, SKPoint.Distance(fingerOne.currentLocation, fingerTwo.currentLocation));
                float scaling = Math.Max(dist / initDist, 0.1f);
                return scaling;
            }
        }

        public static void OnTouchEffectAction(object sender, TouchActionEventArgs e) {
            KTouchClient kTouchClient = sender as KTouchClient;
            KTouchClientData data = kTouchClient.data;
            // Touch location is given in Xamarin independent display units, and must be multiplied by display density to work at all platforms and resolutions
            // e.Location comes from Android TouchEffect.Droid.FireEvent and iOS TouchRecognizer.FireEvent
            double density = DeviceDisplay.MainDisplayInfo.Density;
            SKPoint location = new SKPoint((float)(e.Location.X * density), (float)(e.Location.Y * density));
            if (e.Type == TouchActionType.Pressed) {
                if (data.fingers.PressedOne(e.Id, location, e.IsInContact)) {
                    data.displayPinchOrigin = true;
                } else if (data.fingers.PressedTwo(e.Id, location, e.IsInContact)) {
                    data.incrementalTranslation = Swipe.Id();
                    data.incrementalScaling = Swipe.Id();
                    data.displayPinchOrigin = true;
                    data.swiping = true;
                }
            } else if (e.Type == TouchActionType.Moved) {
                if (data.fingers.Moved(e.Id, location, e.IsInContact)) {
                    if (data.fingers.OneContact()) {
                        data.pinchOrigin = location; // display the location of swiping
                        data.onTouchSwipeOrMouseDrag?.Invoke(data.fingers.fingerOne.initialLocation, location);
                        data.dragging = true;
                    } else if (data.fingers.TwoContacts()) {
                        SKPoint translation = data.fingers.Translation(); 
                        SKPoint scalingOrigin = data.fingers.Center(); 
                        float scaling = data.fingers.Scaling();
                        data.incrementalTranslation = new Swipe(1, translation);
                        data.incrementalScaling = new Swipe(scaling, new SKPoint((1 - scaling) * scalingOrigin.X, (1 - scaling) * scalingOrigin.Y)); // scaling around the scaleOrigin
                        data.pinchOrigin = scalingOrigin; // display the center of scaling
                        data.setManualPinchPan?.Invoke(data.lastPinchPan * data.incrementalScaling * data.incrementalTranslation);
                        data.invalidateSurface();
                    }
                }
            } else if (e.Type == TouchActionType.Released) {
                if (data.fingers.TwoFingerTapped(e.Id)) {
                    data.lastPinchPan = Swipe.Id();
                    data.setManualPinchPan?.Invoke(data.lastPinchPan);
                    data.invalidateSurface();
                }
                if (data.fingers.ReleasingTwo(e.Id, location, e.IsInContact)) {
                } else if (data.fingers.ReleasingOne(e.Id, location, e.IsInContact)) {
                    if (data.swiping) {
                        data.swiping = false;
                        data.lastPinchPan = data.lastPinchPan * data.incrementalScaling * data.incrementalTranslation;
                        data.lastPinchPan = new Swipe(Math.Max(0.1f, data.lastPinchPan.scale), data.lastPinchPan.translate);
                        data.setManualPinchPan?.Invoke(data.lastPinchPan);
                        data.invalidateSurface();
                    } else {
                        if (data.fingers.DoubleTapped(e.Id)) {
                            data.onTouchDoubletapOrMouseClick?.Invoke(location);
                        } else if (data.fingers.Tapped(e.Id)) {
                            data.onTouchTapOrMouseMove?.Invoke(location);
                        }
                        if (data.dragging) {
                            data.dragging = false;
                            data.onTouchSwipeOrMouseDragEnd?.Invoke(data.fingers.fingerOne.initialLocation, location); 
                        }
                    }
                }
                if (data.fingers.Released(e.Id)) data.displayPinchOrigin = false;
            } else if (e.Type == TouchActionType.Entered) { 
            } else if (e.Type == TouchActionType.Exited) { 
            } else if (e.Type == TouchActionType.Cancelled) {
            }
        }
    }
}
