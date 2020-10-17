using System;
using System.Collections.Generic;
using System.Linq;
using phytestcs.Interface;
using phytestcs.Internal;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public abstract class PhysicalObject : BaseObject, ICollides
    {
        private static readonly Text ForceName = new Text("", Ui.FontMono)
            { OutlineThickness = 2, OutlineColor = Color.Black };

        private readonly List<PhysicalObject> _collIgnore = new List<PhysicalObject>();

        private float _angle;

        private float _angularAirFriction;
        private Vector2f[] _globalPointsCache = null!;

        private float _globalPointsCacheTime = -1;
        private Vector2f _position;

        [ObjProp("Default density", "kg/m²")]
        public static float DefaultDensity { get; set; } = 2;

        protected PhysicalObject(Vector2f pos, Shape shape, bool wall = false, string name = "")
        {
            Name = name;
            
            Shape = shape ?? throw new ArgumentNullException(nameof(shape));
            Shape.Origin = Shape.CenterOfGravity();
            _position = Shape.Position = pos;
            
            Area = Shape.Area();

            Wall = wall;
            Density = wall ? float.PositiveInfinity : DefaultDensity;

            Forces = new SynchronizedCollection<Force> { Gravity, AirFriction, Buoyance };

            UpdateOutline();
        }

        public bool AttractionIsLinear { get; set; } = false;

        [ObjProp("Angular velocity", "rad/s", "rad", shortName: "ω")]
        public float AngularVelocity { get; set; }

        [ObjProp("Velocity", "m/s", "m", shortName:"v")]
        public Vector2f Velocity { get; set; }

        public SynchronizedCollection<Force> Forces { get; }

        [ObjProp("Mass", "kg", shortName: "m")]
        public float Mass { get; set; }

        public float InertialMass => Fixed ? float.PositiveInfinity : Mass;

        [ObjProp("Density", "kg/m²", shortName: "ρ")]
        public float Density
        {
            get => Mass / Area;
            set => Mass = Area * value;
        }

        public float Area { get; }

        [ObjProp("Linear kinetic energy", "J", unitDeriv: "W", shortName: "Kt")]
        public float LinearKineticEnergy => Fixed ? 0 : Mass * (float) Math.Pow(Velocity.Norm(), 2) / 2;

        [ObjProp("Angular kinetic energy", "J", unitDeriv: "W", shortName: "Kr")]
        public float AngularKineticEnergy => Fixed ? 0 : MomentOfInertia * (float) Math.Pow(AngularVelocity, 2) / 2;

        [ObjProp("Kinetic energy", "J", unitDeriv: "W", shortName: "K")]
        public float KineticEnergy => LinearKineticEnergy + AngularKineticEnergy;

        [ObjProp("Potential gravitational energy", "J", unitDeriv: "W", shortName: "Ug")]
        public float GravityEnergy => Fixed ? 0 : Weight * Position.WithUpdate(this).Y;

        [ObjProp("Potential attraction energy", "J", unitDeriv: "W", shortName: "Ua")]
        public float AttractionEnergy => Fixed ? 0 : Simulation.AttractionEnergy(Position, Mass, this);

        [ObjProp("Potential energy", "J", unitDeriv: "W", shortName: "U")]
        public float PotentialEnergy => GravityEnergy + AttractionEnergy;

        [ObjProp("Total energy", "J", unitDeriv: "W", shortName: "E")]
        public float TotalEnergy => KineticEnergy + PotentialEnergy;

        public bool Fixed => Wall || IsMoving || HasFixate || UserFix;
        public bool Wall { get; set; }
        public bool IsMoving { get; set; }

        private Force Gravity { get; } = new Force(ForceType.Gravity, new Vector2f(0, 0), default);
        private Force AirFriction { get; } = new Force(ForceType.AirFriction, new Vector2f(0, 0), default);
        private Force Buoyance { get; } = new Force(ForceType.Buoyancy, new Vector2f(0, 0), default);

        public float Weight => Mass * Simulation.ActualGravity;

        [ObjProp("Restitution", shortName: "e")]
        public float Restitution { get; set; } = 0.5f;

        [ObjProp("Friction", "N/(m/s)", shortName: "µ")]
        public float Friction { get; set; } = 0.5f;

        public bool Killer { get; set; }
        public bool HasFixate { get; set; } = false;
        public bool UserFix { get; set; } = false;

        [ObjProp("Attraction", "Nm²/kg²", shortName: "G")]
        public float Attraction { get; set; } = 0;

        [ObjProp("Momentum", "N⋅s", "N⋅s²", "N", "p")]
        public Vector2f Momentum => Mass * Velocity;

        [ObjProp("Moment of inertia", "kg⋅m²", shortName: "I")]
        public float MomentOfInertia
        {
            get
            {
                return InertiaMultiplier * Shape switch
                {
                    RectangleShape r => Mass * r.Size.NormSquared() / 12,
                    CircleShape c => (float) (Mass * Math.Pow(c.Radius, 2) / 2),
                    { } s => Mass * s.MomentOfInertia(),
                    _ => throw new NotImplementedException()
                };
            }
        }

        protected override IEnumerable<Shape> Shapes => new[] { Shape };

        [ObjProp("Net force", "N", "N⋅s", "N/s", "F")]
        public Vector2f NetForce
        {
            get
            {
                lock (Forces.SyncRoot)
                {
                    return Forces.Where(f => !f.OnlyTorque).Select(f => f.Value).Aggregate((a, b) => a + b);
                }
            }
        }

        public Hinge? Hinge { get; set; } = null;


        /// <summary>
        ///     Total torque around the object's rotation point (<see cref="RotationPoint" />).
        /// </summary>
        /// <remarks>
        ///     Derivative of the angular momentum (<see cref="AngularMomentum" />).
        /// </remarks>
        [ObjProp("Net torque", "N⋅m", "J⋅s", "W", "τ")]
        public float NetTorque
        {
            get
            {
                lock (Forces.SyncRoot)
                {
                    return Forces.Select(f => f.Position.Cross(f.Value.Rotate(-Angle))).Sum();
                }
            }
        }

        public Vector2f[] GlobalPointsCache
        {
            get
            {
                if (_globalPointsCache == null || Simulation.SimDuration != _globalPointsCacheTime)
                {
                    _globalPointsCache = Shape.PointsGlobal();
                    _globalPointsCacheTime = Simulation.SimDuration;
                }

                return _globalPointsCache;
            }
        }

        /// <summary>
        ///     Rate of change of the linear velocity.
        /// </summary>
        /// <remarks>
        ///     Derivative of the linear velocity (<see cref="Velocity" />).
        ///     Antiderivative of the jerk.
        /// </remarks>
        [ObjProp("Acceleration", "m/s²", "m/s", "m/s³", "a")]
        public Vector2f Acceleration => Fixed ? default : NetForce / Mass;

        // <summary>
        /// Rate of change of the angular velocity.
        /// </summary>
        /// <remarks>
        ///     Derivative of the angular velocity (<see cref="AngularVelocity" />).
        ///     Antiderivative of the angular jerk.
        /// </remarks>
        [ObjProp("Angular acceleration", "rad/s²", "rad/s", "rad/s³", "α")]
        public float AngularAcceleration => Fixed ? default : NetTorque / MomentOfInertia + _angularAirFriction;

        [ObjProp("Angular momentum", "J⋅s", shortName: "L")]
        public float AngularMomentum => MomentOfInertia * AngularVelocity;

        /// <summary>
        ///     Dimensionless ratio between the speed of light in vacuum and the speed of light in the object.
        /// </summary>
        [ObjProp("Refractive index", shortName: "n")]
        public float RefractiveIndex { get; set; } = 1.5f;

        /// <summary>
        ///     If true, the object will not collide with others with identical collide set
        /// </summary>
        [ObjProp("No self collision")]
        public bool HeteroCollide { get; set; } = false;

        public override Vector2f Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdatePosition();
                UpdatePhysics(0);
            }
        }

        public override float Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                UpdatePosition();
                UpdatePhysics(0);
            }
        }

        [ObjProp("Air friction multiplier")]
        public float AirFrictionMultiplier { get; set; } = 1;

        [ObjProp("Inertia multiplier")]
        public float InertiaMultiplier { get; set; } = 1;

        [ObjProp("Lock the angle")]
        public bool LockAngle { get; set; } = false;

        public bool ColorFilter => !float.IsPositiveInfinity(ColorFilterWidth);

        [ObjProp("Filter tolerance", "°")]
        public float ColorFilterWidth { get; set; } = float.PositiveInfinity;

        public EventWrapper<CollisionEventArgs> OnCollide { get; } = new EventWrapper<CollisionEventArgs>();

        public EventWrapper<LaserCollisionEventArgs> OnHitByLaser { get; } =
            new EventWrapper<LaserCollisionEventArgs>();

        public uint CollideSet { get; set; } = 1;

        public Shape Shape { get; }

        public override Vector2f Map(Vector2f local)
        {
            return Shape.Transform.TransformPoint(Shape.Origin + local);
        }

        public override Vector2f MapInv(Vector2f global)
        {
            return Shape.InverseTransform.TransformPoint(global) - Shape.Origin;
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
                       ? (float) Math.Log(dist)
                       : -1 / dist);
        }

        public override void UpdatePhysics(float dt)
        {
            if (dt != 0)
            {
                if (float.IsNaN(_position.X) || float.IsNaN(_position.Y) || Math.Abs(_position.X) > 5100 ||
                    Math.Abs(_position.Y) > 5100)
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
                        var mult = Simulation.AirFrictionMultiplier * AirFrictionMultiplier;

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

                            AirFriction.Value = -diam1 * mult *
                                                (Simulation.AirFrictionQuadratic * vn + Simulation.AirFrictionLinear) *
                                                vn * vu;
                        }

                        _angularAirFriction = AngularVelocity
                                              * -Simulation.RotFrictionLinear
                                              * mult;
                    }
                    else
                    {
                        AirFriction.Value = default;

                        _angularAirFriction = 0;
                    }

                    Buoyance.Value = -Simulation.GravityVector * Area * Simulation.AirDensity;
                }

                ApplyForces(dt);

                if (!Fixed)
                {
                    if (Hinge != null && float.IsPositiveInfinity(Hinge.MotorTorque))
                        AngularVelocity = Hinge.MotorSpeed;

                    if (!LockAngle)
                    {
                        var dA = AngularVelocity * dt;
                        _angle += dA;
                        _angle = ((float) Math.Round(_angle, 6)).ClampWrap((float) Math.PI);
                    }

                    if (Hinge != null)
                    {
                        Velocity = default;
                        //var oldPos = _position;

                        //_position = Shape.Transform.TransformPoint(-Hinge.RelPos);
                        _position = Hinge.OriginalPosition - Hinge.End1.RelPos.Rotate(Angle);
                    }
                    else
                    {
                        _position += Velocity * dt;
                    }

                    UpdatePosition();
                }
            }

            base.UpdatePhysics(dt);
        }

        private void UpdatePosition()
        {
            Shape.Position = Position;
            Shape.Rotation = Angle.Degrees();
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
                (float) (rest * Math.Cos(phi) *
                    (v1n * Math.Cos(v1t - phi) * (m1 - m2) + 2 * m2 * v2n * Math.Cos(v2t - phi)) /
                    (m1 + m2) + v1n * Math.Sin(v1t - phi) * -Math.Sin(phi)),
                (float) (rest * Math.Sin(phi) *
                    (v1n * Math.Cos(v1t - phi) * (m1 - m2) + 2 * m2 * v2n * Math.Cos(v2t - phi)) /
                    (m1 + m2) + v1n * Math.Sin(v1t - phi) * Math.Cos(phi))
            );
        }

        public (bool coll, FloatRect rect) Collision(PhysicalObject autre)
        {
            return (Shape.GetGlobalBounds().Intersects(autre.Shape.GetGlobalBounds(), out var overlap), overlap);
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

            _collIgnore.Clear();

            lock (Forces.SyncRoot)
            {
                for (var i = Forces.Count - 1; i >= 0; i--)
                    if ((Forces[i].TimeToLive -= dt) <= 0)
                    {
                        Forces.RemoveAt(i);
                    }
                    else
                    {
                        if (Forces[i].Source is Hinge h)
                        {
                            if (h.End1.Object == this)
                                _collIgnore.Add(h.End2.Object);
                            else
                                _collIgnore.Add(h.End1.Object);
                        }
                    }
            }
        }
        public override void Draw()
        {
            base.Draw();

            Shape.OutlineThickness = (Selected ? -7 : Appearance.Borders ? -2 : 0) / Camera.Zoom;
            Render.Window.Draw(Shape);
        }

        public override void DrawOverlay()
        {
            base.DrawOverlay();

            if (!Render.ShowForces && !Appearance.ShowForces)
                return;
            
            foreach (var f in Forces.ToArrayLocked())
            {
                var origin = Map(f.Position);
                var delta = f.Value * Render.ForcesScale;
                var tip = origin + delta;
                var arrow = delta.Norm() / 5;
                const float arrowAng = (float) (Math.PI / 4);
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
                    new Vertex(tip - Tools.FromPolar(arrow, angle - arrowAng), color)
                }, PrimitiveType.Lines);

                Render.Window.SetView(Camera.MainView);

                if ((ForceName.CharacterSize = (uint) (30 * arrow)) < 300)
                {
                    ForceName.FillColor = color;
                    ForceName.DisplayedString = f.Type.ShortName;
                    ForceName.OutlineThickness = ForceName.CharacterSize / 20f;
                    if (Render.ShowForcesValues)
                        ForceName.DisplayedString += $" = {f.Value.Norm():F2} N";
                    ForceName.Position = tip.ToScreen().F();
                    Render.Window.Draw(ForceName);
                }

                Render.Window.SetView(Camera.GameView);
            }
        }

        private static (Vector2f[], bool, PhysicalObject, PhysicalObject, Vector2f, Vector2f) GetForcePoints(
            PhysicalObject a, PhysicalObject b, Vector2f dpa, Vector2f dpb, bool second = false)
        {
            var ap = a.GlobalPointsCache;
            var bp = b.GlobalPointsCache;

            var colls = new Vector2f[2];
            var inds = new int[2];
            var np = 0;

            for (var k = 0; k < ap.Length; k++)
                if (bp.ContainsPoint(ap[k]))
                {
                    inds[np] = k;
                    colls[np++] = ap[k];
                    if (np == 2)
                        break;
                }

            if (np == 0 && !second)
                return GetForcePoints(b, a, dpb, dpa, true);

            var inters = new Vector2f[2];
            var intersn = 0;

            if (np == 1)
            {
                var iprev = inds[0] == 0 ? ap.Length - 1 : inds[0];
                var inext = (inds[0] + 1) % ap.Length;
                var pprev = ap[iprev];
                var pnext = ap[inext];

                for (var k = 0; k < bp.Length; k++)
                {
                    var cur = bp[k];
                    var nex = bp[(k + 1) % bp.Length];

                    if (Tools.Intersects((cur, nex), (colls[0], pprev), out var inter1, out _))
                        inters[intersn++] = inter1;

                    if (intersn == 2)
                        break;

                    if (Tools.Intersects((cur, nex), (colls[0], pnext), out var inter2, out _))
                        inters[intersn++] = inter2;

                    if (intersn == 2)
                        break;
                }
            }

            var p2out = false;

            if (intersn > 0)
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

            Array.Resize(ref colls, np);

            return (colls, p2out, a, b, dpa, dpb);
        }

        /// <summary>
        ///     Returns the velocity of a particular local point relative to the center of gravity.
        ///     The speed is always normal to the vector from the center of gravity to the point.
        /// </summary>
        public Vector2f SpeedAtPoint(Vector2f local)
        {
            return Velocity - AngularVelocity * local.Ortho().Rotate(Angle);
        }

        public static void ProcessPairs(float dt, PhysicalObject[] phy)
        {
            for (var i = 0; i < phy.Length - 1; i++)
            for (var j = i + 1; j < phy.Length; j++)
            {
                var a = phy[i];
                var b = phy[j];

                if (a.Fixed && b.Fixed)
                    continue;

                if ((a.CollideSet & b.CollideSet) == 0)
                    continue;

                if (a.HeteroCollide && b.HeteroCollide && a.CollideSet == b.CollideSet)
                    continue;

                if (a._collIgnore.Contains(b) || b._collIgnore.Contains(a))
                    continue;
                
                if (Obb.TestCollision(a, b, out var mtv))
                {
                    var unitMtv = mtv.Normalize();
                    var v1p = -a.Velocity.Dot(unitMtv);
                    var v2p = b.Velocity.Dot(unitMtv);

                    var dpa = new Vector2f();
                    var dpb = new Vector2f();

                    // répartition des vitesses de séparation
                    // si un des deux est fixe, l'autre prend tout
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
                        // sinon on répartit proportionnellement
                        var vs = Math.Abs(v1p) + Math.Abs(v2p);
                        if (Math.Abs(vs) < 1e-6f)
                        {
                            dpa = mtv / 2;
                            dpb = -mtv / 2;
                        }
                        else
                        {
                            dpa = mtv * v1p / vs;
                            dpb = -mtv * v2p / vs;
                        }
                    }

                    var (colls, p2out, a_, b_, dpa_, dpb_) = GetForcePoints(a, b, dpa, dpb);

                    var np = colls.Length;

                    a_._position += dpa_;

                    for (var i1 = 0; i1 < np; i1++)
                        colls[i1] += dpa_;

                    b_._position += dpb_;

                    var phi = mtv.Angle();
                    var (v1c, v2c) = (
                        ElasticCollision(a.Mass, b.Mass, a.Velocity, b.Velocity, a.Restitution, b.Restitution, phi),
                        ElasticCollision(b.Mass, a.Mass, b.Velocity, a.Velocity, b.Restitution, a.Restitution, phi)
                    );

                    a.Velocity = v1c;
                    b.Velocity = v2c;


                    var fA = 1f * (v1c - a.Velocity) * a.Mass / dt;
                    var fB = 1f * (v2c - b.Velocity) * b.Mass / dt;
                    var tA = a.NetForce.Dot(unitMtv);
                    var tB = b.NetForce.Dot(unitMtv);
                    fA = -tA * unitMtv;
                    fB = -tB * unitMtv;
                    var wA = new float[np];
                    var wB = new float[np];

                    switch (np)
                    {
                        case 1:
                            wA[0] = 1f;
                            wB[0] = 1f;
                            break;
                        case 2:
                        {
                            var line = colls[1] - colls[0];
                            var X = line.Norm();

                            if (X == 0)
                            {
                                X = 1e-5f;
                            }

                            line /= X;

                            var gA = (a.Position - colls[0]).Dot(line);
                            var dA = X - gA;
                            wA[0] = dA / X;
                            wA[1] = gA / X;

                            if (Math.Sign(wA[0]) != Math.Sign(wA[1]))
                                wA[0] = wA[1] = 0.5f;

                            var gB = (b.Position - colls[0]).Dot(line);
                            var dB = X - gB;
                            wB[0] = dB / X;
                            wB[1] = gB / X;
                        
                            if (Math.Sign(wB[0]) != Math.Sign(wB[1]))
                                wB[0] = wB[1] = 0.5f;

                            /*if (p2out)
                            {
                                w[0] = 0.5f;
                                w[1] = 0.5f;
                                Console.WriteLine("not implemented");
                            }
                            else
                            {
                                w[0] = 0.5f;
                                w[1] = 0.5f;
                            }*/
                            break;
                        }
                    }

                    for (var i1 = 0; i1 < np; i1++)
                    {
                        a.Forces.Add(new Force(ForceType.Normal, fA * wA[i1], a.MapInv(colls[i1]), dt)
                            { Source = b, OnlyTorque = true});
                        b.Forces.Add(new Force(ForceType.Normal, fB * wB[i1], b.MapInv(colls[i1]), dt)
                            { Source = a, OnlyTorque = true });
                    }

                    var friction = (float) Math.Sqrt(a.Friction * b.Friction);
                    var unit = unitMtv.Ortho();
                    var unitF = friction * unit;

                    var unitFA = unitF * tA;
                    var ffA = 0;//a.Velocity.Dot(unit);

                    var unitFB = unitF * tB;
                    var ffB = 0;//b.Velocity.Dot(unit);

                    for (var i1 = 0; i1 < np; i1++)
                    {
                        var localA = a.MapInv(colls[i1]);
                        var localB = b.MapInv(colls[i1]);
                        var sA = a.SpeedAtPoint(localA);
                        var sB = b.SpeedAtPoint(localB);
                        var relVel = -(sA - sB).Dot(unit);
                        
                        a.Forces.Add(new Force(ForceType.Friction,
                                (ffA * wA[i1] - relVel) * unitFA, localA, dt)
                            { Source = b });

                        
                        b.Forces.Add(new Force(ForceType.Friction,
                                (ffB * wB[i1] - relVel) * unitFB, localB, dt)
                            { Source = a });
                    }

                    //if (!a.Fixed && obj._velocity.Y != 0 && obj.Shape.GetGlobalBounds().CollidesY(Shape.GetGlobalBounds()))
                    //    obj.Forces.Add(new Force("Friction Y", new Vector2f(0, (float)(Math.Abs(obj.NetForce.X) * Math.Sqrt(Friction * obj.Friction) * -Math.Sign(obj._velocity.Y))), -1));


                    //Simulation.SetPause(true);

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

    public class CollisionEventArgs : BaseEventArgs
    {
        public CollisionEventArgs(object @this, object other, Vector2f position, Vector2f normal) : base(@this)
        {
            Other = other;
            Position = position;
            Normal = normal;
        }

        public dynamic Other { get; }
        public Vector2f Position { get; }
        public Vector2f Normal { get; }
    }
    
    public sealed class Polygon : PhysicalObject
    {
        private static ConvexShape GetShape(IEnumerable<Vector2f> points, Color col)
        {
            var pointsArr = points.ToArray();
            var shape = new ConvexShape((uint) pointsArr.Length) { FillColor = col };
            
            for (var i = 0; i < pointsArr.Length; i++)
            {
                shape.SetPoint((uint)i, pointsArr[i]);
            }

            return shape;
        }
        
        public Polygon(float x, float y, IEnumerable<Vector2f> points, Color col, bool wall = false,
            string name = "", bool killer = false)
            : this(new Vector2f(x, y), GetShape(points, col), wall, name, killer)
        {
            
        }

        public Polygon(Vector2f pos, ConvexShape rect, bool wall = false, string name = "", bool killer = false)
            : base(pos, rect, wall, name)
        {
            Killer = killer;
        }

        public new ConvexShape Shape => (ConvexShape) base.Shape;
    }

    public sealed class Box : PhysicalObject
    {
        public Box(float x, float y, float w, float h, Color col, bool wall = false,
            string name = "", bool killer = false)
            : this(new Vector2f(x, y), new RectangleShape(new Vector2f(w, h)){FillColor = col}, wall, name, killer)
        {
        }

        public Box(Vector2f pos, RectangleShape rect, bool wall = false, string name = "", bool killer = false)
            : base(pos, rect, wall, name)
        {
            Killer = killer;
        }

        public new RectangleShape Shape => (RectangleShape) base.Shape;
    }
    
    public sealed class Circle : PhysicalObject
    {
        public Circle(float x, float y, float r, Color col, bool wall = false,
            string name = "", bool killer = false)
            : this(new Vector2f(x, y), new CircleShape(r){FillColor = col}, wall, name, killer)
        {
        }

        public Circle(Vector2f pos, CircleShape circle, bool wall = false, string name = "", bool killer = false)
            : base(pos, circle, wall, name)
        {
            Killer = killer;
            
            _protractorLines = new Lazy<VertexArray>(() =>
            {
                const uint numPoints1 = 36;
                const uint numPoints2 = 4;
                const uint numPoints = numPoints1 + numPoints2;
                var lines = new VertexArray(PrimitiveType.Lines, numPoints * 2);

                for (uint i = 0; i < numPoints1; i++)
                {
                    var point = Render.RotCirclePoints[i * 10];
                    float factor;
                    if (i % 9 == 0)
                        factor = 0.5f;
                    else
                        factor = 0.1f;
                    lines[2 * i + 0] = new Vertex(Shape.Radius * point, Color.White);
                    lines[2 * i + 1] = new Vertex(Shape.Radius * point * (1 - factor), Color.White);
                }

                for (uint i = 0; i < numPoints2; i++)
                {
                    var point = Render.RotCirclePoints[45 + i * 90];
                    const float factor = 0.18f;
                    lines[2 * numPoints1 + 2 * i + 0] = new Vertex(Shape.Radius * point, Color.White);
                    lines[2 * numPoints1 + 2 * i + 1] =
                        new Vertex(Shape.Radius * point * (1 - factor), Color.White);
                }

                return lines;
            });
        }

        public new CircleShape Shape => (CircleShape) base.Shape;
        
        private readonly Lazy<VertexArray> _protractorLines;

        public override void Draw()
        {
            base.Draw();

            if (Appearance.DrawCircleCakes)
                using(var cake = Tools.CircleCake(Position, Shape.Radius, Shape.OutlineColor, Angle))
                    Render.Window.Draw(cake);
            
            if (Appearance.Protractor)
            {
                var transform = Transform.Identity;
                transform.Translate(Position);
                transform.Rotate(Angle.Degrees());
                Render.Window.Draw(_protractorLines.Value, new RenderStates(RenderStates.Default){Transform = transform});
            }
        }
    }
}