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
    
        private void button_Device_Click(object sender, EventArgs e) {
            //if (DeviceForm.deviceForm == null) {
            //    DeviceForm.deviceForm = new DeviceForm();
            //    DeviceForm.deviceForm.Show();
            //    button_Device.Image = global::KaemikaWPF.Properties.Resources.icons8device_ON_48x48;
            //} else {
            //    if (!Exec.IsExecuting()) {
            //        DeviceForm.deviceForm.Close();
            //        button_Device.Image = global::KaemikaWPF.Properties.Resources.icons8device_OFF_48x48;
            //    }
         }

        //public override void DeviceUpdate() {
        //    if (DeviceForm.deviceForm == null) return;
        //    if (DeviceForm.deviceForm.deviceControl.InvokeRequired) {
        //        VoidArgVoidReturnDelegate d = new VoidArgVoidReturnDelegate(DeviceUpdate);
        //        DeviceForm.deviceForm.deviceControl.Invoke(d, new object[] { });
        //    } else {
        //        // resize the deviceControl
        //        DeviceForm form = DeviceForm.deviceForm;
        //        Size currentSize = form.deviceControl.Size;
        //        (int newWidth, int newHeight) = ProtocolDevice.DesiredSize();
        //        Size newSize = new Size(newWidth, newHeight);
        //        if (newSize != currentSize) {
        //            form.Size = new Size(newSize.Width, newSize.Height + 40); // window header
        //            DeviceForm.deviceForm.deviceControl.Size = newSize;
        //        }
        //        DeviceForm.deviceForm.deviceControl.Invalidate();
        //        DeviceForm.deviceForm.deviceControl.Update();
        //    }
        //}


    public partial class DeviceForm : Form {

        public static DeviceForm deviceForm = null;
        //public DeviceSKControl deviceControl = null;

        public DeviceForm() {
            InitializeComponent(); // sets Size and Location properties

            //DeviceSKControl deviceSKControl = new DeviceSKControl();
            //this.deviceControl = deviceSKControl;
            //this.mainPanel.Controls.Add(deviceSKControl);

            //deviceSKControl.BackColor = Color.DarkRed;
            //deviceSKControl.Location = new Point(0, 0);
            //deviceSKControl.Size = Size;
            //deviceSKControl.Visible = true;

            ControlBox = false; // disable the window close box because closing the device during execution will cause crashes
                                // instead use the Device button to close the window too, when not executing

            ProtocolDevice.Start(30, 100);
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            base.OnFormClosed(e);
            ProtocolDevice.Stop();
            deviceForm = null;
        }
    }

}
