using System;
using System.Collections.Generic;
using System.Text;
using Kaemika;
using Xamarin.Forms;
using SkiaSharp.Views.Forms;
using SkiaSharp;
using GraphSharp;

namespace KaemikaXM
{
    public class DeviceView : SKCanvasView {

        public DeviceView() {
            this.BackgroundColor = Color.Transparent;
            this.PaintSurface += OnPaintCanvas;

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            this.GestureRecognizers.Add(panGesture);
            var pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += OnPinchUpdated;
            this.GestureRecognizers.Add(pinchGesture);
            var tapGesture = new TapGestureRecognizer();
            tapGesture.NumberOfTapsRequired = 2;
            tapGesture.Tapped += OnTapped;
            this.GestureRecognizers.Add(tapGesture);
        }

        //public static readonly BindableProperty DeviceProperty = BindableProperty.Create(nameof(Device), typeof(ProtocolDevice.Device), typeof(DeviceView), null, propertyChanged: OnDeviceChanged);

        //public ProtocolDevice.Device Device {
        //    get { return (ProtocolDevice.Device)GetValue(DeviceProperty); }
        //    set { SetValue(DeviceProperty, value); }
        //}   

        //private static void OnDeviceChanged(BindableObject bindable, object oldValue, object newValue) {
        //    ((DeviceView)bindable).InvalidateSurface();
        //}

        private void OnPaintCanvas(object sender, SKPaintSurfaceEventArgs e) {
            ProtocolDevice.Draw(e.Surface.Canvas, 0, 0, e.Info.Width, e.Info.Height);
        }

        private Swipe pinchPan = Swipe.Id;    // remember transform at beginning of pinch or pan
        private float pinchAccum = 1.0f;                                // accumulate pinching transform
        private bool panJustStarted = false;    // prevent pan jerk in pinch-pan and pan-pinch-pan sequences when holding one finger down
        private double totalXinit = 0;          // allows smooth pinch-pan and pan-pinch continuation
        private double totalYinit = 0;          // while holding one finger down
        private float mysteriousScaling = 3.5f; // touch position seems scaled by this factor ?!?

        void OnTapped(object sender, EventArgs e) {
            if (!ProtocolDevice.Exists()) return;
            ProtocolDevice.ResetPinchPan();
            this.InvalidateSurface();
        }

        void OnPanUpdated(object sender, PanUpdatedEventArgs e) {
            if (!ProtocolDevice.Exists()) return;
            if (e.StatusType == GestureStatus.Started) {
                panJustStarted = true; 
            } else if (e.StatusType == GestureStatus.Running) {
                if (panJustStarted) { // calibrate pan position to ignore inital jerk
                    pinchPan = ProtocolDevice.PinchPan();
                    totalXinit = e.TotalX;
                    totalYinit = e.TotalY;
                    panJustStarted = false;
                } else {
                    ProtocolDevice.SetPinchPan(pinchPan * new Swipe(1.0f, new SKPoint((float)(mysteriousScaling * (e.TotalX-totalXinit)), (float)(mysteriousScaling * (e.TotalY-totalYinit)))));
                    this.InvalidateSurface();
                }
            } else if (e.StatusType == GestureStatus.Completed) {
                this.InvalidateSurface();
            }
        }

        void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e) {
            if (!ProtocolDevice.Exists()) return;
            if (e.Status == GestureStatus.Started) {
                pinchPan = ProtocolDevice.PinchPan();
                pinchAccum = 1.0f;
                ProtocolDevice.DisplayPinchOrigin(true);
            } else if (e.Status == GestureStatus.Running) {
                pinchAccum = pinchAccum * (float)e.Scale;
                pinchAccum = (float)Math.Max(0.1, pinchAccum);
                float pinchOriginX = (float)(e.ScaleOrigin.X * mysteriousScaling * this.Width);
                float pinchOriginY = (float)(e.ScaleOrigin.Y * mysteriousScaling * this.Height);
                ProtocolDevice.SetPinchPan(pinchPan * new Swipe(pinchAccum, new SKPoint((1 - pinchAccum) * pinchOriginX, (1 - pinchAccum) * pinchOriginY)));
                ProtocolDevice.SetPinchOrigin(new SKPoint(pinchOriginX, pinchOriginY));
                this.InvalidateSurface();
            } else if (e.Status == GestureStatus.Completed) {
                ProtocolDevice.DisplayPinchOrigin(false);
                panJustStarted = true; // force pan to recalibrate if it is still running
                this.InvalidateSurface();
            }
        }
    }
}
