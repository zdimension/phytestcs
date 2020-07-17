using SFML.System;

namespace phytestcs.Objects
{
    public class Hinge : Spring
    {
        private readonly Force _torque1;
        private readonly Force _torque1sup;
        private readonly Force _torque2;
        private readonly Force _torque2sup;

        public Hinge(PhysicalObject object1, Vector2f object1RelPos, float size,
            PhysicalObject object2 = null, Vector2f object2RelPos = default, ForceType type = null)
            : base(1e4f, 0, size, object1, object1RelPos, object2, object2RelPos, type: type ?? ForceType.Hinge)
        {
            // Algodoo: 1e8 Nm, 0.5
            Damping = 1f;
            type ??= ForceType.Hinge;
            _torque1 = new Force(type, new Vector2f(0, 0), new Vector2f(1, 0)) { Source = this };
            _torque2 = new Force(type, new Vector2f(0, 0), new Vector2f(-1, 0)) { Source = this };
            object1.Forces.Add(_torque1);
            object1.Forces.Add(_torque2);

            if (object2 != null)
            {
                _torque1sup = new Force(type, new Vector2f(0, 0), new Vector2f(1, 0)) { Source = this };
                _torque2sup = new Force(type, new Vector2f(0, 0), new Vector2f(-1, 0)) { Source = this };
                object2.Forces.Add(_torque1sup);
                object2.Forces.Add(_torque2sup);
            }
        }

        public float Size { get; }

        [ObjProp("Motor")] public bool Motor { get; set; }

        [ObjProp("Brake")] public bool AutoBrake { get; set; }

        [ObjProp("Reversed")] public bool Reversed { get; set; }

        [ObjProp("Motor speed", "rpm")] public float MotorSpeed { get; set; } = 15;

        [ObjProp("Motor torque", "Nm")] public float MotorTorque { get; set; } = 100;

        public override void Delete(Object source = null)
        {
            End1.Object.Forces.Remove(_torque1);
            End1.Object.Forces.Remove(_torque2);

            End2.Object?.Forces.Remove(_torque1sup);
            End2.Object?.Forces.Remove(_torque2sup);

            base.Delete(source);
        }

        private void UpdateForces()
        {
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

        /*public override void UpdatePhysics(float dt)
        {
            var force = OppForce;
            force += force.Normalize() * Force;

            if (Object2 != null)
            {
                if (!Object1.Fixed && !Object2.Fixed)
                    force /= 2;
                if (!Object1.Fixed)
                    _force1.Value = force;
                if (!Object2.Fixed)
                    _force2.Value = -force;
            }
            else
            {
                _force1.Value = force;
            }
        }*/
    }
}