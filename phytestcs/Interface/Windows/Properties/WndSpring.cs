using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows.Properties
{
    public sealed class WndSpring : WndBase<Spring>
    {
        public WndSpring(Spring obj, Vector2f pos)
            : base(obj, 250, pos)
        {
            Add(new NumberField<float>(0.5f, 25, () => obj.Constant, log: true) { LeftValue = 0 });
            Add(new NumberField<float>(0, 2, () => obj.Damping));
            var actual = obj.TargetLength;
            if (actual == 0)
                actual = 1;
            Add(new NumberField<float>(actual / 10f, actual * 10f, () => obj.TargetLength, log: true));
            Show();
        }
    }
}