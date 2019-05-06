using System;
using SkiaSharp;

namespace GraphSharp
{
    // Replace System.Windows.Vector
    public class VectorStd
    {
        public float X { get; set; }
        public float Y { get; set; }
        public VectorStd(float x, float y) {
            this.X = x;
            this.Y = y;
        }
        public float Length { get { return (float) Math.Sqrt(X* X + Y* Y); } }

        public static VectorStd operator +(VectorStd v1, VectorStd v2) { return new VectorStd(v1.X+v2.X, v1.Y+v2.Y); }
        public static VectorStd operator -(VectorStd v1, VectorStd v2) { return new VectorStd(v1.X - v2.X, v1.Y - v2.Y); }
        public static VectorStd operator *(VectorStd v, float f) { return new VectorStd(v.X * f, v.Y * f); }
        public static VectorStd operator *(float f, VectorStd v) { return new VectorStd(f * v.X, f * v.Y); }
        public static VectorStd operator /(VectorStd v, float f) { return new VectorStd(v.X / f, v.Y / f); }

        public static SKPoint operator +(SKPoint v1, VectorStd v2) { return new SKPoint(v1.X + v2.X, v1.Y + v2.Y); }         // Point Translation
        public static SKPoint Zoom(SKPoint v, float f) { return new SKPoint(v.X * f, v.Y * f); }                             // Point Zoom
        public static SKPoint Zoom(float f, SKPoint v) { return new SKPoint(f * v.X, f * v.Y); }
        public static SKSize Zoom(SKSize s, float f) { return new SKSize(s.Width * f, s.Height * f); }                             // Size Zoom
        public static SKSize Zoom(float f, SKSize s) { return new SKSize(f * s.Width, f * s.Height); }
        public static VectorStd DifferenceVector(SKPoint v1, SKPoint v2) { return new VectorStd(v1.X - v2.X, v1.Y - v2.Y); }

    }

    public class MatrixStd {
        public float M11 { get; }
        public float M12 { get; }
        public float M21 { get; }
        public float M22 { get; }
        public MatrixStd(float m11, float m12, float m21, float m22) {
            this.M11 = m11;
            this.M12 = m12;
            this.M21 = m21;
            this.M22 = m22;
        }
        public static VectorStd operator *(MatrixStd m, VectorStd v) { return new VectorStd(m.M11 * v.X + m.M12 * v.Y, m.M21 * v.X + m.M22 * v.Y); }
        public static SKPoint operator *(MatrixStd m, SKPoint v) { return new SKPoint(m.M11 * v.X + m.M12 * v.Y, m.M21 * v.X + m.M22 * v.Y); }  // Point Transform
   }
}
