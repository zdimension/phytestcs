﻿using System;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using TGUI;
using static phytestcs.Tools;

namespace phytestcs.Interface.Windows.Properties
{
    public sealed  class WndActions : WndBase<PhysicalObject>
    {
        private static readonly (string, string, Action<PhysicalObject>)[] Actions =
        {
            (L["Add center hinge"], "icons/small/spring.png", obj =>
            {
                var obj2 = PhysObjectAtPosition(obj.Position.ToScreen(), obj);
                var obj2Pos = obj.Position;
                if (obj2 != null)
                    obj2Pos = obj2.MapInv(obj2Pos);
                Simulation.Add(new Hinge(DefaultSpringSize, obj, default, obj2, obj2Pos));
            }),
            (L["Add center thruster"], "icons/small/thruster.png",
                obj => { Simulation.Add(new Thruster(obj, default, DefaultObjectSize)); }),
            (L["Attach tracer"], "icons/small/tracer.png",
                obj => { Simulation.Add(new Tracer(obj, default, DefaultObjectSize, obj.Color)); })
        };

        public WndActions(PhysicalObject obj, Vector2f pos)
            : base(obj, 250, pos)
        {
            foreach (var (name, icon, action) in Actions)
            {
                var btn = new BitmapButton { Text = name, Image = new Texture(icon), SizeLayout = new Layout2d("100%", "20")};
                btn.Clicked += delegate { action(obj); };
                Add(btn);
            }

            Show();
        }
    }
}