using System;
using SFML.Graphics;
using SFML.System;
using TGUI;
using static phytestcs.Global;

namespace phytestcs
{
    public static partial class Tools
    {
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

        public static (double r, double g, double b) ToDouble(this Color c)
        {
            return (c.R, c.G, c.B);
        }
        
        public static Color Add(this Color a, Color b)
        {
            return new Color(
                (byte) Tools.Clamp(a.R + b.R, 0, 255), 
                (byte) Tools.Clamp(a.G + b.G, 0, 255),
                (byte) Tools.Clamp(a.B + b.B, 0, 255));
        }

        public static Color Subtract(this Color a, Color b)
        {
            return new Color(
                (byte)Tools.Clamp(a.R - b.R, 0, 255),
                (byte)Tools.Clamp(a.G - b.G, 0, 255),
                (byte)Tools.Clamp(a.B - b.B, 0, 255));
        }

        public static Color Multiply(this Color a, float f)
        {
            return new Color(
                (byte)Tools.Clamp(a.R * f, 0, 255),
                (byte)Tools.Clamp(a.G * f, 0, 255),
                (byte)Tools.Clamp(a.B * f, 0, 255));
        }
        
        public static Vertex[] VertexLine(Vector2f a, Vector2f b, Color c, float w = 1, bool horiz = false)
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
        
        public static VertexArray VertexLineTri(Vector2f[] points, Color c, float w = 1, bool blend=false, int? upto=null)
        {
            if (points.Length <= 1 || (upto != null && upto <= 1))
                return new VertexArray(PrimitiveType.TriangleStrip, 0);
            
            var a = points[0];
            var b = points[1];
            var edge = (b - a).Ortho().Normalize();
            var end = points.Length;
            if (upto != null)
                end = Math.Min(upto.Value, points.Length);
            var res = new VertexArray(PrimitiveType.TriangleStrip, (uint) (2 * end));
            var pos = 0u;
            var col = c;
            float A = 0, dA = 0;
            if (blend)
            {
                col.A = 0;
                dA = (float) c.A / res.VertexCount;
            }

            res[pos++] = new Vertex(a - w * edge / 2, col);
            res[pos++] = new Vertex(a + w * edge / 2, col);
            
            for (var i = 1; i < end - 1; i++)
            {
                var p0 = points[i - 1];
                var p1 = points[i];
                var p2 = points[i + 1];
                edge = (p2 - p1).Ortho().Normalize();
                var tangent = ((p2 - p1).Normalize() + (p1 - p0).Normalize()).Normalize();
                var miter = tangent.Ortho();
                var length = w / miter.Dot(edge);
                col = c;
                if (blend)
                {
                    col.A = (byte) (A += dA);
                }
                res[pos++] = new Vertex(p1 - length * miter, col);
                res[pos++] = new Vertex(p1 + length * miter, col);
            }

            a = points[end - 1];
            res[pos++] = new Vertex(a - w * edge / 2, c);
            res[pos++] = new Vertex(a + w * edge / 2, c);

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
        public static VertexArray CircleOutline(Vector2f center, float radius, float width, Color c, float? angle=null)
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
                    upto: angle == null ? (int?)null : (int) Math.Round(Math.Abs(angle.Value) * Render._rotCirclePointCount / Math.PI / 2));
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
        public static VertexArray CircleSector(Vector2f center, float radius, Color c, float angle, float startAngle = 0)
        {
            var start = (uint)Math.Round(Render._rotCirclePointCount * Math.Abs(startAngle) / (2 * Math.PI));
            var numP = (uint)Math.Round(Render._rotCirclePointCount * Math.Abs(angle) / (2 * Math.PI));
            var res = new VertexArray(PrimitiveType.TriangleFan, numP + 1);
            res[0] = new Vertex(center, c);
            
            for (uint i = 0; i < numP; i++)
            {
                var idx = (int) start;
                if (angle > 0)
                    idx += (int) i;
                else
                    idx -= (int) i;
                idx %= (int)Render._rotCirclePointCount;
                if (idx < 0)
                    idx += (int) Render._rotCirclePointCount;
                res[i + 1] = new Vertex(center + Render._rotCirclePoints[idx] * radius, c);
            }

            return res;
        }

        /// <summary>
        /// Returns a random color uniformly from the RGB space
        /// </summary>
        /// <returns>Color</returns>
        public static Color RandomColor()
        {
            return new Color((byte) RNG.Next(256), (byte) RNG.Next(256), (byte) RNG.Next(256));
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