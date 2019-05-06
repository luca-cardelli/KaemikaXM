using System;
using System.Collections.Generic;
using SkiaSharp;
using QuickGraph;
using GraphSharp.Algorithms.Layout.Simple.Hierarchical;
using KaemikaXM.Pages;

namespace GraphSharp {

    //public class Timecourse {
    //    private object mutex;
    //    private List<ChartEntry> list;
    //    public Timecourse() {
    //        mutex = new object();
    //        list = new List<ChartEntry>();
    //    }

    //    public bool IsClear()  {
    //        lock (mutex) { return list.Count == 0; }
    //    }

    //    public void Add(ChartEntry entry) {
    //        lock (mutex) { list.Add(entry); }
    //    }

    //    private void Inner_Bounds(List<Series> seriesList, out float minX, out float maxX, out float minY, out float maxY) {
    //        if (list.Count() == 0) { minX = 0; maxX = 0; minY = 0; maxY = 0; return; }
    //        minX = float.MaxValue;
    //        maxX = float.MinValue;
    //        minY = float.MaxValue;
    //        maxY = float.MinValue;
    //        for (int i = 0; i < list.Count(); i++) {
    //            minX = Math.Min(minX, list[i].X);
    //            maxX = Math.Max(maxX, list[i].X);
    //            minY = Math.Min(minY, list[i].MinY(seriesList));
    //            maxY = Math.Max(maxY, list[i].MaxY(seriesList));
    //        }
    //    }

    //    private float XlocOfXvalInPlotarea(float Xval, float minX, float maxX, SKSize plotSize) {
    //        return (Xval / (maxX - minX)) * plotSize.Width;
    //    }
    //    private float XvalOfXlocInPlotarea(float Xloc, float minX, float maxX, SKSize plotSize) {
    //        return Xloc / plotSize.Width * (maxX - minX);
    //    }
    //    private float YlocOfYvalInPlotarea(float Yval, float minY, float maxY, SKSize plotSize) {  // the Y axis is flipped
    //        return ((maxY - Yval) * plotSize.Height) / (maxY - minY);
    //    }
    //    private float YvalOfYlocInPlotarea(float Yloc, float minY, float maxY, SKSize plotSize) {  // the Y axis is flipped
    //        return maxY - (Yloc * (maxY - minY)) / plotSize.Height;
    //    }
    //    private float YlocRangeOfYvalRangeInPlotarea(float YvalRange, float minY, float maxY, SKSize plotSize) {
    //        return (YvalRange * plotSize.Height) / (maxY - minY);
    //    }


    //    private void Inner_CalculatePoints(SKPoint plotOrigin, SKSize plotSize, float minX, float maxX, float minY, float maxY) {
    //        for (int i = 0; i < list.Count; i++) {
    //            ChartEntry entry = list[i];
    //            SKPoint[] points = new SKPoint[entry.Y.Length];
    //            float x = plotOrigin.X + XlocOfXvalInPlotarea(entry.X, minX, maxX, plotSize);
    //            for (int j = 0; j < entry.Y.Length; j++) {
    //                var y = plotOrigin.Y + YlocOfYvalInPlotarea(entry.Y[j], minY, maxY, plotSize);
    //                entry.Ypoint[j] = new SKPoint(x, y);
    //                entry.YpointRange[j] = (entry.Yrange[j] == 0) ? 0 : YlocRangeOfYvalRangeInPlotarea(entry.Yrange[j], minY, maxY, plotSize);
    //            }
    //        }
    //    }

    //    private void Inner_DrawLine(SKCanvas canvas, int seriesIndex, LineStyle lineStyle, SKColor color) {
    //        if (list.Count > 1) {
    //            using (var paint = new SKPaint {
    //                Style = SKPaintStyle.Stroke,
    //                Color = color,
    //                StrokeWidth = lineStyle == LineStyle.Thick ? 6 : 2,
    //                IsAntialias = true,
    //            }) {
    //                var path = new SKPath();
    //                path.MoveTo(list[0].Ypoint[seriesIndex]);
    //                for (int i = 0; i < list.Count; i++) path.LineTo(list[i].Ypoint[seriesIndex]);
    //                canvas.DrawPath(path, paint);
    //            }
    //        }
    //    }

    //    private void Inner_DrawLineRange(SKCanvas canvas, int seriesIndex, LineStyle lineStyle, SKColor color) {
    //        if (list.Count > 1) {
    //            using (var paint = new SKPaint {
    //                Style = SKPaintStyle.Fill,
    //                Color = color,
    //                IsAntialias = true,
    //            }) {
    //                var path = new SKPath();
    //                ChartEntry entry0 = list[0];
    //                SKPoint meanPoint0 = entry0.Ypoint[seriesIndex];
    //                float range0 = entry0.YpointRange[seriesIndex];
    //                path.MoveTo(meanPoint0.X, meanPoint0.Y + range0);
    //                path.LineTo(meanPoint0.X, meanPoint0.Y - range0);
    //                for (int i = 0; i < list.Count; i++) {
    //                    ChartEntry entry = list[i];
    //                    SKPoint meanPoint = entry.Ypoint[seriesIndex];
    //                    float range = entry.YpointRange[seriesIndex];
    //                    path.LineTo(meanPoint.X, meanPoint.Y - range);
    //                }
    //                for (int i = list.Count - 1; i >= 0; i--) {
    //                    ChartEntry entry = list[i];
    //                    SKPoint meanPoint = entry.Ypoint[seriesIndex];
    //                    float range = entry.YpointRange[seriesIndex];
    //                    path.LineTo(meanPoint.X, meanPoint.Y + range);
    //                }
    //                path.Close();
    //                canvas.DrawPath(path, paint);
    //            }
    //        }
    //    }

    //    private void Inner_DrawLines(SKCanvas canvas, List<Series> seriesList) {
    //        for (int j = 0; j < seriesList.Count(); j++) {
    //            Series series = seriesList[j];
    //            if (series.visible) {
    //                if (series.lineMode == LineMode.Line) {
    //                    Inner_DrawLine(canvas, j, series.lineStyle, series.color);
    //                } else if (series.lineMode == LineMode.Range && list.Count > 1) {
    //                    Inner_DrawLineRange(canvas, j, series.lineStyle, series.color);
    //                }
    //            }
    //        }
    //    }

    //    private void Inner_DrawXLabels(SKCanvas canvas, SKPoint plotOrigin, SKSize plotSize, float textHeight, SKColor axisTextColor, float minX, float maxX) {
    //        float Xloc = XlocOfXvalInPlotarea(0, minX, maxX, plotSize); // initialize to screen coordinates height of X=0
    //        do {
    //            float Xval = XvalOfXlocInPlotarea(Xloc, minX, maxX, plotSize);
    //            if (Xval < 0.0001 && Xval > -0.0001) Xval = 0;
    //            SKRect bounds = Inner_DrawLabel(canvas, Xval.ToString("G3"), plotOrigin.X + Xloc, plotOrigin.Y + plotSize.Height, true, textHeight, axisTextColor);
    //            Xloc += bounds.Width + 2 * textHeight; //using textHeigth for horizontal spacing
    //        } while (Xloc < plotSize.Width - plotOrigin.X);
    //    }

    //    private void Inner_DrawYLabels(SKCanvas canvas, SKPoint plotOrigin, SKSize plotSize, float textHeight, SKColor axisTextColor, float minY, float maxY) {
    //        float Yloc; // initialize to screen coordinates location of Y=0
    //        // draw >=0 labels going upwards from 0 or minY
    //        Yloc = YlocOfYvalInPlotarea(Math.Max(0,minY), minY, maxY, plotSize);
    //        while (Yloc > textHeight) {
    //            float Yval = YvalOfYlocInPlotarea(Yloc, minY, maxY, plotSize);
    //            if (Yval < 0.0001 && Yval > -0.0001) Yval = 0;
    //            Inner_DrawLabel(canvas, Yval.ToString("G3"), plotOrigin.X, plotOrigin.Y + Yloc, false, textHeight, axisTextColor);
    //            Yloc -= 3 * textHeight;
    //        }
    //        // draw <=0 labels goind downwards from 0 or maxY
    //        Yloc = YlocOfYvalInPlotarea(Math.Min(0,maxY), minY, maxY, plotSize);
    //        while (Yloc < plotSize.Height - 2 * textHeight) {
    //            float Yval = YvalOfYlocInPlotarea(Yloc, minY, maxY, plotSize);
    //            if (Yval < 0.0001 && Yval > -0.0001) Yval = 0;
    //            Inner_DrawLabel(canvas, Yval.ToString("G3"), plotOrigin.X, plotOrigin.Y + Yloc, false, textHeight, axisTextColor);
    //            Yloc += 3 * textHeight;
    //        }
    //    }

    //    public void DrawContent(SKCanvas canvas, SKPoint plotOrigin, SKSize plotSize, List<Series> seriesList, float textHeight, SKColor axisTextColor) {
    //        lock (mutex) {
    //            this.Inner_Bounds(seriesList, out float minX, out float maxX, out float minY, out float maxY);
    //            Inner_DrawXLabels(canvas, plotOrigin, plotSize, textHeight, axisTextColor, minX, maxX);
    //            Inner_DrawYLabels(canvas, plotOrigin, plotSize, textHeight, axisTextColor, minY, maxY);
    //            Inner_CalculatePoints(plotOrigin, plotSize, minX, maxX, minY, maxY);
    //            Inner_DrawLines(canvas, seriesList);
    //        }
    //    }

    //}

    public class GraphLayout {
        private string title = "";
        private float margin { get; set; } = 0;
        private float textHeight { get; set; } = 15.0f;
        private SKColor backgroundColor { get; set; } = SKColors.White;

        public GraphLayout(string title) {
            this.title = title;
        }

        public class Vertex {
            private readonly string label;
            public Vertex(string label) {
                this.label = label;
            }
            public string Label { get { return this.label; } }
        }

        public class Edge<Vertex> : IEdge<Vertex> {
            private readonly Vertex source;
            private readonly Vertex target;
            public Edge(Vertex source, Vertex target) {
                this.source = source;
                this.target = target;
            }
            public Vertex Source { get { return this.source; } }
            public Vertex Target { get { return this.target; } }
        }

        public void Draw(SKCanvas canvas, int width, int height) {
            canvas.Clear(this.backgroundColor);
            this.DrawContent(canvas, width, height);
        }

        private void DrawContent(SKCanvas canvas, int width, int height) {
            DrawGraph(canvas, PlotBounds(width, height));
        }

        private SKRect PlotBounds(int canvasWidth, int canvasHeight) {
            return new SKRect(margin, margin, canvasWidth - margin, canvasHeight - margin);
        }

        private SKRect GraphBounds(IDictionary<Vertex, SKPoint> vertexPositions, Dictionary<Vertex, SKSize> vertexSizes) {
            if (vertexPositions.Count == 0) return new SKRect(0, 0, 0, 0);
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (var kvp in vertexPositions)
            {
                minX = Math.Min(minX, kvp.Value.X - (vertexSizes[kvp.Key].Width / 2));
                maxX = Math.Max(maxX, kvp.Value.X + (vertexSizes[kvp.Key].Width / 2));
                minY = Math.Min(minY, kvp.Value.Y - (vertexSizes[kvp.Key].Height / 2));
                maxY = Math.Max(maxY, kvp.Value.Y + (vertexSizes[kvp.Key].Height / 2));
            }
            return new SKRect(minX, minY, maxX, maxY);
        }

        private static SKPoint CanvasPointOfGraphPoint(SKPoint graphPoint, SKRect graphBounds, SKRect plotBounds) {
            return new SKPoint(
                plotBounds.Left +                                          // left margin
                 +((graphPoint.X - graphBounds.Left)                      // X translated so that that X=0 is in the center
                    / (graphBounds.Right - graphBounds.Left))              // the X proportional location wrt the graph bounds, 
                 * (plotBounds.Right - plotBounds.Left),                   // the available plot width
                plotBounds.Top +                                           // top margin
                 +(graphPoint.Y / (graphBounds.Bottom - graphBounds.Top)) // the Y proportional location wrt the graph bounds
                 * (plotBounds.Bottom - plotBounds.Top)                    // the available plot height
                );
        }

        private static SKSize CanvasSizeOfGraphSize(SKSize graphSize, SKRect graphBounds, SKRect plotBounds) {
            return new SKSize(
                  graphSize.Width / (graphBounds.Right - graphBounds.Left)   // the proportional width wrt the graph width
                  * (plotBounds.Right - plotBounds.Left),                    // the available plot width
                  graphSize.Height / (graphBounds.Bottom - graphBounds.Top)  // the proportional height wrt the graph height
                  * (plotBounds.Bottom - plotBounds.Top)                     // the available plot height
                  );
        }

        private static float CanvasTextHeightOfGraphTextHeight(float graphTextHeight, SKRect graphBounds, SKRect plotBounds) {
            return
                  graphTextHeight / (graphBounds.Bottom - graphBounds.Top)  // the proportional height wrt the graph height
                  * (plotBounds.Bottom - plotBounds.Top);                   // the available plot height
        }

        //MeasureText    https://github.com/mono/SkiaSharp/issues/685
        //the return value is the width of the character, including the "padding" around the character to stop them overlapping when they are drawn.
        //the bounds value is the "tight" rectangle that the character wil fit in RELATIVE to the baseline.

        private SKRect CanvasDrawTextCentered(SKCanvas canvas, string text, SKPoint p, SKPaint paint) {
            var bounds = new SKRect();
            float width = paint.MeasureText(text, ref bounds);
            canvas.DrawText(text,
                p.X - (bounds.Right - bounds.Left) / 2 - bounds.Left,
                p.Y + (bounds.Bottom - bounds.Top) / 2 - bounds.Bottom,
                paint);
            return bounds;
        }

        private SKRect DrawText(SKCanvas canvas, string text, SKPoint p, float textSize, SKColor color, SKPoint translate, float zoom) {
            using (var paint = new SKPaint()) {
                paint.TextSize = textSize * zoom;
                paint.IsAntialias = true;
                paint.Color = color;
                paint.IsStroke = false;
                return CanvasDrawTextCentered(canvas, text, VectorStd.Zoom(p, zoom) + translate, paint);
            }
        }

        private void DrawNode(SKCanvas canvas, SKPoint p, SKSize s, float padding, SKColor color, SKPoint translate, float zoom) {
            using (var paint = new SKPaint()) {
                paint.TextSize = 10;
                paint.IsAntialias = true;
                paint.Color = color;
                paint.IsStroke = true;

                p = VectorStd.Zoom(p, zoom) + translate;
                s = VectorStd.Zoom(s, zoom);

                //canvas.DrawOval(p.X, p.Y, s.Width / 2, s.Height / 2, paint);
                //canvas.DrawRect(p.X - s.Width / 2, p.Y - s.Height / 2, s.Width, s.Height, paint);

                canvas.DrawRect(
                    p.X - s.Width / 2 - padding,
                    p.Y - s.Height / 2 - padding,
                    s.Width + 2 * padding,
                    s.Height + 2 * padding,
                    paint);
            }
        }

        private void DrawVertex(SKCanvas canvas, string text, SKPoint p, SKSize s, float textSize, float padding, SKColor textColor, SKColor nodeColor, SKPoint translate, float zoom) {
            SKRect bounds = DrawText(canvas, text, p, textSize, textColor, translate, zoom);
            DrawNode(canvas, p, s, padding, nodeColor, translate, zoom); // use s, not bounds, for bounding box
        }

        // Get the bounding rect of the text in all the vertices (used mostly for bounding hight)
        private SKRect MeasureVertexText(IEnumerable<Vertex> vertexes, float textHeight) {
            SKRect fitRect = new SKRect(0, 0, 0, 0);
            using (var paint = new SKPaint()) {
                paint.TextSize = textHeight;
                paint.IsAntialias = true;
                paint.Color = SKColors.Black;
                paint.IsStroke = false;
                var bounds = new SKRect();
                foreach (Vertex v in vertexes) {
                    float width = paint.MeasureText(v.Label, ref bounds);
                    fitRect.Left = Math.Min(fitRect.Left, bounds.Left);
                    fitRect.Top = Math.Min(fitRect.Top, bounds.Top);
                    fitRect.Right = Math.Max(fitRect.Right, bounds.Right);
                    fitRect.Bottom = Math.Max(fitRect.Bottom, bounds.Bottom);
                }
            }
            return fitRect;
        }

        // All vertexes have the same vertical size independently of text descenders etc.
        private Dictionary<Vertex, SKSize> ComputeUniformVertexSizes(IEnumerable<Vertex> vertexes, float textHeight, float padding) {
            SKRect fitRect = MeasureVertexText(vertexes, textHeight);
            Dictionary<Vertex, SKSize> vertexSizes = new Dictionary<Vertex, SKSize>();
            using (var paint = new SKPaint()) {
                paint.TextSize = textHeight;
                paint.IsAntialias = true;
                paint.Color = SKColors.Black;
                paint.IsStroke = false;
                var bounds = new SKRect();
                foreach (Vertex v in vertexes) {
                    float width = paint.MeasureText(v.Label, ref bounds);
                    vertexSizes[v] = new SKSize(
                        width + 2 * padding, // use width, not (bounds.Right - bounds.Left)
                        (fitRect.Bottom - fitRect.Top) + 2 * padding);
                }
            }
            return vertexSizes;
        }

        // Vertexes may have different vertical size depending on text descenders etc.
        private Dictionary<Vertex, SKSize> ComputeVertexSizes(IEnumerable<Vertex> vertexes, float textHeight, float padding) {
            Dictionary<Vertex, SKSize> vertexSizes = new Dictionary<Vertex, SKSize>();
            using (var paint = new SKPaint()) {
                paint.TextSize = textHeight;
                paint.IsAntialias = true;
                paint.Color = SKColors.Black;
                paint.IsStroke = false;
                var bounds = new SKRect();
                foreach (Vertex v in vertexes) {
                    float width = paint.MeasureText(v.Label, ref bounds);
                    vertexSizes[v] = new SKSize(
                        width + 2 * padding, // use width, not (bounds.Right - bounds.Left)
                        (bounds.Bottom - bounds.Top) + 2 * padding);
                }
            }
            return vertexSizes;
        }

        private void DrawGraph(SKCanvas canvas, SKRect plotBounds) {

            float nodePadding = textHeight / 2;

            // TEST GRAPH
            //var vertexes = new List<Vertex>() {
            //    new Vertex("AgA"),
            //    new Vertex("BBBBBBBBB"),
            //    new Vertex("C"),
            //    new Vertex("D"),
            //};
            //var edges = new List<Edge<Vertex>>() {
            //    new Edge<Vertex>(vertexes[0], vertexes[1]),
            //    new Edge<Vertex>(vertexes[0], vertexes[2]),
            //    new Edge<Vertex>(vertexes[1], vertexes[3]),
            //    new Edge<Vertex>(vertexes[2], vertexes[3]),
            //};
            var vertexes = new List<Vertex>() {
                new Vertex("A"), //0
                new Vertex("B1"),//1
                new Vertex("Bg2"),//2
                new Vertex("Ccccccccccccc1"),//3
                new Vertex("C2"),//4
                new Vertex("D1"),//5
                new Vertex("D2"),//6
                new Vertex("E1"),//7
                new Vertex("EEEEEEE2"),//8
                new Vertex("F"), //9
            };
            var edges = new List<Edge<Vertex>>() {
                new Edge<Vertex>(vertexes[0], vertexes[1]),
                new Edge<Vertex>(vertexes[0], vertexes[2]),
                new Edge<Vertex>(vertexes[1], vertexes[3]),
                new Edge<Vertex>(vertexes[1], vertexes[4]),
                new Edge<Vertex>(vertexes[2], vertexes[4]),
                new Edge<Vertex>(vertexes[3], vertexes[5]),
                new Edge<Vertex>(vertexes[4], vertexes[5]),
                new Edge<Vertex>(vertexes[4], vertexes[6]),
                new Edge<Vertex>(vertexes[5], vertexes[7]),
                new Edge<Vertex>(vertexes[6], vertexes[7]),
                new Edge<Vertex>(vertexes[6], vertexes[8]),
                new Edge<Vertex>(vertexes[7], vertexes[9]),
                new Edge<Vertex>(vertexes[8], vertexes[9]),
            };

            var g = new AdjacencyGraph<Vertex, Edge<Vertex>>();
            foreach (Vertex v in vertexes) g.AddVertex(v);
            foreach (Edge<Vertex> e in edges) g.AddEdge(e);

            // COMPUTE VERTEX SIZES
            Dictionary<Vertex, SKSize> vertexSizes = ComputeUniformVertexSizes(g.Vertices, textHeight, nodePadding);

            // COMPUTE GRAPH LAYOUT

            // Do not provide the opional vertextPosition dictionary parameters:
            // they are always thrown away and can be useful only to set its initial size of the separate output dictionary
            // Edge predicate cannot be null

            //var alg1 = new SugiyamaLayoutAlgorithm<Vertex, Edge<Vertex>, AdjacencyGraph<Vertex, Edge<Vertex>>>(
            //    visitedGraph: g, 
            //    vertexSizes: dict, 
            //    parameters:null, 
            //    edgePredicate:null);

            var alg = new EfficientSugiyamaLayoutAlgorithm<Vertex, Edge<Vertex>, AdjacencyGraph<Vertex, Edge<Vertex>>>(
                visitedGraph: g,
                parameters: new EfficientSugiyamaLayoutParameters {
                    LayerDistance = textHeight + 2 * nodePadding,
                    VertexDistance = textHeight + 2 * nodePadding,
                    EdgeRouting = SugiyamaEdgeRoutings.Traditional, // or Orthogonal
                },
                vertexSizes: vertexSizes);

            alg.Compute();

            var vertexPositions = alg.VertexPositions;
            var edgeRoutes = alg.EdgeRoutes;

            // DRAW GRAPH LAYOUT

            SKRect graphBounds = GraphBounds(vertexPositions, vertexSizes);
            float guessTextHeight = textHeight;
            float fitTextHeight = textHeight;

            // DRAW VERTICES

            float zoomX = (plotBounds.Right - plotBounds.Left) / (graphBounds.Right - graphBounds.Left);
            float zoomY = (plotBounds.Bottom - plotBounds.Top) / (graphBounds.Bottom - graphBounds.Top);
            float zoom = Math.Min(zoomX, zoomY);
            SKPoint translate = new SKPoint(
                //plotBounds.Left - graphBounds.Left * zoomX, 
                plotBounds.Left + (plotBounds.Right - plotBounds.Left)/2 - graphBounds.Left*zoomX,
                plotBounds.Top);

            foreach (var kvp in vertexPositions) {
                SKPoint canvasPoint = kvp.Value;
                SKSize canvasSize = vertexSizes[kvp.Key];
                DrawVertex(canvas, kvp.Key.Label, canvasPoint, canvasSize, fitTextHeight, nodePadding, SKColors.Red, SKColors.Green, translate, zoom);
            }

            // DRAW EDGES

            // ###

            // DEBUG

            MainTabbedPage.theGraphLayoutPage.SetText(Report(graphBounds, plotBounds, guessTextHeight, fitTextHeight, vertexPositions, edgeRoutes));
        }

        public static string Report(SKRect graphBounds, SKRect plotBounds, float guessTextHeight, float fitTextHeight,
            IDictionary<Vertex, SKPoint> vertexPositions, IDictionary<Edge<Vertex>, SKPoint[]> edgeRoutes) {
            string s = "";
            s += "GraphBounds " + "{Left=" + graphBounds.Left + ", Top=" + graphBounds.Top + ", Right= " + graphBounds.Right + ", Bottom=" + graphBounds.Bottom + "}" + Environment.NewLine;
            s += "PlotBounds " + "{Left=" + plotBounds.Left + ", Top=" + plotBounds.Top + ", Right= " + plotBounds.Right + ", Bottom=" + plotBounds.Bottom + "}" + Environment.NewLine;
            s += "GuessTextHeight = " + guessTextHeight.ToString() + Environment.NewLine;
            s += "FitTextHeight = " + fitTextHeight.ToString() + Environment.NewLine;
            foreach (var kvp in vertexPositions) {
                s += kvp.Key.Label + ": " + kvp.Value.X.ToString() + ", " + kvp.Value.Y.ToString()
                    + " = " + CanvasPointOfGraphPoint(kvp.Value, graphBounds, plotBounds).ToString()
                + Environment.NewLine;
            }
            return s;
        }

    }
}


