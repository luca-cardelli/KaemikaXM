using System;
using SkiaSharp;
using QuickGraph;
using System.Collections.Generic;
using GraphSharp.Algorithms.Layout.Simple.Hierarchical;

namespace GraphSharp
{
    public static class TEST {

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

        public static string Report(IDictionary<Vertex, SKPoint> vertexLayout, IDictionary<Edge<Vertex>, SKPoint[]> edgeLayout) {
            string s = "";
            foreach (var kvp in vertexLayout) {
                s += kvp.Key.Label + ": " + kvp.Value.X.ToString() + ", " + kvp.Value.Y.ToString() + Environment.NewLine;
            }
            return s;
        }

        public static string Test () {

            var g = new AdjacencyGraph<Vertex, Edge<Vertex>>();
            var dict = new Dictionary<Vertex, SKSize>();

            var A = new Vertex("A"); dict[A] = new SKSize(10, 10);
            var B = new Vertex("B"); dict[B] = new SKSize(10, 10);
            var C = new Vertex("C"); dict[C] = new SKSize(10, 10);
            var D = new Vertex("D"); dict[D] = new SKSize(10, 10);

            g.AddVertex(A);
            g.AddVertex(B);
            g.AddVertex(C);
            g.AddVertex(D);
            g.AddEdge(new Edge<Vertex>(A, B));
            g.AddEdge(new Edge<Vertex>(A, C));
            g.AddEdge(new Edge<Vertex>(B, D));
            g.AddEdge(new Edge<Vertex>(C, D));

            // INPUT

            // Do not give the opional vertextPosition dictionary parameters:
            // they are always thrown away and can be useful only to set its initial size of the separate output dictionary
            // Edge predicate cannot be null

            //var alg1 = new SugiyamaLayoutAlgorithm<Vertex, Edge<Vertex>, AdjacencyGraph<Vertex, Edge<Vertex>>>(
            //    visitedGraph: g, 
            //    vertexSizes: dict, 
            //    parameters:null, 
            //    edgePredicate:null);

            var alg2 = new EfficientSugiyamaLayoutAlgorithm<Vertex, Edge<Vertex>, AdjacencyGraph<Vertex, Edge<Vertex>>>(
                visitedGraph: g, 
                parameters: null, 
                vertexSizes: dict);

            // COMPUTE

            //alg1.Compute();
            alg2.Compute();

            // OUTPUT
            return 
                //Report(alg1.VertexPositions, alg1.EdgeRoutes) +
                //Environment.NewLine + Environment.NewLine +
                Report(alg2.VertexPositions, alg2.EdgeRoutes);
        }

}
}



//    static class Program
//    {
//        public static Form1 form;
//        /// <summary>
//        /// The main entry point for the application.
//        /// </summary>
//        [STAThread]
//        static void Main()
//        {
//            Application.EnableVisualStyles();
//            Application.SetCompatibleTextRenderingDefault(false);
//            form = new Form1();
//            form.pictureBox1.BackColor = Color.FromName("red");
//            form.pictureBox1.Image = new Bitmap("C:\\Users\\lucac\\Downloads\\QuickGraph-master\\QuickGraph-master\\Untitled.png");

//            var g = new AdjacencyGraph<Vertex, Edge<Vertex>>();
//            var dict = new Dictionary<Vertex, Size>();
//            var A = new Vertex("A"); dict[A] = new Size(10,10);
//            var B = new Vertex("B"); dict[B] = new Size(20,10);
//            var E = new Edge<Vertex>(A, B);
//            g.AddVertex(A);
//            g.AddVertex(B);
//            g.AddEdge(E);

//            layout.LayoutMode = LayoutMode.Automatic;

//            //var l = new EfficientSugiyamaLayoutAlgorithm<Vertex, Edge<Vertex>, AdjacencyGraph<Vertex, Edge<Vertex>>>(g, null, dict);
//            //l.Compute();

//            GraphLayout
           


//            //// a simple adjacency graph representation
//            //int[][] graph = new int[5][];
//            //graph[0] = new int[] { 1 };
//            //graph[1] = new int[] { 2, 3 };
//            //graph[2] = new int[] { 3, 4 };
//            //graph[3] = new int[] { 4 };
//            //graph[4] = new int[] { };

//            //// interoping with quickgraph
//            //var g = GraphExtensions.ToDelegateVertexAndEdgeListGraph(
//            //    Enumerable.Range(0, graph.Length),
//            //    v => Array.ConvertAll(graph[v], w => new SEquatableEdge<int>(v, w))
//            //    );




//            form.pictureBox1.Show();
//            Application.Run(form);

//        }
//    }
