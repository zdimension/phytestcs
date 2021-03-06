﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using phytestcs.Interface.Windows;
using phytestcs.Interface.Windows.Properties;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using TGUI;
using static phytestcs.Tools;
using Button = TGUI.Button;
using ButtonRenderer = TGUI.ButtonRenderer;
using KeyEventArgs = SFML.Window.KeyEventArgs;
using Panel = TGUI.Panel;

namespace phytestcs.Interface
{
    public static class Ui
    {
        public static BitmapButton BtnPlay = null!;
        public static readonly Texture ImgPlay = new Texture("icons/big/play.png");
        public static readonly Texture ImgPause = new Texture("icons/big/pause.png");
        public static readonly Font FontMono = new Font("fonts/mono.ttf");
        public static Gui Gui = null!;

        public static readonly List<(DrawingType, string, Ref<BitmapButton>, Ref<Texture>)> Actions =
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
                (DrawingType.Laser, "laser", new Ref<BitmapButton>(), new Ref<Texture>())
            };

        public static readonly RendererData BrDef;// GenerateButtonColor(new  Color(210, 210, 210));
        public static readonly RendererData BrToggle;// GenerateButtonColor(new Color(108, 108, 215));
        public static readonly RendererData BrGreen;// GenerateButtonColor(new Color(0x91, 0xbd, 0x3a));
        public static readonly RendererData BrRed;// GenerateButtonColor(new Color(0xfa, 0x16, 0x3f));

        public static Panel BackPanel = null!;


        public static Vector2i ClickPosition;
        public static DateTime MouseDownTime;
        public static Vector2i LastClick;

        public static readonly Dictionary<BaseObject, List<ChildWindowEx>> PropertyWindows =
            new Dictionary<BaseObject, List<ChildWindowEx>>();

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
            back.Clicked += (sender, f) =>
            {
                var pos = f.Value.I();
                var obj = ObjectAtPosition(pos);

                obj?.OnClick.Invoke(new ClickedEventArgs(obj, pos.ToWorld()));
            };
            Gui.Add(back);
        }

        public static Widget? MouseOnWhichWidget(Vector2i pos)
        {
            return Gui.GetWidgetBelowMouseCursor(pos.X, pos.Y);
            //return Gui.Widgets.LastOrDefault(w => w.Visible && w.MouseOnWidget(pos.F()));
        }

        private static void InitMenuBar()
        {
            var menu = new MenuBar();
            menu.AddMenu(L["New"]);
            var newDict = new Dictionary<string, Palette>();
            foreach (var (text, palette) in Palette.Palettes)
            {
                newDict[text] = palette;
                menu.AddMenuItem(text);
            }
            menu.AddMenu(L["Open"]);
            menu.AddMenu(L["Exit"]);
            menu.MenuItemClicked += async (sender, s) =>
            {
                if (s.Value == L["Exit"])
                {
                    Environment.Exit(0);
                }
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
                        await Scene.Load(Scene.LoadScript(ofp.FileName)).ConfigureAwait(true);
                }
                else if (newDict.TryGetValue(s.Value, out var palette))
                {
                    await Scene.New().ConfigureAwait(true);
                    Program.CurrentPalette = palette;
                }
            };
            Gui.Add(menu);
        }
        
        private static List<ChildWindowEx> _childWindows = new List<ChildWindowEx>();
        public static BitmapButton BtnGrav;
        public static BitmapButton BtnAirFr;
        public static BitmapButton BtnGrid;

        static Ui()
        {
            Theme.Default.load("themes/windows/theme.txt");
            //heme.Default = new Theme("themes/windows/theme.txt");
            BrDef = Theme.Default.getRenderer("Button");
            var th = new Theme("themes/windows/theme_toggled.txt");
            BrToggle =th.getRenderer("Button"); 
            BrGreen = Theme.Default.getRenderer("Button");
            BrRed = th.getRenderer("Button");
        }

        public static Widget? GetFocusedWidget(Container? start=null)
        {
            var wgts = start?.Widgets ?? Gui.Widgets;
            while (true)
            {
                var focused = wgts.FirstOrDefault(w => w.Focus);
                if (focused is Container c)
                    wgts = c.Widgets;
                else
                    return focused;
            }
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

            BackPanel.MousePressed += (sender, f) =>
            {
                foreach (var w in _childWindows)
                {
                    if (!w.WasMoved)
                        w.Visible = false;
                }
            };

            static void ConnectButton(ClickableWidget b, ChildWindowEx w, bool right = false, bool center = false)
            {
                w.Visible = false;
                _childWindows.Add(w);

                void Dlg(object? sender, SignalArgsVector2f f)
                {
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (w.Visible = !w.Visible)
                        w.StartPosition = w.Position =
                            b.AbsolutePosition + new Vector2f(center ? (-w.FullSize.X + b.FullSize.X) / 2 : 0,
                                -w.FullSize.Y);
                }

                if (right)
                    b.RightMouseReleased += Dlg;
                else
                    b.MouseReleased += Dlg;
            }

            BtnPlay = new BitmapButton { Image = ImgPlay };

            BtnPlay.Clicked += (sender, f) => { Simulation.TogglePause(); };
            foreach (var (dess, img, bref, text) in Actions)
            {
                var btn = new BitmapButton { Image = text.Value = new Texture($"icons/big/{img}.png") };
                btn.Clicked += delegate { Drawing.DrawMode = dess; };
                buttons.Add(btn);
                bref.Value = btn;
            }

            var wndSim = new ChildWindowEx(L["Simulation"], 250, true, false)
            {
                new NumberField<float>(0.1f, 10.0f, () => Simulation.TimeScale, log: true)
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

            BtnGrid = new BitmapButton { Image = new Texture("icons/big/grid.png") };
            BtnGrid.SetRenderer(BrToggle);
            BtnGrid.MouseReleased += (sender, f) =>
            {
                Render.ShowGrid = !Render.ShowGrid;
            };
            var wndGrid = new ChildWindowEx(L["Grid"], 250, true, false)
            {
                new CheckField(() => Render.SnapToGrid)
            };
            Gui.Add(wndGrid);
            ConnectButton(BtnGrid, wndGrid, true);
            buttons.Add(BtnGrid);

            BtnGrav = new BitmapButton { Image = new Texture("icons/big/gravity.png") };
            BtnGrav.SetRenderer(BrToggle);
            BtnGrav.MouseReleased += (sender, f) =>
            {
                Simulation.GravityEnabled = !Simulation.GravityEnabled;
            };
            var wndGrav = new ChildWindowEx(L["Gravity"], 250, true, false)
            {
                new NumberField<float>(0.1f, 30.0f, () => Simulation.Gravity),
                new NumberField<float>(-180, 180, () => Simulation.GravityAngle, unit: "°"),
                new CheckField(() => Render.ShowGravityField)
            };
            Gui.Add(wndGrav);
            ConnectButton(BtnGrav, wndGrav, true);
            buttons.Add(BtnGrav);

            BtnAirFr = new BitmapButton { Image = new Texture("icons/big/wind.png") };
            BtnAirFr.SetRenderer(BrDef);
            BtnAirFr.MouseReleased += (sender, f) =>
            {
                Simulation.AirFriction = !Simulation.AirFriction;
            };
            var wndAirFr = new ChildWindowEx(L["Air friction"], 250, true, false)
            {
                new NumberField<float>(0.01f, 100,
                    () => Simulation.AirDensity, log: true),
                new NumberField<float>(0.01f, 100,
                    () => Simulation.AirFrictionMultiplier, log: true) { LeftValue = 0 },
                new NumberField<float>(0.0001f, 10,
                    () => Simulation.AirFrictionLinear, log: true) { LeftValue = 0 },
                new NumberField<float>(0.0001f, 1,
                    () => Simulation.AirFrictionQuadratic, log: true) { LeftValue = 0 },
                new NumberField<float>(0, 50,
                    () => Simulation.WindSpeed),
                new NumberField<float>(-180, 180,
                    () => Simulation.WindAngle, unit: "°", conv: PropConverter.AngleDegrees)
            };

            Gui.Add(wndAirFr);
            ConnectButton(BtnAirFr, wndAirFr, true);
            buttons.Add(BtnAirFr);

            var btnSettings = new BitmapButton { Image = new Texture("icons/big/options.png") };
            btnSettings.SetRenderer(BrDef);
            var wndSettings = new ChildWindowEx(L["Settings"], 320, true, false)
            {
                new CheckField(() => Render.ShowForces),
                new CheckField(() => Render.ShowForcesValues),
                new CheckField(() => Render.ShowForcesComponents),
                new NumberField<float>(0.0001f, 500, () => Render.ForcesScale, log: true)
            };
            Gui.Add(wndSettings);
            ConnectButton(btnSettings, wndSettings);
            buttons.Add(btnSettings);
            
            var btnConsole = new BitmapButton { Image = new Texture("icons/big/console.png") };
            btnConsole.SetRenderer(BrDef);
            btnConsole.MouseReleased += delegate
            {
                var w = new WndConsole();
                w.Position = ((BackPanel.Size - w.Size) / 2).Round();
                Gui.Add(w);
                w.Field.Focus = true;
            };
            buttons.Add(btnConsole);

            Gui.Add(buttons);

            buttons.SizeLayout = new Layout2d((buttons.Widgets.Count + numSpaces * 0.25f) * 60, 60);
            buttons.PositionLayout = new Layout2d("50% - w / 2", "&.h - h");
        }

        public static void Init()
        {
            Gui = new Gui(Render.Window);
            Gui.TabKeyUsageEnabled = false;

            InitBackPanel();

            InitMenuBar();

            InitToolButtons();

            Drawing.DrawMode = DrawingType.Off;
        }

        public static void OpenProperties(BaseObject obj, Vector2f pos)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            if (obj is SpringEnd se)
                obj = se.Parent;

            foreach (var g in PropertyWindows.ToList())
                if (g.Key != obj && g.Value.FirstOrDefault(w => w.IsMain) is { } ww)
                    ww.Close();

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
                o.InvokeDeleted();
        }

        public static event Action Drawn = () => { };

        public static void OnDrawn()
        {
            Drawn();
        }
    }
}