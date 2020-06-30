using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.System;

namespace phytestcs.Objects
{
    public class Hinge : Spring
    {
        public Hinge(PhysicalObject object1, Vector2f object1RelPos,
            PhysicalObject object2 = null, Vector2f object2RelPos = default, string name = "Pivot")
            : base(1e8f, 0, object1, object1RelPos, object2, object2RelPos, name: name)
        {
            Damping = 0.5f;
        }

        private Vector2f OppForce => -(Object1.NetForce - _force1.Value);

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
