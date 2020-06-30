using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using phytestcs.Interface;
using SFML.Graphics;
using SFML.System;

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

        [ObjProp("Vitesse angulaire", "rad/s", "rad")]
        public float AngularVelocity { get; set; }

        [ObjProp("Vitesse", "m/s", "m")]
        public Vector2f Velocity { get; set; }

        public Shape Shape { get; }

        public SynchronizedCollection<Force> Forces { get; set; }

        [ObjProp("Masse", "kg")]
        public float Mass
        {
            get;
            set;
        }

        public float InertialMass => Fixed ? float.PositiveInfinity : Mass;

        [ObjProp("Densité", "kg/m²")]
        public float Density
        {
            get => Mass / Shape.Area();
            set => Mass = Shape.Area() * value;
        }

        [ObjProp("Énergie cinétique", "J", unitDeriv:"W")]
        public float KineticEnergy => Fixed ? 0 : Mass * (float) Math.Pow(Velocity.Norm(), 2) / 2;
        [ObjProp("Énergie de pesanteur", "J", unitDeriv: "W")]
        public float GravityEnergy => Fixed ? 0 : Weight * Position.WithUpdate(this).Y;
        [ObjProp("Énergie d'attraction", "J", unitDeriv: "W")]
        public float AttractionEnergy => Fixed ? 0 : Simulation.AttractionEnergy(Position, Mass, this);

        [ObjProp("Énergie totale", "J", unitDeriv: "W")]
        public float TotalEnergy => KineticEnergy + GravityEnergy + AttractionEnergy;
        public bool Fixed => Wall || IsMoving || HasFixate;
        public bool Wall { get; set; }
        public bool IsMoving { get; set; }

        private Force Gravity { get; } = new Force("Gravité", new Vector2f(0, 0), default, sname: "P"){Color=Color.Black};
        private Force AirFriction { get; } = new Force("Frottements de l'air", new Vector2f(0, 0), default, sname: "f") {Color=Color.Red};
        private Force Buoyance { get; } = new Force("Poussée d'Archimède", new Vector2f(0, 0), default, sname: "Φ"){Color=Color.Green};
        public float Weight => Mass * Simulation.ActualGravity;
        [ObjProp("Restitution")]
        public float Restitution { get; set; } = 0.5f;
        [ObjProp("Frottements")]
        public float Friction { get; set; } = 0.5f;
        public bool Killer { get; set; }
        public bool HasFixate { get; set; } = false;
        [ObjProp("Attraction", "Nm²/kg²")]
        public float Attraction { get; set; } = 0;
        public bool AttractionIsLinear = false;
        [ObjProp("Quantité de mouvement", "N⋅s", "N⋅s²", "N")]
        public Vector2f Momentum => Mass * Velocity;

        [ObjProp("Moment d'inertie", "kg⋅m²")]
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

            //Gravity.Value = Simulation.GravityVector * _mass;
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

                    /*AngularAirFriction = (-(Math.Max(diam1, Math.Abs(size.Dot(_velocity))) / 2) * Simulation.AirFrictionMultiplier *
                                          (Simulation.AirFrictionQuadratic * _angVel + Simulation.AirFrictionLinear) *
                                          _angVel);*/
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

            /*if (Fixed)
                _velocity = new Vector2f(0, 0);
            else*/
                ApplyForces(dt);

            if (!Fixed)
            {
                _position += Velocity * dt;
                _angle += AngularVelocity * dt;
                UpdatePosition();
            }

            return;
            PhysicalObject[] objectsArr;

            var objects = Simulation.World.OfType<PhysicalObject>().Where(o => o != this);

            if (Fixed)
                objects = objects.Where(o => !o.Fixed);

            objectsArr = objects.ToArrayLocked(Simulation.World);

            

            foreach (var obj in objectsArr)
            {
                if (OBB.testCollision(Shape, obj.Shape, out var mtv))
                {
                    
                    void ResolveCollision(ref Vector2f v, ref float w, ref Vector2f r, ref float I,
                        ref float cF, ref float cR)
                    {
                        Vector2f u;
                        Vector2f u0;
                        Vector2f k = default;
                        const float tolerance = 0.000001f;

                        //I won't go into the details of how this algorithm works. The details can be found in the document
                        //"A Correction To Brian Mirtich's Thesis" see the top of this file for a link.

                        var v0 = v;
                        var w0 = w;

                        //Step 1. Rotate the space (is not necessary).



                        //Step 2. Initialization.
                        //Calculate the initial separation velocity. 
                        u0.X = v0.X - w0 * r.Y;
                        u0.Y = v0.Y + w0 * r.X;

                        if (u0.Y > 0)
                            return; //The object is not colliding.

                        //Calculate the collision matrix K and its inverse invK.

                        //We are assuming the mass of the object is 1.
                        var K11 = 1 + r.Y * r.Y / I; var K12 = -r.X * r.Y / I;
                        var K21 = -r.X * r.Y / I; var K22 = 1 + r.X * r.X / I;

                        var D = K11 * K22 - K12 * K21; //The determinant of K.

                        var invK11 = K22 / D; var invK12 = -K12 / D;
                        var invK21 = -K21 / D; var invK22 = K11 / D;

                        //Initialize (u.X,u.Y), Wc, and Wd.
                        u = u0;
                        float Wc = 0;
                        float Wd = 0;



                        //Step 3. Determining the type of ray we are on.
                        var bSticking = false;
                        var bConverging = false;
                        var bDiverging = false;

                        if (-tolerance < u.X && u.X < tolerance)
                        {
                            //Step 3a. Sticking has occurred.
                            u.X = 0;
                            bSticking = true;
                        }
                        else
                        if (u.X > 0)
                        {
                            //Step 3b.
                            k.X = -K11 * cF + K12;
                            k.Y = -K21 * cF + K22;

                            if (-tolerance < k.X && k.X < tolerance)
                            {
                                k.X = 0;
                                k.Y = 1 / invK22;
                                bDiverging = true;
                            }
                            else
                            {
                                if (k.X > 0)
                                    bDiverging = true;
                                else
                                    bConverging = true;
                            }
                        }
                        else
                        {
                            //Step 3c.
                            k.X = K11 * cF + K12;
                            k.Y = K21 * cF + K22;

                            if (-tolerance < k.X && k.X < tolerance)
                            {
                                k.X = 0;
                                k.Y = 1 / invK22;
                                bDiverging = true;
                            }
                            else
                            {
                                if (k.X > 0)
                                    bConverging = true;
                                else
                                    bDiverging = true;
                            }
                        }



                        //Step 4. We are on a converging ray.
                        if (bConverging)
                        {
                            //Step 4a.
                            var uOrigin = u.Y - k.Y * u.X / k.X;

                            if (uOrigin <= 0)
                            {
                                //Step 4b.
                                Wc = -u.X * u.Y / k.X + k.Y * u.X * u.X / (2 * k.X * k.X);
                                u.X = 0;
                                u.Y = uOrigin;

                                bSticking = true;
                            }
                            else
                            if (0 < uOrigin && uOrigin < -cR * u.Y)
                            {
                                //Step 4c.
                                Wc = -u.Y * u.Y / (2 * k.Y);
                                Wd = uOrigin * uOrigin / (2 * k.Y);
                                u.X = 0;
                                u.Y = uOrigin;

                                bSticking = true;
                            }
                            else
                            {
                                //Step 4d.
                                u.X = u.X - (1 + cR) * k.X * u.Y / k.Y;
                                u.Y = -cR * u.Y;
                            }
                        }



                        //Step 5. Sticking has occurred.
                        if (bSticking)
                        {
                            u.X = 0; //Just to be safe.

                            if (-K12 <= cF * K11 && K12 <= cF * K11)
                            {
                                //Step 6. Stable sticking has occurred.
                                k.X = 0;
                                k.Y = 1 / invK22;
                                bDiverging = true;
                            }
                            else
                            {
                                //Step 7. Unstable sticking has occurred.
                                k.X = -K11 * cF + K12;
                                k.Y = -K21 * cF + K22;

                                if (k.X <= tolerance)
                                {
                                    k.X = K11 * cF + K12;
                                    k.Y = K21 * cF + K22;

                                    if (k.X >= -tolerance)
                                    {
                                        //Just to be safe.
                                        k.X = 0;
                                        k.Y = 1 / invK22;
                                    }
                                }

                                bDiverging = true;
                            }
                        }



                        //Step 8. We are on a diverging/stationary ray (or stable sticking has occurred).
                        if (bDiverging)
                        {
                            //Step 8a.
                            var uOld = u.Y;

                            if (u.Y < 0)
                            {
                                //Step 8b. We are in a compression phase.
                                Wc += -u.Y * u.Y / (2 * k.Y);
                                u.Y = 0;
                            }

                            //Step 8c. We are in a decompression phase.
                            u.Y = (float)Math.Sqrt(2 * k.Y * (-cR * cR * Wc - Wd) + u.Y * u.Y);
                            u.X = k.X * (u.Y - uOld) / k.Y + u.X;
                        }

                        

                        //Step 9. Calculate the impulse and the new velocities.
                        var duu = u - u0;
                        Vector2f p;
                        p.X = invK11 * (duu.X) + invK12 * (duu.Y);
                        p.Y = invK21 * (duu.X) + invK22 * (duu.Y);

                        v = v0 + p;

                        w = w0 + (r.X * p.Y - r.Y * p.X) / I;



                        //Step 10. Rotate the space (is not necessary).

                        return;
                    }

                    /* var points = 
                         Enumerable.Range(0, (int)Shape.GetPointCount()).Select(i => Shape.Transform.TransformPoint(Shape.GetPoint((uint)i))).ToArray();
                     var minY = points.Min(p => p.Y);
                     var p = points.First(t => t.Y == minY);
                     var I = MomentOfInertia;
                     var cf = (float) Math.Sqrt(Friction * obj.Friction);
                     var cR = (Restitution + obj.Restitution) / 2;
                     ResolveCollision(ref _velocity, ref _angVel, ref p, ref I, ref cf, ref cR);*/
                    //continue;

                    var hm = mtv / 2;
                    /*_position += hm;
                    obj._position -= hm;*/
                    hm /= dt;

                    Forces.Add(new Force("Normale", hm, default, ttl: -1));
                    obj.Forces.Add(new Force("N", -hm, default, ttl: -1));


                    /*(_velocity.X, obj._velocity.X) = (
                        ElasticCollision(Mass, obj.Mass, _velocity.X, obj._velocity.X, Restitution, obj.Restitution),
                        ElasticCollision(obj.Mass, Mass, obj._velocity.X, _velocity.X, obj.Restitution, Restitution));

                    (_velocity.Y, obj._velocity.Y) = (
                        ElasticCollision(Mass, obj.Mass, _velocity.Y, obj._velocity.Y, Restitution, obj.Restitution),
                        ElasticCollision(obj.Mass, Mass, obj._velocity.Y, _velocity.Y, obj.Restitution, Restitution));*/
                    //continue;

                    /*Vector2f waf, wbf;
                    CollisionResponce(
                        Tools.Average(Restitution, obj.Restitution),
                        Mass,
                        obj.Mass,
                        MomentOfInertia,
                        obj.MomentOfInertia,
                        default,
                        default,
                        mtv.Normalize(),
                        _velocity,
                        obj._velocity,
                        Tools.FromPolar(1, _angVel),
                        Tools.FromPolar(1, obj._angVel),
                        out _velocity,
                        out obj._velocity,
                        out waf,
                        out wbf
                    );
                    _angVel = waf.Angle();
                    obj._angVel = wbf.Angle();*/
                }
            }

            /*
            if (!Fixed)
            {
                _position.X += _velocity.X * dt;
                UpdatePosition();
            } 
            foreach (var obj in objectsArr)
            {
                var (coll, crect) = Collision(obj);

                if (coll)
                {
                    obj.ObjectCollided?.Invoke(this);

                    if (!Killer && obj.Killer)
                        Delete();

                    if (!Fixed)
                    {
                        var rect = Shape.GetGlobalBounds();
                        var orect = obj.Shape.GetGlobalBounds();

                        if (crect.Height > crect.Width)
                        {
                            if (rect.Left.IsBetween(orect.Left, orect.Right()))
                            {
                                _position.X = orect.Right();
                            }
                            else if (rect.Right().IsBetween(orect.Left, orect.Right()))
                            {
                                _position.X = orect.Left - rect.Width;
                            }
                        }

                        UpdatePosition();
                    }

                    (_velocity.X, obj._velocity.X) = (
                        ElasticCollision(Mass, obj.Mass, _velocity.X, obj._velocity.X, Restitution, obj.Restitution),
                        ElasticCollision(obj.Mass, Mass, obj._velocity.X, _velocity.X, obj.Restitution, Restitution));
                }

                if (!obj.Fixed && obj._velocity.X != 0 && obj.Shape.GetGlobalBounds().CollidesX(Shape.GetGlobalBounds()))
                    obj.Forces.Add(new Force("Friction X", new Vector2f((float) (Math.Abs(obj.NetForce.Y) * Math.Sqrt(Friction * obj.Friction) * -Math.Sign(obj._velocity.X)), 0), -1));
            }

            if (!Fixed)
            {
                _position.Y += _velocity.Y * dt;
                UpdatePosition();
            }

            foreach (var obj in objectsArr)
            {
                var (coll, crect) = Collision(obj);

                if (coll)
                {
                    obj.ObjectCollided?.Invoke(this);

                    if (!Killer && obj.Killer)
                        Delete();

                    if (!Fixed)
                    {
                        var rect = Shape.GetGlobalBounds();
                        var orect = obj.Shape.GetGlobalBounds();

                        if (crect.Width > crect.Height)
                        {
                            if (rect.Top.IsBetween(orect.Top, orect.Bottom()))
                            {
                                _position.Y = orect.Bottom();
                            }
                            else if (rect.Bottom().IsBetween(orect.Top, orect.Bottom()))
                            {
                                _position.Y = orect.Top - rect.Height;
                            }
                        }

                        UpdatePosition();
                    }

                    (_velocity.Y, obj._velocity.Y) = (
                        ElasticCollision(Mass, obj.Mass, _velocity.Y, obj._velocity.Y, Restitution, obj.Restitution),
                        ElasticCollision(obj.Mass, Mass, obj._velocity.Y, _velocity.Y, obj.Restitution, Restitution));
                }

                if (!obj.Fixed && obj._velocity.Y != 0 && obj.Shape.GetGlobalBounds().CollidesY(Shape.GetGlobalBounds()))
                    obj.Forces.Add(new Force("Friction Y", new Vector2f(0, (float)(Math.Abs(obj.NetForce.X) * Math.Sqrt(Friction * obj.Friction) * -Math.Sign(obj._velocity.Y))), -1));

            }*/

            
        }

        /**
This function calulates the velocities after a 2D collision vaf, vbf, waf and wbf from information about the colliding bodies
@param float e coefficient of restitution which depends on the nature of the two colliding materials
@param float ma total mass of body a
@param float mb total mass of body b
@param float Ia inertia for body a.
@param float Ib inertia for body b.
@param vector ra position of collision point relative to centre of mass of body a in absolute coordinates (if this is
                 known in local body coordinates it must be converted before this is called).
@param vector rb position of collision point relative to centre of mass of body b in absolute coordinates (if this is
                 known in local body coordinates it must be converted before this is called).
@param vector n normal to collision point, the line along which the impulse acts.
@param vector vai initial velocity of centre of mass on object a
@param vector vbi initial velocity of centre of mass on object b
@param vector wai initial angular velocity of object a
@param vector wbi initial angular velocity of object b
@param vector vaf final velocity of centre of mass on object a
@param vector vbf final velocity of centre of mass on object a
@param vector waf final angular velocity of object a
@param vector wbf final angular velocity of object b
*/
        public static void CollisionResponce(float e, float ma, float mb, float Ia, float Ib, Vector2f ra, Vector2f rb, Vector2f n,
            Vector2f Vai, Vector2f Vbi, Vector2f wai, Vector2f wbi, out Vector2f Vaf, out Vector2f Vbf, out Vector2f waf, out Vector2f wbf)
        {
            float k = 1 / (ma * ma) + 2 / (ma * mb) + 1 / (mb * mb) - ra.X * ra.X / (ma * Ia) - rb.X * rb.X / (ma * Ib) - ra.Y * ra.Y / (ma * Ia)
              - ra.Y * ra.Y / (mb * Ia) - ra.X * ra.X / (mb * Ia) - rb.X * rb.X / (mb * Ib) - rb.Y * rb.Y / (ma * Ib)
              - rb.Y * rb.Y / (mb * Ib) + ra.Y * ra.Y * rb.X * rb.X / (Ia * Ib) + ra.X * ra.X * rb.Y * rb.Y / (Ia * Ib) - 2 * ra.X * ra.Y * rb.X * rb.Y / (Ia * Ib);
            float Jx = (e + 1) / k * (Vai.X - Vbi.X)*(1 / ma - ra.X * ra.X / Ia + 1 / mb - rb.X * rb.X / Ib)
               - (e + 1) / k * (Vai.Y - Vbi.Y)*(ra.X * ra.Y / Ia + rb.X * rb.Y / Ib);
            float Jy = -(e + 1) / k * (Vai.X - Vbi.X)*(ra.X * ra.Y / Ia + rb.X * rb.Y / Ib)
               + (e + 1) / k * (Vai.Y - Vbi.Y)*(1 / ma - ra.Y * ra.Y / Ia + 1 / mb - rb.Y * rb.Y / Ib);
            Vaf.X = Vai.X - Jx / ma;
            Vaf.Y = Vai.Y - Jy / ma;
            Vbf.X = Vbi.X + Jx / mb;
            Vbf.Y = Vbi.Y + Jy / mb;
            waf.X = wai.X - (Jx * ra.Y - Jy * ra.X) / Ia;
            waf.Y = wai.Y - (Jx * ra.Y - Jy * ra.X) / Ia;
            wbf.X = wbi.X - (Jx * rb.Y - Jy * rb.X) / Ib;
            wbf.Y = wbi.Y - (Jx * rb.Y - Jy * rb.X) / Ib;
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

        public static float ElasticCollision(float m1, float m2, float v1, float v2, float r1, float r2)
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

            return (rest * m2 * (v2 - v1) + m1 * v1 + m2 * v2) / (m1 + m2);
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

        [ObjProp("Forces", "N")]
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
                               (float) (realPos.X / Shape.GetLocalBounds().Width) *
                               (float) Math.Sin(realAngle) * realPos.Norm()
                               ).Sum();
                }
            }
        }

        [ObjProp("Accélération", "m/s²", "m/s", "m/s³")]
        public Vector2f Acceleration => Fixed ? default : (NetForce / Mass);

        public float AngularAcceleration => Fixed ? default : (NetTorque / MomentOfInertia + AngularAirFriction);

        private float AngularAirFriction = 0;

        [ObjProp("Moment angulaire", "J⋅s")]
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
                    Render.Window.Draw(new[]
                    {
                        new Vertex(origin, f.Color),
                        new Vertex(tip, f.Color),
                        new Vertex(tip, f.Color),
                        new Vertex(tip - Tools.FromPolar(arrow, angle + arrowAng), f.Color),
                        new Vertex(tip, f.Color),
                        new Vertex(tip - Tools.FromPolar(arrow, angle - arrowAng), f.Color),
                    }, PrimitiveType.Lines);

                    Render.Window.SetView(Camera.MainView);
                    forceName.CharacterSize = (uint)(30 * arrow);
                    forceName.FillColor = f.Color;
                    forceName.DisplayedString = $"{f.Value.Norm():F2} N";
                    forceName.Position = tip.ToScreen().F();
                    Render.Window.Draw(forceName);

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
                            a.Forces.Add(new Force("N", fA * w[i1], a.MapInv(colls[i1]), ttl: dt));
                            b.Forces.Add(new Force("N", fB * w[i1], b.MapInv(colls[i1]), ttl: dt));
                        }

                        var friction = (float)Math.Sqrt(a.Friction * b.Friction);
                        var unit = unitMtv.Ortho();

                        if (!a.Fixed)
                        {
                            var vproj = -a.Velocity.Dot(unit);
                            var ff = (vproj * friction) * unit;

                            for (var i1 = 0; i1 < np; i1++)
                            {
                                a.Forces.Add(new Force("T", ff * w[i1], a.MapInv(colls[i1]), ttl: dt));

                            }
                        }

                        if (!b.Fixed)
                        {
                            var vproj = -b.Velocity.Dot(unit);
                            var ff = (vproj * friction) * unit;

                            for (var i1 = 0; i1 < np; i1++)
                            {
                                b.Forces.Add(new Force("T", ff * w[i1], b.MapInv(colls[i1]), ttl: dt));
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
