using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using phytestcs.Objects;
using SFML.System;
using TGUI;

namespace phytestcs.Interface
{
    public class TextField<T>: Panel
    {
        public TextField(float min, float max, string name=null, float val = 0, string unit = null, bool deci = true,
            Expression<Func<T>> bindProp = null, bool log = false, float step = 0.01f,
            PropConverter<T, float> conv = null)
        {
            Log = log;

            if (bindProp != null)
            {
                (_getter, _setter) = bindProp.GetAccessors();
                name ??= bindProp.GetDisplayName();
                unit ??= bindProp.GetObjProp()?.Unit;
                converter = conv ?? PropConverter.Default<T, float>();
                
                UI.Drawn += Update;
            }

            name ??= "";
            unit ??= "";

            SizeLayout = new Layout2d("100%", "60");
            var lblName = new Label(name) {PositionLayout = new Layout2d("0", "3")};
            var lX = lblName.Size.X;
            Add(lblName);
            
            if (!string.IsNullOrWhiteSpace(unit))
            {
                var lblUnité = new Label(unit);
                lX += lblUnité.Size.X;
                Add(lblUnité);
                lblUnité.PositionLayout = new Layout2d("&.w - w", "3");
            }

            Field = new EditBox
            {
                PositionLayout = new Layout2d(lblName.Size.X + 5, 3),
                SizeLayout = new Layout2d("100% - 10 - " + lX.ToString(CultureInfo.InvariantCulture), "18")
            };
            Add(Field);

            void OnValidated(object sender, SignalArgsString s)
            {
                if (deci)
                {
                    if (float.TryParse(s.Value, out var res))
                    {
                        Value = res;
                        return;
                    }
                }
                else
                {
                    if (int.TryParse(s.Value, out var res))
                    {
                        Value = res;
                        return;
                    }
                }

                try
                {
                    var val = CSharpScript.EvaluateAsync(s.Value).Result;
                    Value = Convert.ToSingle(val, CultureInfo.CurrentCulture);
                }
                catch
                {
                    //
                }
            }
            
            Field.ReturnKeyPressed += OnValidated;

            float smin, smax;
            if (Log)
            {
                (smin, smax) = ((float)Math.Log10(min), (float)Math.Log10(max));
            }
            else
            {
                (smin, smax) = (min, max);
            }
            Slider = new Slider(smin, smax);
            if (!Log)
                Slider.Step = step;
            else
                Slider.Step = 0;
            var arr = -(int)Math.Log10(step);
            Slider.SizeLayout = new Layout2d("100% - 20", "10");
            Slider.PositionLayout = new Layout2d(10, 30);
            if (bindProp == null)
                Value = val;
            Add(Slider);
            Slider.ValueChanged += (sender, f) =>
            {
                if (_uiLoading)
                    return;

                var sv = f.Value;

                if (sv == Slider.Minimum && LeftValue != null)
                {
                    sv = LeftValue.Value;
                }
                else if (sv == Slider.Maximum && RightValue != null)
                {
                    sv = RightValue.Value;
                }
                else
                {
                    if (Log)
                    {
                        sv = (float) Math.Round(Math.Pow(10, sv), arr);
                    }
                }

                Value = sv;
            };
        }

        public EditBox Field { get; }
        public Slider Slider { get; }

        public Func<float, bool> Validation { get; set; } = x => true;
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private readonly PropConverter<T, float> converter;
        private float _value;
        public float Value
        {
            get => _value;
            set
            {
                if (Validation(value))
                {
                    _setter?.Invoke(converter.Set(value, _getter()));

                    Simulation.UpdatePhysicsInternal(0);

                    UpdateUI(value);

                    _value = value;
                }
            }
        }

        public float? LeftValue { get; set; }
        public float? RightValue { get; set; }

        public bool Log { get; set; }
        private bool _uiLoading;
 
        private void UpdateUI(float val)
        {
            _uiLoading = true;
            Slider.Value = Log ? (float)Math.Log10(val) : val;
            _uiLoading = false;
            Field.Text = val.ToString(CultureInfo.CurrentCulture);
        }

        private float? _oldLoadedVal;

        public void Update()
        {
            var val = converter.Get(_getter());

            if (val != _oldLoadedVal)
            {
                UpdateUI(val);
                _oldLoadedVal = val;
            }

            _value = val;
        }
    }

    public static class PropConverter
    {
        public static readonly PropConverter<Vector2f, float> VectorNorm = new PropConverter<Vector2f, float>(
            o => o.Norm(), (value, old) => old == default ? new Vector2f(value, 0) : old.Normalize() * value);

        public static readonly PropConverter<Vector2f, float> VectorX = new PropConverter<Vector2f, float>(
            o => o.X, (value, old) => new Vector2f(value, old.Y));

        public static readonly PropConverter<Vector2f, float> VectorY = new PropConverter<Vector2f, float>(
            o => o.Y, (value, old) => new Vector2f(old.X, value));

        public static readonly PropConverter<Vector2f, float> VectorAngle = new PropConverter<Vector2f, float>(
            o => o.Angle(), (value, old) => Tools.FromPolar(old.Norm(), value));

        public static readonly PropConverter<float, float> AngleDegrees = new PropConverter<float, float>(
            o => o.Degrees(), (value, old) => value.Radians());

        public static readonly PropConverter<Vector2f, float> VectorAngleDeg = VectorAngle.Then(AngleDegrees);

        public static PropConverter<Torig, Tdisp> Default<Torig, Tdisp>()
        {
            return new PropConverter<Torig, Tdisp>(o => (Tdisp)Convert.ChangeType(o, typeof(Tdisp), CultureInfo.CurrentCulture),
                (value, old) => (Torig)Convert.ChangeType(value, typeof(Torig), CultureInfo.CurrentCulture));
        }
    }

    public class PropConverter<Torig, Tdisp>
    {
        public delegate Tdisp Getter(Torig o);
        public delegate Torig Setter(Tdisp value, Torig old);

        public Getter Get { get; }
        public Setter Set { get; }

        public PropConverter(Getter get, Setter set)
        {
            Get = get;
            Set = set;
        }

        public PropConverter<Torig, Tout> Then<Tout>(PropConverter<Tdisp, Tout> next)
        {
            return new PropConverter<Torig, Tout>(o => next.Get(Get(o)), (value, old) => Set(next.Set(value, default), old));
        }
    }
}
