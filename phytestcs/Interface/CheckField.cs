using System;
using System.Linq.Expressions;
using TGUI;

namespace phytestcs.Interface
{
    public sealed class CheckField : Panel
    {
        private readonly Func<bool>? _getter;
        private readonly Action<bool>? _setter;

        private bool? _oldLoadedVal;
        private bool _uiLoading;
        private bool _value;

        public CheckField(Expression<Func<bool>> bindProp, string? name = null, Action? onChanged=null)
            : this(name, PropertyReference.FromExpression(bindProp), onChanged)
        {
        }

        public CheckField(string? name = null, PropertyReference<bool>? bindProp = null, Action? onChanged=null)
        {
            if (bindProp != null)
            {
                (_getter, _setter) = bindProp.GetAccessors();
                name ??= bindProp.DisplayName;

                Ui.Drawn += Update;
            }

            Field = new CheckBox(name) { SizeLayout = new Layout2d("20", "20"), PositionLayout = new Layout2d(10, 2) };
            Field.Toggled += (sender, f) =>
            {
                if (_uiLoading)
                    return;

                Value = f.Value;
            };

            if (bindProp != null && _setter == null)
                Field.Enabled = false;

            Add(Field);

            SizeLayout = new Layout2d("100%", "24");

            OnChanged = onChanged;
        }

        public Action? OnChanged { get; set; }

        public CheckBox Field { get; }

        public bool Value
        {
            get => _value;
            set
            {
                _setter?.Invoke(value);
                
                OnChanged?.Invoke();

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

        private void Update()
        {
            var val = _getter!();

            if (val != _oldLoadedVal)
            {
                UpdateUi(val);
                _oldLoadedVal = val;
            }

            _value = val;
        }
    }
}