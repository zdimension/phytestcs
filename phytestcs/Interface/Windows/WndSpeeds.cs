using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows
{
    public class WndSpeeds : WndBase
    {
        public WndSpeeds(PhysicalObject obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            AddEx(new TextField<Vector2f>(0, 25, bindProp: () => obj.Velocity, conv: PropConverter<Vector2f, float>.VectorNorm));
            AddEx(new TextField<Vector2f>(-25, 25, bindProp: () => obj.Velocity, conv: PropConverter<Vector2f, float>.VectorX));
            AddEx(new TextField<Vector2f>(-25, 25, bindProp: () => obj.Velocity, conv: PropConverter<Vector2f, float>.VectorY));
            AddEx(new TextField<float>(0, 25, bindProp: () => obj.AngularVelocity));
            AddEx(new TextField<float>(-180, 180, unit: "°", bindProp: () => obj.Angle, conv: PropConverter<float, float>.AngleDegrees));
            Show();
        }
    }
}
