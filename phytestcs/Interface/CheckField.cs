using System;
using System.Linq.Expressions;
using System.Reflection;
using phytestcs.Objects;
using TGUI;

namespace phytestcs.Interface
{
    public class CheckField : Panel
    {
        public CheckField(string name=null, Expression<Func<bool>> bindProp=null)
        {
            if (bindProp != null)
            {
                var expr = ((MemberExpression) bindProp.Body);
                var BindPropInfo = (PropertyInfo) expr.Member;
                var getMethod = BindPropInfo.GetGetMethod();
                object target = null;

                if (!getMethod.IsStatic)
                {
                    var fieldOnClosureExpression = (MemberExpression) expr.Expression;
                    target = ((FieldInfo) fieldOnClosureExpression.Member).GetValue(
                        ((ConstantExpression) fieldOnClosureExpression.Expression).Value);
                }

                _getter = (Func<bool>) getMethod.CreateDelegate(typeof(Func<bool>), target);
                _setter = (Action<bool>) BindPropInfo.GetSetMethod().CreateDelegate(typeof(Action<bool>), target);

                var attr = BindPropInfo.GetCustomAttribute<ObjPropAttribute>();

                if (attr != null)
                {
                    name ??= attr.DisplayName;
                }

                name ??= BindPropInfo.Name;

                UI.Drawn += Update;
            }

            Field = new CheckBox(name);
            Field.SizeLayout = new Layout2d("20", "20");
            Field.PositionLayout = new Layout2d(10, 5);
            Field.Toggled += (sender, f) =>
            {
                if (_uiLoading)
                    return;

                Value = f.Value;
            };

            Add(Field);

            SizeLayout = new Layout2d("100%", "30");
        }

        public CheckBox Field { get; private set; }
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