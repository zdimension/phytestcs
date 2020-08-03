using System;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;

namespace phytestcs
{
    public static class Obb
    {
        private const float NormalTolerance = 0.0001f;

        // Returns normalized vector
        private static Vector2f GetNormalized(Vector2f v)
        {
            var length = v.Norm();
            if (length < NormalTolerance)
                return new Vector2f();

            return new Vector2f(v.X / length, v.Y / length);
        }

        // Returns right hand perpendicular vector
        private static Vector2f GetNormal(Vector2f v)
        {
            return new Vector2f(-v.Y, v.X);
        }

        // Find minimum and maximum projections of each vertex on the axis
        private static Vector2f projectOnAxis(Vector2f[] vertices, Vector2f axis)
        {
            var min = float.PositiveInfinity;
            var max = float.NegativeInfinity;
            foreach (var vertex in vertices)
            {
                var projection = vertex.Dot(axis);
                if (projection < min)
                    min = projection;

                if (projection > max)
                    max = projection;
            }

            return new Vector2f(min, max);
        }

        // a and b are ranges and it's assumed that a.x <= a.y and b.x <= b.y
        private static bool AreOverlapping(Vector2f a, Vector2f b)
        {
            return a.X <= b.Y && a.Y >= b.X;
        }

        // a and b are ranges and it's assumed that a.x <= a.y and b.x <= b.y
        private static float GetOverlapLength(Vector2f a, Vector2f b)
        {
            if (!AreOverlapping(a, b))
                return 0f;

            return Math.Min(a.Y, b.Y) - Math.Max(a.X, b.X);
        }

        private static Vector2f[] GetVertices(Shape shape)
        {
            var vertices = new Vector2f[shape.GetPointCount()];
            var transform = shape.Transform;
            for (uint i = 0; i < shape.GetPointCount(); ++i)
                vertices[i] = transform.TransformPoint(shape.GetPoint(i));

            return vertices;
        }

        private static Vector2f GetPerpendicularAxis(Vector2f[] vertices, int index)
        {
            return GetNormal(GetNormalized(vertices[index + 1] - vertices[index]));
        }

        // axes for which we'll test stuff. Two for each box, because testing for parallel axes isn't needed
        private static Vector2f[] GetPerpendicularAxes(Vector2f[] vertices1, Vector2f[] vertices2)
        {
            return new[]
            {
                GetPerpendicularAxis(vertices1, 0),
                GetPerpendicularAxis(vertices1, 1),
                GetPerpendicularAxis(vertices2, 0),
                GetPerpendicularAxis(vertices2, 1)
            };
        }

        // Separating Axis Theorem (SAT) collision test
        // Minimum Translation Vector (MTV) is returned for the first Oriented Bounding Box (OBB)
        public static bool TestCollision(PhysicalObject obj1, PhysicalObject obj2, out Vector2f mtv)
        {
            mtv = default;

            var obb1 = obj1.Shape;
            var obb2 = obj2.Shape;

            var vertices1 = GetVertices(obb1);
            var vertices2 = GetVertices(obb2);
            var axes = GetPerpendicularAxes(vertices1, vertices2);

            // we need to find the minimal overlap and axis on which it happens
            var minOverlap = float.PositiveInfinity;

            foreach (var axis in axes)
            {
                var proj1 = projectOnAxis(vertices1, axis);
                var proj2 = projectOnAxis(vertices2, axis);

                var overlap = GetOverlapLength(proj1, proj2);
                if (overlap == 0f)
                    // shapes are not overlapping
                    return false;

                if (overlap < minOverlap)
                {
                    minOverlap = overlap;
                    mtv = axis * minOverlap;
                    // ideally we would do this only once for the minimal overlap
                    // but this is very cheap operation
                }
            }

            // need to reverse MTV if center offset and overlap are not pointing in the same direction
            var notPointingInTheSameDirection = (obj1.Position - obj2.Position).Dot(mtv) < 0;
            if (notPointingInTheSameDirection)
                mtv = -mtv;
                
            return true;
        }
    }
}