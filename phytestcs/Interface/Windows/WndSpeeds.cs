using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows
{
    public class WndSpeeds : WndBase
    {
        public WndSpeeds(PhysicalObject obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            AddEx(new TextField("Vitesse", 0, 25, unit: "m/s", bindObj: obj, bindProp: nameof(PhysicalObject.SpeedNorm)));
            AddEx(new TextField("Angle", -180, 180, unit: "°", bindObj: obj, bindProp: nameof(PhysicalObject.SpeedAngleDeg)));
            AddEx(new TextField("Vitesse X", -25, 25, unit: "m/s", bindObj: obj, bindProp: nameof(PhysicalObject.SpeedX)));
            AddEx(new TextField("Vitesse Y", -25, 25, unit: "m/s", bindObj: obj, bindProp: nameof(PhysicalObject.SpeedY)));
            Show();
        }
    }
}
