using System;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using TGUI;
using static phytestcs.Global;
using static phytestcs.Tools;

namespace phytestcs.Interface.Windows
{
    public class WndActions : WndBase<PhysicalObject>
    {
        private static readonly (string, string, Action<PhysicalObject>)[] Actions =
        {
            (L["Add center hinge"], "icons/small/spring.png", obj =>
            {
                var obj2 = PhysObjectAtPosition(obj.Position.ToScreen(), obj);
                var obj2pos = obj.Position;
                if (obj2 != null)
                    obj2pos = obj2.MapInv(obj2pos);
                Simulation.Add(new Hinge(obj, default, DefaultSpringSize, obj2, obj2pos));
            }),
            (L["Add center thruster"], "icons/small/thruster.png",
                obj => { Simulation.Add(new Thruster(obj, default, DefaultObjectSize)); }),
            (L["Attach tracer"], "icons/small/tracer.png",
                obj => { Simulation.Add(new Tracer(obj, default, DefaultObjectSize, obj.Color)); })
        };

        public WndActions(PhysicalObject obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
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