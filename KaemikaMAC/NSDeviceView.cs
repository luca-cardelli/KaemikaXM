﻿using Foundation;
using System;
using AppKit;
using CoreGraphics;
using Kaemika;

namespace KaemikaMAC
{
    [Register("NSDeviceView")]
    public class NSDeviceView : NSControl
    {
        #region Constructors
        public NSDeviceView()
        {
            // Init
            Initialize();
        }

        public NSDeviceView(IntPtr handle) : base (handle)
        {
            // Init
            Initialize();
        }

        [Export ("initWithFrame:")]
        public NSDeviceView(CGRect frameRect) : base(frameRect) {
            // Init
            Initialize();
        }

        private void Initialize() {
            this.WantsLayer = true;
            this.LayerContentsRedrawPolicy = NSViewLayerContentsRedrawPolicy.OnSetNeedsDisplay;
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

            // flip the coordinate system once for all
            var flipVertical = new CGAffineTransform(xx: 1, yx: 0, xy: 0, yy: -1, x0: 0, y0: context.GetClipBoundingBox().Height);
            context.ConcatCTM(flipVertical);

            ProtocolDevice.Draw(new CGDevicePainter(context), 0, 0, (int)dirtyRect.Width, (int)dirtyRect.Height);
        }
        #endregion

    }
}
