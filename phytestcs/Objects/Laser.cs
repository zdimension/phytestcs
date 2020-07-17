﻿using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public class Laser : PinnedShapedVirtualObject, ICollides
    {
        private readonly RectangleShape _shape = new RectangleShape();

        public readonly SynchronizedCollection<LaserRay> Rays = new SynchronizedCollection<LaserRay>();

        public Laser(PhysicalObject @object, Vector2f relPos, float size)
            : base(@object, relPos)
        {
            Size = size;

            UpdatePhysics(0);
        }

        [ObjProp("Size", "m")]
        public float Size
        {
            get => _shape.Size.X;
            set
            {
                _shape.Size = new Vector2f(value, value / 2);
                _shape.CenterOrigin();
            }
        }

        public float LaserThickness => Size / 6;
        public Vector2f LaserStartingPoint => Map(new Vector2f(Size / 2, 0));
        [ObjProp("Fade distance", "m")] public float FadeDistance { get; set; } = 300;
        public override Shape Shape => _shape;
        public override IEnumerable<Shape> Shapes => new[] { _shape };
        public uint CollideSet { get; set; } = 1;

        public override void UpdatePhysics(float dt)
        {
            base.UpdatePhysics(dt);

            if (Simulation.WorldCachePhy == null)
                return;

            lock (Rays.SyncRoot)
            {
                Rays.Clear();

                void ShootRay(LaserRay ray, int depth = 0)
                {
                    if (depth > 100 || Rays.Count >= Program.NumRays)
                        return;

                    if (ray.Color.A == 0)
                        return;

                    Rays.Add(ray);

                    ray.Length = Math.Min(FadeDistance - ray.StartDistance, ray.Length);

                    if (ray.Length <= 0)
                        return;

                    var end = ray.GetEndClipped();

                    PhysicalObject minObj = null;
                    var minDist = float.PositiveInfinity;
                    Vector2f minInter = default;
                    (Vector2f, Vector2f) minLine = default;

                    foreach (var obj in Simulation.WorldCachePhy)
                    {
                        for (var i = 0; i < obj.GlobalPointsCache.Length; i++)
                        {
                            var a = obj.GlobalPointsCache[i];
                            var b = obj.GlobalPointsCache[(i + 1) % obj.GlobalPointsCache.Length];
                            if (Tools.Intersects((ray.Start, end),
                                (a, b), out var inter, out var dist) && dist > 1e-5f && dist < minDist)
                            {
                                minDist = dist;
                                minInter = inter;
                                minLine = (a, b);
                                minObj = obj;
                            }
                        }
                    }

                    if (minDist > ray.Length)
                        return;

                    if (minDist != float.PositiveInfinity)
                    {
                        ray.Length = minDist;
                        
                        var (sideStart, sideEnd) = minLine;
                        var normal = (sideEnd - sideStart).Ortho();
                        var normalAngle = normal.Angle();
                        
                        var incidenceAngle = ray.Angle - normalAngle;
                        if (incidenceAngle > (Math.PI / 2))
                            incidenceAngle -= (float) Math.PI;
                        else if (incidenceAngle < -(Math.PI / 2))
                            incidenceAngle += (float) Math.PI;
                        
                        // reflected ray angle
                        var reflectedAngle = normalAngle - incidenceAngle;
                        
                        ray.DebugInfo += $"i={incidenceAngle.Degrees(),6:F3}° D={minDist:F8}m";

                        var opacityRefracted = Math.Exp(-Math.Log10(minObj.RefractiveIndex));
                        var opacityReflected = 1 - opacityRefracted;

                        var reflectedRay = new LaserRay(minInter, reflectedAngle, float.PositiveInfinity,
                            ray.Color.MultiplyAlpha(opacityReflected), LaserThickness, ray.EndDistance, ray.RefractiveIndex);
                        reflectedRay.Source = ray;
                        ShootRay(reflectedRay, depth + 1);

                        if (minObj.RefractiveIndex != float.PositiveInfinity)
                        {
                            // refraction
                            var refractionAngle =
                                normalAngle + (float) Math.Asin(Math.Sin(incidenceAngle) * ray.RefractiveIndex / minObj.RefractiveIndex) +
                                (float) Math.PI;
                            var newColor = ray.Color;
                            newColor.A = (byte) (opacityRefracted * newColor.A * (255 - minObj.Color.A) / 255d);
                            var refractedRay = new LaserRay(minInter, refractionAngle, float.PositiveInfinity, newColor,
                                LaserThickness, ray.EndDistance, minObj.RefractiveIndex);
                            refractedRay.DebugInfo = "ref ";
                            refractedRay.Source = ray;
                            ShootRay(refractedRay, depth + 1);
                        }
                    }
                }

                ShootRay(new LaserRay(LaserStartingPoint, ActualAngle, float.PositiveInfinity, Color, LaserThickness, 0,
                    Object?.RefractiveIndex ?? 1));
            }
        }

        public override void Draw()
        {
            base.Draw();

            var cache = Rays.ToArrayLocked();

            foreach (var laserRay in cache)
            {
                Render.Window.Draw(Tools.VertexLineTri(new[]
                {
                    laserRay.Start,
                    laserRay.GetEndClipped()
                }, laserRay.Color, laserRay.Thickness));
            }

            Render.NumRays += cache.Length;
        }
    }

    public class LaserRay
    {
        public LaserRay(Vector2f start, float angle, float length, Color color, float thickness, float startDistance,
            float refrac)
        {
            Start = start;
            Angle = angle;
            Length = length;
            Color = color;
            Thickness = thickness;
            StartDistance = startDistance;
            RefractiveIndex = refrac;
        }

        public Vector2f Start { get; set; }
        public float Angle { get; set; }

        public float Length { get; set; }

        //public Vector2f End => Start + Tools.FromPolar(Length, Angle);
        public Color Color { get; set; }
        public float Thickness { get; set; }
        public float StartDistance { get; set; }
        public float EndDistance => StartDistance + Length;
        public float RefractiveIndex { get; set; }
        public string DebugInfo { get; set; }
        public LaserRay Source { get; set; }

        public Vector2f GetEndClipped()
        {
            var length = Length;
            if (length == float.PositiveInfinity)
                length = Math.Max(Camera.GameView.Size.X, Camera.GameView.Size.Y);
            return Start + Tools.FromPolar(length, Angle);
        }
    }
}