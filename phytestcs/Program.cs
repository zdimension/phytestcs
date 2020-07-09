using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using phytestcs.Interface;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using static phytestcs.Tools;
using static phytestcs.Interface.UI;
using static phytestcs.Global;
using Object = phytestcs.Objects.Object;

namespace phytestcs
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Render.Window = new RenderWindow(new VideoMode(Render.Width, Render.Height), "jeu", Styles.Default, new ContextSettings {AntialiasingLevel = 4});
            //Render.Window.SetVerticalSyncEnabled(true);
            //Window.SetFramerateLimit(240);


            Init();
            Simulation.Pause = false;
            Simulation.TogglePause();

            Camera.GameView = new View();
            Camera.MainView = new View();
            Camera.Center();
            Camera.CalculateWindow();

            Render.Statistics = new Text("", UI.Font, 20);

            Render.Window.Closed += Window_Closed;
            Render.Window.Resized += Window_Resized;
            Render.Window.KeyPressed += Window_KeyPressed;
            Render.Window.KeyReleased += Window_KeyReleased;
            Render.Window.MouseWheelScrolled += Window_MouseWheelScrolled;
            Render.Window.MouseMoved += Window_MouseMoved;

            var sw = new Stopwatch();
            var thrPhy = new Thread(() =>
            {
                while (true)
                {
                    sw.Restart();
                    Simulation.UpdatePhysics();
                    
                    var delta = (Simulation.TargetDT - sw.Elapsed.TotalSeconds) * 1000 * 0.975f;
                    if (delta > 0)
                    {
                        Thread.Sleep((int) delta);
                    }
                }
            });
            /*var tmrPhy = new System.Timers.Timer() {Interval = 10};
            tmrPhy.Elapsed += (s, e) => { Simulation.UpdatePhysics(); };*/

            var lastUpd = DateTime.Now;

            Task.Run(async () =>
            {
                await Scene.Load(Scene.LoadScript(args.Length > 0 ? args[0] : "scenes/energie.csx")).ConfigureAwait(true);
                //tmrPhy.Start();
                if (!thrPhy.IsAlive)
                    thrPhy.Start();
            });

            var txl = new Text(L["Loading..."], UI.Font, 32) {FillColor = Color.White};
            txl.Origin = txl.GetLocalBounds().Size() / 2;

            while (Render.Window.IsOpen)
            {
                Render.Window.DispatchEvents();

                if (Scene.Loaded)
                {
                    Simulation.WorldCache = Simulation.World.ToArrayLocked();

                    Camera.UpdateZoom();

                    Render.Window.SetView(Camera.GameView);

                    Render.DrawGame();
                    
                    Render.DrawRotation();

                    Render.Window.SetView(Camera.MainView);

                    //Console.Write("\r" + Simulation.FPS + "     ");

                    if (Render.ShowGravityField)
                        Render.DrawGravityField();

                    Render.DrawStatistics();

                    Render.DrawLegend();

                    OnDrawn();

                    GUI.Draw();

                    Render.DrawDrawing();
                }
                else
                {
                    Render.Window.SetView(Camera.MainView);

                    Render.Window.Clear(Color.Blue);

                    txl.Position = Render.WindowF / 2;

                    Render.Window.Draw(txl);
                }

                Render.Window.Display();

                var dt = (float)(DateTime.Now - lastUpd).TotalSeconds;

                if (CameraMoveVel != default)
                {
                    Camera.GameView.Center += CameraMoveVel.InvertY() * dt / Camera.Zoom;
                    CameraMoveVel *= (float)Math.Exp(-3 * dt);
                    if (CameraMoveVel.Norm() < 0.001f)
                        CameraMoveVel = default;
                }

                Simulation.FPS = 1 / dt;

                lastUpd = DateTime.Now;
            }
        }

        private static (DateTime, Vector2i) _lastMove;

        public static bool _rotating = false;
        public static bool _moving = false;
        private static float _rotatingAngle = 0;
        public static CircleShape _rotCircle = new CircleShape(0, Render._rotCirclePointCount){FillColor = Color.Transparent, OutlineColor = new Color(255, 255, 255, 180)};
        public static Text _rotText = new Text("", UI.Font, 18){FillColor = Color.White, OutlineColor = Color.Black, OutlineThickness = 1f};

        public static float _rotDeltaAngle = 0;
        public static float _rotStartAngle = 0;
        private static void Window_MouseMoved(object sender, MouseMoveEventArgs e)
        {
            if (!_rotating && !_moving && Mouse.IsButtonPressed(Mouse.Button.Right) && ObjectAtPosition(ClickPosition) is Object obj && obj is IRotHasPos rot)
            {
                Drawing.SelectObject(obj);
                if (obj is PhysicalObject phy)
                    phy.UserFix = true;
                _rotating = true;
                _rotatingAngle = rot.Angle;
                _rotCircle.Radius = 135 / Camera.Zoom;
                _rotCircle.CenterOrigin();
                _rotCircle.Position = rot.Position;
                _rotText.Position = rot.Position.ToScreen().F();
                _rotStartAngle = (ClickPosition.ToWorld() - rot.Position).Angle();
            }

            if (_rotating)
            {
                var rotObj = (IRotHasPos) Drawing.SelectedObject;
                var rotCur = e.Position().ToWorld() - rotObj.Position;
                _rotDeltaAngle = rotCur.Angle() - _rotStartAngle;
                var newAng = _rotatingAngle + _rotDeltaAngle;
                if (rotCur.Norm() < _rotCircle.Radius)
                {
                    newAng = ((float)(15 * Math.Round(newAng.Degrees() / 15))).Radians();
                    _rotDeltaAngle = newAng - _rotatingAngle;
                }
                rotObj.Angle = newAng;
                Simulation.UpdatePhysicsInternal(0);
            }
            
            if (!_rotating && Camera.CameraMoveOrigin != null &&
                (Mouse.IsButtonPressed(Mouse.Button.Right) || Mouse.IsButtonPressed(Mouse.Button.Middle)))
            {
                _moving = true;
                Camera.GameView.Center = Camera.CameraMoveOrigin.Value +
                                       (ClickPosition - e.Position()).F().InvertY() / Camera.Zoom;
                _lastMove = (DateTime.Now, e.Position());
            }

            if (Drawing.DragObject != null && Mouse.IsButtonPressed(Mouse.Button.Left))
            {
                if (Drawing.DrawMode == DrawingType.Move)
                {
                    Drawing.DragObject.Position = e.Position().ToWorld() - Drawing.DragObjectRelPosDirect;
                    Simulation.UpdatePhysicsInternal(0);
                }
                else
                {
                    Drawing.DragSpring.End2.RelPos = e.Position().ToWorld();
                    if (Drawing.DrawMode == DrawingType.Spring)
                        Drawing.DragSpring.TargetLength = Drawing.DragSpring.Delta.Norm();
                }
            }
            
            Drawing.DragSpring?.UpdatePhysics(0);
        }

        private static void Window_MouseWheelScrolled(object sender, MouseWheelScrollEventArgs e)
        {
            var factor = 1 + Camera.ZoomDelta;
            if (e.Delta < 0)
                factor = 1 / factor;

            var dz = 1 / Camera.Zoom - 1 / (Camera.Zoom * factor);

            Camera.SetZoom(factor, Camera.GameView.Center + dz * (e.Position().F() - Render.WindowF / 2).InvertY());
        }

        public const float DoubleClickTime = 500; // millisecondes

        private static void Window_DoubleClick()
        {
            if (Drawing.SelectedObject != null)
            {
                OpenProperties(Drawing.SelectedObject, LastClick.F());
            }
        }

        public static void Window_MouseButtonReleased(Vector2i pos, Mouse.Button btn)
        {
            var moved = pos != ClickPosition;

            if (!moved)
            {
                Drawing.SelectObject(ObjectAtPosition(pos));
            }

            if (btn == Mouse.Button.Left)
            {
                if (pos == LastClick && (DateTime.Now - MouseDownTime).TotalMilliseconds < DoubleClickTime)
                {
                    Window_DoubleClick();
                }

                if (Drawing.DragObject != null)
                {
                    Drawing.DragSpring?.Delete();

                    if (Drawing.DrawMode != DrawingType.Move && Drawing.DrawMode != DrawingType.Spring)
                    {
                        Drawing.DragObject = null;
                        Drawing.DragObjectRelPos = default;
                        Drawing.DragSpring = null;
                    }
                }
                if (Drawing.DrawMode != DrawingType.Off)
                {
                    FinishDrawing();
                }
            }
            
            if (btn == Mouse.Button.Right)
            {
                if (_rotating)
                {
                    _rotating = false;
                    if (Drawing.SelectedObject is PhysicalObject phy)
                        phy.UserFix = false;
                }
                else
                {
                    if (moved)
                    {
                        var (time, mpos) = _lastMove;
                        var dt = DateTime.Now - time;
                        var dp = mpos - pos;
                        CameraMoveVel = dp.F() / (float) dt.TotalSeconds;
                    }
                    else
                    {
                        var obj = ObjectAtPosition(pos);
                        if (obj != null)
                        {
                            OpenProperties(obj, pos.F());
                        }
                    }
                }
            }

            Camera.CameraMoveOrigin = null;
            _lastMove = default;

            ClickPosition = default;

            LastClick = pos;
        }

        private static Vector2f CameraMoveVel;

        public static void Window_MouseButtonPressed(Vector2i pos, Mouse.Button btn)
        {
            MouseDownTime = DateTime.Now;
            ClickPosition = pos;

            switch (btn)
            {
                case Mouse.Button.Left:
                    if (Drawing.DrawMode != DrawingType.Off && Drawing.DrawMode != DrawingType.Spring &&
                        Drawing.DrawMode != DrawingType.Move)
                    {
                        Drawing.DrawColor = RandomColor();
                    }
                    else
                    {
                        var under = ObjectAtPosition(pos);

                        if (under is IMoveable obj)
                        {
                            Drawing.DragObject = obj;
                            Drawing.DragObjectRelPos = obj.MapInv(pos.ToWorld());
                            Drawing.DragObjectRelPosDirect = pos.ToWorld() - obj.Position;

                            if (obj is PhysicalObject phy)
                            {
                                if (Drawing.DrawMode == DrawingType.Move)
                                {
                                    phy.IsMoving = true;
                                }
                                else
                                {
                                    Simulation.Add(Drawing.DragSpring =
                                        new Spring(Drawing.DragConstant, 0, DefaultSpringSize,
                                            phy, Drawing.DragObjectRelPos, null,
                                            pos.ToWorld(),
                                            ForceType.Drag) {Damping = 1});
                                }
                            }
                        }
                    }

                    break;
                case Mouse.Button.Right:
                case Mouse.Button.Middle:
                    Camera.CameraMoveOrigin = Camera.GameView.Center;
                    CameraMoveVel = default;
                    _lastMove = (DateTime.Now, pos);
                    _moving = false;
                    _rotating = false;
                    break;
            }
        }

        static void FinishDrawing()
        {
            var mouse = Mouse.GetPosition(Render.Window);
            var moved = mouse != ClickPosition;

            switch (Drawing.DrawMode)
            {
                case DrawingType.Rectangle when moved:
                    Simulation.Add(new PhysicalObject(Render.DrawRectangle.Position + Render.DrawRectangle.Size / 2, new RectangleShape(Render.DrawRectangle)));
                    break;
                case DrawingType.Circle when moved:
                    Simulation.Add(new PhysicalObject(Render.DrawCircle.Position + Render.DrawCircle.GetLocalBounds().Size() / 2, new CircleShape(Render.DrawCircle)));
                    break;
                case DrawingType.Spring:
                {
                    if (moved && Drawing.DragSpring != null)
                    {
                        var obj = PhysObjectAtPosition(mouse);

                        if (obj != Drawing.DragObject)
                        {
                            PhysicalObject obj2;
                            Vector2f obj2Pos;
                            if (obj != null)
                            {
                                obj2 = obj;
                                obj2Pos = obj.MapInv(mouse.ToWorld());
                            }
                            else
                            {
                                obj2 = null;
                                obj2Pos = mouse.ToWorld();
                            }

                            Simulation.Add(new Spring(Drawing.DragSpring.Constant,
                                Drawing.DragSpring.TargetLength,
                                DefaultSpringSize,
                                Drawing.DragSpring.End1.Object, Drawing.DragSpring.End1.RelPos, obj2, obj2Pos));
                        }
                    }

                    Drawing.DragObject = null;
                    Drawing.DragObjectRelPos = default;
                    Drawing.DragSpring = null;
                    break;
                }
                case DrawingType.Fixate:
                {
                    if (!moved)
                    {
                        var obj = PhysObjectAtPosition(mouse);

                        if (obj != null && !obj.HasFixate)
                        {
                            Simulation.Add(new Fixate(obj, obj.MapInv(mouse.ToWorld()), DefaultObjectSize));
                        }
                    }

                    break;
                }
                case DrawingType.Hinge:
                {
                    if (!moved)
                    {
                        var obj = PhysObjectAtPosition(mouse);

                        if (obj != null)
                        {
                            var obj2 = PhysObjectAtPosition(mouse, obj);
                            var obj2pos = mouse.ToWorld();
                            if (obj2 != null)
                                obj2pos = obj2.MapInv(obj2pos);
                            Simulation.Add(new Hinge(obj, obj.MapInv(mouse.ToWorld()), DefaultSpringSize, obj2, obj2pos));
                        }
                    }

                    break;
                }
                case DrawingType.Move:
                {
                    if (Drawing.DragObject != null)
                    {
                        if (Drawing.DragObject is PhysicalObject phy)
                            phy.IsMoving = false;
                        Drawing.DragObject = null;
                    }

                    Drawing.DragObjectRelPos = default;
                    break;
                }
                case DrawingType.Tracer:
                {
                    if (!moved)
                    {
                        var obj = PhysObjectAtPosition(mouse);

                        if (obj != null && !obj.HasFixate)
                        {
                            Simulation.Add(new Tracer(obj, obj.MapInv(mouse.ToWorld()), DefaultObjectSize, RandomColor()));
                        }
                    }

                    break;
                }
                case DrawingType.Thruster:
                {
                    if (!moved)
                    {
                        var obj = PhysObjectAtPosition(mouse);

                        if (obj != null)
                        {
                            Simulation.Add(new Thruster(obj, obj.MapInv(mouse.ToWorld()), DefaultObjectSize));
                        }
                    }

                    break;
                }
            }
        }

        private const float DefaultObjectSizeFactor = 68.3366809f;
        private static float DefaultObjectSize => DefaultObjectSizeFactor / Camera.Zoom;
        private static float DefaultSpringSize => DefaultObjectSize * 0.4f;

        private static void Window_KeyReleased(object sender, KeyEventArgs e)
        {
            switch (e.Code)
            {
                case Keyboard.Key.Right:
                case Keyboard.Key.Left:
                    MoveForce.Value = new Vector2f(0, MoveForce.Value.Y);
                    break;
                case Keyboard.Key.Up:
                    MoveForce.Value = new Vector2f(MoveForce.Value.X, 0);
                    break;
            }
        }

        public static Force MoveForce = new Force(ForceType.User, new Vector2f(0, 0), default);

        private static void Window_KeyPressed(object sender, KeyEventArgs e)
        {
            switch (e.Code)
            {
                case Keyboard.Key.Right:
                    MoveForce.Value = new Vector2f(Simulation.Walk, MoveForce.Value.Y);
                    break;
                case Keyboard.Key.Left:
                    MoveForce.Value = new Vector2f(-Simulation.Walk, MoveForce.Value.Y);
                    break;
                case Keyboard.Key.Up:
                    MoveForce.Value = new Vector2f(MoveForce.Value.X, Simulation.Jump);
                    break;
                case Keyboard.Key.R:
                    Simulation.Player.Position = new Vector2f(0, 1);
                    break;
                case Keyboard.Key.Num0:
                    SetDrawMode(DrawingType.Off);
                    break;
                case Keyboard.Key.Num1:
                    SetDrawMode(DrawingType.Rectangle);
                    break;
                case Keyboard.Key.Num2:
                    SetDrawMode(DrawingType.Circle);
                    break;
                case Keyboard.Key.Num3:
                    SetDrawMode(DrawingType.Spring);
                    break;
                case Keyboard.Key.Num4:
                    SetDrawMode(DrawingType.Fixate);
                    break;
                case Keyboard.Key.Num5:
                    SetDrawMode(DrawingType.Move);
                    break;
                case Keyboard.Key.C:
                    Camera.Center();
                    break;
                case Keyboard.Key.Add:
                    Simulation.TimeScale *= 2;
                    break;
                case Keyboard.Key.Subtract:
                    Simulation.TimeScale /= 2;
                    break;
                case Keyboard.Key.P:
                    Simulation.TogglePause();

                    break;
                case Keyboard.Key.Delete:
                    if (Drawing.SelectedObject != null)
                    {
                        Drawing.SelectedObject.Delete();
                        Drawing.SelectObject(null);
                    }

                    break;
                case Keyboard.Key.Y:
                    Simulation.Player.Velocity = new Vector2f(5, 0);
                    break;
                case Keyboard.Key.T:
                    Simulation.Player.Velocity = new Vector2f(-5, 0);
                    break;
                case Keyboard.Key.G:
                    var debut = DateTime.Now;
                    Simulation.TogglePause();
                    while ((DateTime.Now - debut).TotalSeconds < 1)
                        Render.Window.DispatchEvents();
                    Simulation.TogglePause();
                    break;
                case Keyboard.Key.S:
                    Simulation.UpdatePhysics(true);
                    break;
            }
        }

        private static void Window_Resized(object sender, SizeEventArgs e)
        {
            //SetZoom(CameraZoom * e.Width / Width);
            Render.Width = e.Width;
            Render.Height = e.Height;
            Camera.CalculateWindow();
            Render.ResizeTextures();
        }

        private static void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
