using System;
using System.Reflection;
using TGUI;

namespace phytestcs.Interface
{
    public class CheckField : Panel
    {
        public CheckField(string name, object bindObj, string bindProp)
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

            Field = new CheckBox(name);
            Field.SizeLayout = new Layout2d("20", "20");
            Field.PositionLayout = new Layout2d(10, 20);
            Field.Toggled += (sender, f) =>
            {
                if (_uiLoading)
                    return;

                Value = f.Value;
            };

            Add(Field);

            UI.Drawn += Update;

            SizeLayout = new Layout2d("100%", "60");
        }

        public CheckBox Field { get; private set; }
        public object BindObject { get; set; }
        public PropertyInfo BindPropInfo { get; set; }
        private bool _uiLoading;
        private void UpdateUI(bool val)
        {
            _uiLoading = true;
            Field.Checked = val;
            _uiLoading = false;
        }
        private bool _value;
        public bool Value
        {
            get => _value;
            set
            {
                BindPropInfo?.SetValue(BindObject, Convert.ChangeType(value, BindPropInfo.PropertyType));

                Simulation.UpdatePhysicsInternal(0);

                UpdateUI(value);

                _value = value;
            }
        }

        private bool? _oldLoadedVal;

        public void Update()
        {
            var val = Convert.ToBoolean(BindPropInfo?.GetValue(BindObject));

            if (val != _oldLoadedVal)
            {
                UpdateUI(val);
                _oldLoadedVal = val;
            }

            _value = val;
        }
    }
}