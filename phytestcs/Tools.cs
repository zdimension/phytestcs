using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using TGUI;
using static phytestcs.Global;
using Object = phytestcs.Objects.Object;

namespace phytestcs
{
    public sealed class Tools
    {
        public static Vector2f FromPolar(float r, float theta)
        {
            return new Vector2f((float) (r * Math.Cos(theta)), (float) (r * Math.Sin(theta)));
        }

        public static T Transition<T>(T a, T b, DateTime start, float duration)
        {
            return a + ((dynamic)b - (dynamic)a) * (float)(DateTime.Now - start).TotalSeconds / duration;
        }

        public static T Clamp<T>(T x, T a, T b)
        {
            if ((dynamic)x < a) return a;
            if ((dynamic)x > b) return b;
            return x;
        }

        public static Object ObjectAtPosition(Vector2i pos)
        {
            var loc = pos.ToWorld();

            return Simulation.WorldCache.LastOrDefault(o => o.Contains(loc));
        }

        public static PhysicalObject PhysObjectAtPosition(Vector2i pos, PhysicalObject excl=null)
        {
            var loc = pos.ToWorld();

            return Simulation.WorldCache.OfType<PhysicalObject>().LastOrDefault(o => o != excl && o.Contains(loc));
        }

        public static T Average<T>(T a, T b)
        {
            return a + ((dynamic) b - a) / 2;
        }

        public static IEnumerable<Vertex> VertexLine2(Vector2f a, Vector2f b, Color c, int w = 1, bool horiz = false, float wf=1)
        {
            var dx = horiz ? 0 : 1;
            var dy = horiz ? 1 : 0;
            return Enumerable.Range(0, w).SelectMany(i => new[]
            {
                new Vertex(new Vector2f(a.X + dx * i * wf, a.Y + dy * i * wf), c),
                new Vertex(new Vector2f(b.X + dx * i * wf, b.Y + dy * i * wf), c)
            });
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

        public static event PropertyChangedEventHandler StaticPropertyChanged;

        public static void NotifyStaticPropertyChanged([CallerMemberName] string name = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(name));
        }

        public static bool SetField<T>(ref T field, T value,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            NotifyStaticPropertyChanged(propertyName);
            return true;
        }
    }

    public class Ref<T>
    {
        public T Value { get; set; }
    }

    public class ColorWrapper
    {
        private readonly Func<Color> getter;
        private readonly Action<Color> setter;

        public ColorWrapper(Expression<Func<Color>> bindProp)
        {
            (getter, setter) = bindProp.GetAccessors();
        }

        public byte R
        {
            get => getter().R;
            set => setter(new Color(getter()) {R = value});
        }

        public byte G
        {
            get => getter().G;
            set => setter(new Color(getter()) { G = value });
        }

        public byte B
        {
            get => getter().B;
            set => setter(new Color(getter()) { B = value });
        }

        public double H
        {
            get => ValueHSV.h;
            set
            {
                var hsv = ValueHSV;
                hsv.h = value;
                setter(hsv.Scatter(HsvToRgb).ToColor());
            }
        }

        public double S
        {
            get => ValueHSV.s;
            set
            {
                var hsv = ValueHSV;
                hsv.s = value;
                setter(hsv.Scatter(HsvToRgb).ToColor());
            }
        }

        public double V
        {
            get => ValueHSV.v;
            set
            {
                var hsv = ValueHSV;
                hsv.v = value;
                setter(hsv.Scatter(HsvToRgb).ToColor());
            }
        }

        private (double h, double s, double v) ValueHSV => getter().ToDouble().Scatter(RgbToHsv);

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static (double h, double s, double v) RgbToHsv(double r, double g, double b)
        {
            r /= 255.0;
            g /= 255.0;
            b /= 255.0;
            var max = new[] { r, g, b }.Max();
            var min = new[] { r, g, b }.Min();
            var delta = max - min;
            var h = 0.0;
            var s = max != 0 ? delta / max : 0;
            var v = max;
            if (s == 0)
            {
                return (h, s, v);
            }
            if (r == max)
            {
                h = (g - b) / delta;
            }
            else if (g == max)
            {
                h = (b - r) / delta + 2.0;
            }
            else if (b == max)
            {
                h = (r - g) / delta + 4.0;
            }
            h *= 60.0;
            if (h < 0)
            {
                h += 360.0;
            }
            return (h, s, v);
        }

        public static (byte r, byte g, byte b) HsvToRgb(double h, double s, double v)
        {
            var r = 0d;
            var g = 0d;
            var b = 0d;

            var i = Math.Floor(h / 60);
            var f = h / 60 - i;
            var p = v * (1 - s);
            var q = v * (1 - f * s);
            var t = v * (1 - (1 - f) * s);

            switch (i % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                case 5: r = v; g = p; b = q; break;
            }

            return ((byte) (r * 255), (byte) (g * 255), (byte) (b * 255));
        }
    }
}
