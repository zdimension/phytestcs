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
                points[i] = s.GetPoint(i) - s.Origin;

            return points;
        }

        public static Vector2f[] PointsGlobal(this Shape s)
        {
            var t = s.Transform;
            var points = new Vector2f[s.GetPointCount()];
            for (uint i = 0; i < points.Length; i++)
                points[i] = t.TransformPoint(s.GetPoint(i));

            return points;
        }

        // http://geomalgorithms.com/a03-_inclusion.html#wn_PnPoly()
        public static bool ContainsPoint(this Vector2f[] v, Vector2f p)
        {
            static double IsLeft(Vector2f p0, Vector2f p1, Vector2f p2)
            {
                return (p1.X - p0.X) * (p2.Y - p0.Y)
                       - (p2.X - p0.X) * (p1.Y - p0.Y);
            }

            var wn = 0; // the  winding number counter
            var j = v.Length - 1;

            // loop through all edges of the polygon
            for (var i = 0; i < v.Length; j = i++)
            {
                if (p.IsOnLine(v[i], v[j]))
                    return true;
                if (v[i].Y <= p.Y)
                {
                    // start y <= P.y
                    if (v[j].Y > p.Y) // an upward crossing
                        if (IsLeft(v[i], v[j], p) > 0) // P left of  edge
                            ++wn; // have  a valid up intersect
                }
                else
                {
                    // start y > P.y (no test needed)
                    if (v[j].Y <= p.Y) // a downward crossing
                        if (IsLeft(v[i], v[j], p) < 0) // P right of  edge
                            --wn; // have  a valid down intersect
                }
            }

            return wn != 0;
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