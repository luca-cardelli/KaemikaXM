using System;
using AppKit;
using CoreGraphics;
using Foundation;
using Kaemika;
using SkiaSharp;

namespace KaemikaMAC {

    [Register("NSChartView")]
    public class NSChartView : NSControl { //### NSChartView --> KChartNSControl // ### Add KTouchable interface for gesture handling for charts, see NSScoreView (Mac) and SKScoreView (PC)

        #region Constructors
        public NSChartView() { OnLoad(); }
        public NSChartView(IntPtr handle) : base(handle) { OnLoad(); }
        [Export("initWithFrame:")]
        public NSChartView(CGRect frameRect) : base(frameRect) { OnLoad(); }
        #endregion

        #region OnLoad setup

        private static NSChartView chartControl = null;  // The only NSChartView, same as "this", but accessible from static methods
                                                         // CURRENTLY UNUSED
        private KTouchClientData touch = null;           // data for pan&zoom by mouse drag and scroll

        private void OnLoad() {
            this.WantsLayer = true;
            this.LayerContentsRedrawPolicy = NSViewLayerContentsRedrawPolicy.OnSetNeedsDisplay;
            chartControl = this;

            /* Initialize Interface KTouchClient with a locally-sourced KTouchClientData closure */
            touch = new KTouchClientData(
                invalidateSurface: () => { Invalidate(); },
                setManualPinchPan: (Swipe pinchPan) => { KChartHandler.SetManualPinchPan(pinchPan); }
            );
            touch.onTouchSwipeOrMouseDrag = OnMouseDrag;
            touch.onTouchSwipeOrMouseDragEnd = OnMouseDragEnd;
            touch.onTouchDoubletapOrMouseClick = OnMouseClick;
            touch.onTouchPinchOrMouseZoom = OnMouseZoom;
            touch.lastPinchPan = Swipe.Id();
            touch.incrementalScaling = Swipe.Id();
            touch.incrementalTranslation = Swipe.Id();
            KChartHandler.RegisterKTouchClientData(touch);
            Tracking();
        }

        private void OnMouseDrag(SKPoint from, SKPoint to) {
            touch.swiping = true;
            touch.incrementalTranslation = new Swipe(1, new SKPoint(to.X - from.X, to.Y - from.Y));
            touch.setManualPinchPan?.Invoke(touch.lastPinchPan * touch.incrementalScaling * touch.incrementalTranslation);
            touch.invalidateSurface();
        }

        private void OnMouseDragEnd(SKPoint from, SKPoint to) {
            if (touch.swiping) {
                touch.swiping = false;
                touch.lastPinchPan = touch.lastPinchPan * touch.incrementalScaling * touch.incrementalTranslation;
                touch.incrementalScaling = Swipe.Id();
                touch.incrementalTranslation = Swipe.Id();
                touch.setManualPinchPan?.Invoke(touch.lastPinchPan);
                touch.invalidateSurface();
            }
        }

        private void OnMouseClick(SKPoint p) {
            touch.lastPinchPan = Swipe.Id();
            touch.incrementalScaling = Swipe.Id();
            touch.incrementalTranslation = Swipe.Id();
            touch.setManualPinchPan?.Invoke(touch.lastPinchPan);
            touch.invalidateSurface();
        }

        private void OnMouseZoom(SKPoint scalingOrigin, float scroll) {
            float scaling = (scroll < 0) ? 0.9f : 1.1f;
            touch.incrementalScaling = touch.incrementalScaling * new Swipe(scaling, new SKPoint((1 - scaling) * scalingOrigin.X, (1 - scaling) * scalingOrigin.Y)); // scaling around the scaleOrigin
            touch.setManualPinchPan?.Invoke(touch.lastPinchPan * touch.incrementalScaling * touch.incrementalTranslation);
            touch.invalidateSurface();
        }

        // https://github.com/xamarin/mac-samples/blob/master/MouseTrackingExample/MouseTrackingExample/MyTrackingView.cs#L26
        public override bool AcceptsFirstResponder() { return true; }
        public override void UpdateTrackingAreas() { Tracking(); }

        NSTrackingArea trackingArea = null;

        private void Tracking() {
            if (trackingArea != null) this.RemoveTrackingArea(trackingArea);
            trackingArea = new NSTrackingArea(Frame,
                NSTrackingAreaOptions.ActiveInKeyWindow
                | NSTrackingAreaOptions.MouseEnteredAndExited
                | NSTrackingAreaOptions.MouseMoved
                //| NSTrackingAreaOptions.EnabledDuringMouseDrag  // otherwise no enter/exit events while mouse dragging
                                                                  // mouse move events are NEVER sent when mouse button is down!!!
                , this, null);
            this.AddTrackingArea(trackingArea);
        }

        // Converts a raw macOS mouse event point into the coordinates of the currently canvas.
        private (CGPoint native, SKPoint flipped) ConvertToCanvasPoint(NSEvent theEvent) {
            var location = theEvent.LocationInWindow;
            var native = ConvertPointFromView(location, null);
            var flipped = new SKPoint((float)native.X, (float)Frame.Size.Height - (float)native.Y);
            return (native, flipped);
        }
        #endregion

        #region Draw Methods

        public void Invalidate() {
            NeedsDisplay = true;
        }

        // Detect size changes for this view
        public override void ResizeWithOldSuperviewSize(CGSize oldSize) {
            base.ResizeWithOldSuperviewSize(oldSize);
            Invalidate();
        }

        // Implement this to draw on the canvas.
        public override void DrawRect(CGRect dirtyRect) {
            base.DrawRect(dirtyRect);
            var context = NSGraphicsContext.CurrentContext.CGContext;
            CG.FlipCoordinateSystem(context);
            KChartHandler.Draw(new CGChartPainter(context), (int)dirtyRect.X, (int)dirtyRect.Y, (int)dirtyRect.Width, (int)dirtyRect.Height);
        }
        #endregion

        #region Interaction

        private DateTime lastTooltipUpdate = DateTime.MinValue;
        bool mouseInsideChartControl = false;
        public static SKPoint mouseDownPoint = new SKPoint(0, 0);

        public override void MouseEntered(NSEvent e) {
            base.MouseEntered(e);
            mouseInsideChartControl = true;
            showTooltip = !shiftKeyDown;
            KChartHandler.ShowEndNames(!shiftKeyDown);
            if (!KControls.IsSimulating()) Invalidate();
        }

        public override void MouseExited(NSEvent e) {
            base.MouseExited(e);
            mouseInsideChartControl = false;
            showTooltip = false; UpdateTooltip(e);
            KChartHandler.ShowEndNames(false);
            if (!KControls.IsSimulating()) Invalidate();
        }

        public override void MouseDown(NSEvent e) { // handle MouseDown, MouseDrag, MouseUp, MouseClick
            base.MouseDown(e);
            (CGPoint native, SKPoint location) = ConvertToCanvasPoint(e);
            KGui.kControls.CloseOpenMenu();

            bool isDragging = false;
            bool isTracking = true;

            while (isTracking) {
                if (e.Type == NSEventType.LeftMouseDown) {
                    // mouse down
                    KGui.kControls.CloseOpenMenu();
                    mouseDownPoint = location;
                } else if (e.Type == NSEventType.LeftMouseUp) {
                    // mouse up
                    isTracking = false;
                    if (isDragging) {
                        // end of dragging
                        touch.onTouchSwipeOrMouseDragEnd?.Invoke(mouseDownPoint, location);
                    } else {
                        // mouse click
                        touch.onTouchDoubletapOrMouseClick?.Invoke(location);
                    }
                } else if (e.Type == NSEventType.LeftMouseDragged) { // generated only when the mouse is down and moving
                    if (isDragging) {
                        touch.onTouchSwipeOrMouseDrag?.Invoke(mouseDownPoint, location);
                        // mouse dragging
                    } else if (SKPoint.Distance(location, mouseDownPoint) > 4) {
                        isDragging = true;
                    }
                    showTooltip = false; UpdateTooltip(e);
                }
                if (isTracking) {
                    e = Window.NextEventMatchingMask(NSEventMask.LeftMouseDragged | NSEventMask.LeftMouseUp);
                    (native, location) = ConvertToCanvasPoint(e);
                }
            }
        }

        public override void MouseMoved(NSEvent e) {
            base.MouseMoved(e);
            if (!mouseInsideChartControl) return;
            (CGPoint native, SKPoint location) = ConvertToCanvasPoint(e);
            lastLocationForZoom = location;
            if (DateTime.Now.Subtract(lastTooltipUpdate).TotalSeconds > 0.01) {
                KChartHandler.ShowEndNames(!shiftKeyDown);
                showTooltip = !shiftKeyDown; UpdateTooltip(e);
                lastTooltipUpdate = DateTime.Now;
                if (!KControls.IsSimulating()) Invalidate(); // because of ShowEndNames
            }
        }

        private SKPoint lastLocationForZoom = new SKPoint(0,0);

        public override void ScrollWheel(NSEvent e) {
            base.ScrollWheel(e); // Do not access e.AbsoluteX/e.AbsoluteY they are invalid for this event and crash
            touch.onTouchPinchOrMouseZoom?.Invoke(lastLocationForZoom, (float)e.ScrollingDeltaY); 
        }

        private static bool shiftKeyDown = false;
        public bool MyModifiersChanged(NSEvent e) {
            if (e.ModifierFlags.HasFlag(NSEventModifierMask.ShiftKeyMask)) { //0x38 kVK_Shift
                shiftKeyDown = true;
                if (mouseInsideChartControl) {
                    KChartHandler.ShowEndNames(false);
                    showTooltip = false; UpdateTooltip(e);
                    if (!KControls.IsSimulating()) Invalidate();
                }
                return true;
            } else {
                shiftKeyDown = false;
                if (mouseInsideChartControl) {
                    KChartHandler.ShowEndNames(true);
                    showTooltip = true; UpdateTooltip(e);
                    if (!KControls.IsSimulating()) Invalidate();
                }
                return false;
            }
        }

        private bool showTooltip = true;

        private void UpdateTooltip(NSEvent e) {
            (CGPoint native, SKPoint flipped) = ConvertToCanvasPoint(e);
            flipped = Swipe.Inverse(flipped, KChartHandler.GetManualPinchPan()); // adjust for current pinchPan
            string tip = (showTooltip) ? KChartHandler.HitListTooltip(flipped, 10) : "";
            if (tip == "") MacGui.macGui.SetChartTooltip("", new CGPoint(0, 0), new CGRect(0, 0, 0, 0));
            else MacGui.macGui.SetChartTooltip(tip, native, Frame);
        }

        #endregion

    }
}
