﻿using phytestcs.Objects;
using SFML.System;

namespace phytestcs.Interface.Windows.Properties
{
    public sealed class WndLaser : WndBase<Laser>
    {
        public WndLaser(Laser obj, Vector2f pos)
            : base(obj, 250, pos)
        {
            Add(new NumberField<float>(316, 650, () => obj.Wavelength));
            Add(new NumberField<float>(1, 1000, () => obj.FadeDistance, log: true));
            Add(new NumberField<float>(0.01f, 5, () => obj.Size, log: true));
            Show();
        }
    }
}