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

            Add(new TextField<byte>(0, 255, deci: false, bindProp: () => wrapper.R));
            Add(new TextField<byte>(0, 255, deci: false, bindProp: () => wrapper.G));
            Add(new TextField<byte>(0, 255, deci: false, bindProp: () => wrapper.B));

            Add(new TextField<double>(0, 360, unit: "°", bindProp: () => wrapper.H));
            Add(new TextField<double>(0, 1, bindProp: () => wrapper.S));
            Add(new TextField<double>(0, 1, bindProp: () => wrapper.V));

            Show();
        }
    }
}
