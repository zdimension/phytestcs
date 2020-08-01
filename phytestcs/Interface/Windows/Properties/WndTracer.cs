using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows.Properties
{
    public sealed class WndTracer : WndBase<Tracer>
    {
        public WndTracer(Tracer obj, Vector2f pos)
            : base(obj, 250, pos)
        {
            Add(new NumberField<float>(0.1f, 600, () => obj.FadeTime, log: true) { LeftValue = 0 });
            Add(new NumberField<float>(0.01f, 5, () => obj.Size, log: true));
            Show();
        }
    }
}