using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows
{
    public class WndMaterial : WndBase
    {
        public WndMaterial(PhysicalObject obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            AddEx(new TextField("Densité", 0.001f, 100f, unit: "kg/m²", bindObj: obj, bindProp: nameof(PhysicalObject.Density), log: true));
            AddEx(new TextField("Masse", 0.001f, 1000f, unit: "kg", bindObj: obj, bindProp: nameof(PhysicalObject.Mass), log: true));
            AddEx(new TextField("Frottement", 0, 2, unit: "", bindObj: obj, bindProp: nameof(PhysicalObject.Friction)) { RightValue = float.PositiveInfinity });
            AddEx(new TextField("Restitution", 0, 1, unit: "", bindObj: obj, bindProp: nameof(PhysicalObject.Restitution)));
            AddEx(new TextField("Attraction", 0.01f, 100, unit: "Nm²/kg²", bindObj: obj, bindProp: nameof(PhysicalObject.Attraction), log: true) { LeftValue = 0 });
            Show();
        }
    }
}
