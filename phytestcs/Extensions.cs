using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using TGUI;

namespace phytestcs
{
    public static class Extensions
    {
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

        public static float ClampWrap(this float angle, float bound)
        {
            if (angle > bound)
                angle = -bound + (angle % bound);
            else if (angle < -bound)
                angle = bound + (angle % bound);
            return angle;
        }

        public static float Degrees(this float f)
        {
            return ((float)(f * 180 / Math.PI)).ClampWrap(180);
        }

        public static void Scatter<T0, T1, T2>(in this (T0 i0, T1 i1, T2 i2) t, Action<T0, T1, T2> a) => a(t.i0, t.i1, t.i2);
        public static T Scatter<T0, T1, T2, T>(in this (T0 i0, T1 i1, T2 i2) t, Func<T0, T1, T2, T> a) => a(t.i0, t.i1, t.i2);

        public static void Deconstruct(this Color c, out byte r, out byte g, out byte b)
        {
            r = c.R;
            g = c.G;
            b = c.B;
        }

        public static void Deconstruct(this Color c, out double r, out double g, out double b)
        {
            r = c.R;
            g = c.G;
            b = c.B;
        }

        public static (double r, double g, double b) ToDouble(this Color c)
        {
            return (c.R, c.G, c.B);
        }

        public static Color ToColor(this (byte r, byte g, byte b) v)
        {
            return new Color(v.r, v.g, v.b);
        }

        public static float Radians(this float f)
        {
            return ((float)(f * Math.PI / 180)).ClampWrap((float)Math.PI);
        }

        public static RenderTexture RenderTexture(this Canvas c)
        {
            return c?.GetType().GetField("myRenderTexture", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(c) as RenderTexture;
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

        public static float Area(this Shape forme)
        {
            switch (forme)
            {
                case RectangleShape r:
                    return Math.Abs(r.Size.X * r.Size.Y);
                case CircleShape c:
                    return (float) Math.Abs(Math.PI * Math.Pow(c.Radius, 2));
                default:
                    throw new NotImplementedException();
            }
        }

        public static float Right(this FloatRect r)
        {
            return r.Left + r.Width;
        }

        public static float Bottom(this FloatRect r)
        {
            return r.Top + r.Height;
        }

        public static Vector2f Size(this FloatRect r)
        {
            return new Vector2f(r.Width, r.Height);
        }

        public static bool IsBetween<T>(this T x, T a, T b)
            where T : IComparable<T>
        {
            return x.CompareTo(a) >= 0 && x.CompareTo(b) <= 0;
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

        private const float CollTolerance = 0.05f;

        public static bool CollidesX(this FloatRect a, FloatRect b)
        {
            return a.Left <= b.Right() && a.Right() >= b.Left && (Math.Abs(a.Bottom() - b.Top) < CollTolerance || Math.Abs(a.Top - b.Bottom()) < CollTolerance);
        }

        public static bool CollidesY(this FloatRect a, FloatRect b)
        {
            return a.Top <= b.Bottom() && a.Bottom() >= b.Top && (Math.Abs(a.Right() - b.Left) < CollTolerance || Math.Abs(a.Left - b.Right()) < CollTolerance);
        }

        public static bool Contains(this CircleShape c, Vector2f p)
        {
            return (p - c.Position - c.Origin + new Vector2f(c.Radius, c.Radius)).Norm() <= c.Radius;
        }

        public static Vector2f Normalize(this Vector2f vec)
        {
            var n = vec.Norm();
            if (float.IsNaN(n)) return default;
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

        public static Color Add(this Color a, Color b)
        {
            return new Color(
                (byte) Tools.Clamp(a.R + b.R, 0, 255), 
                (byte) Tools.Clamp(a.G + b.G, 0, 255),
                (byte) Tools.Clamp(a.B + b.B, 0, 255));
        }

        public static Color Subtract(this Color a, Color b)
        {
            return new Color(
                (byte)Tools.Clamp(a.R - b.R, 0, 255),
                (byte)Tools.Clamp(a.G - b.G, 0, 255),
                (byte)Tools.Clamp(a.B - b.B, 0, 255));
        }

        public static Color Multiply(this Color a, float f)
        {
            return new Color(
                (byte)Tools.Clamp(a.R * f, 0, 255),
                (byte)Tools.Clamp(a.G * f, 0, 255),
                (byte)Tools.Clamp(a.B * f, 0, 255));
        }


        public static Vector2f[] PointsLocal(this Shape s)
        {
            var points = new Vector2f[s.GetPointCount()];
            for (uint i = 0; i < points.Length; i++)
            {
                points[i] = s.GetPoint(i);
            }

            return points;
        }

        public static Vector2f[] PointsGlobal(this Shape s)
        {
            var t = s.Transform;
            var points = new Vector2f[s.GetPointCount()];
            for (uint i = 0; i < points.Length; i++)
            {
                points[i] = t.TransformPoint(s.GetPoint(i));
            }

            return points;
        }

        // http://csharphelper.com/blog/2014/07/determine-whether-a-point-is-inside-a-polygon-in-c/
        public static bool ContainsPoint(this Vector2f[] points, Vector2f p)
        {
            static float GetAngle(Vector2f a, Vector2f b, Vector2f c)
            {
                var ab = a - b;
                var bc = c - b;

                return (float)Math.Atan2(ab.Cross(bc), ab.Dot(bc));
            }

            var max_point = points.Length - 1;
            var total_angle = GetAngle(
                points[max_point],
                p,
                points[0]);

            for (var i = 0; i < max_point; i++)
            {
                total_angle += GetAngle(
                    points[i],
                    p,
                    points[i + 1]);
            }

            return Math.Abs(total_angle) > 1;
        }

        public static bool Contains(this FloatRect r, Vector2f p)
        {
            return r.Contains(p.X, p.Y);
        }

        public static bool Contains(this Shape s, Vector2f p)
        {
            return s switch
            {
                CircleShape c => c.Contains(p),
                RectangleShape r => r.GetLocalBounds().Contains(r.InverseTransform.TransformPoint(p)),
                _ => s.PointsGlobal().ContainsPoint(p)
            };
        }

        public static CircleShape CenterOrigin(this CircleShape c)
        {
            c.Origin = new Vector2f(c.Radius, c.Radius);
            return c;
        }

        public static T CenterOrigin<T>(this T shape)
            where T : Shape
        {
            shape.Origin = shape.GetLocalBounds().Size() / 2;
            return shape;
        }

        public static (Func<T> getter, Action<T> setter) GetAccessors<T>(this Expression<Func<T>> prop)
        {
            var expr = ((MemberExpression) prop.Body);
            var BindPropInfo = (PropertyInfo) expr.Member;
            var getMethod = BindPropInfo.GetGetMethod();
            object target = null;

            if (!getMethod!.IsStatic)
            {
                var fieldOnClosureExpression = (MemberExpression) expr.Expression;
                target = ((FieldInfo) fieldOnClosureExpression.Member).GetValue(
                    ((ConstantExpression) fieldOnClosureExpression.Expression).Value);
            }

            var setMethod = BindPropInfo.GetSetMethod();

            return (
                (Func<T>)getMethod.CreateDelegate(typeof(Func<T>), target),
                (Action<T>)setMethod!.CreateDelegate(typeof(Action<T>), target)
                );
        }

        public static ObjPropAttribute GetObjProp<T>(this Expression<Func<T>> prop)
        {
            var expr = ((MemberExpression) prop.Body);
            var BindPropInfo = (PropertyInfo) expr.Member;
            return BindPropInfo.GetCustomAttribute<ObjPropAttribute>();
        }

        public static string? GetDisplayName<T>(this Expression<Func<T>> prop)
        {
            return prop.GetObjProp()?.DisplayName ?? prop!.Name;
        }

        public static bool IsNaN(this Vector2f v)
        {
            return float.IsNaN(v.X) || float.IsNaN(v.Y);
        }
    }
}
