using System;
using Xamarin.Forms;
using SkiaSharp.Views.Forms;
using SkiaSharp;
using Kaemika;
using XFormsTouch; // see KTouch.cs

namespace KaemikaXM {

    public class ChartView : SKCanvasView, KTouchable, KTouchClient {

        public ChartView() {
            // register this as KTouchable so that touch callbacks can be attached through interface KTouchable:
            // (If we want to add special actions for Tap, etc. beyond built-in two-finger swiping and zooming)
            // KChartHandler.Register(this); 

            HeightRequest = 300;
            this.BackgroundColor = Color.White;
            this.PaintSurface += OnPaintCanvas;

            /* Attact Touch effect from KTouch.OnTouchEffectAction */
            TouchEffect touchEffect = new TouchEffect();
            touchEffect.TouchAction += KTouchServer.OnTouchEffectAction;
            touchEffect.Capture = true; // "This has the effect of delivering all subsequent events to the same event handler"
            this.Effects.Add(touchEffect);

            /* Initialize Interface KTouchClient with a locally-sourced KTouchClientData closure */
            this.data = new KTouchClientData(
                invalidateSurface: () => { this.InvalidateSurface(); },
                setManualPinchPan: (Swipe pinchPan) => { KChartHandler.SetManualPinchPan(pinchPan); }
                );
        }

        public /*Interface KTouchClient */ KTouchClientData data { get; }

        private void OnPaintCanvas(object sender, SKPaintSurfaceEventArgs e) {
            SKChartPainter painter = new SKChartPainter(e.Surface.Canvas);
            KChartHandler.Draw(painter, 0, 0, e.Info.Width, e.Info.Height);
            data.DisplayTouchLocation(painter);
        }

        // Two finger swipe: Pan
        // Two finger pinch: Zoom
        // Two finger tap: Pan/Zoom reset
        public /*Interface KTouchable*/ void OnTouchTapOrMouseMove(Action<SKPoint> action) { data.onTouchTapOrMouseMove = action; }                     // Hover/Hilight item
        public /*Interface KTouchable*/ void OnTouchDoubletapOrMouseClick(Action<SKPoint> action) { data.onTouchDoubletapOrMouseClick = action; }       // Activate Item
        public /*Interface KTouchable*/ void OnTouchSwipeOrMouseDrag(Action<SKPoint, SKPoint> action) { data.onTouchSwipeOrMouseDrag = action; }        // Drag Item
        public /*Interface KTouchable*/ void OnTouchSwipeOrMouseDragEnd(Action<SKPoint, SKPoint> action) { data.onTouchSwipeOrMouseDragEnd = action; }  // Drag Item End
        public /*Interface KTouchable*/ void DoShow() { this.IsVisible = true; }
        public /*Interface KTouchable*/ void DoHide() { this.IsVisible = false; }
        public /*Interface KTouchable*/ void DoInvalidate() {
            // iOS required BeginInvokeOnMainThread:
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                this.InvalidateSurface();
            });
        }

    }
}

