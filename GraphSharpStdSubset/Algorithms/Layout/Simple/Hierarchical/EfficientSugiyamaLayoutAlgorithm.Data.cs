using QuickGraph;
using System.Diagnostics;
using SkiaSharp;

namespace GraphSharp.Algorithms.Layout.Simple.Hierarchical
{
    public partial class EfficientSugiyamaLayoutAlgorithm<TVertex, TEdge, TGraph>
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
    {
        protected class SugiEdge : TaggedEdge<SugiVertex, TEdge>
        {
            public SugiEdge(TEdge originalEdge, SugiVertex source, SugiVertex target)
                : base(source, target, originalEdge) { }

            /// <summary>
            /// Gets the original edge of this SugiEdge.
            /// </summary>
            public TEdge OriginalEdge { get { return Tag; } }

            /// <summary>
            /// Gets or sets that the edge is included in a 
            /// type 1 conflict as a non-inner segment (true) or not (false).
            /// </summary>
            public bool Marked = false;

            public bool TempMark = false;

            public void SaveMarkedToTemp()
            {
                TempMark = Marked;
            }

            public void LoadMarkedFromTemp()
            {
                Marked = TempMark;
            }
        }

        protected enum VertexTypes
        {
            Original,
            PVertex,
            QVertex,
            RVertex
        }

        protected enum EdgeTypes
        {
            NonInnerSegment,
            InnerSegment
        }

        protected interface IData
        {
            int Position { get; set; }
        }

        protected abstract class Data : IData
        {
            public int Position { get; set; }

            /* Used by horizontal assignment */
            public readonly Data[] Sinks = new Data[4];
            public readonly float[] Shifts = new[] { float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity };
        }

        [DebuggerDisplay("{Type}: {OriginalVertex} - {Position} ; {MeasuredPosition} on layer {LayerIndex}")]
        protected class SugiVertex : Data
        {
            public readonly float[] HorizontalPositions = new[] { float.NaN, float.NaN, float.NaN, float.NaN };
            public float HorizontalPosition = float.NaN;
            public float VerticalPosition = float.NaN;
            public readonly SugiVertex[] Roots = new SugiVertex[4];
            public readonly SugiVertex[] Aligns = new SugiVertex[4];
            public readonly float[] BlockWidths = new[] { float.NaN, float.NaN, float.NaN, float.NaN };
            public int IndexInsideLayer;
            public int PermutationIndex;
            public int TempPosition;
            public bool DoNotOpt;
            public readonly SKSize Size;
            public TVertex OriginalVertex;
            public VertexTypes Type;
            public Segment Segment;
            public int LayerIndex { get; set; }
            public double MeasuredPosition { get; set; }

            public SugiVertex()
            {
                Size = new SKSize();
            }

            public SugiVertex(TVertex originalVertex, SKSize size)
            {
                Size = size;
                OriginalVertex = originalVertex;
                Type = VertexTypes.Original;
                Segment = null;
            }

            public void SavePositionToTemp()
            {
                TempPosition = Position;
            }

            public void LoadPositionFromTemp()
            {
                Position = TempPosition;
            }
        }

        protected class Segment : Data
        {
            /// <summary>
            /// Gets or sets the p-vertex of the segment.
            /// </summary>
            public SugiVertex PVertex;

            /// <summary>
            /// Gets or sets the q-vertex of the segment.
            /// </summary>
            public SugiVertex QVertex;
        }
    }
}
