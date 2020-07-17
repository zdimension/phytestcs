using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using phytestcs.Interface.Windows;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using TGUI;

namespace phytestcs
{
    public static class Extensions
    {
        public static float ClampWrap(this float angle, float bound)
        {
            if (angle > bound)
                angle = -bound + (angle % bound);
            else if (angle < -bound)
                angle = bound + (angle % bound);
            return angle;
        }

        public static float ClampWrapPositive(this float angle, float bound)
        {
            if (angle > bound)
                return angle % bound;
            if (angle < 0)
                return bound + angle % bound;
            return angle;
        }

        public static float Degrees(this float f)
        {
            return ((float) (f * 180 / Math.PI)).ClampWrap(180);
        }

        public static void Scatter<T0, T1, T2>(in this (T0 i0, T1 i1, T2 i2) t, Action<T0, T1, T2> a) =>
            a(t.i0, t.i1, t.i2);

        public static T Scatter<T0, T1, T2, T3, T>(in this (T0 i0, T1 i1, T2 i2, T3 i3) t, Func<T0, T1, T2, T3, T> a) =>
            a(t.i0, t.i1, t.i2, t.i3);

        public static T Scatter<T0, T1, T2, T>(in this (T0 i0, T1 i1, T2 i2) t, Func<T0, T1, T2, T> a) =>
            a(t.i0, t.i1, t.i2);

        public static T Scatter<T0, T1, T>(in this (T0 i0, T1 i1) t, Func<T0, T1, T> a) => a(t.i0, t.i1);

        public static float Radians(this float f)
        {
            return ((float) (f * Math.PI / 180)).ClampWrap((float) Math.PI);
        }

        public static RenderTexture RenderTexture(this Canvas c)
        {
            return c?.GetType().GetField("myRenderTexture", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(c) as RenderTexture;
        }

        public static T[] ToArrayLocked<T>(this SynchronizedCollection<T> coll)
        {
            lock (coll.SyncRoot)
            {
                return coll.ToArray();
            }
        }

        public static T[] ToArrayLocked<T, Tc>(this IEnumerable<T> coll, SynchronizedCollection<Tc> c)
        {
            lock (c.SyncRoot)
            {
                return coll.ToArray();
            }
        }

        public static T With<T>(this T obj, Action<T> map)
        {
            map(obj);
            return obj;
        }


        public static bool IsBetween<T>(this T x, T a, T b)
            where T : IComparable<T>
        {
            return x.CompareTo(a) >= 0 && x.CompareTo(b) <= 0;
        }


        public static CircleShape CenterOrigin(this CircleShape c)
        {
            c.Origin = new Vector2f(c.Radius, c.Radius);
            return c;
        }


        public static PropertyInfo GetPropertyInfo<T>(this Expression<Func<T>> prop)
        {
            return (PropertyInfo) ((MemberExpression) prop.Body).Member;
        }

        public static ObjPropAttribute GetObjProp(this MemberInfo prop)
        {
            return prop.GetCustomAttribute<ObjPropAttribute>();
        }

        public static T Eval<T>(this string s)
        {
            return CSharpScript.EvaluateAsync<T>(s).Result;
        }

        public static string? ToString<Ta, Tb>(this (Ta, Tb) tuple, CultureInfo culture)
            where Ta : IFormattable
            where Tb : IFormattable
        {
            return $"({tuple.Item1.ToString(null, culture)}, {tuple.Item2.ToString(null, culture)})";
        }

        public static string? GetDisplayName(this PropertyInfo prop)
        {
            return prop.GetObjProp()?.DisplayName ?? prop!.Name;
        }

        public static bool IsNaN(this Vector2f v)
        {
            return float.IsNaN(v.X) || float.IsNaN(v.Y);
        }
    }
}