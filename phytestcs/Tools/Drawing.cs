using System;
using SFML.Graphics;
using SFML.System;
using TGUI;
using static phytestcs.Tools;

namespace phytestcs
{
    public static partial class Tools
    {
        private static readonly VertexArray _cake = CircleSector(default, 1, Color.Black, Math.PI / 8,
            (float) (-Math.PI / 16));

        public static Color ToColor(this (byte r, byte g, byte b) v)
        {
            return new Color(v.r, v.g, v.b);
        }

        public static void Deconstruct(this Color c, out byte r, out byte g, out byte b)
        {
            r = c.R;
            g = c.G;
            b = c.B;
        }

        public static void Deconstruct(this Color c, out double r, out double g, out double b)
        {
            r = c.R;
            g = c.G;
            b = c.B;
        }

        public static (double r, double g, double b, double a) ToDouble(this Color c)
        {
            return (c.R / 255d, c.G / 255d, c.B / 255d, c.A / 255d);
        }

        public static void Deconstruct(this Color color, out double r, out double g, out double b, out double a)
        {
            (r, g, b, a) = color.ToDouble();
        }

        public static Color Add(this Color a, Color b)
        {
            return new Color(
                (byte) Clamp(a.R + b.R, 0, 255),
                (byte) Clamp(a.G + b.G, 0, 255),
                (byte) Clamp(a.B + b.B, 0, 255));
        }

        public static Color Subtract(this Color a, Color b)
        {
            return new Color(
                (byte) Clamp(a.R - b.R, 0, 255),
                (byte) Clamp(a.G - b.G, 0, 255),
                (byte) Clamp(a.B - b.B, 0, 255));
        }

        public static Color Multiply(this Color a, float f)
        {
            return new Color(
                (byte) Clamp(a.R * f, 0, 255),
                (byte) Clamp(a.G * f, 0, 255),
                (byte) Clamp(a.B * f, 0, 255));
        }
        
        public static Vertex[] VertexLineThin(Vector2f a, Vector2f b, Color c)
        {
            return new[]
            {
                new Vertex(a, c),
                new Vertex(b, c)
            };
        }

        public static Vertex[] VertexLine(Vector2f a, Vector2f b, Color c, float w = 1)
        {
            var edge = (b - a).Ortho().Normalize() * w;
            return new[]
            {
                new Vertex(a - edge / 2, c),
                new Vertex(a + edge / 2, c),
                new Vertex(b + edge / 2, c),
                new Vertex(b - edge / 2, c),
            };
        }

        public enum BlendType
        {
            Linear,
            Exp,
            ExpSymmetric
        }

        public static VertexArray VertexLineTri(Vector2f[] points, Color c, float w = 1, bool blend = false,
            int? upto = null, Color? c2_ = null, byte endAlpha=0, BlendType blendMode=BlendType.ExpSymmetric, bool outsideInvert=false, float startAngle=0f, float endAngle=0f, bool squaryEnd=false)
        {
            if (points.Length <= 1 || (upto != null && upto <= 1))
                return new VertexArray(PrimitiveType.TriangleStrip, 0);

            var a = points[0];
            var b = points[1];
            var dir = (b - a).Normalize();
            var edge = dir.Ortho();
            
            if (outsideInvert)
            {
                edge = -edge;
                startAngle = -startAngle;
                endAngle = -endAngle;
            }

            var end = points.Length;
            if (upto != null)
                end = Math.Min(upto.Value, points.Length);
            var count = (uint) (2 * end);
            var res = new VertexArray(PrimitiveType.TriangleStrip, count);
            var pos = 0u;
            var col = c;
            var c2 = c2_ ?? c;
            var col2 = c2;

            Func<int, byte> alpha;
            switch (blendMode)
            {
                case BlendType.Linear:
                {
                    var linFactor = ((float) col.A - endAlpha) / (end - 1);
                    alpha = x => (byte) (endAlpha + linFactor * x);
                    break;
                }
                case BlendType.Exp:
                {
                    var blendFac = (float) (Math.Log((double) endAlpha / col.A) / (end - 1));
                    alpha = x => (byte) (c.A * Math.Exp(blendFac * (end - 1 - x)));
                    break;
                }
                case BlendType.ExpSymmetric:
                {
                    var blendFac = ((float) col.A - endAlpha) / col.A;
                    alpha = x => (byte) (c.A - blendFac * (Math.Pow(c.A + 1, 1f - x / (end - 1f)) - 1));
                    break;
                }
                default:
                    throw new NotImplementedException();
            }

            if (blend)
            {
                col.A = endAlpha;
                if (c2_ == null)
                    col2.A = endAlpha;
            }

            var startDiff = dir * (float) (w * Math.Tan(startAngle) / 2);

            var addSign = squaryEnd ? -1 : 1;
            void AddPair(Vector2f origin, Vector2f delta, Color color1, Color color2)
            {
                res[pos++] = new Vertex(origin + addSign * delta, color1);
                res[pos++] = new Vertex(origin - addSign * delta, color2);
            }

            AddPair(a, w * edge / 2 - startDiff, col, col2);

            for (var i = 1; i < end - 1; i++)
            {
                var p0 = points[i - 1];
                var p1 = points[i];
                var p2 = points[i + 1];
                edge = (p2 - p1).Ortho().Normalize();
                var tangent = ((p2 - p1).Normalize() + (p1 - p0).Normalize()).Normalize();
                var miter = tangent.Ortho();
                var length = w / miter.Dot(edge) / 2;

                if (blend)
                {
                    var al = alpha(i);
                    col.A = al;
                    if (c2_ == null)
                        col2.A = al;
                }

                AddPair(p1, length * miter, col, col2);
            }
            
            var endDiff = dir * (float) (w * Math.Tan(endAngle) / 2);

            AddPair( points[end - 1], w * edge / 2 + endDiff, c, c2);

            return res;
        }

        /// <summary>
        /// Returns the outline for a circle
        /// </summary>
        /// <param name="center">Circle center</param>
        /// <param name="radius">Distance from the center to the inner side of the outline</param>
        /// <param name="width">Thickness of the outline</param>
        /// <param name="c">Color</param>
        /// <param name="angle">Arc length. Default is full circle</param>
        /// <returns>Triangle list (strip)</returns>
        public static VertexArray CircleOutline(Vector2f center, float radius, float width, Color c,
            float? angle = null)
        {
            var pts = new Vector2f[Render._rotCirclePointCount];
            var rad = radius + width / 2;
            for (var i = 0; i < Render._rotCirclePointCount; i++)
            {
                var pt = Render._rotCirclePoints[i];
                if (angle < 0)
                    pt.Y = -pt.Y;
                pts[i] = center + pt * rad;
            }

            return VertexLineTri(
                pts,
                c,
                width,
                upto: angle == null
                    ? (int?) null
                    : (int) Math.Round(Math.Abs(angle.Value) * Render._rotCirclePointCount / Math.PI / 2));
        }

        /// <summary>
        /// Returns a circle sector
        /// </summary>
        /// <param name="center">Circle center</param>
        /// <param name="radius">Radius</param>
        /// <param name="c">Color</param>
        /// <param name="angle">Arc length</param>
        /// <param name="startAngle">Start angle</param>
        /// <returns>Triangle list (fan)</returns>
        public static VertexArray CircleSector(Vector2f center, float radius, Color c, double angle,
            float startAngle = 0)
        {
            startAngle = startAngle.ClampWrapPositive((float) (2 * Math.PI));
            var start = (uint) Math.Round(Render._rotCirclePointCount * Math.Abs(startAngle) / (2 * Math.PI));
            var numP = (uint) Math.Round(Render._rotCirclePointCount * Math.Abs(angle) / (2 * Math.PI));
            var res = new VertexArray(PrimitiveType.TriangleFan, numP + 1);
            res[0] = new Vertex(center, c);

            for (uint i = 0; i < numP; i++)
            {
                var idx = (int) start;
                if (angle > 0)
                    idx += (int) i;
                else
                    idx -= (int) i;
                idx %= (int) Render._rotCirclePointCount;
                if (idx < 0)
                    idx += (int) Render._rotCirclePointCount;
                res[i + 1] = new Vertex(center + Render._rotCirclePoints[idx] * radius, c);
            }

            return res;
        }

        public static VertexArray CircleCake(Vector2f center, float radius, Color c, float angle)
        {
            var res = new VertexArray(_cake);

            for (uint i = 0; i < res.VertexCount; i++)
            {
                var vertex = res[i];
                vertex.Position *= radius;
                vertex.Position = vertex.Position.Rotate(angle);
                vertex.Position += center;
                vertex.Color = c;
                res[i] = vertex;
            }

            return res;
        }

        /// <summary>
        /// Returns a random color uniformly from the RGB space
        /// </summary>
        /// <returns>Color</returns>
        public static Color RandomColor()
        {
            return Program.CurrentPalette.ColorRange.RandomColor();
        }

        /// <summary>
        /// Returns a random color uniformly from the RGB space
        /// </summary>
        /// <returns>Color</returns>
        public static Color RandomColorGlobal()
        {
            return new Color((byte) RNG.Next(256), (byte) RNG.Next(256), (byte) RNG.Next(256));
        }

        public static Color WithAlpha(this Color c, byte a)
        {
            return new Color(c.R, c.G, c.B, a);
        }

        public static Color MultiplyAlpha(this Color c, double a)
        {
            return new Color(c.R, c.G, c.B, (byte) (c.A * a));
        }

        public static RendererData GenerateButtonColor(Color c)
        {
            var delta = new Color(20, 20, 20);
            return new ButtonRenderer(new Button().Renderer)
            {
                BackgroundColorHover = c.Add(delta),
                BackgroundColor = c,
                BackgroundColorDown = c.Subtract(delta)
            }.Data;
        }
    }
}