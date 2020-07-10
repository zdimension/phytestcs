using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows
{
    public class WndMaterial : WndBase<PhysicalObject>
    {
        public WndMaterial(PhysicalObject obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            Add(new NumberField<float>(0.001f, 100f, bindProp: () => obj.Density, log: true));
            Add(new NumberField<float>(0.001f, 1000f, bindProp: () => obj.Mass, log: true));
            Add(new NumberField<float>(0, 2, bindProp: () => obj.Friction) { RightValue = float.PositiveInfinity });
            Add(new NumberField<float>(0, 1, bindProp: () => obj.Restitution));
            Add(new NumberField<float>(1, 100, bindProp: () => obj.RefractiveIndex, log: true){RightValue = float.PositiveInfinity});
            Add(new NumberField<float>(0.01f, 100, bindProp: () => obj.Attraction, log: true) { LeftValue = 0 });
            Show();
        }
    }
}
