﻿using System.Collections.Generic;
using QuickGraph.Algorithms;
using QuickGraph;
using SkiaSharp;

namespace GraphSharp.Algorithms.Layout
{
    public delegate void LayoutIterationEndedEventHandler<TVertex>( object sender, ILayoutIterationEventArgs<TVertex> e )
        where TVertex : class;

    /// <summary>
    /// Reports the progress of the layout.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="percent">The status of the progress in percent.</param>
    public delegate void ProgressChangedEventHandler(object sender, double percent);

    public interface ILayoutAlgorithm<TVertex, TEdge, TGraph, TVertexInfo, TEdgeInfo> : ILayoutAlgorithm<TVertex, TEdge, TGraph>
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
    {
        /// <summary>
        /// Extra informations, calculated by the layout
        /// </summary>
        IDictionary<TVertex, TVertexInfo> VertexInfos { get; }

        /// <summary>
        /// Extra informations, calculated by the layout
        /// </summary>
        IDictionary<TEdge, TEdgeInfo> EdgeInfos { get; }
    }

    public interface ILayoutAlgorithm<TVertex, in TEdge, TGraph> : IAlgorithm<TGraph>
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
    {
        IDictionary<TVertex, SKPoint> VertexPositions { get; }

        IDictionary<TVertex, double> VertexAngles { get; } 

        /// <summary>
        /// Returns with the extra layout information of the vertex (or null).
        /// </summary>
        object GetVertexInfo( TVertex vertex );

        /// <summary>
        /// Returns with the extra layout information of the edge (or null).
        /// </summary>
        object GetEdgeInfo( TEdge edge );

        event LayoutIterationEndedEventHandler<TVertex> IterationEnded;

        event ProgressChangedEventHandler ProgressChanged;
    }
}