using System;
using System.Globalization;
using System.Linq.Expressions;
using TGUI;

namespace phytestcs.Interface
{
    public sealed class NumberField<T> : Panel
    {
        private readonly PropConverter<T, float>? _converter;
        private readonly Func<T>? _getter;
        private readonly Action<T>? _setter;

        private float? _oldLoadedVal;
        private bool _uiLoading;
        private float _value;

        public NumberField(float min, float max,
            Expression<Func<T>> bindProp, string? name = null, float val = 0, string? unit = null,
            bool deci = true,
            bool log = false, float step = 0.01f,
            PropConverter<T, float>? conv = null, float factor = 1, bool inline = false, int? round = null)
            : this(min, max, PropertyReference.FromExpression(bindProp), name, val, unit, deci,
                log, step, conv, factor, inline, round)
        {
        }

        public NumberField(float min, float max,
            PropertyReference<T> bindProp, string? name = null, float val = 0, string? unit = null,
            bool deci = true,
            bool log = false, float step = 0.01f,
            PropConverter<T, float>? conv = null, float factor = 1, bool inline = false, int? round = 2)
        {
            Log = log;
            Factor = factor;

            if (bindProp != null)
            {
                (_getter, _setter) = bindProp.GetAccessors();
                name ??= bindProp.DisplayName;
                unit ??= bindProp.Unit;
                _converter = conv ?? PropConverter.Default<T, float>();

                Ui.Drawn += Update;
            }

            name ??= "";
            unit ??= "";

            if (conv?.NameFormat != null)
                name = string.Format(CultureInfo.InvariantCulture, conv.NameFormat, name);

            SizeLayout = new Layout2d("100%", inline ? "24" : "60");
            var lblName = new Label(name) { PositionLayout = new Layout2d(5, inline ? 4 : 10) };
            lblName.CeilSize();
            Add(lblName, "lblName");

            var lblUnit = new Label(unit);
            Add(lblUnit, "lblUnit");
            lblUnit.PositionLayout = new Layout2d("&.w - w - 5", inline ? "4" : "10");
            lblUnit.SizeLayout = new Layout2d(inline ? 20 : string.IsNullOrWhiteSpace(unit) ? 0 : lblUnit.Size.X, 18);

            const int size = 40;
            Field = new EditBox
            {
                PositionLayout = new Layout2d(inline ? $"lblUnit.left - 5 - {size}" : "lblName.right + 5", inline ? "3" : "9"),
                SizeLayout = new Layout2d(inline ? $"{size}" : "lblUnit.left - 5 - x", "18")
            };
            Add(Field, "txtValue");

            if (_setter == null)
                Field.ReadOnly = true;

            void OnValidated(object? sender, SignalArgsString s)
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
                    var res = s.Value.Eval<object>().Result;
                    Value = Convert.ToSingle(res, CultureInfo.CurrentCulture);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            Field.ReturnKeyPressed += OnValidated;

            float smin, smax;
            if (Log)
                (smin, smax) = ((float) Math.Log10(min), (float) Math.Log10(max));
            else
                (smin, smax) = (min, max);

            Slider = new Slider(smin, smax);
            if (!Log)
                Slider.Step = step;
            else
                Slider.Step = 0;
            var arr = -(int) Math.Log10(step);
            Slider.SizeLayout = new Layout2d(inline ? $"txtValue.left - lblName.right - 18" : "parent.iw - 20", "22");
            Slider.PositionLayout = new Layout2d(inline ? "lblName.width + 13" : "10", inline ? "7" : "37");
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
                        sv = (float) Math.Round(Math.Pow(10, sv), arr);
                }

                Value = sv;
            };

            Round = round;
        }

        public EditBox Field { get; }
        public Slider Slider { get; }
        public float Factor { get; set; }
        public int? Round { get; set; }

        public Func<float, bool> Validation { get; set; } = x => true;

        public float Value
        {
            get => _value;
            set
            {
                if (Validation(value))
                {
                    _setter?.Invoke(_converter!.Set(value / Factor, _getter!()));

                    Simulation.UpdatePhysicsInternal(0);

                    UpdateUi(value);

                    _value = value;
                }
            }
        }

        public float? LeftValue { get; set; }
        public float? RightValue { get; set; }

        public bool Log { get; set; }

        private void UpdateUi(float val)
        {
            _uiLoading = true;
            Slider.Value = Log ? (float) Math.Log10(val) : val;
            _uiLoading = false;
            if (Round != null)
                val = (float) Math.Round(val, Round.Value);
            Field.Text = val.ToString(CultureInfo.CurrentCulture);
        }

        public void Update()
        {
            var val = Factor * _converter!.Get(_getter!());

            if (val != _oldLoadedVal)
            {
                UpdateUi(val);
                _oldLoadedVal = val;
            }

            _value = val;
        }
    }
}