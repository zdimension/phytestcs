using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
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

        public static float Radians(this float f)
        {
            return ((float) (f * Math.PI / 180)).ClampWrap((float) Math.PI);
        }

        public static RenderTexture RenderTexture(this Canvas c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            
            return (RenderTexture)c.GetType().GetField("myRenderTexture", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(c)!;
        }

        public static T[] ToArrayLocked<T>(this SynchronizedCollection<T> coll)
        {
            if (coll == null) throw new ArgumentNullException(nameof(coll));
            
            lock (coll.SyncRoot)
            {
                return coll.ToArray();
            }
        }

        public static T With<T>(this T obj, Action<T> map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            
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
            if (c == null) throw new ArgumentNullException(nameof(c));
            
            c.Origin = new Vector2f(c.Radius, c.Radius);
            return c;
        }

        public static ObjPropAttribute? GetObjProp(this MemberInfo prop)
        {
            return prop?.GetCustomAttribute<ObjPropAttribute>();
        }

        public static T Eval<T>(this string s)
        {
            return CSharpScript.EvaluateAsync<T>(s).Result;
        }

        public static string? ToString<T1, T2>(this (T1, T2) tuple, CultureInfo culture)
            where T1 : IFormattable
            where T2 : IFormattable
        {
            return $"({tuple.Item1.ToString(null, culture)}, {tuple.Item2.ToString(null, culture)})";
        }

        public static bool IsNaN(this Vector2f v)
        {
            return float.IsNaN(v.X) || float.IsNaN(v.Y);
        }
    }
}