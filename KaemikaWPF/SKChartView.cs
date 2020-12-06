using System;
using System.Drawing;
using System.Windows.Forms;
using SkiaSharp;
using Kaemika;

namespace KaemikaWPF {

    public class KChartSKControl : SkiaSharp.Views.Desktop.SKControl {  // ### Add KTouchable interface for gesture handling for charts, see NSScoreView (Mac) and SKScoreView (PC)

        private static KChartSKControl chartControl = null; // The only KChartSKControl, same as "this", but accessible from static methods
        private KTouchClientData touch = null;

        public KChartSKControl() : base() {
            chartControl = this;
            chartControl.Location = new Point(0, 0);
            Label toolTip = WinGui.winGui.label_Tooltip;
            toolTip.Visible = false;
            toolTip.BackColor = WinControls.cPanelButtonDeselected;
            toolTip.Font = WinGui.winGui.GetFont(8, true);
            toolTip.MouseEnter +=
                (object sender, EventArgs e) => { UpdateTooltip(new Point(0, 0), ""); };

            /* Initialize Interface KTouchClient with a locally-sourced KTouchClientData closure */
            touch = new KTouchClientData(
                invalidateSurface: () => { InvalidateAndUpdate(); },
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

        // Static methods, accessing chartControl

        public static void SetSize(Size size) {
            if (chartControl != null) chartControl.Size = size;
        }

        public static void InvalidateAndUpdate() {
            if (chartControl != null) {
                chartControl.Invalidate();
                chartControl.Update();
            }
        }
   
        public static bool shiftKeyDown = false; // See also WinGui_KeyDown, WinGui_KeyUp

        public static void OnShiftKeyDown() {  // called from GuiToWin.GuiToWin_KeyDown form callback
            shiftKeyDown = true;
            if (mouseInsideChartControl) {
                KChartHandler.ShowEndNames(false);
                chartControl.UpdateTooltip(new Point(0, 0), "");
                if (!KControls.IsSimulating()) chartControl.Invalidate();
            }
        }

        public static void OnShiftKeyUp() {  // called from GuiToWin.GuiToWin_KeyUp form callback
            shiftKeyDown = false;
            if (mouseInsideChartControl) {
                KChartHandler.ShowEndNames(true);
                if (!KControls.IsSimulating()) chartControl.Invalidate();
            }
        }

        // Implement this to draw on the canvas.
        protected override void OnPaintSurface(SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e) {
            base.OnPaintSurface(e);
            KChartHandler.Draw(new SKChartPainter(e.Surface.Canvas), Location.X, Location.Y, e.Info.Width, e.Info.Height);
        }

        private DateTime lastTooltipUpdate = DateTime.MinValue;
        public static bool mouseInsideChartControl = false;
        public static bool mouseDown = false;
        public static SKPoint mouseDownPoint = new SKPoint(0, 0);

        protected override void OnMouseEnter(EventArgs e) {
            base.OnMouseEnter(e);
            mouseInsideChartControl = true;
            KChartHandler.ShowEndNames(true);
            if (!KControls.IsSimulating()) Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            mouseInsideChartControl = false;
            UpdateTooltip(new Point(0, 0), "");
            KChartHandler.ShowEndNames(false);
            if (!KControls.IsSimulating()) Invalidate();
        }
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            KGui.kControls.CloseOpenMenu();
            mouseDown = true;
            mouseDownPoint = new SKPoint(e.Location.X, e.Location.Y);
        }
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            mouseDown = false;
            SKPoint mouseUpPoint = new SKPoint(e.Location.X, e.Location.Y);
            if (mouseUpPoint == mouseDownPoint) touch.onTouchDoubletapOrMouseClick?.Invoke(mouseDownPoint);
            else touch.onTouchSwipeOrMouseDragEnd?.Invoke(mouseDownPoint, mouseUpPoint);
        }
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (DateTime.Now.Subtract(lastTooltipUpdate).TotalSeconds > 0.01) {
                KChartHandler.ShowEndNames(!shiftKeyDown);
                if (mouseDown) UpdateTooltip(new Point(0, 0), "");
                else if (!shiftKeyDown) UpdateTooltip(new Point(e.X, e.Y), KChartHandler.HitListTooltip(Swipe.Inverse(new SKPoint(e.X, e.Y), KChartHandler.GetManualPinchPan()), 10));
                lastTooltipUpdate = DateTime.Now;
                if (!KControls.IsSimulating()) Invalidate(); // because of ShowEndNames
            }
            if (mouseDown) {
                touch.onTouchSwipeOrMouseDrag?.Invoke(mouseDownPoint, new SKPoint(e.Location.X, e.Location.Y));
            }
        }
        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel(e);
            touch.onTouchPinchOrMouseZoom?.Invoke(new SKPoint(e.Location.X, e.Location.Y), e.Delta);
        }

        private void UpdateTooltip(Point point, string tip) {
            int off = 6;
            int pointerWidth = 16;
            Label toolTip = WinGui.winGui.label_Tooltip;
            Panel chart = WinGui.winGui.panel_KChart;
            if (tip == "") {
                toolTip.Text = "";
                toolTip.Visible = false;
            } else {
                toolTip.Text = tip;
                int tipX = (point.X < chart.Width / 2) ? (int)point.X + off : (int)point.X - toolTip.Width - off;
                int tipY = (point.Y < chart.Height / 2) ? (int)point.Y + off : (int)point.Y - toolTip.Height - off;
                if ((point.X < chart.Width / 2) && (point.Y < chart.Height / 2)) tipX += pointerWidth;
                toolTip.Location = new Point(tipX, tipY);
                toolTip.Visible = true;
                toolTip.BringToFront();
            }
        }

    }

}
