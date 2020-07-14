using System;
using System.Collections.Generic;
using System.Linq;
using phytestcs.Objects;
using SFML.System;
using SFML.Window;

namespace phytestcs
{
    public static partial class Tools
    {
        /// <summary>
        /// Creates a vector from its polar form
        /// </summary>
        /// <param name="r">Magnitude</param>
        /// <param name="theta">Angle</param>
        /// <returns>Vector</returns>
        public static Vector2f FromPolar(float r, float theta)
        {
            return new Vector2f((float) (r * Math.Cos(theta)), (float) (r * Math.Sin(theta)));
        }
        
        public static Vector2f Normalize(this Vector2f vec)
        {
            var n = vec.Norm();
            if (Single.IsNaN(n)) return default;
            return new Vector2f(vec.X / n, vec.Y / n);
        }

        public static float Dot(this Vector2f a, Vector2f b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static float Cross(this Vector2f a, Vector2f b)
        {
            return a.X * b.Y - a.Y * b.X;
        }
        
        public static bool IsOnLine(this Vector2f p, Vector2f v, Vector2f w)
        {
            const float tol = 1e-8f;
            var l2 = (w-v).NormSquared();  // i.e. |w-v|^2 -  avoid a sqrt
            if (l2 == 0.0) return (v-p).Norm() <= tol;   // v == w case
            // Consider the line extending the segment, parameterized as v + t (w - v).
            // We find projection of point p onto the line. 
            // It falls where t = [(p-v) . (w-v)] / |w-v|^2
            // We clamp t from [0,1] to handle points outside the segment vw.
            var t = Math.Max(0, Math.Min(1, (p - v).Dot(w - v) / l2));
            var projection = v + t * (w - v);  // Projection falls on the segment
            return (p-projection).Norm() <= tol;
        }
        
        public static void Deconstruct(this Vector2i vec, out int x, out int y)
        {
            x = vec.X;
            y = vec.Y;
        }

        public static void Deconstruct(this Vector2f vec, out float x, out float y)
        {
            x = vec.X;
            y = vec.Y;
        }
        
        public static Vector2i Position(this MouseMoveEventArgs e)
        {
            return new Vector2i(e.X, e.Y);
        }

        public static Vector2i Position(this MouseButtonEventArgs e)
        {
            return new Vector2i(e.X, e.Y);
        }

        public static Vector2i Position(this MouseWheelScrollEventArgs e)
        {
            return new Vector2i(e.X, e.Y);
        }

        public static Vector2f Div(this Vector2f a, Vector2f b)
        {
            return new Vector2f(a.X / b.X, a.Y / b.Y);
        }

        public static Vector2f Prod(this Vector2f a, Vector2f b)
        {
            return new Vector2f(a.X * b.X, a.Y * b.Y);
        }

        public static Vector2f Round(this Vector2f vec)
        {
            return new Vector2f((float) Math.Round(vec.X), (float) Math.Round(vec.Y));
        }

        public static Vector2f ToWorld(this Vector2i pos)
        {
            return Render.Window.MapPixelToCoords(pos, Camera.GameView);
        }

        public static Vector2i ToScreen(this Vector2f pos)
        {
            return Render.Window.MapCoordsToPixel(pos, Camera.GameView);
        }

        public static Vector2f F(this Vector2i vec)
        {
            return new Vector2f(vec.X, vec.Y);
        }

        public static Vector2i I(this Vector2u vec)
        {
            return new Vector2i((int) vec.X, (int) vec.Y);
        }

        public static Vector2f InvertY(this Vector2f vec)
        {
            return new Vector2f(vec.X, -vec.Y);
        }
        
        public static Vector2f WithUpdate(this Vector2f v, PhysicalObject o)
        {
            return v - Simulation.TargetDT * o.Velocity / 2;
        }

        public static Vector2f Sum(this IEnumerable<Vector2f> arr)

        {
            return arr.Aggregate(default(Vector2f), (current, vec) => current + vec);
        }

        public static Vector2f Ortho(this Vector2f vec)
        {
            return new Vector2f(vec.Y, -vec.X);
        }

        public static Vector2i I(this Vector2f vec)
        {
            return new Vector2i((int)vec.X, (int)vec.Y);
        }

        public static string Display(this Vector2f vec)
        {
            return $"{vec.DisplayPoint()} R={vec.Norm(),7:F2} θ={vec.Angle().Degrees(),6:F1}°";
        }

        public static string DisplayPoint(this Vector2f vec)
        {
            return $"({vec.X,7:F2} ; {vec.Y,7:F2})";
        }

        public static Vector2f Sum<T>(this IEnumerable<T> arr, Func<T, Vector2f> map)
        {
            return arr.Aggregate(default(Vector2f), (current, obj) => current + map(obj));
        }

        public static float Norm(this Vector2f vec)
        {
            return (float) Math.Sqrt(vec.NormSquared());
        }

        public static float NormSquared(this Vector2f vec)
        {
            return vec.X * vec.X + vec.Y * vec.Y;
        }

        public static float Angle(this Vector2f vec)
        {
            return (float) Math.Atan2(vec.Y, vec.X);
        }

        public static (float, float) ToPolar(this Vector2f vec)
        {
            return (vec.Norm(), vec.Angle());
        }

        public static Vector2f Rotate(this Vector2f vec, float angle)
        {
            return vec.SetAngle(vec.Angle() + angle);
        }

        public static Vector2f SetAngle(this Vector2f vec, float angle)
        {
            return Tools.FromPolar(vec.Norm(), angle);
        }

        public static float AngleBetween(this Vector2f a, Vector2f b)
        {
            return a.Dot(b) / (a.Norm() * b.Norm());
        }
    }
}