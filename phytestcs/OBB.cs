using System;
using SFML.Graphics;
using SFML.System;

namespace phytestcs
{
    public static class OBB
    {
        const float NORMAL_TOLERANCE = 0.0001f;

        // Returns normalized vector
        static Vector2f getNormalized(Vector2f v)
        {
            float length = v.Norm();
            if (length < NORMAL_TOLERANCE)
            {
                return new Vector2f();
            }

            return new Vector2f(v.X / length, v.Y / length);
        }

        // Returns right hand perpendicular vector
        static Vector2f getNormal(Vector2f v)
        {
            return new Vector2f(-v.Y, v.X);
        }

        // Find minimum and maximum projections of each vertex on the axis
        static Vector2f projectOnAxis(Vector2f[] vertices, Vector2f axis)
        {
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            foreach (var vertex in vertices)
            {
                float projection = vertex.Dot(axis);
                if (projection < min)
                {
                    min = projection;
                }

                if (projection > max)
                {
                    max = projection;
                }
            }
            return new Vector2f(min, max);
        }

        // a and b are ranges and it's assumed that a.x <= a.y and b.x <= b.y
        static bool areOverlapping(Vector2f a, Vector2f b)
        {
            return a.X <= b.Y && a.Y >= b.X;
        }

        // a and b are ranges and it's assumed that a.x <= a.y and b.x <= b.y
        static float getOverlapLength(Vector2f a, Vector2f b)
        {
            if (!areOverlapping(a, b))
            {
                return 0f;
            }

            return Math.Min(a.Y, b.Y) - Math.Max(a.X, b.X);
        }

        static Vector2f getCenter(Shape shape)
        {
            var transform = shape.Transform;
            var local = shape.GetLocalBounds();
            return transform.TransformPoint(local.Width / 2f, local.Height / 2f);
        }

        static Vector2f[] getVertices(Shape shape)
        {
            var vertices = new Vector2f[shape.GetPointCount()];
            var transform = shape.Transform;
            for (uint i = 0; i < shape.GetPointCount(); ++i)
            {
                vertices[i] = transform.TransformPoint(shape.GetPoint(i));
            }

            return vertices;
        }

        static Vector2f getPerpendicularAxis(Vector2f[] vertices, int index)
        {
            return getNormal(getNormalized(vertices[index + 1] - vertices[index]));
        }

        // axes for which we'll test stuff. Two for each box, because testing for parallel axes isn't needed
        static Vector2f[] getPerpendicularAxes(Vector2f[] vertices1, Vector2f[] vertices2)
        {
            return new[]
            {
                getPerpendicularAxis(vertices1, 0),
                getPerpendicularAxis(vertices1, 1),
                getPerpendicularAxis(vertices2, 0),
                getPerpendicularAxis(vertices2, 1)
            };
        }

        // Separating Axis Theorem (SAT) collision test
        // Minimum Translation Vector (MTV) is returned for the first Oriented Bounding Box (OBB)
        public static bool testCollision(Shape obb1, Shape obb2, out Vector2f mtv)
        {
            mtv = default;

            var vertices1 = getVertices(obb1);
            var vertices2 = getVertices(obb2);
            var axes = getPerpendicularAxes(vertices1, vertices2);

            // we need to find the minimal overlap and axis on which it happens
            float minOverlap = float.PositiveInfinity;

            foreach (var axis in axes)
            {
                Vector2f proj1 = projectOnAxis(vertices1, axis);
                Vector2f proj2 = projectOnAxis(vertices2, axis);

                float overlap = getOverlapLength(proj1, proj2);
                if (overlap == 0f)
                {
                    // shapes are not overlapping
                    return false;
                }

                if (overlap < minOverlap)
                {
                    minOverlap = overlap;
                    mtv = axis * minOverlap;
                    // ideally we would do this only once for the minimal overlap
                    // but this is very cheap operation
                }
            }

            // need to reverse MTV if center offset and overlap are not pointing in the same direction
            bool notPointingInTheSameDirection = (getCenter(obb1) - getCenter(obb2)).Dot(mtv) < 0;
            if (notPointingInTheSameDirection)
            {
                mtv = -mtv;
            }

            return true;
        }
    }
}
