using System;
using System.Globalization;
using SFML.System;
using TGUI;

namespace phytestcs.Interface
{
    public class TextFieldBase : Panel
    {
        public EditBox Field { get; protected set; }
        public float NameWidth
        {
            get => NameLabel.Size.X;
            set => NameLabel.Size = new Vector2f(value, NameLabel.Size.Y);
        }
        public Label NameLabel { get; protected set; }
    }
    
    public class TextField<T> : TextFieldBase
    {
        private readonly Func<T> _getter;
        private readonly Action<T>? _setter;
        private readonly PropConverter<T, string> converter;

        private string? _oldLoadedVal;
        private string _value;

        public TextField(PropertyReference<T> bindProp, string? name, PropConverter<T, string> conv = null)
        {
            if (bindProp == null) throw new ArgumentNullException(nameof(bindProp));

            (_getter, _setter) = bindProp.GetAccessors();
            name ??= bindProp.DisplayName;
            if (conv?.NameFormat != null)
                name = string.Format(CultureInfo.InvariantCulture, conv.NameFormat, name);
            converter = conv ?? PropConverter.Default<T, string>();

            Ui.Drawn += Update;

            name ??= "";

            SizeLayout = new Layout2d("100%", "24");
            NameLabel = new Label(name) { PositionLayout = new Layout2d("0", "3") };
            Add(NameLabel, "lblName");

            Field = new EditBox
            {
                PositionLayout = new Layout2d("lblName.right + 5", "3"),
                SizeLayout = new Layout2d("100% - 10 - lblName.width", "18")
            };
            Add(Field);

            if (_setter == null)
                Field.ReadOnly = true;

            void Validate()
            {
                try
                {
                    Value = Field.Text = Field.Text.Trim();
                }
                catch
                {
                    //
                }
            }

            Field.Unfocused += (sender, args) =>
            {
                Validate();
            };

            Field.ReturnKeyPressed += (sender, s) =>
            {
                Validate();
            };
            /*if (!multiline)
            {
                Field.TextChanged += (sender, s) =>
                {
                    if (s.Value[^1] == '\n')
                        Validate();
                };
            }*/
        }

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
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private void UpdateUI(string val)
        {
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