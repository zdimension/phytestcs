using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows
{
    public class WndTracer : WndBase<Tracer>
    {
        public WndTracer(Tracer obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            Add(new NumberField<float>(0.1f, 600, bindProp: () => obj.FadeTime, log: true) { LeftValue = 0 });
            Add(new NumberField<float>(0.01f, 5, bindProp: () => obj.Size, log: true));
            Show();
        }
    }
}