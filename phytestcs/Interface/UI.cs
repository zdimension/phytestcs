using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using phytestcs.Interface.Windows;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using TGUI;
using Object = phytestcs.Objects.Object;
using Panel = TGUI.Panel;
using static phytestcs.Global;

namespace phytestcs.Interface
{
    public static class UI
    {
        public static BitmapButton btnPlay;
        public static readonly Texture imgPlay = new Texture("icons/big/play.png");
        public static readonly Texture imgPause = new Texture("icons/big/pause.png");
        public static readonly Font Font = new Font(@"C:\Windows\Fonts\consola.ttf");
        public static Gui GUI;

        public static List<(DrawingType, string, Ref<BitmapButton>, Ref<Texture>)> actions = new List<(DrawingType, string, Ref<BitmapButton>, Ref<Texture>)>
        {
            (DrawingType.Off, "drag", new Ref<BitmapButton>(), new Ref<Texture>()),
            (DrawingType.Move, "move",  new Ref<BitmapButton>(), new Ref<Texture>()),
            (DrawingType.Rectangle, "rectangle", new Ref<BitmapButton>(), new Ref<Texture>()),
            (DrawingType.Circle, "circle", new Ref<BitmapButton>(), new Ref<Texture>()),
            (DrawingType.Spring, "coil", new Ref<BitmapButton>(), new Ref<Texture>()),
            (DrawingType.Fixate, "fix", new Ref<BitmapButton>(), new Ref<Texture>()),
            (DrawingType.Hinge, "hinge", new Ref<BitmapButton>(), new Ref<Texture>()),
            (DrawingType.Tracer, "tracer", new Ref<BitmapButton>(), new Ref<Texture>()),
            (DrawingType.Thruster, "thruster", new Ref<BitmapButton>(), new Ref<Texture>()),
            (DrawingType.Laser, "laser", new Ref<BitmapButton>(), new Ref<Texture>()),
        };
        
        public static void SetDrawMode(DrawingType mode)
        {
            Drawing.DrawMode = mode;
            foreach (var (dess, _, bref, text) in actions)
            {
                bref.Value.SetRenderer(dess == mode ? brToggle : brDef);
                if (dess == mode)
                    Render.DrawSprite.Texture = text.Value;
            }
        }

        private static readonly RendererData brDef = Tools.GenerateButtonColor(new Color(210, 210, 210));
        private static readonly RendererData brToggle = Tools.GenerateButtonColor(new Color(108, 108, 215));
        public static readonly RendererData brGreen = Tools.GenerateButtonColor(new Color(0x91, 0xbd, 0x3a));
        public static readonly RendererData brRed = Tools.GenerateButtonColor(new Color(0xfa, 0x16, 0x3f));

        public static Panel BackPanel;

        private static void InitBackPanel()
        {
            var back = new Panel();
            BackPanel = back;
            back.Renderer.BackgroundColor = Color.Transparent;
            back.SizeLayout = new Layout2d("100%, 100%");
            back.MousePressed += (sender, f) => { Program.Window_MouseButtonPressed(f.Value.I(), Mouse.Button.Left); };
            back.MouseReleased += (sender, f) => { Program.Window_MouseButtonReleased(f.Value.I(), Mouse.Button.Left); };
            back.RightMousePressed += (sender, f) => { Program.Window_MouseButtonPressed(f.Value.I(), Mouse.Button.Right); };
            back.RightMouseReleased += (sender, f) => { Program.Window_MouseButtonReleased(f.Value.I(), Mouse.Button.Right); };
            GUI.Add(back);
        }

        private static void InitMenuBar()
        {
            var menu = new MenuBar();
            menu.AddMenu( L["Exit"]);
            menu.AddMenu(L["Open"]);
            menu.MenuItemClicked += async (sender, s) =>
            {
                if (s.Value == L["Exit"])
                    Environment.Exit(0);
                else if (s.Value == L["Open"])
                {
                    var ofp = new OpenFileDialog
                    {
                        AddExtension = true,
                        CheckFileExists = true,
                        CheckPathExists = true,
                        AutoUpgradeEnabled = true,
                        DefaultExt = "csx",
                        Filter = $"{L["Scene"]} (*.csx)|*.csx",
                        InitialDirectory = Path.Combine(Environment.CurrentDirectory, "scenes")
                    };

                    if (ofp.ShowDialog() == DialogResult.OK)
                    {
                        await Scene.Load(Scene.LoadScript(ofp.FileName)).ConfigureAwait(true);
                    }
                }
            };
            GUI.Add(menu);
        }

        private static void InitToolButtons()
        {
            const float spaceSize = 0.25f;

            var buttons = new HorizontalLayout();
            var numSpaces = 0;

            void space()
            {
                buttons.AddSpace(spaceSize);
                numSpaces++;
            }

            static void connectButton(ClickableWidget b, ChildWindowEx w, bool right=false, bool center=false)
            {
                w.Visible = false;

                void dlg(object sender, SignalArgsVector2f f)
                {
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (w.Visible = !w.Visible)
                    {
                        w.StartPosition = w.Position = b.AbsolutePosition + new Vector2f(center ? (-w.FullSize.X + b.FullSize.X) / 2 : 0, -w.FullSize.Y);
                    }
                }

                if (right)
                    b.RightMouseReleased += dlg;
                else
                    b.MouseReleased += dlg;
            }

            btnPlay = new BitmapButton {Image = imgPlay};

            btnPlay.Clicked += (sender, f) => { Simulation.TogglePause(); };
            foreach (var (dess, img, bref, text) in actions)
            {
                var btn = new BitmapButton {Image = text.Value = new Texture($"icons/big/{img}.png")};
                btn.Clicked += delegate { SetDrawMode(dess); };
                buttons.Add(btn);
                bref.Value = btn;
            }

            var wndSim = new ChildWindowEx(L["Simulation"], 250, true, false)
            {
                new NumberField<float>(0.1f, 10.0f, log: true, bindProp: () => Simulation.TimeScale)
            };
            GUI.Add(wndSim);
            connectButton(btnPlay, wndSim, true, true);

            space();

            buttons.Add(btnPlay);
            var btnRestart = new BitmapButton {Image = new Texture("icons/big/reset.png")};
            btnRestart.SetRenderer(brDef);
            btnRestart.Clicked += async (sender, f) => { await Scene.Restart().ConfigureAwait(true); };
            buttons.Add(btnRestart);

            space();

            var btnGrav = new BitmapButton {Image = new Texture("icons/big/gravity.png")};
            btnGrav.SetRenderer(brToggle);
            btnGrav.MouseReleased += (sender, f) =>
            {
                Simulation.GravityEnabled = !Simulation.GravityEnabled;
                btnGrav.SetRenderer(Simulation.GravityEnabled ? brToggle : brDef);
            };
            var wndGrav = new ChildWindowEx(L["Gravity"], 250, true, false)
            {
                new NumberField<float>(0.1f, 30.0f, bindProp: () => Simulation.Gravity),
                new NumberField<float>(-180, 180, unit: "°", bindProp: () => Simulation.GravityAngle,
                    conv: PropConverter.AngleDegrees),
                new CheckField(bindProp: () => Render.ShowGravityField)
            };
            GUI.Add(wndGrav);
            connectButton(btnGrav, wndGrav, true);
            buttons.Add(btnGrav);

            var btnAirFr = new BitmapButton {Image = new Texture("icons/big/wind.png")};
            btnAirFr.SetRenderer(brDef);
            btnAirFr.MouseReleased += (sender, f) =>
            {
                Simulation.AirFriction = !Simulation.AirFriction;
                btnAirFr.SetRenderer(Simulation.AirFriction ? brToggle : brDef);
            };
            var wndAirFr = new ChildWindowEx(L["Air friction"], 250, true, false)
            {
                new NumberField<float>(0.01f, 100,
                    bindProp: () => Simulation.AirDensity, log: true),
                new NumberField<float>(0.01f, 100,
                    bindProp: () => Simulation.AirFrictionMultiplier, log: true),
                new NumberField<float>(0.0001f, 10,
                    bindProp: () => Simulation.AirFrictionLinear, log: true) {LeftValue = 0},
                new NumberField<float>(0.0001f, 1,
                    bindProp: () => Simulation.AirFrictionQuadratic, log: true) {LeftValue = 0},
                new NumberField<float>(0, 50,
                    bindProp: () => Simulation.WindSpeed),
                new NumberField<float>(-180, 180, unit: "°",
                    bindProp: () => Simulation.WindAngle, conv: PropConverter.AngleDegrees)
            };

            GUI.Add(wndAirFr);
            connectButton(btnAirFr, wndAirFr, true);
            buttons.Add(btnAirFr);

            var btnSettings = new BitmapButton { Image = new Texture("icons/big/options.png") };
            btnSettings.SetRenderer(brDef);
            var wndSettings = new ChildWindowEx(L["Settings"], 320, true, false)
            {
                new CheckField(bindProp: () => Render.ShowForces),
                new CheckField(bindProp: () => Render.ShowForcesValues),
                new CheckField(bindProp: () => Render.ShowForcesComponents),
                new NumberField<float>(0.0001f, 500, bindProp: () => Render.ForcesScale, log: true)
            };
            GUI.Add(wndSettings);
            connectButton(btnSettings, wndSettings);
            buttons.Add(btnSettings);

            GUI.Add(buttons);

            buttons.SizeLayout = new Layout2d((buttons.Widgets.Count + numSpaces * 0.25f) * 60, 60);
            buttons.PositionLayout = new Layout2d("50% - w / 2", "&.h - h");
        }

        public static void Init()
        {
            GUI = new Gui(Render.Window);

            InitBackPanel();

            InitMenuBar();

            InitToolButtons();

            SetDrawMode(DrawingType.Off);
        }

        public static void OpenProperties(Object obj, Vector2f pos)
        {
            foreach (var g in PropertyWindows.ToList())
            {
                if (g.Key != obj && g.Value.FirstOrDefault(w => w.IsMain) is { } ww)
                {
                    ww.Close();
                }
            }

            if (!PropertyWindows.ContainsKey(obj))
            {
                PropertyWindows[obj] = new List<ChildWindowEx>();
            }
            else
            {
                if (PropertyWindows[obj].FirstOrDefault(w => w.IsMain) is { } ww)
                {
                    ww.Position = pos;
                    return;
                }
            }

            var wnd = new WndBase(obj, obj.Name, 150, pos) {IsMain = true};

            Vector2f posEnfant() => wnd.Position + new Vector2f(wnd.Size.X, 0);

            var btnEff = new BitmapButton {Text = L["Clear"], Image = new Texture("icons/small/delete.png")};
            btnEff.Clicked += delegate { obj.Delete(); };
            wnd.Add(btnEff);
            
            // liquify
            // spongify
            // clone
            // mirror

            var windows = new[]
            {
                (typeof(WndPlot), L["Plot"], "icons/small/plot.png"),
                (typeof(WndAppearance), L["Appearance"], "icons/small/settings.png"),
                // text
                (typeof(WndMaterial), L["Material"], "icons/small/settings.png"),
                (typeof(WndSpeeds), L["Velocities"], "icons/small/settings.png"),
                (typeof(WndSpring), L["Spring"], "icons/small/spring.png"),
                (typeof(WndHinge), L["Hinge"], "icons/small/spring.png"),
                (typeof(WndTracer), L["Tracer"], "icons/small/tracer.png"),
                (typeof(WndLaser), L["Laser"], "icons/small/tracer.png"),
                (typeof(WndThruster), L["Thruster"], "icons/small/thruster.png"),
                (typeof(WndInfos), L["Informations"], "icons/small/info.png"),
                (typeof(WndCollision), L["Collision layers"], "icons/small/settings.png"),
                (typeof(WndActions), L["Geometry actions"], "icons/small/settings.png"),
                // csg
                // controller
                (typeof(WndScript), L["Script"], "icons/small/script.png"),
            };

            foreach (var (type, name, icon) in windows)
            {
                if (!type.BaseType.GenericTypeArguments[0].IsAssignableFrom(obj.GetType()))
                    continue;
                
                var btn = new BitmapButton {Text = name, Image = new Texture(icon)};
                btn.Clicked += delegate { Activator.CreateInstance(type, obj, posEnfant()); };
                wnd.Add(btn);
            }

            wnd.Show();

            void CloseAll(bool exceptMoved=false)
            {
                if (!PropertyWindows.ContainsKey(obj)) return;

                foreach (var w in PropertyWindows[obj].ToList())
                {
                    if (!PropertyWindows.ContainsKey(obj))
                        return;

                    if (w.CPointer == IntPtr.Zero)
                    {
                        PropertyWindows[obj].Remove(w);
                        continue;
                    }

                    if (exceptMoved && w.WasMoved)
                        continue;

                    w.Close();
                }

                if (PropertyWindows.ContainsKey(obj) && !PropertyWindows[obj].Any())
                    PropertyWindows.Remove(obj);
            }

            obj.Deleted += () =>
            {
                CloseAll();
            };

            void ClickClose(object sender, SignalArgsVector2f signalArgsVector2F)
            {
                CloseAll(true);
            }

            BackPanel.MousePressed += ClickClose;
            BackPanel.RightMouseReleased += delegate
            {
                if (Drawing.SelectedObject == null)
                    CloseAll(true);
            };

            wnd.PositionChanged += (sender, f) =>
            {
                foreach (var w in PropertyWindows[obj].Where(w => w != wnd && !w.WasMoved))
                {
                    w.StartPosition = w.Position = f.Value + new Vector2f(wnd.Size.X, 0);
                }

                wnd.StartPosition = wnd.Position;
            };

            wnd.Closed += delegate
            {
                BackPanel.MousePressed -= ClickClose;
                CloseAll(true);
            };
        }

        public static T W<T>(this T o)
        {
            GC.KeepAlive(o);
            return o;
        }

        public static void ClearPropertyWindows()
        {
            foreach (var o in PropertyWindows.Keys.ToArray())
            {
                o.InvokeDeleted();
            }
        }


        public static Vector2i ClickPosition;
        public static DateTime MouseDownTime;
        public static Vector2i LastClick;

        public static Dictionary<Object, List<ChildWindowEx>> PropertyWindows = new Dictionary<Object, List<ChildWindowEx>>();

        public static event Action Drawn;

        public static void OnDrawn()
        {
            Drawn?.Invoke();
        }
    }
}