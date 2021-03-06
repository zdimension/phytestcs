﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using TGUI;

namespace phytestcs
{
    public static partial class Tools
    {
        public static T Clamp<T>(this T v, T min, T max)
            where T : IComparable<T>
        {
            if (v.CompareTo(min) < 0)
                return min;
            if (v.CompareTo(max) > 0)
                return max;
            return v;
        }

        public static float ClampWrap(this float angle, float bound)
        {
            if (angle > bound)
                angle = -bound + angle % bound;
            else if (angle < -bound)
                angle = bound + angle % bound;
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
            return f.DegreesNoWrap().ClampWrap(180);
        }

        public static float DegreesNoWrap(this float f)
        {
            return (float) (f * 180 / Math.PI);
        }

        public static float Radians(this float f)
        {
            return ((float) (f * Math.PI / 180)).ClampWrap((float) Math.PI);
        }

        public static RenderTexture RenderTexture(this Canvas c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));

            return (RenderTexture) c.GetType()
                    .GetField("myRenderTexture", BindingFlags.Instance | BindingFlags.NonPublic)!
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

        public static void OnKeyPressed(this Widget w, KeyHandler handler)
        {
            if (Program.WidgetKeyHandlers.ContainsKey(w))
            {
                Console.WriteLine("Warning: overwriting key handler for widget");
            }

            Program.WidgetKeyHandlers[w] = e =>
            {
                if (w.CPointer != IntPtr.Zero)
                    return handler(e);
                
                Program.WidgetKeyHandlers.Remove(w);
                return true;
            };
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

        public static async Task<T> Eval<T>(this string s, Func<ScriptOptions, ScriptOptions>? opt = null,
            object? globals = null, Type? globalsType = null)
        {
            return (await s.Exec<T>(opt, globals, globalsType).ConfigureAwait(true)).ReturnValue;
        }
        
        public static async Task<ScriptState<T>> Exec<T>(this string s, Func<ScriptOptions, ScriptOptions>? opt = null,
            object? globals = null, Type? globalsType = null)
        {
            var bopt = Scene.DefaultScriptOptions;
            if (opt != null)
                bopt = opt(bopt);
            return await CSharpScript.RunAsync<T>(s, bopt, globals, globalsType).ConfigureAwait(true);
        }

        public static string Repr<T1, T2>(this (T1, T2) tuple)
            where T1 : IFormattable
            where T2 : IFormattable
        {
            return
                $"({tuple.Item1.ToString(null, CultureInfo.InvariantCulture)}, {tuple.Item2.ToString(null, CultureInfo.InvariantCulture)})";
        }

        public static string Repr<T1, T2, T3>(this (T1, T2, T3) tuple)
            where T1 : IFormattable
            where T2 : IFormattable
            where T3 : IFormattable
        {
            return
                $"({tuple.Item1.ToString(null, CultureInfo.InvariantCulture)}, {tuple.Item2.ToString(null, CultureInfo.InvariantCulture)}, {tuple.Item3.ToString(null, CultureInfo.InvariantCulture)})";
        }

        public static string Repr<T1, T2, T3, T4>(this (T1, T2, T3, T4) tuple)
            where T1 : IFormattable
            where T2 : IFormattable
            where T3 : IFormattable
            where T4 : IFormattable
        {
            return
                $"({tuple.Item1.ToString(null, CultureInfo.InvariantCulture)}, {tuple.Item2.ToString(null, CultureInfo.InvariantCulture)}, {tuple.Item3.ToString(null, CultureInfo.InvariantCulture)}, {tuple.Item4.ToString(null, CultureInfo.InvariantCulture)})";
        }

        public static bool IsNaN(this Vector2f v)
        {
            return float.IsNaN(v.X) || float.IsNaN(v.Y);
        }

        public static string GetAssemblyLoadPath(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            return type.Assembly.Location;
        }

        public static string GetSystemAssemblyPathByName(string assemblyName)
        {
            var root = Path.GetDirectoryName(typeof(object).GetAssemblyLoadPath())!;
            return Path.Combine(root, assemblyName);
        }

        public static float RoundTo(this float f, float n)
        {
            return (float) (Math.Round(f / n) * n);
        }

        public static void CeilSize(this Widget w)
        {
            w.Size = w.Size.Ceil();
        }
    }
}