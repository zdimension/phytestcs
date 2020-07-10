using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows
{
    public class WndLaser : WndBase<Laser>
    {
        public WndLaser(Laser obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            Add(new NumberField<float>(1, 1000, bindProp: () => obj.FadeDistance, log: true));
            Add(new NumberField<float>(0.01f, 5, bindProp: () => obj.Size, log: true));
            Show();
        }
    }
}