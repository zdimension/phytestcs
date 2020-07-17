using System;
using System.Globalization;
using SFML.System;
using static phytestcs.Global;

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
            o => o.Angle(), (value, old) => Tools.FromPolar(old.Norm(), value), L["{0} (θ)"]);

        public static readonly PropConverter<float, float> AngleDegrees = new PropConverter<float, float>(
            o => o.Degrees(), (value, old) => value.Radians());

        public static readonly PropConverter<Vector2f, float> VectorAngleDeg = VectorAngle.Then(AngleDegrees);
        
        public static readonly PropConverter<float, string> FloatString  = new PropConverter<float, string>(
            o => o.ToString(CultureInfo.InvariantCulture), (value, old) => (float)double.Parse(value, CultureInfo.InvariantCulture));
        
        public static readonly PropConverter<Vector2f, string> Vector2fString  = new PropConverter<Vector2f, string>(
            o => (o.X, o.Y).ToString(CultureInfo.InvariantCulture), (value, old) =>
            {
                var (x, y) = value.Eval<(double, double)>();
                return new Vector2f((float)x, (float)y);
            });
        
        public static PropConverter<Torig, Tdisp> Default<Torig, Tdisp>()
        {
            return new PropConverter<Torig, Tdisp>(o => (Tdisp)Convert.ChangeType(o, typeof(Tdisp), CultureInfo.CurrentCulture),
                (value, old) => (Torig)Convert.ChangeType(value, typeof(Torig), CultureInfo.CurrentCulture));
        }

        public static PropConverter<T, string> EvalString<T>()
        {
            return new PropConverter<T, string>(o => o.ToString(), (value, old) => value.Eval<T>());
        }
    }

    public class PropConverter<Torig, Tdisp>
    {
        public delegate Tdisp Getter(Torig o);
        public delegate Torig Setter(Tdisp value, Torig old);

        public Getter Get { get; }
        public Setter Set { get; }
        public string? NameFormat { get; }

        public PropConverter(Getter get, Setter set, string? fmt=null)
        {
            Get = get;
            Set = set;
            NameFormat = fmt;
        }

        public PropConverter<Torig, Tout> Then<Tout>(PropConverter<Tdisp, Tout> next)
        {
            return new PropConverter<Torig, Tout>(o => next.Get(Get(o)), (value, old) => Set(next.Set(value, default), old));
        }
    }
}