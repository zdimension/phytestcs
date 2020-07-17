using System;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using TGUI;

namespace phytestcs.Interface
{
    public class NumberField<T>: Panel
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private readonly PropConverter<T, float> converter;

        private float? _oldLoadedVal;
        private bool _uiLoading;
        private float _value;

        public NumberField(float min, float max, string name = null, float val = 0, string unit = null, bool deci = true,
            Expression<Func<T>> bindProp = null, bool log = false, float step = 0.01f,
            PropConverter<T, float> conv = null)
        : this(min, max, name, val, unit, deci, PropertyReference.FromExpression(bindProp), log, step, conv)
        {
        }

        public NumberField(float min, float max, string name=null, float val = 0, string unit = null, bool deci = true,
            PropertyReference<T> bindProp = null, bool log = false, float step = 0.01f,
            PropConverter<T, float> conv = null)
        {
            Log = log;

            if (bindProp != null)
            {
                (_getter, _setter) = bindProp.GetAccessors();
                name ??= bindProp.DisplayName;
                unit ??= bindProp.Unit;
                converter = conv ?? PropConverter.Default<T, float>();
                
                UI.Drawn += Update;
            }

            name ??= "";
            unit ??= "";
            
            if (conv?.NameFormat != null)
                name = string.Format(CultureInfo.InvariantCulture, conv.NameFormat, name);

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

            if (_setter == null)
                Field.ReadOnly = true;

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

        private void UpdateUI(float val)
        {
            _uiLoading = true;
            Slider.Value = Log ? (float)Math.Log10(val) : val;
            _uiLoading = false;
            Field.Text = val.ToString(CultureInfo.CurrentCulture);
        }

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
}
