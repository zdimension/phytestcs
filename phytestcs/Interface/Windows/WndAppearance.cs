using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows
{
    public class WndAppearance : WndBase<PhysicalObject>
    {
        public WndAppearance(PhysicalObject obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            var wrapper = new ColorWrapper(() => obj.Color);

            Add(new NumberField<byte>(0, 255, deci: false, bindProp: () => wrapper.A));
            Add(new NumberField<byte>(0, 255, deci: false, bindProp: () => wrapper.R));
            Add(new NumberField<byte>(0, 255, deci: false, bindProp: () => wrapper.G));
            Add(new NumberField<byte>(0, 255, deci: false, bindProp: () => wrapper.B));

            Add(new NumberField<double>(0, 360, unit: "°", bindProp: () => wrapper.H));
            Add(new NumberField<double>(0, 1, bindProp: () => wrapper.S));
            Add(new NumberField<double>(0, 1, bindProp: () => wrapper.V));

            Show();
        }
    }
}