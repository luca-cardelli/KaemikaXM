﻿using System.Collections.Generic;
using SkiaSharp;

namespace GraphSharp.Algorithms.Layout
{
    public interface ILayoutIterationEventArgs<TVertex>
        where TVertex : class
    {
        /// <summary>
        /// Represent the status of the layout algorithm in percent.
        /// </summary>
        double StatusInPercent { get; }

        /// <summary>
        /// If the user sets this value to <code>true</code>, the algorithm aborts ASAP.
        /// </summary>
        bool Abort { get; set; }

        /// <summary>
        /// Number of the actual iteration.
        /// </summary>
        int Iteration { get; }

        /// <summary>
        /// Message, textual representation of the status of the algorithm.
        /// </summary>
        string Message { get; }

        IDictionary<TVertex, SKPoint> VertexPositions { get; }

        IDictionary<TVertex, double> VertexAngles { get; } 
    }
}
