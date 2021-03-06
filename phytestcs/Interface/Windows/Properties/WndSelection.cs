﻿using System.Linq;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using TGUI;
using static phytestcs.Tools;

namespace phytestcs.Interface.Windows.Properties
{
    public sealed class WndSelection : WndBase<BaseObject>
    {
        private static readonly (string, string, System.Action<BaseObject>)[] Actions =
        {
            (L["Move to back"], "icons/small/move_back.png",
                obj => { obj.ZDepth = Simulation.WorldCacheNonLaser.Min(o => o.ZDepth) - 1f; }),
            (L["Move to front"], "icons/small/move_front.png",
                obj => { obj.ZDepth = Simulation.WorldCacheNonLaser.Max(o => o.ZDepth) + 1f; })
        };

        public WndSelection(BaseObject obj, Vector2f pos)
            : base(obj, 250, pos)
        {
            foreach (var (name, icon, action) in Actions)
            {
                var btn = new BitmapButton { Text = name, Image = new Texture(icon) };
                btn.Clicked += delegate { action(obj); };
                Add(btn);
            }

            Show();
        }
    }
}