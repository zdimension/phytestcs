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
        public static readonly Texture imgPlay = new Texture("icones/play.png");
        public static readonly Texture imgPause = new Texture("icones/pause.png");
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
            (DrawingType.Hinge, "hinge", new Ref<BitmapButton>(), new Ref<Texture>())
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
            menu.MenuItemClicked += (sender, s) =>
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
                        Scene.Load(Scene.LoadScript(ofp.FileName));
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
                var btn = new BitmapButton {Image = text.Value = new Texture($"icones/{img}.png")};
                btn.Clicked += delegate { SetDrawMode(dess); };
                buttons.Add(btn);
                bref.Value = btn;
            }

            var wndSim = new ChildWindowEx(L["Simulation"], 250, true, false)
            {
                new TextField<float>(0.1f, 10.0f, log: true, bindProp: () => Simulation.TimeScale)
            };
            GUI.Add(wndSim);
            connectButton(btnPlay, wndSim, true, true);

            space();

            buttons.Add(btnPlay);
            var btnRestart = new BitmapButton {Image = new Texture("icones/reset.png")};
            btnRestart.SetRenderer(brDef);
            btnRestart.Clicked += (sender, f) => { Scene.Restart(); };
            buttons.Add(btnRestart);

            space();

            var btnGrav = new BitmapButton {Image = new Texture("icones/gravity.png")};
            btnGrav.SetRenderer(brToggle);
            btnGrav.MouseReleased += (sender, f) =>
            {
                Simulation.GravityEnabled = !Simulation.GravityEnabled;
                btnGrav.SetRenderer(Simulation.GravityEnabled ? brToggle : brDef);
            };
            var wndGrav = new ChildWindowEx(L["Gravity"], 250, true, false)
            {
                new TextField<float>(0.1f, 30.0f, bindProp: () => Simulation.Gravity),
                new TextField<float>(-180, 180, unit: "°", bindProp: () => Simulation.GravityAngle,
                    conv: PropConverter.AngleDegrees),
                new CheckField(bindProp: () => Render.ShowGravityField)
            };
            GUI.Add(wndGrav);
            connectButton(btnGrav, wndGrav, true);
            buttons.Add(btnGrav);

            var btnAirFr = new BitmapButton {Image = new Texture("icones/wind.png")};
            btnAirFr.SetRenderer(brDef);
            btnAirFr.MouseReleased += (sender, f) =>
            {
                Simulation.AirFriction = !Simulation.AirFriction;
                btnAirFr.SetRenderer(Simulation.AirFriction ? brToggle : brDef);
            };
            var wndAirFr = new ChildWindowEx(L["Air friction"], 250, true, false)
            {
                new TextField<float>(0.01f, 100,
                    bindProp: () => Simulation.AirDensity, log: true),
                new TextField<float>(0.01f, 100,
                    bindProp: () => Simulation.AirFrictionMultiplier, log: true),
                new TextField<float>(0.0001f, 10,
                    bindProp: () => Simulation.AirFrictionLinear, log: true) {LeftValue = 0},
                new TextField<float>(0.0001f, 1,
                    bindProp: () => Simulation.AirFrictionQuadratic, log: true) {LeftValue = 0},
                new TextField<float>(0, 50,
                    bindProp: () => Simulation.WindSpeed),
                new TextField<float>(-180, 180, unit: "°",
                    bindProp: () => Simulation.WindAngle, conv: PropConverter.AngleDegrees)
            };


            GUI.Add(wndAirFr);
            connectButton(btnAirFr, wndAirFr, true);
            buttons.Add(btnAirFr);

            var btnSettings = new BitmapButton { Image = new Texture("icones/options.png") };
            btnSettings.SetRenderer(brDef);
            var wndSettings = new ChildWindowEx(L["Settings"], 320, true, false)
            {
                new CheckField(bindProp: () => Render.ShowForces),
                new CheckField(bindProp: () => Render.ShowForcesValues),
                new CheckField(bindProp: () => Render.ShowForcesComponents),
                new TextField<float>(0.0001f, 500, bindProp: () => Render.ForcesScale, log: true)
            };
            GUI.Add(wndSettings);
            connectButton(btnSettings, wndSettings);
            buttons.Add(btnSettings);

            GUI.Add(buttons);

            buttons.SizeLayout = new Layout2d((11 + numSpaces * 0.25f) * 60, 60);
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

            var btnEff = new BitmapButton {Text = L["Clear"], Image = new Texture("icones/delete.png")};
            btnEff.Clicked += delegate { obj.Delete(); };
            wnd.Add(btnEff);

            void btnFen<Tfen>(Object o, string nom, string image)
            {
                var btn = new BitmapButton {Text = nom, Image = new Texture(image)};
                btn.Clicked += delegate { Activator.CreateInstance(typeof(Tfen), o, posEnfant()); };
                wnd.Add(btn);
            }

            btnFen<WndInfos>(obj, L["Informations"], "icones/info.png");

            if (obj is PhysicalObject op)
            {
                btnFen<WndMaterial>(op, L["Material"], "icones/settings.png");
                btnFen<WndAppearance>(op, L["Appearance"], "icones/settings.png");
                btnFen<WndSpeeds>(op, L["Velocities"], "icones/speed.png");
                btnFen<WndPlot>(op, L["Plot"], "icones/sine.png");
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