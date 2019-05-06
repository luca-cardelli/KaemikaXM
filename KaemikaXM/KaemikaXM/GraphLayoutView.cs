// Copyright (c) Aloïs DENIEL. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace GraphSharp
{
    using Xamarin.Forms;
    using SkiaSharp.Views.Forms;

    public class GraphLayoutView : SKCanvasView
    {
        public GraphLayoutView()
        {
            this.BackgroundColor = Color.Transparent;
            this.PaintSurface += OnPaintCanvas;
        }

        public static readonly BindableProperty GraphLayoutProperty = BindableProperty.Create(nameof(GraphLayout), typeof(GraphLayout), typeof(GraphLayoutView), null, propertyChanged: OnGraphLayoutChanged);

        public GraphLayout GraphLayout
        {
            get { return (GraphLayout)GetValue(GraphLayoutProperty); }
            set { SetValue(GraphLayoutProperty, value); }
        }

        private static void OnGraphLayoutChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((GraphLayoutView)bindable).InvalidateSurface();
        }

        private void OnPaintCanvas(object sender, SKPaintSurfaceEventArgs e)
        {
            if (this.GraphLayout != null)
            {
                this.GraphLayout.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height);
            }
        }
    }
}
