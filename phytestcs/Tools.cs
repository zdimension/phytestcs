using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

            return Simulation.WorldCache.FirstOrDefault(o => o.Contains(loc));
        }

        public static PhysicalObject PhysObjectAtPosition(Vector2i pos)
        {
            var loc = pos.ToWorld();

            return Simulation.WorldCache.OfType<PhysicalObject>().FirstOrDefault(o => o.Contains(loc));
        }

        public static T Average<T>(T a, T b)
        {
            return a + ((dynamic) b - a) / 2;
        }

        public static IEnumerable<Vertex> VertexLine(Vector2f a, Vector2f b, Color c, int w = 1, bool horiz = false, float wf=1)
        {
            var dx = horiz ? 0 : 1;
            var dy = horiz ? 1 : 0;
            return Enumerable.Range(0, w).SelectMany(i => new[]
            {
                new Vertex(new Vector2f(a.X + dx * i * wf, a.Y + dy * i * wf), c),
                new Vertex(new Vector2f(b.X + dx * i * wf, b.Y + dy * i * wf), c)
            });
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
        private Func<Color> getter;
        private Action<Color> setter;

        public ColorWrapper(Func<Color> getter, Action<Color> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public ColorWrapper(object obj, string prop)
        {
            var pi = obj.GetType().GetProperty(prop);
            getter = () => (Color)pi.GetValue(obj);
            setter = c => pi.SetValue(obj, c);
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
