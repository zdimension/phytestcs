using System;
using System.Linq.Expressions;
using System.Reflection;

namespace phytestcs.Interface
{
    public class PropertyReference<T>
    {
        public PropertyReference(PropertyInfo property, object? target)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Target = target;

            Getter = (Func<T>) property.GetGetMethod()!.CreateDelegate(typeof(Func<T>), target)!;
            Setter = (Action<T>) property.GetSetMethod()?.CreateDelegate(typeof(Action<T>), target)!;
        }

        public PropertyReference(Func<T> getter, Action<T>? setter = null, MemberInfo? member = null)
        {
            Getter = getter;
            Setter = setter;
            Property = member;
        }

        public MemberInfo? Property { get; }
        public object? Target { get; }

        public Func<T> Getter { get; }
        public Action<T>? Setter { get; }
        public bool ReadOnly => Setter == null;
        public string? Unit => Property?.GetObjProp()?.Unit;
        public string? DisplayName => Property?.GetObjProp()?.DisplayName ?? Property?.Name;

        public static implicit operator PropertyReference<T>(Expression<Func<T>> property)
        {
            return PropertyReference.FromExpression(property);
        }

        public (Func<T> getter, Action<T>? setter) GetAccessors()
        {
            return (Getter, Setter);
        }
    }

    public static class PropertyReference
    {
        public static PropertyReference<T> FromExpression<T>(Expression<Func<T>> property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            var member = (MemberExpression) property.Body;

            switch (member.Member)
            {
                case PropertyInfo info:
                    object? target = null;

                    if (!info.GetGetMethod()!.IsStatic)
                    {
                        var fieldOnClosureExpression = (MemberExpression) member.Expression;
                        target = fieldOnClosureExpression.GetValueDeep();
                    }

                    return new PropertyReference<T>(info, target);
                default:
                    var param = Expression.Parameter(typeof(T));
                    return new PropertyReference<T>(
                        Expression.Lambda<Func<T>>(member).Compile(),
                        Expression.Lambda<Action<T>>(Expression.Assign(member, param), param).Compile(),
                        member.Member
                    );
            }
        }
    }
}

namespace phytestcs
{
    public static partial class Tools
    {
        public static object? GetValueDeep(this MemberExpression expr)
        {
            if (expr == null) throw new ArgumentNullException(nameof(expr));

            return ((FieldInfo) expr.Member).GetValue(expr.Expression switch
            {
                ConstantExpression cons => cons.Value,
                MemberExpression memb => memb.GetValueDeep(),
                _ => throw new NotImplementedException()
            });
        }
    }
}