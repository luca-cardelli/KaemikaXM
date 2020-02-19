using System;
using SkiaSharp;

namespace Kaemika
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
        public static VectorStd DifferenceVector(SKPoint v1, SKPoint v2) { return new VectorStd(v1.X - v2.X, v1.Y - v2.Y); }
    }

    public class Swipe {
        public float scale;
        public SKPoint translate;
        public Swipe(float scale, SKPoint translate) {
            this.scale = scale;
            this.translate = translate;
        }
        public static Swipe Id() { return new Swipe(1.0f, new SKPoint(0.0f, 0.0f)); } // avoid sharing Id and getting it globally clobbered!
        public static bool Same(Swipe swipe1, Swipe swipe2) { return swipe1.scale == swipe2.scale && swipe1.translate.X == swipe2.translate.X && swipe1.translate.Y == swipe2.translate.Y; }
        // Apply a transformation to things
        public static float operator %(Swipe w, float f) { return f * w.scale; }
        public static SKPoint operator %(Swipe w, SKPoint p) { return new SKPoint(p.X * w.scale + w.translate.X, p.Y * w.scale + w.translate.Y); }
        public static SKSize operator %(Swipe w, SKSize s) { return new SKSize(s.Width * w.scale, s.Height * w.scale); }
        public static SKRect operator %(Swipe w, SKRect r) { return new SKRect(r.Left * w.scale + w.translate.X, r.Top * w.scale + w.translate.Y, r.Right * w.scale + w.translate.X, r.Bottom * w.scale + w.translate.Y); }
        // Inverse transformations
        public static SKPoint Inverse(SKPoint p, Swipe w) { return new SKPoint((p.X - w.translate.X) / w.scale, (p.Y - w.translate.Y) / w.scale); }
        // Modify a transformation
        public static Swipe operator *(float f, Swipe w) { return new Swipe(f * w.scale, w.translate); }
        public static Swipe operator +(SKPoint p, Swipe w) { return new Swipe(w.scale, new SKPoint(p.X+w.translate.X, p.Y + w.translate.Y)); }
        public static Swipe operator +(Swipe w1, Swipe w2) { return new Swipe(w1.scale * w2.scale, new SKPoint(w1.translate.X + w2.translate.X, w1.translate.Y + w2.translate.Y)); }
        public static Swipe operator *(Swipe w1, Swipe w2) { return new Swipe(w1.scale * w2.scale, new SKPoint(w1.translate.X*w2.scale + w2.translate.X, w1.translate.Y * w2.scale + w2.translate.Y)); }
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
