﻿using System;
using System.Collections.Generic;
using System.Linq;
using phytestcs.Objects;
using SFML.System;
using SFML.Window;
using Object = phytestcs.Objects.Object;

namespace phytestcs
{
    public static partial class Tools
    {
        public static IEnumerable<Object> ActualObjects => Render.WorldCache.Where(o =>
            o != Drawing.DragSpring &&
            o != Drawing.DragSpring?.End1 &&
            o != Drawing.DragSpring?.End2);

        public static Vector2i MousePos => Mouse.GetPosition(Render.Window);

        public static T Transition<T>(T a, T b, DateTime start, float duration)
        {
            return a + ((dynamic) b - (dynamic) a) *
                Math.Min((float) (DateTime.Now - start).TotalSeconds / duration, 1);
        }

        public static T Clamp<T>(T x, T a, T b)
            where T : IComparable
        {
            if (x.CompareTo(a) < 0) return a;
            if (x.CompareTo(b) > 0) return b;
            return x;
        }

        public static Object ObjectAtPosition(Vector2i pos)
        {
            var loc = pos.ToWorld();

            return ActualObjects.LastOrDefault(o => o.Contains(loc));
        }

        public static PhysicalObject PhysObjectAtPosition(Vector2i pos, PhysicalObject excl = null)
        {
            var loc = pos.ToWorld();

            return ActualObjects.OfType<PhysicalObject>().LastOrDefault(o => o != excl && o.Contains(loc));
        }

        public static T Average<T>(T a, T b)
        {
            return a + ((dynamic) b - a) / 2;
        }

        public static (double min, double max) Extrema(this IEnumerable<double> source)
        {
            var min = double.PositiveInfinity;
            var max = double.NegativeInfinity;

            foreach (var item in source)
            {
                if (item > max)
                    max = item;

                if (item < min)
                    min = item;
            }

            return (min, max);
        }

        public static double NextDouble(this Random rng, double start, double end)
        {
            if (end < start)
                (start, end) = (end, start);
            return rng.NextDouble() * (end - start) + start;
        }
        
        public static T GetDefault<T>()
        {
            return default!;
        }
    }

    public class Ref<T>
    {
        public T Value { get; set; }
    }
}