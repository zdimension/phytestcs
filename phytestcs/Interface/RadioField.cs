using TGUI;

namespace phytestcs.Interface
{
    public sealed class RadioField : Panel
    {
        private bool _uiLoading;
        private bool _value;

        public RadioField(string name)
        {
            Field = new CheckBox(name) { SizeLayout = new Layout2d("20", "20"), PositionLayout = new Layout2d(10, 3) };
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

        public bool Value
        {
            get => _value;
            set
            {
                Simulation.UpdatePhysicsInternal(0);

                UpdateUi(value);

                _value = value;
            }
        }

        private void UpdateUi(bool val)
        {
            _uiLoading = true;
            Field.Checked = val;
            _uiLoading = false;
        }
    }
}