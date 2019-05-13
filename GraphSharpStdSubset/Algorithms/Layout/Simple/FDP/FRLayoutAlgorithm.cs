using System;
using System.Collections.Generic;
using QuickGraph;
using SkiaSharp;

namespace GraphSharp.Algorithms.Layout.Simple.FDP
{
    public class FRLayoutAlgorithm<TVertex, TEdge, TGraph> : ParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, FRLayoutParametersBase>
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
    {
        /// <summary>
        /// Actual temperature of the 'mass'.
        /// </summary>
        private double _temperature;

        private double _maxWidth = double.PositiveInfinity;
        private double _maxHeight = double.PositiveInfinity;

        protected override FRLayoutParametersBase DefaultParameters
        {
            get { return new FreeFRLayoutParameters(); }
        }

        #region Constructors
        public FRLayoutAlgorithm(TGraph visitedGraph)
            : base(visitedGraph) { }

        public FRLayoutAlgorithm(TGraph visitedGraph, IDictionary<TVertex, SKPoint> vertexPositions, FRLayoutParametersBase parameters)
            : base(visitedGraph, vertexPositions, parameters) { }
        #endregion

        /// <summary>
        /// It computes the layout of the vertices.
        /// </summary>
        protected override void InternalCompute()
        {
            //initializing the positions
            if (Parameters is BoundedFRLayoutParameters)
            {
                var param = Parameters as BoundedFRLayoutParameters;
                InitializeWithRandomPositions(param.Width, param.Height);
                _maxWidth = param.Width;
                _maxHeight = param.Height;
            }
            else
            {
                InitializeWithRandomPositions(10.0, 10.0);
            }
            Parameters.VertexCount = VisitedGraph.VertexCount;

            // Actual temperature of the 'mass'. Used for cooling.
            var minimalTemperature = Parameters.InitialTemperature*0.01;
            _temperature = Parameters.InitialTemperature;
            for (int i = 0;
                  i < Parameters._iterationLimit
                  && _temperature > minimalTemperature
                  && State != QuickGraph.Algorithms.ComputationState.PendingAbortion;
                  i++)
            {
                IterateOne();

                //make some cooling
                switch (Parameters._coolingFunction)
                {
                    case FRCoolingFunction.Linear:
                        _temperature *= (1.0 - (double) i / Parameters._iterationLimit);
                        break;
                    case FRCoolingFunction.Exponential:
                        _temperature *= Parameters._lambda;
                        break;
                }

                //iteration ended, do some report
                if (ReportOnIterationEndNeeded)
                {
                    double statusInPercent = (double) i / Parameters._iterationLimit;
                    OnIterationEnded(i, statusInPercent, string.Empty, true);
                }
            }
        }


        protected void IterateOne()
        {
            //create the forces (zero forces)
            var forces = new Dictionary<TVertex, VectorStd>();

            #region Repulsive forces
            var force = new VectorStd(0, 0);
            foreach (TVertex v in VisitedGraph.Vertices)
            {
                force.X = 0; force.Y = 0;
                SKPoint posV = VertexPositions[v];
                foreach (TVertex u in VisitedGraph.Vertices)
                {
                    //doesn't repulse itself
                    if (u.Equals(v))
                        continue;

                    //calculating repulsive force
                    VectorStd delta = VectorStd.DifferenceVector(posV, VertexPositions[u]);
                    float length = (float)Math.Max(delta.Length, double.Epsilon);
                    delta = delta / length * Parameters.ConstantOfRepulsion / length;

                    force += delta;
                }
                forces[v] = force;
            }
            #endregion

            #region Attractive forces
            foreach (TEdge e in VisitedGraph.Edges)
            {
                TVertex source = e.Source;
                TVertex target = e.Target;

                //vonz�er� sz�m�t�sa a k�t pont k�zt
                VectorStd delta = VectorStd.DifferenceVector(VertexPositions[source], VertexPositions[target]);
                float length = (float)Math.Max(delta.Length, double.Epsilon);
                delta = delta / length * ((float)Math.Pow(length, 2)) / Parameters.ConstantOfAttraction;

                forces[source] -= delta;
                forces[target] += delta;
            }
            #endregion

            #region Limit displacement
            foreach (TVertex v in VisitedGraph.Vertices)
            {
                SKPoint pos = VertexPositions[v];

                //er� limit�l�sa a temperature-el
                VectorStd delta = forces[v];
                float length = (float)Math.Max(delta.Length, double.Epsilon);
                delta = delta / length * (float)Math.Min(delta.Length, _temperature);

                //er�hat�s a pontra
                pos += delta;

                //falon ne menj�nk ki
                pos.X = (float)Math.Min(_maxWidth, Math.Max(0, pos.X));
                pos.Y = (float)Math.Min(_maxHeight, Math.Max(0, pos.Y));
                VertexPositions[v] = pos;
            }
            #endregion
        }
    }
}