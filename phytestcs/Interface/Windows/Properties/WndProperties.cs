using System;
using System.Linq;
using SFML.Graphics;
using SFML.System;
using TGUI;
using static phytestcs.Tools;
using Object = phytestcs.Objects.Object;

namespace phytestcs.Interface.Windows.Properties
{
    public class WndProperties : WndBase<Object>
    {
        public WndProperties(Object obj, Vector2f pos)
            : base(obj, 150, pos)
        {
            IsMain = true;

            Vector2f PosEnfant()
            {
                return Position + new Vector2f(Size.X, 0);
            }

            var btnEff = new BitmapButton { Text = L["Clear"], Image = new Texture("icons/small/delete.png") };
            btnEff.Clicked += delegate { obj.Delete(); };
            Add(btnEff);

            // liquify
            // spongify
            // clone
            // mirror

            var windows = new[]
            {
                (typeof(WndPlot), L["Plot"], "icons/small/plot.png"),
                (typeof(WndSelection), L["Selection"], "icons/small/settings.png"),
                (typeof(WndAppearance), L["Appearance"], "icons/small/appearance.png"),
                // text
                (typeof(WndMaterial), L["Material"], "icons/small/settings.png"),
                (typeof(WndSpeeds), L["Velocities"], "icons/small/speed.png"),
                (typeof(WndSpring), L["Spring"], "icons/small/spring.png"),
                (typeof(WndHinge), L["Hinge"], "icons/small/spring.png"),
                (typeof(WndTracer), L["Tracer"], "icons/small/tracer.png"),
                (typeof(WndLaser), L["Laser"], "icons/small/laser.png"),
                (typeof(WndThruster), L["Thruster"], "icons/small/thruster.png"),
                (typeof(WndInfos), L["Informations"], "icons/small/info.png"),
                (typeof(WndCollision), L["Collision layers"], "icons/small/layers.png"),
                (typeof(WndActions), L["Geometry actions"], "icons/small/settings.png"),
                // csg
                // controller
                (typeof(WndSpecial), L["Special"], "icons/small/settings.png"),
                (typeof(WndScript), L["Script"], "icons/small/script.png")
            };

            foreach (var (type, name, icon) in windows)
            {
                if (!type.BaseType!.GenericTypeArguments[0].IsInstanceOfType(obj))
                    continue;

                var btn = new BitmapButton { Text = name, Image = new Texture(icon) };
                btn.Clicked += delegate { Activator.CreateInstance(type, obj, PosEnfant()); };
                Add(btn);
            }

            obj.Deleted += () => { CloseAll(); };

            Ui.BackPanel.MousePressed += ClickClose;
            Ui.BackPanel.RightMouseReleased += RightClickClose;

            PositionChanged += (sender, f) =>
            {
                foreach (var w in Ui.PropertyWindows[obj].Where(w => w != this && !w.WasMoved))
                    w.StartPosition = w.Position = f.Value + new Vector2f(Size.X, 0);

                StartPosition = Position;
            };

            Closed += delegate
            {
                Ui.BackPanel.MousePressed -= ClickClose;
                Ui.BackPanel.RightMouseReleased -= RightClickClose;
                CloseAll(true);
            };
        }

        private void ClickClose(object sender, SignalArgsVector2f signalArgsVector2F)
        {
            CloseAll(true);
        }

        private void RightClickClose(object? sender, SignalArgsVector2f e)
        {
            if (Drawing.SelectedObject == null) CloseAll(true);
        }

        private void CloseAll(bool exceptMoved = false)
        {
            if (!Ui.PropertyWindows.ContainsKey(Object)) return;

            foreach (var w in Ui.PropertyWindows[Object].ToList())
            {
                if (!Ui.PropertyWindows.ContainsKey(Object))
                    return;

                if (w.CPointer == IntPtr.Zero)
                {
                    Ui.PropertyWindows[Object].Remove(w);
                    continue;
                }

                if (exceptMoved && w.WasMoved)
                    continue;

                w.Close();
            }

            if (Ui.PropertyWindows.ContainsKey(Object) && !Ui.PropertyWindows[Object].Any())
                Ui.PropertyWindows.Remove(Object);
        }
    }
}