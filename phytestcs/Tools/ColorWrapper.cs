using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using phytestcs.Interface;
using SFML.Graphics;

namespace phytestcs
{
    public class ColorWrapper
    {
        private readonly Func<Color> getter;
        private readonly Action<Color> setter;

        public ColorWrapper(Expression<Func<Color>> bindProp)
            : this(PropertyReference.FromExpression(bindProp))
        {
        }
        
        public ColorWrapper(PropertyReference<Color> bindProp)
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
        
        public byte A
        {
            get => getter().A;
            set => setter(new Color(getter()) { A = value });
        }
        
        public double Ad
        {
            get => getter().A / 255d;
            set => setter(new Color(getter()) { A = (byte)(value * 255) });
        }

        public double H
        {
            get => ValueHSV.H;
            set
            {
                var hsv = ValueHSV;
                hsv.H = value;
                setter(hsv.ToColor());
            }
        }

        public double S
        {
            get => ValueHSV.S;
            set
            {
                var hsv = ValueHSV;
                hsv.S = value;
                setter(hsv.ToColor());
            }
        }

        public double V
        {
            get => ValueHSV.V;
            set
            {
                var hsv = ValueHSV;
                hsv.V = value;
                setter(hsv.ToColor());
            }
        }

        private HSVA ValueHSV => getter();
    }

    public struct HSVA
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

        public HSVA(double h, double s, double v, double a)
        {
            H = h;
            S = s;
            V = v;
            A = a;
        }

        private HSVA(Color color)
        {
            var (r, g, b, a) = color;
            var (min, max) = new[] {r, g, b}.Extrema();
            var delta = max - min;
            H = 0.0;
            S = max != 0 ? delta / max : 0;
            V = max;
            A = a;
            if (S == 0)
            {
                return;
            }
            if (r == max)
            {
                H = (g - b) / delta;
            }
            else if (g == max)
            {
                H = (b - r) / delta + 2.0;
            }
            else if (b == max)
            {
                H = (r - g) / delta + 4.0;
            }
            H *= 60.0;
            if (H < 0)
            {
                H += 360.0;
            }
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
                case 0: r = V; g = t; b = p; break;
                case 1: r = q; g = V; b = p; break;
                case 2: r = p; g = V; b = t; break;
                case 3: r = p; g = q; b = V; break;
                case 4: r = t; g = p; b = V; break;
                case 5: r = V; g = p; b = q; break;
            }

            return new Color((byte) (r * 255), (byte) (g * 255), (byte) (b * 255), (byte)(A * 255));
        }

        public static implicit operator HSVA(Color color)
        {
            return new HSVA(color);
        }

        public static implicit operator Color(HSVA hsva)
        {
            return hsva.ToColor();
        }
    }
}