using System.Windows.Forms;
using Kaemika;

namespace KaemikaWPF {

    public class DeviceSKControl : SkiaSharp.Views.Desktop.SKControl {

        private static DeviceSKControl deviceControl = null; // The only DeviceSKControl, same as "this", but accessible from static methods

        public DeviceSKControl() : base() {
            deviceControl = this;
            deviceControl.Location = new System.Drawing.Point(0, 0);
        }

        // Static methods, accessing deviceControl

        public static bool Exists() {
            return deviceControl != null;
        }

        public static void SetSize(System.Drawing.Size size) {
            if (deviceControl != null) deviceControl.Size = size;
        }

        public static bool IsVisible() {
            if (deviceControl != null) return deviceControl.Visible; else return false;
        }

        public static void InvalidateAndUpdate() {
            if (deviceControl != null) {
                deviceControl.Invalidate();
                deviceControl.Update();
            }
        }

        // Implement this to draw on the canvas.
        protected override void OnPaintSurface(SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e) {
            base.OnPaintSurface(e);
            KDeviceHandler.Draw(new SKDevicePainter(e.Surface.Canvas), Location.X, Location.Y, e.Info.Width, e.Info.Height);
        }

        protected override void OnMouseClick(MouseEventArgs e) {
            base.OnMouseClick(e);
            KGui.kControls.CloseOpenMenu();
        }
    }

}
