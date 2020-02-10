using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public class PhysicalObject : Object
    {
        private float _mass;
        private Vector2f _position;
        private Vector2f _speed;

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

        [ObjProp("Vitesse", "m/s", "m")]
        public Vector2f Speed
        {
            get => _speed;
            set => _speed = value;
        }

        public float SpeedAngle
        {
            get => _speed.Angle();
            set => _speed = Tools.FromPolar(_speed.Norm(), value);
        }

        public float SpeedAngleDeg
        {
            get => SpeedAngle.Degrees();
            set => SpeedAngle = value.Radians();
        }

        public float SpeedNorm
        {
            get => _speed.Norm();
            set => _speed = _speed == default ? new Vector2f(value, 0) : _speed.Normalize() * value;
        }

        public float SpeedX
        {
            get => _speed.X;
            set => _speed.X = value;
        }

        public float SpeedY
        {
            get => _speed.Y;
            set => _speed.Y = value;
        }

        public Shape Shape { get; }

        public SynchronizedCollection<Force> Forces { get; set; }

        public float Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                //Gravity.Value = new Vector2f(0, -Weight);
            }
        }

        public float Density
        {
            get => _mass / Shape.Area();
            set => _mass = Shape.Area() * value;
        }

        [ObjProp("Énergie cinétique", "J", unitDeriv:"W")]
        public float KineticEnergy => Fixed ? 0 : Mass * (float) Math.Pow(Speed.Norm(), 2) / 2;
        [ObjProp("Énergie de pesanteur", "J", unitDeriv: "W")]
        public float GravityEnergy => Fixed ? 0 : Weight * CenterOfMass.Y;
        [ObjProp("Énergie d'attraction", "J", unitDeriv: "W")]
        public float AttractionEnergy => Fixed ? 0 : Simulation.AttractionEnergy(CenterOfMass, _mass, this);

        [ObjProp("Énergie totale", "J", unitDeriv: "W")]
        public float TotalEnergy => KineticEnergy + GravityEnergy + AttractionEnergy;
        public bool Fixed => Wall || IsMoving || HasFixate;
        public bool Wall { get; set; }
        public bool IsMoving { get; set; }

        private Force Gravity { get; } = new Force("Gravité", new Vector2f(0, 0));
        private Force AirFriction { get; } = new Force("Frottements de l'air", new Vector2f(0, 0));
        private Force Buoyance { get; } = new Force("Poussée d'Archimède", new Vector2f(0, 0));
        public float Weight => _mass * Simulation.ActualGravity;
        public float Restitution { get; set; } = 0.5f;
        public float Friction { get; set; } = 0.5f;
        public bool Killer { get; set; }
        public bool HasFixate { get; set; } = false;
        public Vector2f CenterOfMass => Position + Shape.GetGlobalBounds().Size() / 2;
        public float Attraction { get; set; } = 0;
        public bool AttractionIsLinear = false;
        [ObjProp("Quantité de mouvement", "N⋅s", "N⋅s²", "N")]
        public Vector2f Momentum => _mass * _speed;

        public float MomentOfInertia
        {
            get
            {
                switch (Shape)
                {
                    case RectangleShape r:
                        return _mass * (r.Size.NormSquared()) / 12;
                    default:
                        return 0; // todo
                }
            }
        }

        public float NormeChampGenerale(float masse = 1f)
        {
            return Attraction * _mass * masse;
        }

        public Vector2f AttractionField(Vector2f pos, float masse = 1f)
        {
            if (Attraction == 0.0f)
                return default;

            var delta = CenterOfMass - pos;
            var dist = delta.Norm();

            if (!AttractionIsLinear)
                dist *= dist;

            return delta.Normalize() * NormeChampGenerale(masse) / dist;
        }

        public float AttractionEnergyCaused(Vector2f pos, float masse = 1f)
        {
            if (Attraction == 0.0f)
                return 0;

            var dist = (CenterOfMass - pos).Norm();

            return NormeChampGenerale(masse) *
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
            Gravity.Value = Simulation.GravityField(CenterOfMass, _mass, this);

            if (!Fixed)
            {
                if (Simulation.AirFriction)
                {
                    var vn = SpeedNorm;
                    if (vn == 0)
                    {
                        AirFriction.Value = default;
                    }
                    else
                    {
                        var vu = _speed / vn;
                        var diam = Math.Abs(Shape.GetGlobalBounds().Size().Dot(vu.Ortho()));
                        AirFriction.Value = (-diam * Simulation.AirFrictionMultiplier *
                                                 (Simulation.AirFrictionQuadratic * vn + Simulation.AirFrictionLinear) *
                                                 vn) * vu;
                    }
                }
                else
                {
                    AirFriction.Value = default;
                }

                Buoyance.Value = -Simulation.GravityVector * Shape.Area() * Simulation.AirDensity;
            }

            if (Fixed)
                _speed = new Vector2f(0, 0);
            else
                ApplyForces(dt);

            if (!Fixed)
            {
                _position.X += _speed.X * dt;
                UpdatePosition();
            }

            PhysicalObject[] objectsArr;

            lock (Simulation.World.SyncRoot)
            {
                var objects = Simulation.World.OfType<PhysicalObject>().Where(o => o != this);

                if (Fixed)
                    objects = objects.Where(o => !o.Fixed);

                objectsArr = objects.ToArray();
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

                    (_speed.X, obj._speed.X) = (
                        ElasticCollision(Mass, obj.Mass, _speed.X, obj._speed.X, Restitution, obj.Restitution),
                        ElasticCollision(obj.Mass, Mass, obj._speed.X, _speed.X, obj.Restitution, Restitution));
                }

                if (!obj.Fixed && obj._speed.X != 0 && obj.Shape.GetGlobalBounds().CollidesX(Shape.GetGlobalBounds()))
                    obj.Forces.Add(new Force("Friction X", new Vector2f((float) (Math.Abs(obj.NetForce.Y) * Math.Sqrt(Friction * obj.Friction) * -Math.Sign(obj._speed.X)), 0), -1));
            }

            if (!Fixed)
            {
                _position.Y += _speed.Y * dt;
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

                    (_speed.Y, obj._speed.Y) = (
                        ElasticCollision(Mass, obj.Mass, _speed.Y, obj._speed.Y, Restitution, obj.Restitution),
                        ElasticCollision(obj.Mass, Mass, obj._speed.Y, _speed.Y, obj.Restitution, Restitution));
                }

                if (!obj.Fixed && obj._speed.Y != 0 && obj.Shape.GetGlobalBounds().CollidesY(Shape.GetGlobalBounds()))
                    obj.Forces.Add(new Force("Friction Y", new Vector2f(0, (float)(Math.Abs(obj.NetForce.X) * Math.Sqrt(Friction * obj.Friction) * -Math.Sign(obj._speed.Y))), -1));

            }
        }

        public override bool Contains(Vector2f point)
        {
            return Shape.GetGlobalBounds().Contains(point.X, point.Y);
        }

        private void UpdatePosition()
        {
            Shape.Position = _position;
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

        [ObjProp("Accélération", "m/s²", "m/s", "m/s³")]
        public Vector2f Acceleration => NetForce / Mass;

        private void ApplyForces(float dt)
        {
            _speed += Acceleration * dt;

            lock (Forces.SyncRoot)
            {
                for (var i = Forces.Count - 1; i >= 0; i--)
                {
                    if ((Forces[i].TimeToLive -= dt) < 0)
                        Forces.RemoveAt(i);
                }
            }
        }

        public PhysicalObject(Vector2f pos, Shape shape, bool wall=false, string name="")
        {
            Name = name;

            Shape = shape;
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
    }
}
