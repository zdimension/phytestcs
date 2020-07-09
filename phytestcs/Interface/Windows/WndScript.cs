using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using phytestcs.Objects;
using SFML.System;
using TGUI;
using Object = phytestcs.Objects.Object;

namespace phytestcs.Interface.Windows
{
    public class WndScript : WndBase<Object>
    {
        public WndScript(Object obj, Vector2f pos)
            : base(obj, obj.Name, 440, pos)
        {
            foreach (var prop in obj
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .OrderBy(p => p.Name))
            {
                object converter = null;
                var type = prop.PropertyType;

                if (type == typeof(float))
                {
                    converter = PropConverter.FloatString;
                }
                else if (type == typeof(Vector2f))
                {
                    converter = PropConverter.Vector2fString;
                }
                else
                {
                    continue;
                }

                var propRef = Activator.CreateInstance(typeof(PropertyReference<>).MakeGenericType(type),
                    prop, obj);
                Add((Widget)Activator.CreateInstance(typeof(TextField<>).MakeGenericType(type), propRef, prop.Name, converter));
            }
            
            /*Add(new TextField<float>(0.001f, 100f, bindProp: () => obj.Density, log: true));
            Add(new TextField<float>(0.001f, 1000f, bindProp: () => obj.Mass, log: true));
            Add(new TextField<float>(0, 2, bindProp: () => obj.Friction) { RightValue = float.PositiveInfinity });
            Add(new TextField<float>(0, 1, bindProp: () => obj.Restitution));
            Add(new TextField<float>(0.01f, 100, bindProp: () => obj.Attraction, log: true) { LeftValue = 0 });*/
            Show();
        }
    }

    public sealed class PropertyReference
    {
        public static PropertyReference<T> FromExpression<T>(Expression<Func<T>> property)
        {
            var member = (MemberExpression) property.Body;
            var info = (PropertyInfo) member.Member;
            object target = null;
            
            if (!info.GetGetMethod()!.IsStatic)
            {
                var fieldOnClosureExpression = (MemberExpression) member.Expression;
                target = ((FieldInfo) fieldOnClosureExpression.Member).GetValue(
                    ((ConstantExpression) fieldOnClosureExpression.Expression).Value);
            }

            return new PropertyReference<T>(info, target);
        }
    }

    public class PropertyReference<T>
    {
        public PropertyInfo Property { get; }
        public object Target { get; set; }
        
        public PropertyReference(PropertyInfo property, object target)
        {
            Property = property;
            Target = target;
        }

        public static implicit operator PropertyReference<T>(Expression<Func<T>> property)
        {
            return PropertyReference.FromExpression(property);
        }
        
        public (Func<T> getter, Action<T>? setter) GetAccessors()
        {
            Debug.Assert(Property != null);
            return (
                (Func<T>)Property.GetGetMethod()!.CreateDelegate(typeof(Func<T>), Target),
                (Action<T>)Property.GetSetMethod()?.CreateDelegate(typeof(Action<T>), Target)
            );
        }

        public PropertyReference<T> ToPropertyReference()
        {
            throw new NotImplementedException();
        }
    }
}