using System;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using phytestcs.Interface.Windows;
using TGUI;

namespace phytestcs.Interface
{
    public class TextField<T> : Panel
    {
        public TextField(PropertyReference<T> bindProp, string name, PropConverter<T, string> conv = null)
        {
            (_getter, _setter) = bindProp.GetAccessors();
            name ??= bindProp.DisplayName;
            if (conv?.NameFormat != null)
                name = string.Format(CultureInfo.InvariantCulture, conv.NameFormat, name);
            converter = conv ?? PropConverter.Default<T, string>();

            UI.Drawn += Update;

            SizeLayout = new Layout2d("100%", "24");
            var lblName = new Label(name) {PositionLayout = new Layout2d("0", "3")};
            var lX = lblName.Size.X;
            Add(lblName);

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
                try
                {
                    Value = s.Value;
                }
                catch
                {
                    //
                }
            }

            Field.ReturnKeyPressed += OnValidated;
        }

        public EditBox Field { get; }

        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private readonly PropConverter<T, string> converter;
        private string _value;

        public string Value
        {
            get => _value;
            set
            {
                try
                {
                    _setter?.Invoke(converter.Set(value, _getter()));

                    Simulation.UpdatePhysicsInternal(0);

                    UpdateUI(value);

                    _value = value;
                }
                catch
                {
                    //
                }
            }
        }

        private void UpdateUI(string val)
        {
            Field.Text = val.ToString(CultureInfo.CurrentCulture);
        }

        private string? _oldLoadedVal;

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