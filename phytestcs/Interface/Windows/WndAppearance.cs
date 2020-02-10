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

            AddEx(new TextField("R", 0, 255, deci: false, bindObj: wrapper,
                bindProp: nameof(wrapper.R)));
            AddEx(new TextField("G", 0, 255, deci: false, bindObj: wrapper,
                bindProp: nameof(wrapper.G)));
            AddEx(new TextField("B", 0, 255, deci: false, bindObj: wrapper,
                bindProp: nameof(wrapper.B)));

            AddEx(new TextField("H", 0, 360, unit: "°", bindObj: wrapper, bindProp: nameof(wrapper.H)));

            AddEx(new TextField("S", 0, 1, unit: "", bindObj: wrapper,
                bindProp: nameof(wrapper.S)));
            AddEx(new TextField("V", 0, 1, unit: "", bindObj: wrapper,
                bindProp: nameof(wrapper.V)));

            Show();
        }
    }
}
