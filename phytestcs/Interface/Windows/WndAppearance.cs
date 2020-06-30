using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows
{
    public class WndAppearance : WndBase
    {
        public WndAppearance(PhysicalObject obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            var wrapper = new ColorWrapper(obj.Shape, nameof(PhysicalObject.Shape.FillColor));

            AddEx(new TextField<byte>(0, 255, deci: false, bindProp: () => wrapper.R));
            AddEx(new TextField<byte>(0, 255, deci: false, bindProp: () => wrapper.G));
            AddEx(new TextField<byte>(0, 255, deci: false, bindProp: () => wrapper.B));

            AddEx(new TextField<double>(0, 360, unit: "°", bindProp: () => wrapper.H));
            AddEx(new TextField<double>(0, 1, bindProp: () => wrapper.S));
            AddEx(new TextField<double>(0, 1, bindProp: () => wrapper.V));

            Show();
        }
    }
}
