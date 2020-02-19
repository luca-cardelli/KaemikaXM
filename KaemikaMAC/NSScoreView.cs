using Foundation;
using System;
using AppKit;
using CoreGraphics;
using SkiaSharp;
using Kaemika;

namespace KaemikaMAC {

    [Register("NSScoreView")]
    public class NSScoreView : NSControl, KTouchable { //### NSScoreView --> KScoreNSControl

        #region Constructors
        public NSScoreView() { OnLoad(); }
        public NSScoreView(IntPtr handle) : base(handle) { OnLoad(); }
        [Export("initWithFrame:")]
        public NSScoreView(CGRect frameRect) : base(frameRect) { OnLoad(); }
        #endregion

        #region OnLoad setup

        private static NSScoreView scoreControl = null;  // The only NSScoreView, same as "this", but accessible from static methods

        private void OnLoad() {
            this.WantsLayer = true;
            this.LayerContentsRedrawPolicy = NSViewLayerContentsRedrawPolicy.OnSetNeedsDisplay;
            scoreControl = this;
            KScoreHandler.Register(scoreControl);
            Tracking();
        }

        // https://github.com/xamarin/mac-samples/blob/master/MouseTrackingExample/MouseTrackingExample/MyTrackingView.cs#L26
        public override bool AcceptsFirstResponder() { return true; }
        public override void UpdateTrackingAreas() { Tracking(); }

        NSTrackingArea trackingArea = null;
        //bool mouseInsideChartControl = false;  // for NSTrackingAreaOptions.MouseEnteredAndExited

        private void Tracking() {
            if (trackingArea != null) this.RemoveTrackingArea(trackingArea);
            trackingArea = new NSTrackingArea(Frame,
                NSTrackingAreaOptions.ActiveInKeyWindow |
                // NSTrackingAreaOptions.MouseEnteredAndExited |
                NSTrackingAreaOptions.MouseMoved
                //| NSTrackingAreaOptions.EnabledDuringMouseDrag ???
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

        public static void SetSize(System.Drawing.Size size) {
            if (scoreControl != null) scoreControl.SetFrameSize(new CGSize((float)size.Width, (float)size.Height));
        }
        // Detect size changes for this view
        public override void ResizeWithOldSuperviewSize(CGSize oldSize) {
            base.ResizeWithOldSuperviewSize(oldSize);
            Invalidate();
        }

        public /*KScoreContol Interface */ void DoInvalidate() {
            if (NSThread.IsMain) {
                // SetSize(MainClass.guiToMac.ScoreSize());  //###
                Invalidate();
            } else { _ = MacGui.BeginInvokeOnMainThreadAsync(() => { DoInvalidate(); return MacGui.ack; }).Result; }
        }
        private void Invalidate() {
            NeedsDisplay = true;
        }

        public /*KScoreContol Interface */ void DoHide() {
            if (NSThread.IsMain) {
                MacGui.macGui.ScoreHide();
            } else { _ = MacGui.BeginInvokeOnMainThreadAsync(() => { DoHide(); return MacGui.ack; }).Result; }
        }

        public /*KScoreContol Interface */ void DoShow() {
            if (NSThread.IsMain) {
                MacGui.macGui.ScoreShow();
            } else { _ = MacGui.BeginInvokeOnMainThreadAsync(() => { DoShow(); return MacGui.ack; }).Result; }
        }

        // Implement this to draw on the canvas.
        public override void DrawRect(CGRect dirtyRect) {
            base.DrawRect(dirtyRect);
            var context = NSGraphicsContext.CurrentContext.CGContext;
            CG.FlipCoordinateSystem(context);
            KScoreHandler.Draw(new CGPainter(context), 0, 0, (int)dirtyRect.Width, (int)dirtyRect.Height);
        }

        #endregion

        #region Interaction

        private Action<SKPoint> onTouchTapOrMouseMove;
        private Action<SKPoint> onTouchDoubletapOrMouseClick;
        private Action<SKPoint, SKPoint> onTouchSwipeOrMouseDrag;
        private Action<SKPoint, SKPoint> onTouchSwipeOrMouseDragEnd;

        public /*KScoreContol Interface */ void OnTouchTapOrMouseMove(Action<SKPoint> action) { this.onTouchTapOrMouseMove = action; }
        public /*KScoreContol Interface */ void OnTouchDoubletapOrMouseClick(Action<SKPoint> action) { this.onTouchDoubletapOrMouseClick = action; }
        public /*KScoreContol Interface */ void OnTouchSwipeOrMouseDrag(Action<SKPoint, SKPoint> action) { this.onTouchSwipeOrMouseDrag = action; }
        public /*KScoreContol Interface */ void OnTouchSwipeOrMouseDragEnd(Action<SKPoint, SKPoint> action) { this.onTouchSwipeOrMouseDragEnd = action; }

        private static SKPoint mouseDownPoint;
        private static SKPoint mouseMovePoint;
        private static bool mouseDown = false;
        private static bool dragging = false;

        public override void MouseDown(NSEvent e) {
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
                        this.onTouchSwipeOrMouseDragEnd?.Invoke(mouseDownPoint, location);
                    } else {
                        // mouse click
                        this.onTouchDoubletapOrMouseClick?.Invoke(location);
                    }
                } else if (e.Type == NSEventType.LeftMouseDragged) { // generated only when the mouse is down and moving
                    if (isDragging) {
                        this.onTouchSwipeOrMouseDrag?.Invoke(mouseDownPoint, location);
                        // mouse dragging
                    } else if (SKPoint.Distance(location, mouseDownPoint) > 16) {
                        isDragging = true;
                    }
                }
                if (isTracking) {
                    e = Window.NextEventMatchingMask(NSEventMask.LeftMouseDragged | NSEventMask.LeftMouseUp);
                    (native, location) = ConvertToCanvasPoint(e);
                }
            }
        }

        public override void MouseMoved(NSEvent e) { // generated only while the mouse is up and moving
            base.MouseMoved(e);
            //if (!mouseInsideChartControl) return;
            (CGPoint native, SKPoint location) = ConvertToCanvasPoint(e);
            this.onTouchTapOrMouseMove?.Invoke(location);
        }

        #endregion

    }
}
