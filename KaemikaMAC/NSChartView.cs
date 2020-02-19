using System;
using AppKit;
using CoreGraphics;
using Foundation;
using Kaemika;
using SkiaSharp;

namespace KaemikaMAC {

    [Register("NSChartView")]
    public class NSChartView : NSControl { //### NSChartView --> KChartNSControl // Add KTouchable interface for gesture handling for charts

        #region Constructors
        public NSChartView() { OnLoad(); }
        public NSChartView(IntPtr handle) : base(handle) { OnLoad(); }
        [Export("initWithFrame:")]
        public NSChartView(CGRect frameRect) : base(frameRect) { OnLoad(); }
        #endregion

        #region OnLoad setup

        private static NSChartView chartControl = null;  // The only NSChartView, same as "this", but accessible from static methods
                                                         // CURRENTLY UNUSED
        private void OnLoad() {
            this.WantsLayer = true;
            this.LayerContentsRedrawPolicy = NSViewLayerContentsRedrawPolicy.OnSetNeedsDisplay;
            chartControl = this;
            //KChartHandler.Register(chartControl);  // Add registration if we want to add KTouchable interface for charts
            Tracking();
        }

        // https://github.com/xamarin/mac-samples/blob/master/MouseTrackingExample/MouseTrackingExample/MyTrackingView.cs#L26
        public override bool AcceptsFirstResponder() { return true; }
        public override void UpdateTrackingAreas() { Tracking(); }

        NSTrackingArea trackingArea = null;
        bool mouseInsideChartControl = false;

        private void Tracking() {
            if (trackingArea != null) this.RemoveTrackingArea(trackingArea);
            trackingArea = new NSTrackingArea(Frame,
                NSTrackingAreaOptions.ActiveInKeyWindow |
                NSTrackingAreaOptions.MouseEnteredAndExited |
                NSTrackingAreaOptions.MouseMoved
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

        public override void MouseMoved(NSEvent e) {
            base.MouseMoved(e);
            if (!mouseInsideChartControl) return;
            if (DateTime.Now.Subtract(lastTooltipUpdate).TotalSeconds > 0.01) {
                KChartHandler.ShowEndNames(!shiftKeyDown);
                showTooltip = !shiftKeyDown; UpdateTooltip(e);
                lastTooltipUpdate = DateTime.Now;
                if (!KControls.IsSimulating()) Invalidate(); // because of ShowEndNames
            }
        }

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

        public override void MouseDown(NSEvent e) {
            base.MouseDown(e);
            KGui.kControls.CloseOpenMenu();
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
            string tip = (showTooltip) ? KChartHandler.HitListTooltip(flipped, 10) : "";
            if (tip == "") MacGui.macGui.SetChartTooltip("", new CGPoint(0, 0), new CGRect(0, 0, 0, 0));
            else MacGui.macGui.SetChartTooltip(tip, native, Frame);
        }

        #endregion

    }
}
