using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows
{
    public class WndMaterial : WndBase
    {
        public WndMaterial(PhysicalObject obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            AddEx(new TextField<float>(0.001f, 100f, bindProp: () => obj.Density, log: true));
            AddEx(new TextField<float>(0.001f, 1000f, bindProp: () => obj.Mass, log: true));
            AddEx(new TextField<float>(0, 2, bindProp: () => obj.Friction) { RightValue = float.PositiveInfinity });
            AddEx(new TextField<float>(0, 1, bindProp: () => obj.Restitution));
            AddEx(new TextField<float>(0.01f, 100, bindProp: () => obj.Attraction, log: true) { LeftValue = 0 });
            Show();
        }
    }
}
