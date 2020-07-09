﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using phytestcs.Interface.Windows;
using phytestcs.Objects;
using TGUI;

namespace phytestcs.Interface
{
    public class CheckField : Panel
    {
        public CheckField(string name = null, Expression<Func<bool>> bindProp = null)
        : this(name, PropertyReference.FromExpression(bindProp))
        {
        }
        
        public CheckField(string name=null, PropertyReference<bool> bindProp=null)
        {
            if (bindProp != null)
            {
                (_getter, _setter) = bindProp.GetAccessors();
                name ??= bindProp.Property.GetDisplayName();

                UI.Drawn += Update;
            }

            Field = new CheckBox(name) {SizeLayout = new Layout2d("20", "20"), PositionLayout = new Layout2d(10, 5)};
            Field.Toggled += (sender, f) =>
            {
                if (_uiLoading)
                    return;

                Value = f.Value;
            };

            if (bindProp != null && _setter == null)
            {
                Field.Enabled = false;
            }

            Add(Field);

            SizeLayout = new Layout2d("100%", "30");
        }

        public CheckBox Field { get; }
        private readonly Func<bool> _getter;
        private readonly Action<bool> _setter;
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
                _setter?.Invoke(value);

                Simulation.UpdatePhysicsInternal(0);

                UpdateUI(value);

                _value = value;
            }
        }

        private bool? _oldLoadedVal;

        public void Update()
        {
            var val = _getter();

            if (val != _oldLoadedVal)
            {
                UpdateUI(val);
                _oldLoadedVal = val;
            }

            _value = val;
        }
    }
}