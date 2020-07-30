using System;
using System.Collections.Generic;
using System.Linq;
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

        [ObjProp("Size", "m", shortName:"⌀")]
        public float Size
        {
            get => _shape.Size.X;
            set
            {
                _shape.Size = new Vector2f(value, value / 2);
                _shape.CenterOrigin();
            }
        }

        private float LaserThickness => Size * Simulation.LaserWidth;
        private Vector2f LaserStartingPoint => Map(new Vector2f(Size / 2, 0));

        [ObjProp("Fade distance", "m", shortName:"d")]
        public float FadeDistance { get; set; } = 300;

        public override Shape Shape => _shape;
        protected override IEnumerable<Shape> Shapes => new[] { _shape };
        public uint CollideSet { get; set; } = 1;

        private readonly Dictionary<PhysicalObject, (Vector2f pos, Vector2f normal)> _hits =
            new Dictionary<PhysicalObject, (Vector2f pos, Vector2f normal)>();

        public const float StrengthEpsilon = 0.9f / 255f;

        public override void UpdatePhysics(float dt)
        {
            base.UpdatePhysics(dt);

            if (Simulation.WorldCachePhy == null)
                return;

            lock (Rays.SyncRoot)
            {
                Rays.Clear();
                
                _hits.Clear();

                void ShootRay(LaserRay ray, int depth = 0)
                {
                    if (depth > 100 || Rays.Count >= Program.NumRays)
                        return;

                    if (ray.Strength <= StrengthEpsilon)
                        return;

                    Rays.Add(ray);

                    ray.Length = Math.Min(FadeDistance - ray.StartDistance, ray.Length);

                    if (ray.Length <= 0)
                        return;

                    var end = ray.GetEndClipped();

                    PhysicalObject? minObj = null;
                    var minDist = float.PositiveInfinity;
                    Vector2f minInter = default;
                    (Vector2f, Vector2f) minLine = default;

                    foreach (var obj in Simulation.WorldCachePhy.Where(o => (CollideSet & o.CollideSet) != 0))
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

                    if (!float.IsPositiveInfinity(minDist))
                    {
                        ray.Length = minDist;
                        
                        var (sideStart, sideEnd) = minLine;
                        var side = sideEnd - sideStart;
                        var normal = side.Ortho();
                        
                        if (!_hits.ContainsKey(minObj!))
                            _hits.Add(minObj!, (minInter, normal));
                        
                        var normalAngle = normal.Angle();
                        var insideObject = side.Cross(ray.Start - sideStart) > 0;
                        if (insideObject)
                            normalAngle += (float) Math.PI;

                        var incidenceAngle = ray.Angle - normalAngle;
                        if (incidenceAngle > (Math.PI / 2))
                            incidenceAngle -= (float) Math.PI;
                        else if (incidenceAngle < -(Math.PI / 2))
                            incidenceAngle += (float) Math.PI;

                        // reflected ray angle
                        var reflectedAngle = normalAngle - incidenceAngle;

                        ray.DebugInfo += $"i={incidenceAngle.Degrees(),6:F3}° D={minDist:F8}m";

                        var opacityRefracted = Math.Exp(-Math.Log10(minObj!.RefractiveIndex));
                        var opacityReflected = 1 - opacityRefracted;

                        var reflectedRay = new LaserRay(minInter, reflectedAngle, float.PositiveInfinity,
                            ray.Color, (float) (ray.EndStrength * opacityReflected), LaserThickness, ray.EndDistance,
                            ray.RefractiveIndex, this);
                        reflectedRay.Source = ray;
                        ShootRay(reflectedRay, depth + 1);

                        // infinite index means that the speed of light inside the object is equal to zero
                        // the end result here is that there is no refracted ray
                        // i.e. 100% of the light is reflected
                        // hence the object is a perfect mirror
                        if (float.IsPositiveInfinity(minObj.RefractiveIndex)) return;

                        var newIndex = insideObject ? 1 : minObj.RefractiveIndex;
                        newIndex += (float)(1.206e-4d * (((HSVA) ray.Color).H - 180) * (newIndex * newIndex));
                        var refractionAngle =
                            normalAngle +
                            (float) Math.Asin(Math.Sin(incidenceAngle) * ray.RefractiveIndex / newIndex) +
                            (float) Math.PI;
                        var refractedRay = new LaserRay(minInter, refractionAngle, float.PositiveInfinity, ray.Color,
                            (float) (opacityRefracted * ray.EndStrength * (255 - minObj.Color.A) / 255d),
                            LaserThickness, ray.EndDistance, newIndex, this);
                        refractedRay.DebugInfo = "ref ";
                        refractedRay.Source = ray;
                        ShootRay(refractedRay, depth + 1);
                    }
                }

                var initial = new LaserRay(LaserStartingPoint, ActualAngle, float.PositiveInfinity, Color, 1,
                    LaserThickness, 0,
                    Object?.RefractiveIndex ?? 1, this);
                ShootRay(initial);

                foreach (var (obj, (pos, normal)) in _hits)
                {
                    obj.OnHitByLaser.Invoke(new CollisionEventArgs(obj, this, pos, normal));
                    OnLaserHit.Invoke(new CollisionEventArgs(this, obj, pos, normal));
                }
            }
        }

        public override void Draw()
        {
            base.Draw();

            var cache = Rays.ToArrayLocked();

            foreach (var laserRay in cache)
            {
                var (start, end) = (laserRay.Start, laserRay.GetEndClipped());
                var dir = end - start;
                var newThick = laserRay.Thickness * Simulation.LaserFuzziness / 2;
                var norm = dir.Ortho().Normalize() * newThick;
                var inside = laserRay.Color;
                var outside = new Color(laserRay.Color) { A = 0 };
                var endAlpha = laserRay.EndAlpha;

                Render.Window.Draw(Tools.VertexLineTri(new[]
                {
                    end + norm,
                    start + norm
                }, inside, newThick, true, endAlpha: endAlpha, blendLin: true, c2_: outside), new RenderStates(BlendMode.Add));
                
                Render.Window.Draw(Tools.VertexLineTri(new[]
                {
                    end,
                    start
                }, inside, newThick, true, endAlpha: endAlpha, blendLin: true), new RenderStates(BlendMode.Add));
                
                Render.Window.Draw(Tools.VertexLineTri(new[]
                {
                    end - norm,
                    start - norm
                }, inside, newThick, true, endAlpha: endAlpha, blendLin: true, c2_: outside, outsideInvert: true), new RenderStates(BlendMode.Add));
            }

            Render.NumRays += cache.Length;
        }
        
        public EventWrapper<CollisionEventArgs> OnLaserHit { get; } = new EventWrapper<CollisionEventArgs>();
    }

    public class LaserRay
    {
        public LaserRay(Vector2f start, float angle, float length, Color color, float strength, float thickness, float startDistance,
            float refrac, Laser parent)
        {
            Start = start;
            Angle = angle;
            Length = length;
            Strength = strength * Simulation.LaserSuperBoost;
            Color = new Color(color) { A = (byte) Math.Min(255, strength * 255) };
            Thickness = thickness;
            StartDistance = startDistance;
            RefractiveIndex = refrac;
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public Vector2f Start { get; set; }
        public float Angle { get; set; }

        public float Length { get; set; }

        //public Vector2f End => Start + Tools.FromPolar(Length, Angle);
        public float Strength { get; set; }
        public Color Color { get; set; }
        public float EndStrength => Math.Max(0, Strength * (1 - Length / (Parent.FadeDistance - StartDistance)));
        public byte EndAlpha => (byte) Math.Min(255, EndStrength * 255);
        public Color EndColor => new Color(Color) { A = EndAlpha };
        public float Thickness { get; set; }
        public float StartDistance { get; set; }
        public float EndDistance => StartDistance + Length;
        public float RefractiveIndex { get; set; }
        public string? DebugInfo { get; set; }
        public LaserRay? Source { get; set; }
        public Laser Parent { get; set; }

        public Vector2f GetEndClipped()
        {
            var length = Length;
            if (float.IsPositiveInfinity(length))
                length = Math.Max(Camera.GameView.Size.X, Camera.GameView.Size.Y);
            return Start + Tools.FromPolar(length, Angle);
        }
    }
}