using Foundation;
using System;
using AppKit;
using CoreGraphics;
using Kaemika;

namespace KaemikaMAC {

    [Register("NSDeviceView")]
    public class NSDeviceView : NSControl { //### NSDeviceView --> KDeviceNSControl // Add KTouchable interface for gesture handling for charts

        #region Constructors
        public NSDeviceView() { Initialize(); }
        public NSDeviceView(IntPtr handle) : base (handle) { Initialize(); }
        [Export ("initWithFrame:")]
        public NSDeviceView(CGRect frameRect) : base(frameRect) { Initialize(); }
        #endregion

        #region OnLoad setup

        private static NSDeviceView deviceControl = null;  // The only NSDeviceView, same as "this", but accessible from static methods
                                                           // CURRENTLY UNUSED
        private void Initialize() {
            this.WantsLayer = true;
            this.LayerContentsRedrawPolicy = NSViewLayerContentsRedrawPolicy.OnSetNeedsDisplay;
            deviceControl = this;
            //KChartHandler.Register(deviceControl);  // Add registration if we want to add KTouchable interface for charts
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
        public override void DrawRect (CGRect dirtyRect) {
            base.DrawRect (dirtyRect);
            var context = NSGraphicsContext.CurrentContext.CGContext;
            CG.FlipCoordinateSystem(context);
            KDeviceHandler.Draw(new CGDevicePainter(context), 0, 0, (int)dirtyRect.Width, (int)dirtyRect.Height);
        }
        #endregion

        public override void MouseDown(NSEvent theEvent) {
            base.MouseDown(theEvent);
            KGui.kControls.CloseOpenMenu();
        }

    }
}
