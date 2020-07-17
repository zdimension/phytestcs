using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows
{
    public class WndSpeeds : WndBase<PhysicalObject>
    {
        public WndSpeeds(PhysicalObject obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            Add(new NumberField<Vector2f>(0, 25, bindProp: () => obj.Velocity, conv: PropConverter.VectorNorm));
            Add(new NumberField<Vector2f>(-180, 180, unit: "°", bindProp: () => obj.Velocity, conv: PropConverter.VectorAngleDeg));
            Add(new NumberField<Vector2f>(-25, 25, bindProp: () => obj.Velocity, conv: PropConverter.VectorX));
            Add(new NumberField<Vector2f>(-25, 25, bindProp: () => obj.Velocity, conv: PropConverter.VectorY));
            Add(new NumberField<float>(0, 25, unit: "°", bindProp: () => obj.AngularVelocity, conv: PropConverter.AngleDegrees));

            Add(new CheckField(bindProp: () => obj.Appearance.ShowForces));
            Add(new CheckField(bindProp: () => obj.Appearance.ShowMomentums));
            Add(new CheckField(bindProp: () => obj.Appearance.ShowVelocities));
            
            Show();
        }
    }
}
