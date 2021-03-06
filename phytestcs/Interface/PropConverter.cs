﻿using System;
using System.Data;
using System.Globalization;
using phytestcs.Internal;
using SFML.Graphics;
using SFML.System;
using static phytestcs.Tools;

namespace phytestcs.Interface
{
    public static class PropConverter
    {
        public static readonly PropConverter<Vector2f, float> VectorNorm = new PropConverter<Vector2f, float>(
            o => o.Norm(), (value, old) => old == default ? new Vector2f(value, 0) : old.Normalize() * value);

        public static readonly PropConverter<Vector2f, float> VectorX = new PropConverter<Vector2f, float>(
            o => o.X, (value, old) => new Vector2f(value, old.Y), L["{0} (X)"]);

        public static readonly PropConverter<Vector2f, float> VectorY = new PropConverter<Vector2f, float>(
            o => o.Y, (value, old) => new Vector2f(old.X, value), L["{0} (Y)"]);

        public static readonly PropConverter<Vector2f, float> VectorAngle = new PropConverter<Vector2f, float>(
            o => o.Angle(), (value, old) => FromPolar(old.Norm(), value), L["{0} (θ)"]);

        public static readonly PropConverter<float, float> AngleDegrees = new PropConverter<float, float>(
            o => o.Degrees(), (value, old) => value.Radians());

        public static readonly PropConverter<float, float> AngleDegreesNoWrap = new PropConverter<float, float>(
            o => o.DegreesNoWrap(), (value, old) => value.Radians());

        public static readonly PropConverter<Vector2f, float> VectorAngleDeg = VectorAngle.Then(AngleDegrees);

        public static readonly PropConverter<bool, string> BoolString = new PropConverter<bool, string>(
            o => o.Repr(),
            (value, old) => value.ToUpperInvariant() switch
            {
                "TRUE" => true,
                "FALSE" => false,
                _ => throw new SyntaxErrorException()
            });

        public static readonly PropConverter<float, string> FloatString = new PropConverter<float, string>(
            o => o.ToString(CultureInfo.InvariantCulture),
            (value, old) => (float) double.Parse(value, CultureInfo.InvariantCulture));

        public static readonly PropConverter<Vector2f, string> Vector2FString = new PropConverter<Vector2f, string>(
            o => o.Repr(), (value, old) =>
            {
                var (x, y) = value.Eval<(double, double)>().Result;
                return new Vector2f((float) x, (float) y);
            });

        public static readonly PropConverter<Color, string> ColorString = new PropConverter<Color, string>(
            o => o.Repr(), (value, old) =>
            {
                var (r, g, b, a) = value.Eval<(int, int, int, int)>().Result;
                return new Color((byte) r, (byte) g, (byte) b, (byte) a);
            });

        public static readonly PropConverter<Hsva, string> ColorHsvaString = new PropConverter<Hsva, string>(
            o => o.Repr(), (value, old) =>
            {
                var (h, s, v, a) = value.Eval<(double, double, double, double)>().Result;
                return new Hsva(h, s, v, a);
            });

        public static PropConverter<EventWrapper<T>, string> EventWrapper<T>()
            where T : Delegate
        {
            return new PropConverter<EventWrapper<T>, string>(
                lsw => lsw.Wrapper.Code, (value, old) =>
                {
                    old.Wrapper.Code = value;
                    return old;
                });
        }

        public static PropConverter<TOrig, TDisp> Default<TOrig, TDisp>()
        {
            return new PropConverter<TOrig, TDisp>(
                o => (TDisp) Convert.ChangeType(o, typeof(TDisp), CultureInfo.CurrentCulture)!,
                (value, old) => (TOrig) Convert.ChangeType(value, typeof(TOrig), CultureInfo.CurrentCulture)!);
        }

        public static PropConverter<T, string> EvalString<T>()
        {
            return new PropConverter<T, string>(o => o?.ToString() ?? "null", (value, old) => value.Eval<T>().Result);
        }
    }

    public sealed class PropConverter<TOrig, TDisp>
    {
        public delegate TDisp Getter(TOrig o);

        public delegate TOrig Setter(TDisp value, TOrig old);

        public PropConverter(Getter get, Setter set, string? fmt = null)
        {
            Get = get;
            Set = set;
            NameFormat = fmt;
        }

        public Getter Get { get; }
        public Setter Set { get; }
        public string? NameFormat { get; }

        public PropConverter<TOrig, TOut> Then<TOut>(PropConverter<TDisp, TOut> next)
        {
            return new PropConverter<TOrig, TOut>(o => next.Get(Get(o)),
                (value, old) => Set(next.Set(value, default!), old));
        }
    }
}