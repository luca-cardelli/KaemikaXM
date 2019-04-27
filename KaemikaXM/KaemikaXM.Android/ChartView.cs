namespace Microcharts.Droid
{
    using System;
    using Android.Content;
    using Android.Util;
    using Android.Runtime;
    using SkiaSharp.Views.Android;

    public class ChartView : SKCanvasView
    {
        #region Constructors

        public ChartView(Context context) : base(context)
        {
            this.PaintSurface += OnPaintCanvas;
        }

        public ChartView(Context context, IAttributeSet attributes) : base(context, attributes)
        {
            this.PaintSurface += OnPaintCanvas;
        }

        public ChartView(Context context, IAttributeSet attributes, int defStyleAtt) : base(context, attributes, defStyleAtt)
        {
            this.PaintSurface += OnPaintCanvas;
        }

        public ChartView(IntPtr ptr, JniHandleOwnership jni) : base(ptr, jni)
        {
            this.PaintSurface += OnPaintCanvas;
        }

        #endregion

        private Microcharts.Chart chart;

        public Microcharts.Chart Chart
        {
            get => this.chart;
            set
            {
                if (this.chart != value)
                {
                    this.chart = value;
                    this.Invalidate();
                }
            }
        }

        private void OnPaintCanvas(object sender, SKPaintSurfaceEventArgs e)
        {
            if (this.chart != null)
            {
                this.chart.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height);
            }
        }
    }
}
