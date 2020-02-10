using System;
using System.Globalization;
using System.Reflection;
using TGUI;
using CheckBox = TGUI.CheckBox;
using Label = TGUI.Label;
using MessageBox = System.Windows.Forms.MessageBox;
using Panel = TGUI.Panel;

namespace phytestcs.Interface
{
    public class TextField: Panel
    {
        public TextField(string name, float min, float max, float val=0, string unit="", bool deci=true, object bindObj=null, string bindProp=null, bool log=false, float step=0.01f)
        {
            Log = log;

            if (bindObj != null)
            {
                Type t;

                if (bindObj is Type o)
                {
                    t = o;
                }
                else
                {
                    t = bindObj.GetType();
                    BindObject = bindObj;
                }

                BindPropInfo = t.GetProperty(bindProp);

                UI.Drawn += Update;
            }

            SizeLayout = new Layout2d("100%", "60");
            var lblName = new Label(name);
            lblName.PositionLayout = new Layout2d("0", "3");
            var lX = lblName.Size.X;
            Add(lblName);
            if (!string.IsNullOrWhiteSpace(unit))
            {
                var lblUnité = new Label(unit);
                lX += lblUnité.Size.X;
                Add(lblUnité);
                lblUnité.PositionLayout = new Layout2d("&.w - w", "3");
            }
            Field = new EditBox();
            Field.PositionLayout = new Layout2d(lblName.Size.X + 5, 3);
            Field.SizeLayout = new Layout2d("100% - 10 - " + lX.ToString(CultureInfo.InvariantCulture), "18");
            Add(Field);
            Field.ReturnKeyPressed += delegate(object sender, SignalArgsString s)
            {
                if (deci)
                {
                    if (float.TryParse(s.Value, out var res))
                        Value = res;
                }
                else
                {
                    if (int.TryParse(s.Value, out var res))
                        Value = res;
                }
            };
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

        public EditBox Field { get; private set; }
        public Slider Slider { get; private set; }

        public Func<float, bool> Validation { get; set; } = x => true;
        public object BindObject { get; set; }
        public PropertyInfo BindPropInfo { get; set; }

        private float _value;
        public float Value
        {
            get => _value;
            set
            {
                if (Validation(value))
                {
                    BindPropInfo?.SetValue(BindObject, Convert.ChangeType(value, BindPropInfo.PropertyType));

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
            Field.Text = val.ToString();
        }

        private float? _oldLoadedVal;

        public void Update()
        {
            var val = Convert.ToSingle(BindPropInfo?.GetValue(BindObject));

            if (val != _oldLoadedVal)
            {
                UpdateUI(val);
                _oldLoadedVal = val;
            }

            _value = val;
        }
    }
}
