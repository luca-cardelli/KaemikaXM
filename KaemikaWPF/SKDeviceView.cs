using System.Windows.Forms;
using Kaemika;

namespace KaemikaWPF {

    public class DeviceSKControl : SkiaSharp.Views.Desktop.SKControl {

        public static DeviceSKControl deviceControl = null;

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

            ProtocolDevice.Draw(new SKDevicePainter(canvas), canvasX, canvasY, canvasWidth, canvasHeight);
        }

        protected override void OnMouseClick(MouseEventArgs e) {
            base.OnMouseClick(e);
            App.guiToWin.kControls.CloseOpenMenu();
        }
    }

}
