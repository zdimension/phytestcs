using System;
using System.Linq.Expressions;
using System.Reflection;
using phytestcs.Interface.Windows;
using phytestcs.Objects;
using TGUI;

namespace phytestcs.Interface
{
    public class RadioField : Panel
    {
        public RadioField(string name)
        {
            Field = new CheckBox(name) {SizeLayout = new Layout2d("20", "20"), PositionLayout = new Layout2d(10, 3)};
            Field.Toggled += (sender, f) =>
            {
                if (_uiLoading)
                    return;

                Value = f.Value;
            };

            Add(Field);

            SizeLayout = new Layout2d("100%", "24");
        }

        public RadioButton Field { get; }
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
                Simulation.UpdatePhysicsInternal(0);

                UpdateUI(value);

                _value = value;
            }
        }
    }
}