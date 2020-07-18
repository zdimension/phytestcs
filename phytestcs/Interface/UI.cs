using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using phytestcs.Interface.Windows;
using phytestcs.Interface.Windows.Properties;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using TGUI;
using static phytestcs.Global;
using Button = TGUI.Button;
using Object = phytestcs.Objects.Object;
using Panel = TGUI.Panel;

namespace phytestcs.Interface
{
    public static class Ui
    {
        public static BitmapButton BtnPlay;
        public static readonly Texture ImgPlay = new Texture("icons/big/play.png");
        public static readonly Texture ImgPause = new Texture("icons/big/pause.png");
        public static readonly Font Font = new Font(@"C:\Windows\Fonts\consola.ttf");
        public static Gui Gui;

        private static List<(DrawingType, string, Ref<BitmapButton>, Ref<Texture>)> _actions =
            new List<(DrawingType, string, Ref<BitmapButton>, Ref<Texture>)>
            {
                (DrawingType.Off, "drag", new Ref<BitmapButton>(), new Ref<Texture>()),
                (DrawingType.Move, "move", new Ref<BitmapButton>(), new Ref<Texture>()),
                (DrawingType.Rectangle, "rectangle", new Ref<BitmapButton>(), new Ref<Texture>()),
                (DrawingType.Circle, "circle", new Ref<BitmapButton>(), new Ref<Texture>()),
                (DrawingType.Spring, "coil", new Ref<BitmapButton>(), new Ref<Texture>()),
                (DrawingType.Fixate, "fix", new Ref<BitmapButton>(), new Ref<Texture>()),
                (DrawingType.Hinge, "hinge", new Ref<BitmapButton>(), new Ref<Texture>()),
                (DrawingType.Tracer, "tracer", new Ref<BitmapButton>(), new Ref<Texture>()),
                (DrawingType.Thruster, "thruster", new Ref<BitmapButton>(), new Ref<Texture>()),
                (DrawingType.Laser, "laser", new Ref<BitmapButton>(), new Ref<Texture>()),
            };

        private static readonly RendererData BrDef = Tools.GenerateButtonColor(new Color(210, 210, 210));
        private static readonly RendererData BrToggle = Tools.GenerateButtonColor(new Color(108, 108, 215));
        public static readonly RendererData BrGreen = Tools.GenerateButtonColor(new Color(0x91, 0xbd, 0x3a));
        public static readonly RendererData BrRed = Tools.GenerateButtonColor(new Color(0xfa, 0x16, 0x3f));

        public static Panel BackPanel;


        public static Vector2i ClickPosition;
        public static DateTime MouseDownTime;
        public static Vector2i LastClick;

        public static readonly Dictionary<Object, List<ChildWindowEx>> PropertyWindows =
            new Dictionary<Object, List<ChildWindowEx>>();

        public static void SetDrawMode(DrawingType mode)
        {
            Drawing.DrawMode = mode;
            foreach (var (dess, _, bref, text) in _actions)
            {
                bref.Value.SetRenderer(dess == mode ? BrToggle : BrDef);
                if (dess == mode)
                    Render.DrawSprite.Texture = text.Value;
            }
        }

        private static void InitBackPanel()
        {
            var back = new Panel();
            BackPanel = back;
            back.Renderer.BackgroundColor = Color.Transparent;
            back.SizeLayout = new Layout2d("100%, 100%");
            back.MousePressed += (sender, f) => { Program.Window_MouseButtonPressed(f.Value.I(), Mouse.Button.Left); };
            back.MouseReleased += (sender, f) =>
            {
                Program.Window_MouseButtonReleased(f.Value.I(), Mouse.Button.Left);
            };
            back.RightMousePressed += (sender, f) =>
            {
                Program.Window_MouseButtonPressed(f.Value.I(), Mouse.Button.Right);
            };
            back.RightMouseReleased += (sender, f) =>
            {
                Program.Window_MouseButtonReleased(f.Value.I(), Mouse.Button.Right);
            };
            Gui.Add(back);
        }

        private static void InitMenuBar()
        {
            var wnd = new ChildWindowEx(L["New"], 200, true, false);
            foreach (var (text, palette) in Palette.Palettes)
            {
                var btn = new Button(text);
                btn.Clicked += async delegate
                {
                    wnd.Close();
                    Program.CurrentPalette = palette;
                    await Scene.New().ConfigureAwait(true);
                };
                wnd.Add(btn);
            }

            wnd.Visible = false;
            Gui.Add(wnd);
            
            
            var menu = new MenuBar();
            menu.AddMenu(L["New"]);
            menu.AddMenu(L["Open"]);
            menu.AddMenu(L["Exit"]);
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
                else if (s.Value == L["New"])
                {
                    if (!wnd.Visible)
                    {
                        wnd.PositionLayout = new Layout2d("50% - w / 2", "50% - h / 2");
                        wnd.Show();
                    }
                }
            };
            Gui.Add(menu);
        }

        private static void InitToolButtons()
        {
            const float spaceSize = 0.25f;

            var buttons = new HorizontalLayout();
            var numSpaces = 0;

            void Space()
            {
                buttons.AddSpace(spaceSize);
                numSpaces++;
            }

            static void ConnectButton(ClickableWidget b, ChildWindowEx w, bool right = false, bool center = false)
            {
                w.Visible = false;

                void Dlg(object sender, SignalArgsVector2f f)
                {
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (w.Visible = !w.Visible)
                    {
                        w.StartPosition = w.Position =
                            b.AbsolutePosition + new Vector2f(center ? (-w.FullSize.X + b.FullSize.X) / 2 : 0,
                                -w.FullSize.Y);
                    }
                }

                if (right)
                    b.RightMouseReleased += Dlg;
                else
                    b.MouseReleased += Dlg;
            }

            BtnPlay = new BitmapButton { Image = ImgPlay };

            BtnPlay.Clicked += (sender, f) => { Simulation.TogglePause(); };
            foreach (var (dess, img, bref, text) in _actions)
            {
                var btn = new BitmapButton { Image = text.Value = new Texture($"icons/big/{img}.png") };
                btn.Clicked += delegate { SetDrawMode(dess); };
                buttons.Add(btn);
                bref.Value = btn;
            }

            var wndSim = new ChildWindowEx(L["Simulation"], 250, true, false)
            {
                new NumberField<float>(0.1f, 10.0f, log: true, bindProp: () => Simulation.TimeScale)
            };
            Gui.Add(wndSim);
            ConnectButton(BtnPlay, wndSim, true, true);

            Space();

            buttons.Add(BtnPlay);
            var btnRestart = new BitmapButton { Image = new Texture("icons/big/reset.png") };
            btnRestart.SetRenderer(BrDef);
            btnRestart.Clicked += async (sender, f) => { await Scene.Restart().ConfigureAwait(true); };
            buttons.Add(btnRestart);

            Space();

            var btnGrid = new BitmapButton { Image = new Texture("icons/big/grid.png") };
            btnGrid.SetRenderer(BrToggle);
            btnGrid.MouseReleased += (sender, f) =>
            {
                Render.ShowGrid = !Render.ShowGrid;
                btnGrid.SetRenderer(Render.ShowGrid ? BrToggle : BrDef);
            };

            var btnGrav = new BitmapButton { Image = new Texture("icons/big/gravity.png") };
            btnGrav.SetRenderer(BrToggle);
            btnGrav.MouseReleased += (sender, f) =>
            {
                Simulation.GravityEnabled = !Simulation.GravityEnabled;
                btnGrav.SetRenderer(Simulation.GravityEnabled ? BrToggle : BrDef);
            };
            var wndGrav = new ChildWindowEx(L["Gravity"], 250, true, false)
            {
                new NumberField<float>(0.1f, 30.0f, bindProp: () => Simulation.Gravity),
                new NumberField<float>(-180, 180, unit: "°", bindProp: () => Simulation.GravityAngle,
                    conv: PropConverter.AngleDegrees),
                new CheckField(() => Render.ShowGravityField)
            };
            Gui.Add(wndGrav);
            ConnectButton(btnGrav, wndGrav, true);
            buttons.Add(btnGrav);

            var btnAirFr = new BitmapButton { Image = new Texture("icons/big/wind.png") };
            btnAirFr.SetRenderer(BrDef);
            btnAirFr.MouseReleased += (sender, f) =>
            {
                Simulation.AirFriction = !Simulation.AirFriction;
                btnAirFr.SetRenderer(Simulation.AirFriction ? BrToggle : BrDef);
            };
            var wndAirFr = new ChildWindowEx(L["Air friction"], 250, true, false)
            {
                new NumberField<float>(0.01f, 100,
                    bindProp: () => Simulation.AirDensity, log: true),
                new NumberField<float>(0.01f, 100,
                    bindProp: () => Simulation.AirFrictionMultiplier, log: true),
                new NumberField<float>(0.0001f, 10,
                    bindProp: () => Simulation.AirFrictionLinear, log: true) { LeftValue = 0 },
                new NumberField<float>(0.0001f, 1,
                    bindProp: () => Simulation.AirFrictionQuadratic, log: true) { LeftValue = 0 },
                new NumberField<float>(0, 50,
                    bindProp: () => Simulation.WindSpeed),
                new NumberField<float>(-180, 180, unit: "°",
                    bindProp: () => Simulation.WindAngle, conv: PropConverter.AngleDegrees)
            };

            Gui.Add(wndAirFr);
            ConnectButton(btnAirFr, wndAirFr, true);
            buttons.Add(btnAirFr);

            var btnSettings = new BitmapButton { Image = new Texture("icons/big/options.png") };
            btnSettings.SetRenderer(BrDef);
            var wndSettings = new ChildWindowEx(L["Settings"], 320, true, false)
            {
                new CheckField(() => Render.ShowForces),
                new CheckField(() => Render.ShowForcesValues),
                new CheckField(() => Render.ShowForcesComponents),
                new NumberField<float>(0.0001f, 500, bindProp: () => Render.ForcesScale, log: true)
            };
            Gui.Add(wndSettings);
            ConnectButton(btnSettings, wndSettings);
            buttons.Add(btnSettings);

            Gui.Add(buttons);

            buttons.SizeLayout = new Layout2d((buttons.Widgets.Count + numSpaces * 0.25f) * 60, 60);
            buttons.PositionLayout = new Layout2d("50% - w / 2", "&.h - h");
        }

        public static void Init()
        {
            Gui = new Gui(Render.Window);

            InitBackPanel();

            InitMenuBar();

            InitToolButtons();

            SetDrawMode(DrawingType.Off);
        }

        public static void OpenProperties(Object obj, Vector2f pos)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

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

            var wnd = new WndProperties(obj, pos);
            PropertyWindows[obj].Add(wnd);
            wnd.Show();
        }

        public static void ClearPropertyWindows()
        {
            foreach (var o in PropertyWindows.Keys.ToArray())
            {
                o.InvokeDeleted();
            }
        }

        public static event Action Drawn = () => { };

        public static void OnDrawn()
        {
            Drawn();
        }
    }
}