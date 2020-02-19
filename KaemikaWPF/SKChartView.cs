using System;
using System.Drawing;
using System.Windows.Forms;
using SkiaSharp;
using Kaemika;

namespace KaemikaWPF {

    public class KChartSKControl : SkiaSharp.Views.Desktop.SKControl {

        private static KChartSKControl chartControl = null; // The only KChartSKControl, same as "this", but accessible from static methods

        public KChartSKControl() : base() {
            chartControl = this;
            chartControl.Location = new Point(0, 0);
            Label toolTip = WinGui.winGui.label_Tooltip;
            toolTip.Visible = false;
            toolTip.BackColor = WinControls.cPanelButtonDeselected;
            toolTip.Font = WinGui.winGui.GetFont(8, true);
            toolTip.MouseEnter +=
                (object sender, EventArgs e) => { UpdateTooltip(new Point(0, 0), ""); };
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

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (DateTime.Now.Subtract(lastTooltipUpdate).TotalSeconds > 0.01) {
                KChartHandler.ShowEndNames(!shiftKeyDown);
                if (!shiftKeyDown) UpdateTooltip(new Point(e.X, e.Y), KChartHandler.HitListTooltip(new SKPoint(e.X, e.Y), 10));
                lastTooltipUpdate = DateTime.Now;
                if (!KControls.IsSimulating()) Invalidate(); // because of ShowEndNames
            }
        }
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
        protected override void OnMouseClick(MouseEventArgs e) {
            base.OnMouseClick(e);
            KGui.kControls.CloseOpenMenu();
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
