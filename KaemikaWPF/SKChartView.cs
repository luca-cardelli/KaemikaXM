using System;
using System.Drawing;
using System.Windows.Forms;
using SkiaSharp;
using Kaemika;

namespace KaemikaWPF {

    public class KChartSKControl : SkiaSharp.Views.Desktop.SKControl {

        public static KChartSKControl chartControl = null;

        public KChartSKControl() : base() {
            Label toolTip = App.guiToWin.label_Tooltip;
            toolTip.Visible = false;
            toolTip.BackColor = WinControls.cPanelButtonDeselected;
            toolTip.Font = App.guiToWin.GetFont(8, true);
            toolTip.MouseEnter +=
                (object sender, EventArgs e) => { UpdateTooltip(new Point(0, 0), ""); };
        }

        // Implement this to draw on the canvas.
        protected override void OnPaintSurface(SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e) {
            // call the base method
            base.OnPaintSurface(e);

            var surface = e.Surface;
            var canvas = surface.Canvas;
            int canvasWidth = e.Info.Width;
            int canvasHeight = e.Info.Height;
            int canvasX = Location.X;
            int canvasY = Location.Y;

            // draw on the canvas

            KChartHandler.Draw(new SKChartPainter(canvas), canvasX, canvasY, canvasWidth, canvasHeight);
        }

        // See also GuiToWin_KeyDown, GuiToWin_KeyUp
        private DateTime lastTooltipUpdate = DateTime.MinValue;
        public static bool shiftKeyDown = false;
        public static bool mouseInsideChartControl = false;

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (DateTime.Now.Subtract(lastTooltipUpdate).TotalSeconds > 0.01) {
                KChartHandler.ShowEndNames(!shiftKeyDown);
                if (!shiftKeyDown) UpdateTooltip(new Point(e.X, e.Y), KChartHandler.HitListTooltip(new SKPoint(e.X, e.Y), 10));
                lastTooltipUpdate = DateTime.Now;
                if (!Exec.IsExecuting()) Invalidate(); // because of ShowEndNames
            }
        }
        protected override void OnMouseEnter(EventArgs e) {
            base.OnMouseEnter(e);
            mouseInsideChartControl = true;
            KChartHandler.ShowEndNames(true);
            if (!Exec.IsExecuting()) Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            mouseInsideChartControl = false;
            UpdateTooltip(new Point(0, 0), "");
            KChartHandler.ShowEndNames(false);
            if (!Exec.IsExecuting()) Invalidate();
        }
        protected override void OnMouseClick(MouseEventArgs e) {
            base.OnMouseClick(e);
            App.guiToWin.kControls.CloseOpenMenu();
        }
        public static void OnShiftKeyDown() {  // called from GuiToWin.GuiToWin_KeyDown form callback
            shiftKeyDown = true;
            if (mouseInsideChartControl) {
                KChartHandler.ShowEndNames(false);
                chartControl.UpdateTooltip(new Point(0, 0), "");
                if (!Exec.IsExecuting()) chartControl.Invalidate();
            }
        }
        public static void OnShiftKeyUp() {  // called from GuiToWin.GuiToWin_KeyUp form callback
            shiftKeyDown = false;
            if (mouseInsideChartControl) {
                KChartHandler.ShowEndNames(true);
                if (!Exec.IsExecuting()) chartControl.Invalidate();
            }
        }

        private void UpdateTooltip(Point point, string tip) {
            int off = 6;
            int pointerWidth = 16;
            Label toolTip = App.guiToWin.label_Tooltip;
            Panel chart = App.guiToWin.panel_KChart;
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
