using System;
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
        private static (double h, double s, double v) RgbToHsv(double r, double g, double b)
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