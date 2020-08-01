using System;
using System.Linq.Expressions;
using phytestcs.Interface;
using phytestcs.Objects;
using SFML.Graphics;

namespace phytestcs
{
    public class ColorWrapper
    {
        private readonly Func<Color> _getter;
        private readonly Action<Color> _setter;

        public ColorWrapper(Expression<Func<Color>> bindProp)
            : this(PropertyReference.FromExpression(bindProp))
        {
        }

        public ColorWrapper(PropertyReference<Color> bindProp)
        {
            (_getter, _setter) = bindProp.GetAccessors();
            ValueChanged += _setter;
        }

        public byte R
        {
            get => _getter().R;
            set => ValueChanged(new Color(_getter()) { R = value });
        }

        public byte G
        {
            get => _getter().G;
            set => ValueChanged(new Color(_getter()) { G = value });
        }

        public byte B
        {
            get => _getter().B;
            set => ValueChanged(new Color(_getter()) { B = value });
        }

        public byte A
        {
            get => _getter().A;
            set => ValueChanged(new Color(_getter()) { A = value });
        }

        [ObjProp("A")]
        public double Ad
        {
            get => _getter().A / 255d;
            set => ValueChanged(new Color(_getter()) { A = (byte) (value * 255) });
        }

        public double H
        {
            get => ValueHsv.H;
            set
            {
                var hsv = ValueHsv;
                hsv.H = value;
                ValueChanged(hsv.ToColor());
            }
        }

        public double S
        {
            get => ValueHsv.S;
            set
            {
                var hsv = ValueHsv;
                hsv.S = value;
                ValueChanged(hsv.ToColor());
            }
        }

        public double V
        {
            get => ValueHsv.V;
            set
            {
                var hsv = ValueHsv;
                hsv.V = value;
                ValueChanged(hsv.ToColor());
            }
        }

        public Color Value
        {
            get => _getter();
            set => ValueChanged(value);
        }

        public Hsva ValueHsv
        {
            get => _getter();
            set => ValueChanged(value);
        }

        public event Action<Color> ValueChanged = delegate { };
    }

    public struct Hsva : IRepr
    {
        public void Deconstruct(out double h, out double s, out double v, out double a)
        {
            h = H;
            s = S;
            v = V;
            a = A;
        }

        public double H;
        public double S;
        public double V;
        public double A;

        public Hsva(double h, double s, double v, double a)
        {
            H = h;
            S = s;
            V = v;
            A = a;
        }

        private Hsva(Color color)
        {
            var (r, g, b, a) = color;
            var (min, max) = new[] { r, g, b }.Extrema();
            var delta = max - min;
            H = 0.0;
            S = max != 0 ? delta / max : 0;
            V = max;
            A = a;
            if (S == 0)
                return;

            if (r == max)
                H = (g - b) / delta;
            else if (g == max)
                H = (b - r) / delta + 2.0;
            else if (b == max)
                H = (r - g) / delta + 4.0;

            H *= 60.0;
            if (H < 0)
                H += 360.0;
        }

        public Color ToColor()
        {
            var r = 0d;
            var g = 0d;
            var b = 0d;

            var i = Math.Floor(H / 60);
            var f = H / 60 - i;
            var p = V * (1 - S);
            var q = V * (1 - f * S);
            var t = V * (1 - (1 - f) * S);

            switch (i % 6)
            {
                case 0:
                    r = V;
                    g = t;
                    b = p;
                    break;
                case 1:
                    r = q;
                    g = V;
                    b = p;
                    break;
                case 2:
                    r = p;
                    g = V;
                    b = t;
                    break;
                case 3:
                    r = p;
                    g = q;
                    b = V;
                    break;
                case 4:
                    r = t;
                    g = p;
                    b = V;
                    break;
                case 5:
                    r = V;
                    g = p;
                    b = q;
                    break;
            }

            return new Color((byte) (r * 255), (byte) (g * 255), (byte) (b * 255), (byte) (A * 255));
        }

        public static implicit operator Hsva(Color color)
        {
            return new Hsva(color);
        }

        public static implicit operator Color(Hsva hsva)
        {
            return hsva.ToColor();
        }

        public string Repr()
        {
            return (H, S, V, A).Repr();
        }
    }
}