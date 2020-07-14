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

        // http://geomalgorithms.com/a03-_inclusion.html#wn_PnPoly()
        public static bool ContainsPoint(this Vector2f[] V, Vector2f P)
        {
            static double isLeft(Vector2f p0, Vector2f p1, Vector2f p2)
            {
                return ((p1.X - p0.X) * (p2.Y - p0.Y)
                        - (p2.X - p0.X) * (p1.Y - p0.Y));
            }

            var wn = 0; // the  winding number counter
            var j = V.Length - 1;
            
            // loop through all edges of the polygon
            for (var i = 0; i < V.Length; j = i++)
            {
                if (P.IsOnLine(V[i], V[j]))
                    return true;
                if (V[i].Y <= P.Y)
                {
                    // start y <= P.y
                    if (V[j].Y > P.Y) // an upward crossing
                        if (isLeft(V[i], V[j], P) > 0) // P left of  edge
                            ++wn; // have  a valid up intersect
                }
                else
                {
                    // start y > P.y (no test needed)
                    if (V[j].Y <= P.Y) // a downward crossing
                        if (isLeft(V[i], V[j], P) < 0) // P right of  edge
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