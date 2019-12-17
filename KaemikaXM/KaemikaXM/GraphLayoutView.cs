// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using Xamarin.Forms;
using SkiaSharp.Views.Forms;
using SkiaSharp;

namespace GraphSharp {

    public class GraphLayoutView : SKCanvasView {

        public GraphLayoutView() {
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

        public static readonly BindableProperty GraphLayoutProperty = BindableProperty.Create(nameof(GraphLayout), typeof(GraphLayout), typeof(GraphLayoutView), null, propertyChanged: OnGraphLayoutChanged);

        public GraphLayout GraphLayout {
            get { return (GraphLayout)GetValue(GraphLayoutProperty); }
            set { SetValue(GraphLayoutProperty, value); }
        }

        private static void OnGraphLayoutChanged(BindableObject bindable, object oldValue, object newValue) {
            // ((GraphLayoutView)bindable).InvalidateSurface();
            //###iOS required:
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
                ((GraphLayoutView)bindable).InvalidateSurface();
            });

        }

        private void OnPaintCanvas(object sender, SKPaintSurfaceEventArgs e) {
            if (this.GraphLayout != null) {
                this.GraphLayout.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height);
            }
        }

        private Swipe pinchPan = Swipe.Id;    // remember transform at beginning or pinch or pan
        private float pinchAccum = 1.0f;                                // accumulate pinching transform
        private bool panJustStarted = false;    // prevent pan jerk in pinch-pan and pan-pinch-pan sequences when holding one finger down
        private double totalXinit = 0;          // allows smooth pinch-pan and pan-pinch continuation
        private double totalYinit = 0;          // while holding one finger down
        private float mysteriousScaling = 3.5f; // touch position seems scaled by this factor ?!?

        void OnTapped(object sender, EventArgs e) {
            if (GraphLayout == null) return;
            GraphLayout.pinchPan = Swipe.Id;
            this.InvalidateSurface();
        }

        void OnPanUpdated(object sender, PanUpdatedEventArgs e) {
            if (GraphLayout == null) return;
            if (e.StatusType == GestureStatus.Started) {
                panJustStarted = true; 
            } else if (e.StatusType == GestureStatus.Running) {
                if (panJustStarted) { // calibrate pan position to ignore inital jerk
                    pinchPan = GraphLayout.pinchPan;
                    totalXinit = e.TotalX;
                    totalYinit = e.TotalY;
                    panJustStarted = false;
                } else {
                    GraphLayout.pinchPan = pinchPan * new Swipe(1.0f, new SKPoint((float)(mysteriousScaling * (e.TotalX-totalXinit)), (float)(mysteriousScaling * (e.TotalY-totalYinit))));
                    this.InvalidateSurface();
                }
            } else if (e.StatusType == GestureStatus.Completed) {
                this.InvalidateSurface();
            }
        }

        void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e) {
            if (GraphLayout == null) return;
            if (e.Status == GestureStatus.Started) {
                pinchPan = GraphLayout.pinchPan;
                pinchAccum = 1.0f;
                GraphLayout.displayPinchOrigin = true;
            } else if (e.Status == GestureStatus.Running) {
                pinchAccum = pinchAccum * (float)e.Scale;
                pinchAccum = (float)Math.Max(0.1, pinchAccum);
                float pinchOriginX = (float)(e.ScaleOrigin.X * mysteriousScaling * this.Width);
                float pinchOriginY = (float)(e.ScaleOrigin.Y * mysteriousScaling * this.Height);
                GraphLayout.pinchPan = pinchPan * new Swipe(pinchAccum, new SKPoint((1 - pinchAccum) * pinchOriginX, (1 - pinchAccum) * pinchOriginY));
                GraphLayout.pinchOrigin = new SKPoint(pinchOriginX, pinchOriginY);
                this.InvalidateSurface();
            } else if (e.Status == GestureStatus.Completed) {
                GraphLayout.displayPinchOrigin = false;
                panJustStarted = true; // force pan to recalibrate if it is still running
                this.InvalidateSurface();
            }
        }

    }
}

