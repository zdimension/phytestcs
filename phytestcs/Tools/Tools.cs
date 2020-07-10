﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using phytestcs.Interface.Windows;
using phytestcs.Objects;
using SFML.System;
using SFML.Window;
using TGUI;
using static phytestcs.Global;
using Object = phytestcs.Objects.Object;

namespace phytestcs
{
    public static partial class Tools
    {
        public static T Transition<T>(T a, T b, DateTime start, float duration)
        {
            return a + ((dynamic)b - (dynamic)a) * Math.Min((float)(DateTime.Now - start).TotalSeconds / duration, 1);
        }

        public static T Clamp<T>(T x, T a, T b)
            where T : IComparable
        {
            if (x.CompareTo(a) < 0) return a;
            if (x.CompareTo(b) > 0) return b;
            return x;
        }

        public static IEnumerable<Object> ActualObjects => Render.WorldCache.Where(o =>
            o != Drawing.DragSpring &&
            o != Drawing.DragSpring?.End1 &&
            o != Drawing.DragSpring?.End2);

        public static Object ObjectAtPosition(Vector2i pos)
        {
            var loc = pos.ToWorld();

            return ActualObjects.LastOrDefault(o => o.Contains(loc));
        }

        public static PhysicalObject PhysObjectAtPosition(Vector2i pos, PhysicalObject excl=null)
        {
            var loc = pos.ToWorld();

            return ActualObjects.OfType<PhysicalObject>().LastOrDefault(o => o != excl && o.Contains(loc));
        }

        public static T Average<T>(T a, T b)
        {
            return a + ((dynamic) b - a) / 2;
        }
        
        public static Vector2i MousePos => Mouse.GetPosition(Render.Window);
    }

    public class Ref<T>
    {
        public T Value { get; set; }
    }
}
