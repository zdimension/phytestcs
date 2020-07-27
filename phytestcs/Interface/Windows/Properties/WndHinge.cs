using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows.Properties
{
    public class WndHinge : WndBase<Hinge>
    {
        public WndHinge(Hinge obj, Vector2f pos)
            : base(obj, 250, pos)
        {
            Add(new CheckField(() => obj.Motor));
            Add(new CheckField(() => obj.AutoBrake));
            Add(new CheckField(() => obj.Reversed));
            Add(new NumberField<float>(0.1f, 450, bindProp: () => obj.MotorSpeed, log: true) { LeftValue = 0 });
            Add(new NumberField<float>(0.1f, 50000, bindProp: () => obj.MotorTorque, log: true));
            Show();
        }
    }
}