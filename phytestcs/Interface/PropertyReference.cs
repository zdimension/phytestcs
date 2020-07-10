using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace phytestcs.Interface
{
    public class PropertyReference<T>
    {
        private readonly PropertyInfo _property;
        private readonly object _target;
        public Func<T> Getter { get; }
        public Action<T>? Setter { get; }
        public bool ReadOnly => Setter == null;
        public string? Unit => _property?.GetObjProp()?.Unit;
        public string? DisplayName => _property?.GetObjProp()?.DisplayName ?? _property?.Name;
        
        public PropertyReference(PropertyInfo property, object target)
        {
            _property = property;
            _target = target;
            Getter = (Func<T>) _property!.GetGetMethod()!.CreateDelegate(typeof(Func<T>), _target);
            Setter = (Action<T>) _property!.GetSetMethod()?.CreateDelegate(typeof(Action<T>), _target);
        }

        public PropertyReference(Func<T> getter, Action<T>? setter = null)
        {
            Getter = getter;
            Setter = setter;
        }

        public static implicit operator PropertyReference<T>(Expression<Func<T>> property)
        {
            return PropertyReference.FromExpression(property);
        }
        
        public (Func<T> getter, Action<T>? setter) GetAccessors()
        {
            return (Getter, Setter);
        }

        public PropertyReference<T> ToPropertyReference()
        {
            throw new NotImplementedException();
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
}