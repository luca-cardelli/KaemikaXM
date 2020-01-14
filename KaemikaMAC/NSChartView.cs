using System;
using System.Collections.Generic;
using Foundation;
using AppKit;
using SkiaSharp;
using CoreGraphics;
using Kaemika;

namespace KaemikaMAC
{
    [Register("NSChartView")]
    public class NSChartView : NSControl
    {
        #region Constructors
        public NSChartView()
        {
            // Init
            Initialize();
        }

        public NSChartView(IntPtr handle) : base (handle)
        {
            // Init
            Initialize();
        }

        [Export ("initWithFrame:")]
        public NSChartView(CGRect frameRect) : base(frameRect) {
            // Init
            Initialize();
        }

        private void Initialize() {
            this.WantsLayer = true;
            this.LayerContentsRedrawPolicy = NSViewLayerContentsRedrawPolicy.OnSetNeedsDisplay;
            Tracking();
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

        public override void DrawRect (CGRect dirtyRect) {
            base.DrawRect (dirtyRect);
            var context = NSGraphicsContext.CurrentContext.CGContext;

            CG.FlipCoordinateSystem(context);

            KChartHandler.Draw(new CGChartPainter(context), (int)dirtyRect.X, (int)dirtyRect.Y, (int)dirtyRect.Width, (int)dirtyRect.Height);
        }
        #endregion


        #region Interaction

        // https://github.com/xamarin/mac-samples/blob/master/MouseTrackingExample/MouseTrackingExample/MyTrackingView.cs#L26

        public override bool AcceptsFirstResponder () {
            return true;
        }

        public override void UpdateTrackingAreas() {
            Tracking();
        }

        NSTrackingArea trackingArea = null;
        bool insideArea = false;

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

		public override void MouseEntered (NSEvent theEvent) {
			base.MouseEntered (theEvent);
            insideArea = true;
            KChartHandler.ShowEndNames(true);
            Invalidate();
        }

        public override void MouseExited (NSEvent theEvent) {
			base.MouseExited (theEvent);
            MainClass.form.SetChartTooltip("", new CGPoint(0,0), new CGRect(0,0,0,0));
            insideArea = false;
            KChartHandler.ShowEndNames(false);
            Invalidate();
        }

        public override void MouseDown(NSEvent theEvent) {
            base.MouseDown(theEvent);
            MainClass.form.clickerHandler.CloseOpenMenu();
        }

        private DateTime lastTooltipUpdate = DateTime.MinValue;

		public override void MouseMoved (NSEvent theEvent) {
			base.MouseMoved (theEvent);
            if (!insideArea) return;
            if (DateTime.Now.Subtract(lastTooltipUpdate).TotalSeconds > 0.01) {
                UpdateTooltip(theEvent);
                lastTooltipUpdate = DateTime.Now;
            }
		}

        private void UpdateTooltip(NSEvent theEvent) {
            (CGPoint native, SKPoint flipped) = ConvertToCanvasPoint(theEvent);
            var shiftKeyDown = ((theEvent.ModifierFlags & NSEventModifierMask.ShiftKeyMask) == NSEventModifierMask.ShiftKeyMask);
            string tip = KChartHandler.HitListTooltip(flipped, 10);
            if (tip == "") MainClass.form.SetChartTooltip("", new CGPoint(0, 0), new CGRect(0, 0, 0, 0));
            else MainClass.form.SetChartTooltip(tip, native, Frame);
        }

        #endregion

    }
}
