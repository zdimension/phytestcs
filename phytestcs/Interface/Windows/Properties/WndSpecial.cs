﻿using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows.Properties
{
    public sealed class WndSpecial : WndBase<PhysicalObject>
    {
        public WndSpecial(PhysicalObject obj, Vector2f pos)
            : base(obj, 250, pos)
        {
            Add(new NumberField<float>(0.01f, 100, () => obj.AirFrictionMultiplier, log: true)
                { LeftValue = 0 });
            Add(new NumberField<float>(0.01f, 100, () => obj.InertiaMultiplier, log: true) { LeftValue = 0 });

            Show();
        }
    }
}