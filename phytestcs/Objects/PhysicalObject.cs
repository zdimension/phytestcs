using System;
using System.Collections.Generic;
using System.Linq;
using phytestcs.Interface;
using SFML.Graphics;
using SFML.System;
using static phytestcs.Global;

namespace phytestcs.Objects
{
    public class PhysicalObject : Object
    {
        private Vector2f _position;
        private float _angle;

        [ObjProp("Position", "m", "m\u22c5s", "m/s")]
        public Vector2f Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdatePosition();
                UpdatePhysics(0);
            }
        }

        [ObjProp("Angle", "rad", "rad\u22c5s", "rad/s")]
        public float Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                UpdatePosition();
                UpdatePhysics(0);
            }
        }

        [ObjProp("Angular velocity", "rad/s", "rad")]
        public float AngularVelocity { get; set; }

        [ObjProp("Velocity", "m/s", "m")]
        public Vector2f Velocity { get; set; }

        public Shape Shape { get; }

        public SynchronizedCollection<Force> Forces { get; }

        [ObjProp("Mass", "kg")]
        public float Mass
        {
            get;
            set;
        }

        public float InertialMass => Fixed ? float.PositiveInfinity : Mass;

        [ObjProp("Density", "kg/m²")]
        public float Density
        {
            get => Mass / Shape.Area();
            set => Mass = Shape.Area() * value;
        }

        [ObjProp("Linear kinetic energy", "J", unitDeriv:"W")]
        public float LinearKineticEnergy => Fixed ? 0 : Mass * (float) Math.Pow(Velocity.Norm(), 2) / 2;
        [ObjProp("Angular kinetic energy", "J", unitDeriv: "W")]
        public float AngularKineticEnergy => Fixed ? 0 : MomentOfInertia * (float)Math.Pow(AngularVelocity, 2) / 2;
        [ObjProp("Kinetic energy", "J", unitDeriv: "W")]
        public float KineticEnergy => LinearKineticEnergy + AngularKineticEnergy;
        [ObjProp("Potential gravitational energy", "J", unitDeriv: "W")]
        public float GravityEnergy => Fixed ? 0 : Weight * Position.WithUpdate(this).Y;
        [ObjProp("Potential attraction energy", "J", unitDeriv: "W")]
        public float AttractionEnergy => Fixed ? 0 : Simulation.AttractionEnergy(Position, Mass, this);
        [ObjProp("Potential energy", "J", unitDeriv: "W")]
        public float PotentialEnergy => GravityEnergy + AttractionEnergy;

        [ObjProp("Total energy", "J", unitDeriv: "W")]
        public float TotalEnergy => KineticEnergy + PotentialEnergy;
        public bool Fixed => Wall || IsMoving || HasFixate;
        public bool Wall { get; set; }
        public bool IsMoving { get; set; }

        private Force Gravity { get; } = new Force(ForceType.Gravity, new Vector2f(0, 0), default);
        private Force AirFriction { get; } = new Force(ForceType.AirFriction, new Vector2f(0, 0), default);
        private Force Buoyance { get; } = new Force(ForceType.Buoyancy, new Vector2f(0, 0), default);
        public float Weight => Mass * Simulation.ActualGravity;
        [ObjProp("Restitution")]
        public float Restitution { get; set; } = 0.5f;
        [ObjProp("Friction")]
        public float Friction { get; set; } = 0.5f;
        public bool Killer { get; set; }
        public bool HasFixate { get; set; } = false;
        [ObjProp("Attraction", "Nm²/kg²")]
        public float Attraction { get; set; } = 0;
        public bool AttractionIsLinear = false;
        [ObjProp("Momentum", "N⋅s", "N⋅s²", "N")]
        public Vector2f Momentum => Mass * Velocity;

        [ObjProp("Moment of inertia", "kg⋅m²")] // m^4 ?
        public float MomentOfInertia
        {
            get
            {
                return Shape switch
                {
                    RectangleShape r => Mass * (r.Size.NormSquared()) / 12,
                    CircleShape c => Mass * (float) Math.Pow(c.Radius, 4) / 2,
                    _ => throw new NotImplementedException()
                };
            }
        }

        public float FieldNorm(float masse = 1f)
        {
            return Attraction * Mass * masse;
        }

        public Vector2f AttractionField(Vector2f pos, float masse = 1f)
        {
            if (Attraction == 0.0f)
                return default;

            var delta = Position - pos;
            var dist = delta.Norm();

            if (!AttractionIsLinear)
                dist *= dist;

            return delta.Normalize() * FieldNorm(masse) / dist;
        }

        public float AttractionEnergyCaused(Vector2f pos, float masse = 1f)
        {
            if (Attraction == 0.0f)
                return 0;

            var dist = (Position - pos).Norm();

            return FieldNorm(masse) *
                   (AttractionIsLinear
                       ? (float)Math.Log(dist)
                       : -1 / dist);
        }

        public delegate void CollisionHandler(PhysicalObject source);

        public event CollisionHandler ObjectCollided; 

        public override void UpdatePhysics(float dt)
        {
            if (dt == 0)
                return;

            if (float.IsNaN(_position.X) || float.IsNaN(_position.Y) || Math.Abs(_position.X) > 5100 || Math.Abs(_position.Y) > 5100)
            {
                Console.WriteLine($"RIP {Name}");
                Delete();
                return;
            }

            Gravity.Value = Simulation.GravityField(Position, Mass, this);

            if (!Fixed)
            {
                if (Simulation.AirFriction)
                {
                    if (Velocity == default)
                    {
                        AirFriction.Value = default;
                    }
                    else
                    {
                        var velRel = Velocity - Simulation.WindVector;
                        var vn = velRel.Norm();
                        var vu = velRel / vn;
                        var size = Shape.GetGlobalBounds().Size();
                        var diam1 = Math.Abs(size.Dot(velRel.Ortho()));

                        AirFriction.Value = (-diam1 * Simulation.AirFrictionMultiplier *
                                                 (Simulation.AirFrictionQuadratic * vn + Simulation.AirFrictionLinear) *
                                                 vn) * vu;
                    }

                    AngularAirFriction = AngularVelocity 
                                         * -Simulation.RotFrictionLinear
                                         * Simulation.AirFrictionMultiplier;
                }
                else
                {
                    AirFriction.Value = default;

                    AngularAirFriction = 0;
                }

                Buoyance.Value = -Simulation.GravityVector * Shape.Area() * Simulation.AirDensity;
            }

            ApplyForces(dt);

            if (!Fixed)
            {
                _position += Velocity * dt;
                _angle += AngularVelocity * dt;
                _angle = (float) Math.Round(_angle, 2);
                UpdatePosition();
            }
        }

        public override bool Contains(Vector2f point)
        {
            return Shape.GetGlobalBounds().Contains(point.X, point.Y);
        }

        private void UpdatePosition()
        {
            Shape.Position = _position;
            Shape.Rotation = _angle.Degrees();
        }

        public static Vector2f ElasticCollision(float m1, float m2, Vector2f v1, Vector2f v2, float r1, float r2,
            float phi)
        {
            if (float.IsInfinity(m1))
            {
                m1 = 1;
                m2 = 0;
            }
            else if (float.IsInfinity(m2))
            {
                m1 = 0;
                m2 = 1;
            }

            var rest = Tools.Average(r1, r2);

            var (v1n, v1t) = v1.ToPolar();
            var (v2n, v2t) = v2.ToPolar();

            return new Vector2f(
                (float) (rest * Math.Cos(phi) * (v1n * Math.Cos(v1t - phi) * (m1 - m2) + 2 * m2 * v2n * Math.Cos(v2t - phi)) /
                    (m1 + m2) + v1n * Math.Sin(v1t - phi) * -Math.Sin(phi)),
                (float) (rest * Math.Sin(phi) * (v1n * Math.Cos(v1t - phi) * (m1 - m2) + 2 * m2 * v2n * Math.Cos(v2t - phi)) /
                    (m1 + m2) + v1n * Math.Sin(v1t - phi) * Math.Cos(phi))
            );
        }

        public (bool coll, FloatRect rect) Collision(PhysicalObject autre)
        {
            return (Shape.GetGlobalBounds().Intersects(autre.Shape.GetGlobalBounds(), out var overlap), overlap);
        }

        [ObjProp("Net force", "N")]
        public Vector2f NetForce
        {
            get
            {
                lock (Forces.SyncRoot)
                {
                    return Forces.Select(f => f.Value).Aggregate((a, b) => a + b);
                }
            }
        }

        public Vector2f RotationPoint
        {
            get
            {
                lock (Forces.SyncRoot)
                {
                    var sum = Forces.Sum(f => f.Value.Norm());
                    if (sum == default)
                        return default;
                    return Forces.Sum(f => f.Position * f.Value.Norm()) / sum;
                }
            }
        }

        public float NetTorque
        {
            get
            {
                var avgAppPoint = RotationPoint;

                lock (Forces.SyncRoot)
                {
                    return (from f in Forces
                        let realPos = f.Position - avgAppPoint
                        let realAngle = f.Value.Angle() - Angle
                        select f.Value.Norm() * 
                               (realPos.X / Shape.GetLocalBounds().Width) *
                               (float) Math.Sin(realAngle) * realPos.Norm()
                               ).Sum();
                }
            }
        }

        [ObjProp("Acceleration", "m/s²", "m/s", "m/s³")]
        public Vector2f Acceleration => Fixed ? default : (NetForce / Mass);

        public float AngularAcceleration => Fixed ? default : (NetTorque / MomentOfInertia + AngularAirFriction);

        private float AngularAirFriction;

        [ObjProp("Angular momentum", "J⋅s")]
        public float AngularMomentum => MomentOfInertia * AngularVelocity;
        
        public Vector2f Map(Vector2f point)
        {
            return Shape.Transform.TransformPoint(Shape.Origin + point);
        }

        public Vector2f MapInv(Vector2f point)
        {
            return Shape.InverseTransform.TransformPoint(point) - Shape.Origin;
        }

        private void ApplyForces(float dt)
        {
            if (Fixed)
            {
                Velocity = default;
                AngularVelocity = 0;
            }
            else
            {
                Velocity += Acceleration * dt;
                AngularVelocity += AngularAcceleration * dt;
            }
            //_angVel *= (float)Math.Exp(-0.031 * dt);

            lock (Forces.SyncRoot)
            {
                for (var i = Forces.Count - 1; i >= 0; i--)
                {
                    if ((Forces[i].TimeToLive -= dt) <= 0)
                        Forces.RemoveAt(i);
                }
            }
        }

        public PhysicalObject(Vector2f pos, Shape shape, bool wall=false, string name="")
        {
            Name = name;

            Shape = shape;
            Shape.Origin = Shape.GetLocalBounds().Size() / 2;
            _position = Shape.Position = pos;

            Wall = wall;
            Mass = wall ? float.PositiveInfinity : shape.Area();

            Forces = new SynchronizedCollection<Force> { Gravity, AirFriction, Buoyance };
        }

        public static PhysicalObject Rectangle(float x, float y, float w, float h, Color col, bool wall=false, string name="", bool killer=false)
        {
            return new PhysicalObject(new Vector2f(x, y), new RectangleShape(new Vector2f(w, h)){FillColor = col}, wall, name){Killer=killer};
        }

        public static PhysicalObject Cercle(float x, float y, float r, Color col)
        {
            return new PhysicalObject(new Vector2f(x, y), new CircleShape(r) { FillColor = col });
        }

        public override void Draw()
        {
            base.Draw();

            Render.Window.Draw(Shape);
        }

        private const float CircleSize = 0.05f;

        private readonly CircleShape rotCenter = new CircleShape(CircleSize)
            { FillColor = Color.Red, Origin = new Vector2f(CircleSize, CircleSize) };
        private static readonly Text forceName = new Text("", UI.Font);

        public override void DrawOverlay()
        {
            base.DrawOverlay();

            if (Render.ShowForces)
            {
                foreach (var f in Forces.ToArrayLocked())
                {
                    var origin = Map(f.Position);
                    var delta = f.Value * Render.ForcesScale;
                    var tip = origin + delta;
                    var arrow = delta.Norm() / 5;
                    var arrowAng = (float)(Math.PI / 4);
                    var angle = f.Value.Angle();
                    var color = f.Type.Color;
                    if (Render.ShowForcesComponents)
                    {
                        var colorTrans = color;
                        colorTrans.A = 80;
                        
                        Render.Window.Draw(new[]
                        {
                            new Vertex(origin, colorTrans),
                            new Vertex(origin + new Vector2f(delta.X, 0), colorTrans),
                            new Vertex(tip, color),
                            new Vertex(origin + new Vector2f(0, delta.Y), colorTrans),
                            new Vertex(origin, colorTrans)
                        }, PrimitiveType.LineStrip);
                    }
                    Render.Window.Draw(new[]
                    {
                        new Vertex(origin, color),
                        new Vertex(tip, color),
                        new Vertex(tip, color),
                        new Vertex(tip - Tools.FromPolar(arrow, angle + arrowAng), color),
                        new Vertex(tip, color),
                        new Vertex(tip - Tools.FromPolar(arrow, angle - arrowAng), color),
                    }, PrimitiveType.Lines);

                    Render.Window.SetView(Camera.MainView);

                    if ((forceName.CharacterSize = (uint) (30 * arrow)) < 300)
                    {
                        forceName.FillColor = color;
                        forceName.DisplayedString = f.Type.ShortName;
                        if (Render.ShowForcesValues)
                            forceName.DisplayedString += $" = {f.Value.Norm():F2} N";
                        forceName.Position = tip.ToScreen().F();
                        Render.Window.Draw(forceName);
                    }

                    Render.Window.SetView(Camera.GameView);
                }

                rotCenter.Position = Map(RotationPoint);
                Render.Window.Draw(rotCenter);
            }
        }

        private static (Vector2f[], bool, PhysicalObject, PhysicalObject, Vector2f, Vector2f) GetForcePoints(PhysicalObject a, PhysicalObject b, Vector2f dpa, Vector2f dpb, bool second=false)
        {
            var ap = a.Shape.PointsGlobal();
            var bp = b.Shape.PointsGlobal();

            var colls = new Vector2f[2];
            var inds = new int[2];
            var np = 0;

            for (var k = 0; k < ap.Length; k++)
            {
                if (bp.ContainsPoint(ap[k]))
                {
                    inds[np] = k;
                    colls[np++] = ap[k];
                    if (np == 2)
                        break;
                }
            }

            if (np == 0 && !second)
            {
                return GetForcePoints(b, a, dpb, dpa, true);
            }

            var inters = new Vector2f[2];
            var intersn = 0;

            if (np == 1)
            {
                var iprev = inds[0] == 0 ? ap.Length - 1 : inds[0];
                var inext = (inds[0] + 1) % ap.Length;
                var pprev = ap[iprev];
                var pnext = ap[inext];

                static bool Intersects((Vector2f, Vector2f) a, (Vector2f, Vector2f) b,
                    out Vector2f inter)
                {
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
                            inter = a1 + t * aD;
                            return true;
                        }
                    }

                    return false;
                }

                for (var k = 0; k < bp.Length; k++)
                {
                    var cur = bp[k];
                    var nex = bp[(k + 1) % bp.Length];

                    if (Intersects((cur, nex), (colls[0], pprev), out var inter1))
                    {
                        inters[intersn++] = inter1;
                    }

                    if (Intersects((cur, nex), (colls[0], pnext), out var inter2))
                    {
                        inters[intersn++] = inter2;
                    }
                }
            }
            
            var p2out = false;

            if (intersn > 0)
            {
                for (var i1 = 0; i1 < intersn; i1++)
                {
                    inters[i1] += dpa;

                    if (bp.ContainsPoint(inters[i1] - dpb))
                    {
                        colls[np++] = inters[i1];
                        p2out = true;
                        break;
                    }
                }
            }

            Array.Resize(ref colls, np);

            return (colls, p2out, a, b, dpa, dpb);
        }

        public static void ProcessPairs(float dt, PhysicalObject[] phy)
        {
            for (var i = 0; i < phy.Length - 1; i++)
            {
                for (var j = i + 1; j < phy.Length; j++)
                {
                    var a = phy[i];
                    var b = phy[j];

                    if (a.Fixed && b.Fixed)
                        continue;

                    if (OBB.testCollision(a.Shape, b.Shape, out var mtv))
                    {
                        var unitMtv = mtv.Normalize();
                        var v1p = a.Velocity.Dot(unitMtv);
                        var v2p = b.Velocity.Dot(unitMtv);

                        var dpa = new Vector2f();
                        var dpb = new Vector2f();

                        if (a.Fixed)
                        {
                            dpb = -mtv;
                        }
                        else if (b.Fixed)
                        {
                            dpa = mtv;
                        }
                        else
                        {
                            var vs = v1p + v2p;
                            dpa = mtv * v1p / vs;
                            dpb = mtv * v2p / vs;
                        }

                        var (colls, p2out, a_, b_, dpa_, dpb_) = GetForcePoints(a, b, dpa, dpb);

                        var np = colls.Length;

                        a_._position += dpa_;

                        for (var i1 = 0; i1 < np; i1++)
                        {
                            colls[i1] += dpa_;
                        }

                        b_._position += dpb_;

                        var phi = mtv.Angle();
                        var (v1c, v2c) = (
                            ElasticCollision(a.Mass, b.Mass, a.Velocity, b.Velocity, a.Restitution, b.Restitution, phi),
                            ElasticCollision(b.Mass, a.Mass, b.Velocity, a.Velocity, b.Restitution, a.Restitution, phi)
                        );

                        var fA = ((v1c) - a.Velocity) * a.Mass / dt;
                        var fB = ((v2c) - b.Velocity) * b.Mass / dt;
                        var w = new float[np];

                        if (np == 1)
                        {
                            w[0] = 1f;
                        }
                        else if (np == 2)
                        {
                            if (p2out)
                            {

                            }
                            else
                            {
                                w[0] = 0.5f;
                                w[1] = 0.5f;
                            }
                        }

                        for (var i1 = 0; i1 < np; i1++)
                        {
                            a.Forces.Add(new Force(ForceType.Normal, fA * w[i1], a.MapInv(colls[i1]), ttl: dt));
                            b.Forces.Add(new Force(ForceType.Normal, fB * w[i1], b.MapInv(colls[i1]), ttl: dt));
                        }

                        var friction = (float)Math.Sqrt(a.Friction * b.Friction);
                        var unit = unitMtv.Ortho();

                        if (!a.Fixed)
                        {
                            var vproj = -a.Velocity.Dot(unit);
                            var ff = (vproj * friction) * unit;

                            for (var i1 = 0; i1 < np; i1++)
                            {
                                a.Forces.Add(new Force(ForceType.Friction, ff * w[i1], a.MapInv(colls[i1]), ttl: dt));

                            }
                        }

                        if (!b.Fixed)
                        {
                            var vproj = -b.Velocity.Dot(unit);
                            var ff = (vproj * friction) * unit;

                            for (var i1 = 0; i1 < np; i1++)
                            {
                                b.Forces.Add(new Force(ForceType.Friction, ff * w[i1], b.MapInv(colls[i1]), ttl: dt));
                            }
                        }

                        //if (!a.Fixed && obj._velocity.Y != 0 && obj.Shape.GetGlobalBounds().CollidesY(Shape.GetGlobalBounds()))
                        //    obj.Forces.Add(new Force("Friction Y", new Vector2f(0, (float)(Math.Abs(obj.NetForce.X) * Math.Sqrt(Friction * obj.Friction) * -Math.Sign(obj._velocity.Y))), -1));


                        //Simulation.Pause = true;

                        /*(a._velocity.X, b._velocity.X) = (
                            ElasticCollision(a.Mass, b.Mass, a._velocity.X, b._velocity.X, a.Restitution, b.Restitution),
                            ElasticCollision(b.Mass, a.Mass, b._velocity.X, a._velocity.X, b.Restitution, a.Restitution));

                        (a._velocity.Y, b._velocity.Y) = (
                            ElasticCollision(a.Mass, b.Mass, a._velocity.Y, b._velocity.Y, a.Restitution, b.Restitution),
                            ElasticCollision(b.Mass, a.Mass, b._velocity.Y, a._velocity.Y, b.Restitution, a.Restitution));*/
                    }
                }
            }
        }
    }
}
