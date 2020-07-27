using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public class Hinge : PinnedShapedVirtualObject
    {
        private readonly Force _torque1;
        private readonly Force _torque1Sup;
        private readonly Force _torque2;
        private readonly Force _torque2Sup;
        private readonly Force _test;

        public Hinge(PhysicalObject @object, Vector2f relPos, float size, ForceType type = null)
            : base(@object, relPos)
        {
            if (@object == null) throw new ArgumentNullException(nameof(@object));

            // Algodoo equivalent spring: 1e8 Nm, 0.5

            type ??= ForceType.Hinge!;
            _torque1 = new Force(type, new Vector2f(0, 0), new Vector2f(1, 0)) { Source = this };
            _torque2 = new Force(type, new Vector2f(0, 0), new Vector2f(-1, 0)) { Source = this };
            _test = new Force(type, new Vector2f(0, 0), relPos) { Source = this };
            @object.Forces.Add(_torque1);
            @object.Forces.Add(_torque2);
            @object.Forces.Add(_test);
            @object.Hinge = this;

            Size = size;

            OriginalPosition = Position;
        }
        
        public Vector2f OriginalPosition { get; set; }

        public float Size
        {
            get => _shape.Radius * 2;
            set
            {
                _shape.Radius = value / 2;
                _shape.CenterOrigin();
            }
        }


        [ObjProp("Motor")]
        public bool Motor { get; set; }

        [ObjProp("Brake")]
        public bool AutoBrake { get; set; }

        [ObjProp("Reversed")]
        public bool Reversed { get; set; }

        [ObjProp("Motor speed", "rpm")]
        public float MotorSpeed { get; set; } = 15;

        public float MotorSpeedRadians => (float) (-MotorSpeed * Math.PI / 30);

        [ObjProp("Motor torque", "Nm")]
        public float MotorTorque { get; set; } = 100;

        private readonly CircleShape _shape = new CircleShape();
        public override Shape Shape => _shape;
        protected override IEnumerable<Shape> Shapes => new[] { _shape };

        public override void Delete(Object source = null)
        {
            Object.Forces.Remove(_torque1);
            Object.Forces.Remove(_torque2);
            Object.Forces.Remove(_test);
            Object.Hinge = null;

            base.Delete(source);
        }

        private void UpdateForces(float dt)
        {
            float force;
            var diff = Object.AngularVelocity - MotorSpeedRadians;
            if (Motor && diff != 0 && !float.IsPositiveInfinity(MotorTorque))
            {
                var minDiscrete = 0f;
                force = Math.Min(Math.Abs(diff) * Object.MomentOfInertia / Simulation.ActualDT, (float)(MotorTorque * (1 - 0.9999999999 * Math.Exp(-10000 * Math.Pow(diff, 2)))));
                force *= Math.Sign(diff);
            }
            else
            {
                force = 0;
            }

            var origLocal = Object.MapInv(OriginalPosition);
            _torque1.Position = origLocal + new Vector2f(1, 0);
            _torque2.Position = origLocal - new Vector2f(1, 0);

            _torque1.Value = new Vector2f(0, -force / 2).Rotate(Object.Angle);
            _torque2.Value = new Vector2f(0, force / 2).Rotate(Object.Angle);
            _test.Value = -(Object.NetForce - _test.Value - _torque1.Value - _torque2.Value);
        }

        //private Vector2f OppForce => -(Object1.NetForce - _force1.Value);

        //public override float Force => OppForce.Norm();

        /*public override float Force
        {
            get
            {
                var force = (float)Math.Pow(DeltaLength, 4);

                if (Damping != 0)
                {
                    force += -Speed * Damping * 20;
                }

                return force;
            }
        }*/

        public override void UpdatePhysics(float dt)
        {
            UpdateForces(dt);
        }
    }
}