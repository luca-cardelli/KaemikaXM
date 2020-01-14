using Foundation;
using System;
using AppKit;
using CoreGraphics;
using Kaemika;

namespace KaemikaMAC
{
    [Register("NSKaemikaChart")]
    public class NSKaemikaChart : NSControl
    {
        #region Constructors
        public NSKaemikaChart()
        {
            // Init
            Initialize();
        }

        public NSKaemikaChart(IntPtr handle) : base (handle)
        {
            // Init
            Initialize();
        }

        [Export ("initWithFrame:")]
        public NSKaemikaChart(CGRect frameRect) : base(frameRect) {
            // Init
            Initialize();
        }

        private void Initialize() {
            this.WantsLayer = true;
            this.LayerContentsRedrawPolicy = NSViewLayerContentsRedrawPolicy.OnSetNeedsDisplay;
        }
        #endregion

        private KChart chart = new KChart("", "");

        public void SetChart(KChart chart) {
            if (NSThread.IsMain) {
                this.chart = chart;
                Invalidate();
            } else { _ = GUI_Mac.BeginInvokeOnMainThreadAsync(() => { SetChart(chart); return GUI_Mac.ack; }).Result; }
        }
       
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

            this.chart.Draw(new CGChartPainter(context), (int)dirtyRect.Width, (int)dirtyRect.Height);

            //// https://github.com/NickSpag/Workbooks/blob/master/MacOS%20Custom%20Drawing.workbook
            //if (false) {
            //    NSColor.Red.Set();
            //    NSBezierPath.StrokeLine(new CGPoint(10, 10), new CGPoint(100, 100));
            //} else {
            //    var context = NSGraphicsContext.CurrentContext.CGContext;
            //    context.SetStrokeColor(NSColor.Black.CGColor);
            //    context.SetLineWidth(1);
            //    var rectangleCGPath = CGPath.FromRoundedRect(new CGRect(10, 10, 100, 100), 4, 4);
            //    context.AddPath(rectangleCGPath);
            //    context.StrokePath();
            //}
        }
        #endregion

    }
}
