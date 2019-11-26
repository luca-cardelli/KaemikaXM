using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using GraphSharp;
using Kaemika;

namespace KaemikaWPF
{
    public partial class DeviceForm : Form {

        public static DeviceForm deviceForm = null;
        public DeviceSKControl deviceControl = null;

        public DeviceForm() {
            InitializeComponent(); // sets Size and Location properties

            DeviceSKControl deviceSKControl = new DeviceSKControl();
            this.deviceControl = deviceSKControl;
            this.mainPanel.Controls.Add(deviceSKControl);

            deviceSKControl.BackColor = Color.DarkRed;
            deviceSKControl.Location = new Point(0, 0);
            deviceSKControl.Size = Size;
            deviceSKControl.Visible = true;

            ControlBox = false; // disable the window close box because closing the device during execution will cause crashes
                                // instead use the Device button to close the window too, when not executing

            ProtocolDevice.Start(30, 100);
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            base.OnFormClosed(e);
            ProtocolDevice.Stop();
            deviceForm = null;
        }

        public class DeviceSKControl : SKControl {

            // Implement this to draw on the canvas.
            protected override void OnPaintSurface(SKPaintSurfaceEventArgs e) {
                // call the base method
                base.OnPaintSurface(e);

                var surface = e.Surface;
                var canvas = surface.Canvas;
                int canvasWidth = e.Info.Width;
                int canvasHeight = e.Info.Height;
                int canvasX = Location.X;
                int canvasY = Location.Y;

                // draw on the canvas

                ProtocolDevice.Draw(canvas, canvasX, canvasY, canvasWidth, canvasHeight);

            }

        }
    }
}
