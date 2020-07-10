using System;
using SFML.Graphics;
using SFML.System;

namespace phytestcs
{
    public static partial class Tools
    {
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
        
        public static bool Contains(this CircleShape c, Vector2f p)
        {
            return (p - c.Position - c.Origin + new Vector2f(c.Radius, c.Radius)).Norm() <= c.Radius;
        }
    }
}