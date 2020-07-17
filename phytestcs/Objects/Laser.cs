using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public class Laser : PinnedShapedVirtualObject, ICollides
    {
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
        public uint CollideSet { get; set; } = 1;
        public float LaserThickness => Size / 6;
        public Vector2f LaserStartingPoint => Map(new Vector2f(Size / 2, 0));
        [ObjProp("Fade distance", "m")] public float FadeDistance { get; set; } = 300;
        public override Shape Shape => _shape;
        private readonly RectangleShape _shape = new RectangleShape();
        public override IEnumerable<Shape> Shapes => new[] {_shape};
        public Laser(PhysicalObject @object, Vector2f relPos, float size)
            : base(@object, relPos)
        {
            Size = size;
            
            UpdatePhysics(0);
        }
        
        public readonly SynchronizedCollection<LaserRay> Rays = new SynchronizedCollection<LaserRay>();

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
                    if (depth > 10 || Rays.Count >= Program.NumRays)
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
                        var a = ray.Angle;
                        var (A, B) = minLine;
                        var N = (B - A).Ortho();
                        var aI = N.Angle();
                        var dA = a - aI;
                        if (dA > (Math.PI / 2))
                            dA -= (float) Math.PI;
                        else if (dA < -(Math.PI / 2))
                            dA += (float) Math.PI;
                        var newAngle = aI - dA;
                        ray.DebugInfo += $"i={dA.Degrees(),6:F3}° D={minDist:F8}m";
                        
                        var next = new LaserRay(minInter, newAngle, float.PositiveInfinity, ray.Color, LaserThickness, ray.EndDistance, ray.RefractiveIndex);
                        next.Source = ray;
                        ShootRay(next, depth + 1);

                        if (minObj.RefractiveIndex != float.PositiveInfinity)
                        {
                            // refraction
                            var refAngle =
                                aI + (float) Math.Asin(Math.Sin(dA) * ray.RefractiveIndex / minObj.RefractiveIndex) + (float)Math.PI;
                            var refra = new LaserRay(minInter, refAngle, float.PositiveInfinity, new Color(0,0,255), LaserThickness, ray.EndDistance, minObj.RefractiveIndex);
                            refra.DebugInfo = "ref ";
                            refra.Source = ray;
                            ShootRay(refra, depth + 1);
                        }
                    }
                }
                
                ShootRay(new LaserRay(LaserStartingPoint, ActualAngle, float.PositiveInfinity, Color, LaserThickness, 0, Object?.RefractiveIndex ?? 1));
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
        }
    }

    public class LaserRay
    {
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

        public LaserRay(Vector2f start, float angle, float length, Color color, float thickness, float startDistance, float refrac)
        {
            Start = start;
            Angle = angle;
            Length = length;
            Color = color;
            Thickness = thickness;
            StartDistance = startDistance;
            RefractiveIndex = refrac;
        }

        public Vector2f GetEndClipped()
        {
            var length = Length;
            if (length == float.PositiveInfinity)
                length = Math.Max(Camera.GameView.Size.X, Camera.GameView.Size.Y);
            return Start + Tools.FromPolar(length, Angle);
        }
    }
}