﻿using System;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using TGUI;
using static phytestcs.Tools;

namespace phytestcs.Interface.Windows.Properties
{
    public class WndMaterial : WndBase<PhysicalObject>
    {
        public static readonly (string, Action<PhysicalObject>)[] Materials =
        {
            (L["Default"], o =>
            {
                //
            }),
            (L["Glass"], o =>
            {
                o.Friction = 0.2f;
                o.Restitution = 0.5f;
                o.Density = 2.5f;
                o.RefractiveIndex = 1.5f;
                o.Color = new Color(191, 229, 255, 95);
            }),
            (L["Gold"], o =>
            {
                o.Friction = 0.49f;
                o.Restitution = 0.1f;
                o.Density = 19.3f;
                o.Color = new Color(255, 211, 12, 255);
            }),
            (L["Helium"], o =>
            {
                o.Friction = 1.0f;
                o.Restitution = 0.25f;
                o.Density = 0.005f;
                o.Color = new Color(255, 165, 191, 165);
            }),
            (L["Ice"], o =>
            {
                o.Friction = 0.05f;
                o.Restitution = 0.05f;
                o.Density = 0.9f;
                o.RefractiveIndex = 1.31f;
                o.Color = new Color(191, 242, 255, 223);
            }),
            (L["Rubber"], o =>
            {
                o.Friction = 1.5f;
                o.Restitution = 0.85f;
                o.Density = 1.5f;
                o.Color = new Color(82, 82, 82, 255);
            }),
            (L["Steel"], o =>
            {
                o.Friction = 0.74f;
                o.Restitution = 0.75f;
                o.Density = 7.8f;
                o.Color = new Color(178, 191, 204, 255);
            }),
            (L["Stone"], o =>
            {
                o.Friction = 0.9f;
                o.Restitution = 0.2f;
                o.Density = 2.4f;
                o.Color = new Color(229, 204, 204, 255);
            }),
            (L["Wood"], o =>
            {
                o.Friction = 0.4f;
                o.Restitution = 0.4f;
                o.Density = 0.6f;
                o.Color = new Color(255, 165, 63, 255);
            })
        };

        public WndMaterial(PhysicalObject obj, Vector2f pos)
            : base(obj, 250, pos)
        {
            foreach (var (text, action) in Materials)
            {
                var btn = new Button(text);
                btn.Clicked += delegate { action(obj); };
                Add(btn);
            }

            Add(new NumberField<float>(0.001f, 100f, bindProp: () => obj.Density, log: true));
            Add(new NumberField<float>(0.001f, 1000f, bindProp: () => obj.Mass, log: true));
            Add(new NumberField<float>(0, 2, bindProp: () => obj.Friction) { RightValue = float.PositiveInfinity });
            Add(new NumberField<float>(0, 1, bindProp: () => obj.Restitution));
            Add(new NumberField<float>(1, 100, bindProp: () => obj.RefractiveIndex, log: true)
                { RightValue = float.PositiveInfinity });
            Add(new NumberField<float>(0.01f, 100, bindProp: () => obj.Attraction, log: true) { LeftValue = 0 });
            Show();
        }
    }
}