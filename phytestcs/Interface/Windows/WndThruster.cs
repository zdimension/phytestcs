using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows
{
    public class WndThruster : WndBase<Thruster>
    {
        public WndThruster(Thruster obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            Add(new NumberField<float>(0.1f, 1e5f, bindProp: () => obj.Force, log: true) { LeftValue = 0 });
            Show();
        }
    }
}