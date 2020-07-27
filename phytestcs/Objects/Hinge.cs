using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public class Hinge : Spring
    {
        private readonly Force _torque1;
        private readonly Force _torque1Sup;
        private readonly Force _torque2;
        private readonly Force _torque2Sup;
        private readonly Force _test;

        public Hinge(float size, PhysicalObject object1, Vector2f object1RelPos,
            PhysicalObject? object2 = null, Vector2f object2RelPos = default, ForceType type = null)
        : base(1e8f, 0, size, object1, object1RelPos, object2, object2RelPos, type ?? ForceType.Hinge)
        {
            type ??= ForceType.Hinge!;
            _torque1 = new Force(type, new Vector2f(0, 0), new Vector2f(1, 0)) { Source = this };
            _torque2 = new Force(type, new Vector2f(0, 0), new Vector2f(-1, 0)) { Source = this };
            object1.Forces.Add(_torque1);
            object1.Forces.Add(_torque2);
            object1.Hinge = this;

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

        public float MotorSpeedReal => Reversed ? -MotorSpeed : MotorSpeed;
        public float MotorSpeedRadians => (float) (-MotorSpeedReal * Math.PI / 30);

        [ObjProp("Motor torque", "Nm")]
        public float MotorTorque { get; set; } = 100;

        private readonly CircleShape _shape = new CircleShape();
        protected override IEnumerable<Shape> Shapes => new[] { _shape };

        public override void Delete(Object source = null)
        {
            End1.Object.Forces.Remove(_torque1);
            End1.Object.Forces.Remove(_torque2);
            End1.Object.Hinge = null;

            base.Delete(source);
        }

        private void UpdateForces(float dt)
        {
            if (_torque1 == null)
                return;
            
            float force;
            var diff = End1.Object.AngularVelocity - MotorSpeedRadians;
            if (Motor && diff != 0 && !float.IsPositiveInfinity(MotorTorque))
            {
                force = Math.Sign(diff) * Math.Min(Math.Abs(diff) * End1.Object.MomentOfInertia / Simulation.ActualDT,
                    (float) (MotorTorque * (1 - 0.9999999999 * Math.Exp(-10000 * Math.Pow(diff, 2)))));
            }
            else
            {
                force = 0;
            }

            var origLocal = End1.Object.MapInv(OriginalPosition);
            _torque1.Position = origLocal + new Vector2f(1, 0);
            _torque2.Position = origLocal - new Vector2f(1, 0);

            _torque1.Value = new Vector2f(0, -force / 2).Rotate(End1.Object.Angle);
            _torque2.Value = new Vector2f(0, force / 2).Rotate(End1.Object.Angle);
            
            _force1.Value = -(End1.Object.NetForce - _force1.Value - _torque1.Value - _torque2.Value);
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
            
            base.UpdatePhysics(dt);
        }
    }
}