using System;
using SFML.Graphics;
using SFML.System;

namespace phytestcs
{
    public static partial class Tools
    {
        public static float Area(this Shape forme)
        {
            switch (forme)
            {
                case RectangleShape r:
                    return Math.Abs(r.Size.X * r.Size.Y);
                case CircleShape c:
                    return (float) Math.Abs(Math.PI * Math.Pow(c.Radius, 2));
                default:
                    var points = forme.PointsLocal();
                    var pcount = points.Length;
                    points = points.Wrap();
                    var area = 0f;
                    for (var i = 0; i < pcount; i++)
                    {
                        area += points[i].Cross(points[i + 1]);
                    }

                    return area;
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

        public static T CenterOrigin<T>(this T shape)
            where T : Shape
        {
            shape.Origin = shape.GetLocalBounds().Size() / 2;
            return shape;
        }

        public static bool Intersects((Vector2f, Vector2f) a, (Vector2f, Vector2f) b,
            out Vector2f inter, out float distance)
        {
            distance = 0;
            inter = default;

            var (a1, a2) = a;
            var (b1, b2) = b;

            var aD = a2 - a1;
            var bD = b2 - b1;

            var cr = aD.Cross(bD);

            if (cr != 0)
            {
                var t = (b1 - a1).Cross(bD) / cr;
                var u = (b1 - a1).Cross(aD) / cr;

                if (t.IsBetween(0, 1) && u.IsBetween(0, 1))
                {
                    var diff = t * aD;
                    distance = diff.Norm();
                    inter = a1 + diff;
                    return true;
                }
            }

            return false;
        }

        public static T[] Wrap<T>(this T[] arr)
        {
            Array.Resize(ref arr, arr.Length + 1);
            arr[^1] = arr[0];
            return arr;
        }
    }
}