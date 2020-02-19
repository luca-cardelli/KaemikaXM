using System;
using System.Windows.Forms;
using Kaemika;
using SkiaSharp;

namespace KaemikaWPF {

    public class KScoreSKControl : SkiaSharp.Views.Desktop.SKControl, KTouchable {

        private static KScoreSKControl scoreControl = null; // The only KScoreSKControl, same as "this", but accessible from static methods

        public KScoreSKControl() : base() {
            scoreControl = this;
            scoreControl.Location = new System.Drawing.Point(0, 0);
            KScoreHandler.Register(scoreControl);
        }

        // Methods accessing scoreControl

        public static void SetSize(System.Drawing.Size size) {
            if (scoreControl != null) scoreControl.Size = size;
        }

        public /* KTouchable Interface */ void DoInvalidate() {
            if (!scoreControl.InvokeRequired) {
                SetSize(WinGui.winGui.ScoreSize());
                scoreControl.Invalidate();
                scoreControl.Update();
            } else scoreControl.Invoke((Action) delegate { DoInvalidate(); });
        }

        public /* KTouchable Interface */ void DoHide() {
            if (!scoreControl.InvokeRequired) {
                WinGui.winGui.ScoreHide();
            } else scoreControl.Invoke((Action) delegate { DoHide(); });
        }

        public /* KTouchable Interface */ void DoShow() {
            if (!scoreControl.InvokeRequired) {
                WinGui.winGui.ScoreShow();
            } else  scoreControl.Invoke((Action) delegate { DoShow(); });
        }

        // Implement this to draw on the canvas.
        protected override void OnPaintSurface(SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e) {
            base.OnPaintSurface(e);
            KScoreHandler.Draw(new SKPainter(e.Surface.Canvas), Location.X, Location.Y, e.Info.Width, e.Info.Height);
        }

        // MOUSE HANDLING EVENTS

        private Action<SKPoint> onTouchTapOrMouseMove;
        private Action<SKPoint> onTouchDoubletapOrMouseClick;
        private Action<SKPoint, SKPoint> onTouchSwipeOrMouseDrag;
        private Action<SKPoint, SKPoint> onTouchSwipeOrMouseDragEnd;

        public /* KTouchable Interface */ void OnTouchTapOrMouseMove(Action<SKPoint> action) { this.onTouchTapOrMouseMove = action; }
        public /* KTouchable Interface */ void OnTouchDoubletapOrMouseClick(Action<SKPoint> action) { this.onTouchDoubletapOrMouseClick = action; }
        public /* KTouchable Interface */ void OnTouchSwipeOrMouseDrag(Action<SKPoint, SKPoint> action) { this.onTouchSwipeOrMouseDrag = action;  }
        public /* KTouchable Interface */ void OnTouchSwipeOrMouseDragEnd(Action<SKPoint, SKPoint> action) { this.onTouchSwipeOrMouseDragEnd = action; }

        private static SKPoint mouseDownPoint;
        private static SKPoint mouseMovePoint;
        private static bool mouseDown = false;
        private static bool dragging = false;

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            KGui.kControls.CloseOpenMenu();
            mouseDownPoint = new SKPoint(e.Location.X, e.Location.Y);
            mouseDown = true;
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            SKPoint location = new SKPoint(e.Location.X, e.Location.Y);
            if (location == mouseMovePoint) return;
            if (mouseDown) {
                dragging = true;
                this.onTouchSwipeOrMouseDrag?.Invoke(mouseDownPoint, location);
            } else {
                this.onTouchTapOrMouseMove?.Invoke(location);
            }
            mouseMovePoint = location;
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            SKPoint location = new SKPoint(e.Location.X, e.Location.Y);
            if (dragging) {
                this.onTouchSwipeOrMouseDragEnd?.Invoke(mouseDownPoint, location);
                dragging = false;
            } else { // Mouse click
                this.onTouchDoubletapOrMouseClick?.Invoke(location);
            }
            mouseDown = false;
        }

        // IF WE WANT TO HANDLE MOUSE DRAGGING AND SCROLLWHEEL ZOOMING
        // we should do it inside here automatically, without registering with the KTouchable interface

        //// Don't use OnMouseClick event, it misclicks: MouseUp at any distance from MouseDown is counted as a click. 
        //// Moreover there is double click lag. Hence we handle up and down transitions ourselves.
        ////protected override void OnMouseClick(MouseEventArgs e) {
        ////    base.OnMouseClick(e);
        ////    if (this.onTouchDoubletapOrMouseClick != null) 
        ////        this.onTouchDoubletapOrMouseClick(new SKPoint(e.Location.X, e.Location.Y));
        ////}

        //protected override void OnMouseDoubleClick(MouseEventArgs e) {
        //    base.OnMouseDoubleClick(e);
        //    KGui.kControls.CloseOpenMenu();
        //    this.onTouchTwofingertapOrMouseDoublelick?.Invoke(new SKPoint(e.Location.X, e.Location.Y));
        //    //### Zoom Reset
        //}

        //protected override void OnMouseWheel(MouseEventArgs e) {
        //    base.OnMouseWheel(e);
        //    if (this.onTouchPinchOrMouseZoom != null) {
        //        //var xMin = xAxis.ScaleView.ViewMinimum;
        //        //var xMax = xAxis.ScaleView.ViewMaximum;
        //        //var yMin = yAxis.ScaleView.ViewMinimum;
        //        //var yMax = yAxis.ScaleView.ViewMaximum;
        //        //const double scale = 0.2;
        //        //var posX = e.Location.X;
        //        //var posY = e.Location.Y;
        //        //if (posX < xMin || posX > xMax || posY < yMin || posY > yMax) return;
        //        //if (e.Delta < 0) { // Scrolled down: zoom out around mouse position              
        //        //    var posXStart = xMin - (posX - xMin) * scale;
        //        //    var posXFinish = xMax + (xMax - posX) * scale;
        //        //    var posYStart = yMin - (posY - yMin) * scale;
        //        //    var posYFinish = yMax + (yMax - posY) * scale;
        //        //    if (posXStart < xAxis.Minimum) posXStart = xAxis.Minimum;
        //        //    if (posXFinish > xAxis.Maximum) posXFinish = xAxis.Maximum;
        //        //    if (posYStart < yAxis.Minimum) posYStart = yAxis.Minimum;
        //        //    if (posYFinish > yAxis.Maximum) posYFinish = yAxis.Maximum;
        //        //    xAxis.ScaleView.Zoom(posXStart, posXFinish);
        //        //    yAxis.ScaleView.Zoom(posYStart, posYFinish);
        //        //    if (posXStart == xAxis.Minimum && posXFinish == xAxis.Maximum) xAxis.ScaleView.ZoomReset();
        //        //    if (posYStart == yAxis.Minimum && posYFinish == yAxis.Maximum) yAxis.ScaleView.ZoomReset();
        //        //} else if (e.Delta > 0) { // Scrolled up: zoom in around mouse position
        //        //    var posXStart = xMin + (posX - xMin) * scale;
        //        //    var posXFinish = xMax - (xMax - posX) * scale;
        //        //    var posYStart = yMin + (posY - yMin) * scale;
        //        //    var posYFinish = yMax - (yMax - posY) * scale;
        //        //    xAxis.ScaleView.Zoom(posXStart, posXFinish);
        //        //    yAxis.ScaleView.Zoom(posYStart, posYFinish);
        //        //}                
        //        //this.onTouchPinchOrMouseZoom(scaleFactor);
        //    }
    }

}
