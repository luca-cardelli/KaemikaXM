using System;
using System.Collections.Generic;
using SkiaSharp;
using QuickGraph;
using GraphSharp.Algorithms.Layout.Simple.Hierarchical;
//using GraphSharp.Algorithms.Layout.Simple.FDP;
using Kaemika;
using KaemikaXM.Pages;

namespace GraphSharp {

    public class ComputeLayout {

        public IDictionary<Vertex, SKPoint> vertexPositions;
        public IDictionary<Kaemika.Edge<Vertex>, SKPoint[]> edgeRoutes;
        public Dictionary<Vertex, SKSize> vertexSizes;
        public float nodePadding; // space between the node border and the text inside
        public float nodeHeight;

        public ComputeLayout(AdjacencyGraph<Vertex, Kaemika.Edge<Vertex>> GRAPH, float textHeight, float nodePadding, float nodeHeight) {
            this.nodePadding = nodePadding;
            this.nodeHeight = nodeHeight;
            this.vertexSizes = ComputeUniformVertexSizes(GRAPH.Vertices, textHeight, nodePadding);
            var alg = new EfficientSugiyamaLayoutAlgorithm<Vertex, Kaemika.Edge<Vertex>, AdjacencyGraph<Vertex, Kaemika.Edge<Vertex>>>(
                visitedGraph: GRAPH,
                parameters: new EfficientSugiyamaLayoutParameters {
                    LayerDistance = nodeHeight, VertexDistance = nodeHeight,
                    EdgeRouting = SugiyamaEdgeRoutings.Traditional, // Orthogonal is no good
                },
                vertexSizes: vertexSizes);
            alg.Compute(); // COMPUTE LAYOUT
            this.vertexPositions = alg.VertexPositions;
            this.edgeRoutes = alg.EdgeRoutes;
        }

        public SKRect GraphBounds() {
            if (this.vertexPositions.Count == 0) return new SKRect(0, 0, 0, 0);
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (var kvp in vertexPositions) {
                minX = Math.Min(minX, kvp.Value.X - (this.vertexSizes[kvp.Key].Width / 2));
                maxX = Math.Max(maxX, kvp.Value.X + (this.vertexSizes[kvp.Key].Width / 2));
                minY = Math.Min(minY, kvp.Value.Y - (this.vertexSizes[kvp.Key].Height / 2));
                maxY = Math.Max(maxY, kvp.Value.Y + (this.vertexSizes[kvp.Key].Height / 2));
            }
            return new SKRect(minX, minY, maxX, maxY);
        }

        // Get the bounding rect of the text found in all the vertices (used mostly for bounding hight)
        private SKRect MeasureVertexText(IEnumerable<Vertex> vertexes, float textHeight){
            SKRect fitRect = new SKRect(0, 0, 0, 0);
            using (var paint = new SKPaint()) {
                paint.TextSize = textHeight; paint.IsAntialias = true; paint.Color = SKColors.Black; paint.IsStroke = false;
                var bounds = new SKRect();
                foreach (Vertex v in vertexes) {
                    if (v is Vertex_Label) {
                        float width = GraphLayout.PaintMeasureText(paint, (v as Vertex_Label).Label, ref bounds);
                        fitRect.Left = Math.Min(fitRect.Left, bounds.Left);
                        fitRect.Top = Math.Min(fitRect.Top, bounds.Top);
                        fitRect.Right = Math.Max(fitRect.Right, bounds.Right);
                        fitRect.Bottom = Math.Max(fitRect.Bottom, bounds.Bottom);
                    }
                }
            }
            return fitRect;
        }

        // All label-vertexes are assigned the same vertical size independently of text descenders etc.
        private Dictionary<Vertex, SKSize> ComputeUniformVertexSizes(IEnumerable<Vertex> vertexes, float textHeight, float padding) {
            SKRect fitRect = MeasureVertexText(vertexes, textHeight);
            Dictionary<Vertex, SKSize> vertexSizes = new Dictionary<Vertex, SKSize>();
            using (var paint = new SKPaint()) {
                paint.TextSize = textHeight; paint.IsAntialias = true; paint.Color = SKColors.Black; paint.IsStroke = false;
                var bounds = new SKRect();
                foreach (Vertex v in vertexes) {
                    if (v is Vertex_Label) {
                        float width = GraphLayout.PaintMeasureText(paint, (v as Vertex_Label).Label, ref bounds);
                        vertexSizes[v] = new SKSize(
                            width + 2 * padding, // use width, not (bounds.Right - bounds.Left)
                            (fitRect.Bottom - fitRect.Top) + 2 * padding);
                    } else if (v is Vertex_Rectangle) {
                        SKSize size = (v as Vertex_Rectangle).Size;
                        vertexSizes[v] = new SKSize(textHeight * size.Width, textHeight * size.Height);
                    } else if (v is Vertex_Routing) {
                        vertexSizes[v] = new SKSize(0, 0);
                    }

                }
            }
            return vertexSizes;
        }

        //// Vertexes may be assigned different vertical size depending on text descenders etc.
        //private Dictionary<Vertex, SKSize> ComputeVertexSizes(IEnumerable<Vertex> vertexes, float textHeight, float padding) {
        //    Dictionary<Vertex, SKSize> vertexSizes = new Dictionary<Vertex, SKSize>();
        //    using (var paint = new SKPaint()) {
        //        paint.TextSize = textHeight; paint.IsAntialias = true; paint.Color = SKColors.Black; paint.IsStroke = false;
        //        var bounds = new SKRect();
        //        foreach (Vertex v in vertexes) {
        //            if (v is Vertex_Label) {
        //                float width = PaintMeasureText(paint, (v as Vertex_Label).Label, ref bounds);
        //                vertexSizes[v] = new SKSize(
        //                    width + 2 * padding, // use width, not (bounds.Right - bounds.Left)
        //                    (bounds.Bottom - bounds.Top) + 2 * padding);
        //            } else if (v is Vertex_Rectangle) {
        //                SKSize size = (v as Vertex_Rectangle).Size;
        //                vertexSizes[v] = new SKSize(textHeight * size.Width, textHeight * size.Height);
        //            } else if (v is Vertex_Routing) {
        //                vertexSizes[v] = new SKSize(0, 0);
        //            }
        //        }
        //    }
        //    return vertexSizes;
        //}
    }

    public class GraphLayout {
        private string title = "";
        private float margin { get; set; } = 20;
        private float textHeight { get; set; } = 50.0f; // we rescale anyway to fit available space, but a bigger number may help with precision?
        private SKColor backgroundColor { get; set; } = SKColors.White;
        public AdjacencyGraph<Vertex, Kaemika.Edge<Vertex>> GRAPH = null;        // just the assembled graph
        private ComputeLayout layoutInfo;                                    // the layout information for the graph

        public GraphLayout(string title, AdjacencyGraph<Vertex, Kaemika.Edge<Vertex>> graph) {
            this.title = title;
            this.GRAPH = graph;
            float nodePadding = textHeight / 2; // space between the node border and the text inside
            float nodeHeight = textHeight + 2 * nodePadding;
            this.layoutInfo = new ComputeLayout(GRAPH, textHeight, nodePadding, nodeHeight);   // run the layout algorithm ;
        }

        public static GraphLayout MessageGraph(string a, string b, string c) {
            var graph = new AdjacencyGraph<Vertex, Kaemika.Edge<Vertex>>();
            Vertex dummy1 = new Vertex_Label(a);
            Vertex dummy2 = new Vertex_Label(b);
            Vertex dummy3 = new Vertex_Label(c);
            graph.AddVertex(dummy1);
            graph.AddVertex(dummy2);
            graph.AddVertex(dummy3);
            graph.AddEdge(new Kaemika.Edge<Vertex>(dummy1, dummy2));
            graph.AddEdge(new Kaemika.Edge<Vertex>(dummy2, dummy3));
            return new GraphLayout("Message", graph);
        }

        public Swipe pinchPan = Swipe.Id;
        public bool displayPinchOrigin = false;
        public SKPoint pinchOrigin;

        public void Draw(SKCanvas canvas, int width, int height) {
            if (MainTabbedPage.theOutputPage.currentOutputAction.kind != OutputKind.Graph) return;
            canvas.Clear(this.backgroundColor);
            if (GRAPH == null) return;

            if (displayPinchOrigin) CanvasDrawCircle(canvas, pinchOrigin, 20, false, SKColors.LightGray);

            SKRect plotBounds = new SKRect(margin, margin, width - margin, height - margin);
            SKRect graphBounds = layoutInfo.GraphBounds();

            float zoomX = (plotBounds.Right - plotBounds.Left) / (graphBounds.Right - graphBounds.Left);
            float zoomY = (plotBounds.Bottom - plotBounds.Top) / (graphBounds.Bottom - graphBounds.Top);
            float zoom = Math.Min(zoomX, zoomY);
            SKPoint translate = new SKPoint(plotBounds.MidX - zoom * graphBounds.MidX, plotBounds.MidY - zoom * graphBounds.MidY);
            Swipe swipe = new Swipe(zoom, translate) * pinchPan;

            foreach (var kvp in layoutInfo.vertexPositions)
                DrawVertex(canvas, kvp.Key, kvp.Value, layoutInfo.vertexSizes[kvp.Key], textHeight, layoutInfo.nodePadding, SKColors.Red, SKColors.Green, swipe);

            foreach (var edge in GRAPH.Edges)
                DrawSplineEdge(canvas, edge, layoutInfo.nodeHeight, textHeight, SKColors.Black, swipe);

            //DEBUG
            //CanvasDrawRect(canvas, plotBounds, true, SKColors.Cyan);
            //CanvasDrawRect(canvas, swipe % graphBounds, true, SKColors.Green);
        }

        private void DrawVertex(SKCanvas canvas, Vertex vertex, SKPoint p, SKSize s, float textSize, float padding, SKColor textColor, SKColor nodeColor, Swipe swipe)
        {
            if (vertex is Vertex_Label)  {
                SKRect bounds = DrawLabel(canvas, (vertex as Vertex_Label).Label, p, textSize, textColor, swipe);
                DrawNode(canvas, p, s, padding, nodeColor, swipe); // use s, not bounds, for bounding box
            } else if (vertex is Vertex_Rectangle) {
                SKSize size = (vertex as Vertex_Rectangle).Size;
                float width = textSize * size.Width;
                float height = textSize * size.Height;
                CanvasDrawRect(canvas, swipe % new SKRect(p.X-width/2, p.Y- height / 2, p.X + width / 2, p.Y + height / 2), !(vertex as Vertex_Rectangle).Filled, nodeColor);
            } else if (vertex is Vertex_Routing) {
            }
        }

        private SKRect DrawLabel(SKCanvas canvas, string text, SKPoint p, float textSize, SKColor color, Swipe swipe) {
            return GraphLayout.CanvasDrawTextCentered(canvas, text, swipe % p, swipe % textSize, color, false);
        }

        private void DrawNode(SKCanvas canvas, SKPoint p, SKSize s, float padding, SKColor color, Swipe swipe) {
            CanvasDrawRoundRect(canvas, swipe % p, swipe % s, swipe % padding, color);
        }

        private void DrawEdge(SKCanvas canvas, Kaemika.Edge<Vertex> edge, float nodeHeight, float textSize, SKColor color, Swipe swipe) {
            bool spline = false; bool arc = false;
            using (var paint = new SKPaint()) {
                paint.TextSize = 10.0f; paint.IsAntialias = true; paint.Color = color;

                SKPoint source = layoutInfo.vertexPositions[edge.Source];
                SKPoint target = layoutInfo.vertexPositions[edge.Target];
                SKSize sourceSize = layoutInfo.vertexSizes[edge.Source];
                SKSize targetSize = layoutInfo.vertexSizes[edge.Target];

                if (layoutInfo.edgeRoutes == null) layoutInfo.edgeRoutes = new Dictionary<Kaemika.Edge<Vertex>, SKPoint[]>();
                SKPoint[] route = (layoutInfo.edgeRoutes.ContainsKey(edge)) ? layoutInfo.edgeRoutes[edge] : new SKPoint[] { };

                var routePath = new SKPath(); paint.IsStroke = true;
                SKPoint initialTarget = (route.Length == 0) ? target : route[0];
                SKPoint firstSource = PointOnRectangle(source, initialTarget, sourceSize.Width / 2, sourceSize.Height / 2);
                routePath.MoveTo(swipe % firstSource);
                SKPoint lastSource = firstSource;
                foreach (var nextTarget in route) {
                    if (spline) {
                        (SKPoint control, SKPoint nextControl) = SplineControls(lastSource, nextTarget, new SKSize(textSize, textSize));
                        routePath.CubicTo(swipe % control, swipe % nextControl, swipe % nextTarget);
                    } else routePath.LineTo(swipe % nextTarget);
                    lastSource = nextTarget;
                }
                SKPoint lastTarget = PointOnRectangle(target, lastSource, targetSize.Width / 2, targetSize.Height / 2);
                SKPoint firstTarget = (route.Length == 0) ? lastTarget : initialTarget;
                if (spline) {
                    (SKPoint control, SKPoint nextControl) = SplineControls(lastSource, lastTarget, new SKSize(textSize, textSize));
                    routePath.CubicTo(swipe % control, swipe % nextControl, swipe % lastTarget);
                } else if (arc && route.Length == 0) routePath.ArcTo(swipe % lastSource, swipe % lastTarget, swipe%textSize); // see Postscript arct
                else routePath.LineTo(swipe % lastTarget);
                canvas.DrawPath(routePath, paint);

                SKPoint arrowHeadBase = lastTarget;

                //SKPoint nextSource = source;
                //foreach (var nextTarget in route) {
                //    if (nextSource == source) nextSource = PointOnRectangle(source, nextTarget, sourceSize.Width / 2, sourceSize.Height / 2);
                //    canvas.DrawLine(swipe % nextSource, swipe % nextTarget, paint);
                //    nextSource = nextTarget;
                //}
                //SKPoint lastSource = (nextSource != source) ? nextSource : PointOnRectangle(source, target, sourceSize.Width / 2, sourceSize.Height / 2);
                //SKPoint lastTarget = PointOnRectangle(target, lastSource, targetSize.Width / 2, targetSize.Height / 2);
                //canvas.DrawLine(swipe % lastSource, swipe % lastTarget, paint);

                //SKPoint pointOnLine = lastTarget;

                if (edge.Directed != Directed.No) { // draw arrowhead, update arrowHeadBase
                    VectorStd lineVector = VectorStd.DifferenceVector(lastTarget, lastSource);
                    float lineLength = lineVector.Length;
                    // calculate point at base of arrowhead
                    float arrowWidth = nodeHeight / 6;
                    float tPointOnLine = (float)(arrowWidth / (2 * (Math.Tan(120) / 2) * lineLength));
                    arrowHeadBase = lastTarget + (-tPointOnLine * lineVector);
                    VectorStd normalVector = new VectorStd(-lineVector.Y, lineVector.X);
                    float tNormal = arrowWidth / (2 * lineLength);
                    SKPoint leftPoint = arrowHeadBase + tNormal * normalVector;
                    SKPoint rightPoint = arrowHeadBase + -tNormal * normalVector;
                    if (edge.Directed == Directed.Solid) {
                        var arrowPath = new SKPath(); paint.IsStroke = false;
                        arrowPath.MoveTo(swipe % leftPoint); arrowPath.LineTo(swipe % lastTarget);
                        arrowPath.LineTo(swipe % rightPoint); arrowPath.Close();
                        canvas.DrawPath(arrowPath, paint);
                    }
                    if (edge.Directed == Directed.Pointy) {
                        var arrowPath = new SKPath(); paint.IsStroke = true;
                        arrowPath.MoveTo(swipe % leftPoint); arrowPath.LineTo(swipe % lastTarget);
                        arrowPath.LineTo(swipe % rightPoint);
                        canvas.DrawPath(arrowPath, paint);
                    }
                }

                if (edge.Label != null) { // draw label
                    var saveTextSize = paint.TextSize;
                    paint.TextSize = swipe % textSize / 3;
                    SKPoint labelTarget = (route.Length == 0) ? arrowHeadBase : firstTarget;
                    GraphLayout.CanvasDrawTextCentered(canvas, edge.Label, swipe % new SKPoint((firstSource.X + labelTarget.X) / 2, (firstSource.Y + labelTarget.Y) / 2), paint, true);
                    paint.TextSize = saveTextSize;
                    //var path = new SKPath(); paint.IsStroke = false; 
                    //path.MoveTo(swipe % firstSource);
                    //path.LineTo(swipe % ((route.Length == 0) ? arrowHeadBase : firstTarget));
                    //path.Close(); // so the text wraps back around the closed single-line path
                    //paint.TextSize = swipe % textSize / 3;
                    //canvas.DrawTextOnPath(edge.Label, path, new SKPoint(paint.TextSize/2, -paint.TextSize/2), paint);
                 }
            }
        }

        private (SKPoint edgeSource, SKPoint edgeTarget) AddEdgePath(List<SKPoint> edgePath, Kaemika.Edge<Vertex> edge) {
            SKPoint source = layoutInfo.vertexPositions[edge.Source];
            SKPoint target = layoutInfo.vertexPositions[edge.Target];
            SKSize sourceSize = layoutInfo.vertexSizes[edge.Source];
            SKSize targetSize = layoutInfo.vertexSizes[edge.Target];

            if (layoutInfo.edgeRoutes == null) layoutInfo.edgeRoutes = new Dictionary<Kaemika.Edge<Vertex>, SKPoint[]>();
            SKPoint[] route = (layoutInfo.edgeRoutes.ContainsKey(edge)) ? layoutInfo.edgeRoutes[edge] : new SKPoint[] { };

            SKPoint initialTarget = (route.Length == 0) ? target : route[0];
            SKPoint firstSource = PointOnRectangle(source, initialTarget, sourceSize.Width / 2, sourceSize.Height / 2);
            edgePath.Add(firstSource);
            SKPoint lastSource = firstSource;
            foreach (var nextTarget in route) {
                edgePath.Add(nextTarget);
                lastSource = nextTarget;
            }
            SKPoint lastTarget = PointOnRectangle(target, lastSource, targetSize.Width / 2, targetSize.Height / 2);
            SKPoint firstTarget = (route.Length == 0) ? lastTarget : initialTarget;
            if (!(edge.Target is Vertex_Routing)) edgePath.Add(lastTarget);
            return (firstSource, firstTarget);
        }

        private void DrawSplineEdge(SKCanvas canvas, Kaemika.Edge<Vertex> edge, float nodeHeight, float textSize, SKColor color, Swipe swipe) {
            if (edge.Source is Vertex_Routing) return; // this edge it will be drawn as part of another edge
            List<SKPoint> edgePath = new List<SKPoint>();
            Kaemika.Edge<Vertex> routedEdge = edge;
            (SKPoint firstSource, SKPoint firstTarget) = AddEdgePath(edgePath, routedEdge);
            while (routedEdge.Target is Vertex_Routing) {
                routedEdge = (routedEdge.Target as Vertex_Routing).toEdge;
                AddEdgePath(edgePath, routedEdge);
            }
            edgePath.Insert(0, edgePath[0]); // duplicate first point for spline
            SKPoint ultimate = edgePath[edgePath.Count - 1];
            SKPoint penultimate = edgePath[edgePath.Count - 2];
            edgePath.Insert(edgePath.Count, ultimate); // duplicate last point for spline
            List<SKPoint> controlPoints = ControlPoints(edgePath);
            SKPath path = AddBeziers(new SKPath(), controlPoints.ToArray(), swipe);

            using (var paint = new SKPaint()) {
                paint.TextSize = 10.0f; paint.IsAntialias = true; paint.Color = color; paint.IsStroke = true;
                canvas.DrawPath(path, paint);

                SKPoint arrowHeadBase = ultimate;
                if (routedEdge.Directed != Directed.No) { // draw arrowhead on last routedEdge segment, update arrowHeadBase
                    VectorStd lineVector = VectorStd.DifferenceVector(ultimate, penultimate);
                    float lineLength = lineVector.Length;
                    // calculate point at base of arrowhead
                    float arrowWidth = nodeHeight / 6;
                    float tPointOnLine = (float)(arrowWidth / (2 * (Math.Tan(120) / 2) * lineLength));
                    VectorStd arrowReverseVector = -tPointOnLine * lineVector;
                    arrowHeadBase = ultimate + arrowReverseVector;
                    SKPoint arrowHeadMid = ultimate + (0.5F * arrowReverseVector);
                    VectorStd normalVector = new VectorStd(-lineVector.Y, lineVector.X);
                    float tNormal = arrowWidth / (2 * lineLength);
                    SKPoint leftPoint = arrowHeadBase + tNormal * normalVector;
                    SKPoint rightPoint = arrowHeadBase + -tNormal * normalVector;
                    if (routedEdge.Directed == Directed.Solid) {
                        var arrowPath = new SKPath(); paint.IsStroke = false;
                        arrowPath.MoveTo(swipe % leftPoint); arrowPath.LineTo(swipe % ultimate);
                        arrowPath.LineTo(swipe % rightPoint); arrowPath.Close();
                        canvas.DrawPath(arrowPath, paint);
                    }
                    if (routedEdge.Directed == Directed.Pointy) {
                        var arrowPath = new SKPath(); paint.IsStroke = true;
                        arrowPath.MoveTo(swipe % leftPoint); arrowPath.LineTo(swipe % ultimate);
                        arrowPath.LineTo(swipe % rightPoint);
                        canvas.DrawPath(arrowPath, paint);
                    }
                    if (routedEdge.Directed == Directed.Ball) {
                        var arrowPath = new SKPath(); paint.IsStroke = false;
                        canvas.DrawCircle(swipe % arrowHeadMid, swipe % (arrowReverseVector.Length/2), paint);
                    }
                }
                // if (routedEdge.Directed == Directed.Solid) { paint.IsStroke = false; canvas.DrawCircle(path.LastPoint, 20, paint); }
                // if (routedEdge.Directed == Directed.Pointy) { paint.IsStroke = true; canvas.DrawCircle(path.LastPoint, 20, paint); }

                if (edge.Label != null) { // draw label on first routedEdge segment
                    var saveTextSize = paint.TextSize;
                    paint.TextSize = swipe % textSize / 3;
                    SKPoint labelTarget = (firstTarget == ultimate) ? arrowHeadBase : firstTarget;
                    GraphLayout.CanvasDrawTextCentered(canvas, edge.Label, swipe % new SKPoint((firstSource.X + labelTarget.X) / 2, (firstSource.Y + labelTarget.Y) / 2), paint, true);
                    paint.TextSize = saveTextSize;
                 }

            }
        }

        private SKPath AddBeziers(SKPath path, SKPoint[] controlPoints, Swipe swipe) {
            path.MoveTo(swipe % controlPoints[0]);
            for (int i = 0; i < controlPoints.Length - 2; i += 4) {
                if (i+3 > controlPoints.Length - 1) {
                    path.QuadTo(swipe % controlPoints[i + 1], swipe % controlPoints[i + 2]);
                } else {
                    path.CubicTo(swipe % controlPoints[i + 1], swipe % controlPoints[i + 2], swipe % controlPoints[i + 3]);
                }
            }
            return path;
        }
        
        public List<SKPoint> ControlPoints(List<SKPoint> path) {
	        List<SKPoint> controlPoints = new List<SKPoint>();
	        for ( int i = 1; i < path.Count - 1; i += 2 ) {
		        controlPoints.Add(new SKPoint((path[i - 1].X + path[i].X) / 2, (path[i - 1].Y + path[i].Y) / 2));
		        controlPoints.Add(path[i]);
		        controlPoints.Add(path[i+1]);
                if (i + 2 < path.Count - 1) {
                    controlPoints.Add(new SKPoint((path[i + 1].X + path[i + 2].X)/2, (path[i + 1].Y + path[i + 2].Y) / 2));
		        }
	        }
            return controlPoints;
        }

        public static void CanvasDrawCircle(SKCanvas canvas, SKPoint p, float radius, bool isStroke, SKColor color) {
            using (var paint = new SKPaint()) {
                paint.TextSize = 10; paint.IsAntialias = true; paint.Color = color; paint.IsStroke = isStroke;
                canvas.DrawCircle(p.X, p.Y, radius, paint);
            }
        }

        public static void CanvasDrawRect(SKCanvas canvas, SKRect rect, bool isStroke, SKColor color) {
            using (var paint = new SKPaint()) {
                paint.TextSize = 10; paint.IsAntialias = true; paint.Color = color; paint.IsStroke = isStroke;
                canvas.DrawRect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, paint);
            }
        }

        private static void CanvasDrawRoundRect(SKCanvas canvas, SKPoint p, SKSize s, float padding, SKColor color) {
            using (var paint = new SKPaint()) {
                paint.TextSize = 10; paint.IsAntialias = true; paint.Color = color; paint.IsStroke = true;
                // canvas.DrawRect( p.X - s.Width / 2, p.Y - s.Height / 2, s.Width, s.Height, paint);  // debug
                canvas.DrawRoundRect( // RoundRect inflates the base rect by padding amount
                    p.X - s.Width / 2, p.Y - s.Height / 2,
                    s.Width, s.Height,
                    padding, padding, paint);
            }
        }
     
        //public static float PaintMeasureText(SKPaint paint, string s, ref SKRect bounds) {
        //    if (string.IsNullOrEmpty(s)) { bounds = new SKRect(0, 0, 0, 0); return 0; } // or MeasureText will crash
        //    return paint.MeasureText(s, ref bounds);
        //}

        //private static SKRect CanvasDrawTextCentered(SKCanvas canvas, string text, SKPoint p, SKPaint paint, bool cleared) {
        //    var bounds = new SKRect();
        //    float width = ComputeLayout.PaintMeasureText(paint, text, ref bounds);
        //    SKColor saveColor = paint.Color; paint.Color = SKColors.White;
        //    bool saveIsStroke = paint.IsStroke; paint.IsStroke = false;
        //    if (cleared) canvas.DrawRect(p.X - width / 2, p.Y - bounds.Height / 2, width, bounds.Height, paint);
        //    paint.Color = saveColor; paint.IsStroke = saveIsStroke;
        //    canvas.DrawText(text,
        //        p.X - bounds.Width / 2 - bounds.Left,
        //        p.Y + bounds.Height / 2 - bounds.Bottom,
        //        paint);
        //    return bounds;
        //}

        //private static SKRect CanvasDrawTextCentered(SKCanvas canvas, string text, SKPoint p, float textSize, SKColor color, bool cleared) {
        //    // CanvasDrawTextMeasures(canvas, text, p, textSize, SKColors.Black); // debug
        //    using (var paint = new SKPaint()) {
        //        paint.TextSize = textSize; paint.IsAntialias = true; paint.Color = color; paint.IsStroke = false;
        //        return CanvasDrawTextCentered(canvas, text, p, paint, cleared);
        //    }
        //}

        public static SKPoint PointOnCircle(SKPoint p1, SKPoint p2, float p1Radius) {
            SKPoint Pointref = SKPoint.Subtract(p2, new SKSize(p1));
            double degrees = Math.Atan2(Pointref.Y, Pointref.X);
            double cosx1 = Math.Cos(degrees);
            double siny1 = Math.Sin(degrees);
            return new SKPoint(p1.X + (float)cosx1 * p1Radius, p1.Y + (float)siny1 * p1Radius);
        }

        // http://mathworld.wolfram.com/Two-PointForm.html
        public static SKPoint PointOnRectangle(SKPoint p1, SKPoint p2, float h, float v) {
            float x1 = p1.X; float y1 = p1.Y;
            float x2 = p2.X; float y2 = p2.Y;
            if (x1 == x2) return new SKPoint(x1, y1 + ((y2 > y1) ? v : -v));
            if (y1 == y2) return new SKPoint(x1 + ((x2 > x1) ? h : -h), y1);
            float m = (y2 - y1) / (x2 - x1);
            // find the intersect xh,yh with the horizontal edge of the rectangle
            float yh = y1 + ((y2 > y1) ? v : -v);
            float xh = (yh - y1 + m * x1) / m;
            // find the intersect xv,yv with the vertical edge of the rectangle
            float xv = x1 + ((x2 > x1) ? h : -h);
            float yv = m * (xv - x1) + y1;
            // return the closest to p1
            if ((xh - x1) * (xh - x1) + (yh - y1) * (yh - y1) < (xv - x1) * (xv - x1) + (yv - y1) * (yv - y1))
                return new SKPoint(xh, yh);
            else return new SKPoint(xv, yv);
        }

        private (SKPoint control, SKPoint nextControl) SplineControls(SKPoint point, SKPoint nextPoint, SKSize itemSize) {
            var controlOffset = new SKPoint(itemSize.Width * 0.8f, 0);
            var currentControl = point + controlOffset;
            var nextControl = nextPoint - controlOffset;
            return (currentControl, nextControl);
        }


        //MeasureText    https://github.com/mono/SkiaSharp/issues/685
        //the return value is the width of the character, including the "padding" around the character to stop them overlapping when they are drawn.
        //the bounds value is the "tight" rectangle that the character wil fit in RELATIVE to the baseline.

        //private static void CanvasDrawTextMeasures(SKCanvas canvas, string text, SKPoint p, float textSize, SKColor color) {
        //    using (var paint = new SKPaint()) {
        //        paint.TextSize = textSize; paint.IsAntialias = true; paint.Color = color; paint.IsStroke = true;
        //        var bounds = new SKRect();
        //        float width = ComputeLayout.PaintMeasureText(paint, text, ref bounds);
        //        // canvas.DrawRect(p.X - bounds.Width / 2, p.Y - bounds.Height / 2, bounds.Width, bounds.Height, paint); // text bouding rect
        //        canvas.DrawLine(
        //            new SKPoint(p.X - width / 2, p.Y + bounds.Height / 2 - bounds.Bottom),
        //            new SKPoint(p.X + width / 2, p.Y + bounds.Height / 2 - bounds.Bottom), paint); // text hor baseline
        //        canvas.DrawLine(
        //            new SKPoint(p.X - bounds.Width / 2 - bounds.Left, p.Y - bounds.Height / 2),
        //            new SKPoint(p.X - bounds.Width / 2 - bounds.Left, p.Y + bounds.Height / 2), paint); // text ver startline
        //    }
        //}

        public static float PaintMeasureText(SKPaint paint, string s, ref SKRect bounds) {
            if (string.IsNullOrEmpty(s)) { bounds = new SKRect(0, 0, 0, 0); return 0; } // or MeasureText will crash
            return paint.MeasureText(s, ref bounds);
        }

        public static SKRect CanvasDrawTextCentered(SKCanvas canvas, string text, SKPoint p, SKPaint paint, bool cleared) {
            var bounds = new SKRect();
            float width = PaintMeasureText(paint, text, ref bounds);
            SKColor saveColor = paint.Color; paint.Color = SKColors.White;
            bool saveIsStroke = paint.IsStroke; paint.IsStroke = false;
            if (cleared) canvas.DrawRect(p.X - width / 2, p.Y - bounds.Height / 2, width, bounds.Height, paint);
            paint.Color = saveColor; paint.IsStroke = saveIsStroke;
            canvas.DrawText(text,
                p.X - bounds.Width / 2 - bounds.Left,
                p.Y + bounds.Height / 2 - bounds.Bottom,
                paint);
            return bounds;
        }

        public static SKRect CanvasDrawTextCentered(SKCanvas canvas, string text, SKPoint p, float textSize, SKColor color, bool cleared) {
            // CanvasDrawTextMeasures(canvas, text, p, textSize, SKColors.Black); // debug
            using (var paint = new SKPaint()) {
                paint.TextSize = textSize; paint.IsAntialias = true; paint.Color = color; paint.IsStroke = false;
                return CanvasDrawTextCentered(canvas, text, p, paint, cleared);
            }
        }

    }
}


