using System;
using phytestcs.Internal;
using phytestcs.Objects;
using SFML.Graphics;
using static phytestcs.Tools;

namespace phytestcs
{
    public struct ObjectAppearance
    {
        [ObjProp("Opaque borders")] public bool OpaqueBorders;
        [ObjProp("Draw circle cake")] public bool DrawCircleCakes;
        [ObjProp("Show ruler")] public bool Ruler;
        [ObjProp("Show forces")] public bool ShowForces;
        [ObjProp("Show protractor")] public bool Protractor;
        [ObjProp("Show momenta")] public bool ShowMomentums;
        [ObjProp("Show velocities")] public bool ShowVelocities;
        [ObjProp("Draw borders")] public bool Borders;

        public ObjectAppearance(bool fromDefault)
        {
            this = fromDefault ? Palette.Default.Appearance : default;
        }
    }

    public struct ColorRange
    {
        private readonly Hsva _start;
        private readonly Hsva _end;

        public ColorRange(Hsva start, Hsva end)
        {
            _start = start;
            _end = end;
        }
        
        public readonly Hsva RandomColor()
        {
            return new Hsva(
                Rng.NextDouble(_start.H, _end.H),
                Math.Sqrt(Rng.NextDouble(_start.S, _end.S)),
                Math.Pow(Rng.NextDouble(_start.V, _end.V), 1 / 3d),
                Rng.NextDouble(_start.A, _end.A)
            );
        }

        public override bool Equals(object? obj)
        {
            return obj is ColorRange range && Equals(range);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_start, _end);
        }

        private bool Equals(ColorRange other)
        {
            return _start == other._start && _end == other._end;
        }

        public static bool operator ==(ColorRange left, ColorRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ColorRange left, ColorRange right)
        {
            return !(left == right);
        }
    }

    public struct Palette
    {
        public ObjectAppearance Appearance;

        public bool DrawClouds;

        public Color SkyColor;

        public Color SelectionColor
        {
            get
            {
                var (_, s, v, _) = (Hsva) SkyColor;
                return v >= 0.75f + 0.83f * s ? Color.Black : Color.White;
            }
        }

        public ColorRange ColorRange;

        private Palette(bool fromDefault)
        {
            this = fromDefault ? Default : default;
        }

        public static readonly Palette Default = new Palette
        {
            Appearance = new ObjectAppearance
            {
                OpaqueBorders = true,
                DrawCircleCakes = true,
                Ruler = false,
                ShowForces = false,
                Protractor = false,
                ShowMomentums = false,
                ShowVelocities = false,
                Borders = true
            },
            DrawClouds = true,
            ColorRange = new ColorRange(new Hsva(0, 0, 0, 1), new Hsva(359.9, 1, 1, 1)),
            SkyColor = new Color(140, 140, 255, 255)
        };

        public static readonly (string name, Palette palette)[] Palettes =
        {
            (L["Default"], Default),
            (L["Autumn"], new Palette(true)
            {
                ColorRange = new ColorRange(new Hsva(64.756096, 1, 1, 1), new Hsva(338.41464, 0.78963417, 0.43258467, 1)),
                SkyColor = new Color(100, 144, 216, 255)
            }),
            (L["Black"], new Palette(true)
            {
                ColorRange = new ColorRange(new Hsva(0, 0, 0, 1), new Hsva(359.9, 0, 0.15, 1)),
                SkyColor = new Color(23, 47, 71, 255)
            }),
            (L["Blueprint"], new Palette(true)
            {
                Appearance = new ObjectAppearance(true)
                {
                    DrawCircleCakes = false
                },
                DrawClouds = false,
                ColorRange = new ColorRange(new Hsva(0, 0, 0, 0.02), new Hsva(359.9, 0, 1, 0.02)),
                SkyColor = new Color(0, 76, 153, 255)
            }),
            (L["Chalkboard"], new Palette(true)
            {
                DrawClouds = false,
                ColorRange = new ColorRange(new Hsva(152, 0, 0.625, 1), new Hsva(152, 0.02, 1, 1)),
                SkyColor = new Color(23, 48, 44, 255)
            }),
            (L["Dark"], new Palette(true)
            {
                ColorRange = new ColorRange(new Hsva(0, 0.32, 0.34, 1), new Hsva(359.9, 0.32, 0.34, 1)),
                SkyColor = new Color(249, 193, 98, 255)
            }),
            (L["Greyscale"], new Palette(true)
            {
                ColorRange = new ColorRange(new Hsva(0, 0, 0.10516606, 1), new Hsva(359.9, 0, 0.92682928, 1)),
                SkyColor = new Color(25, 25, 25, 255)
            }),
            (L["Ice"], new Palette(true)
            {
                DrawClouds = false,
                ColorRange = new ColorRange(new Hsva(202, 0, 1, 1), new Hsva(202, 0.235, 1, 1)),
                SkyColor = new Color(24, 88, 124, 255)
            }),
            (L["Light grey"], new Palette(true)
            {
                DrawClouds = false,
                ColorRange = new ColorRange(new Hsva(35, 0, 0.92968750, 1), new Hsva(345, 0, 0.7996875, 1)),
                SkyColor = new Color(255, 255, 255, 255)
            }),
            (L["Optics"], new Palette(true)
            {
                Appearance = new ObjectAppearance(true)
                {
                    DrawCircleCakes = false
                },
                DrawClouds = false,
                ColorRange = new ColorRange(new Hsva(0, 0, 0.15, 0.15), new Hsva(359.9, 0.6945, 1, 0.15)),
                SkyColor = new Color(0, 0, 0, 255)
            }),
            (L["Pastel"], new Palette(true)
            {
                Appearance = new ObjectAppearance(true)
                {
                    Borders = false
                },
                ColorRange = new ColorRange(new Hsva(0, 0.38, 0, 1), new Hsva(359.9, 0.38, 1, 1)),
                SkyColor = new Color(42, 42, 42, 255)
            }),
            (L["Sunset"], new Palette(true)
            {
                DrawClouds = false,
                ColorRange = new ColorRange(new Hsva(0, 0, 0, 0.93), new Hsva(359.9, 0, 0.13, 0.93)),
                SkyColor = new Color(255, 161, 55, 255)
            }),
            (L["Sweet"], new Palette(true)
            {
                ColorRange = new ColorRange(new Hsva(35, 0.735, 1, 1), new Hsva(345, 0, 0.1666667, 1)),
                SkyColor = new Color(122, 141, 226, 255)
            }),
            (L["White"], new Palette(true)
            {
                ColorRange = new ColorRange(new Hsva(135, 0, 0.9, 1), new Hsva(135, 0.001, 1, 1)),
                SkyColor = new Color(12, 12, 12, 255)
            }),
            (L["X-ray"], new Palette(true)
            {
                Appearance = new ObjectAppearance(true)
                {
                    DrawCircleCakes = false
                },
                DrawClouds = false,
                ColorRange = new ColorRange(new Hsva(0, 0, 1, 0), new Hsva(359.9, 0, 1, 0)),
                SkyColor = new Color(0, 0, 0, 255)
            })
        };
    }
}