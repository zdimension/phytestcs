using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using phytestcs.Interface;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using static phytestcs.Tools;
using static phytestcs.Interface.Ui;

namespace phytestcs
{
    public static class Program
    {
        public const float DoubleClickTime = 500; // millisecondes

        private static (DateTime, Vector2i) _lastMove;

        public static bool Rotating;
        public static bool Moving;
        private static float _rotatingAngle;

        public static CircleShape RotCircle = new CircleShape(0, Render.RotCirclePointCount)
            { FillColor = Color.Transparent, OutlineColor = new Color(255, 255, 255, 180) };

        public static Text RotText = new Text("", Ui.Font, 18)
            { FillColor = Color.White, OutlineColor = Color.Black, OutlineThickness = 1f };

        public static float RotDeltaAngle;
        public static float RotStartAngle;

        private static Vector2f _cameraMoveVel;

        public static Force MoveForce = new Force(ForceType.User, new Vector2f(0, 0), default);

        public static int NumRays = 100;

        public static Palette CurrentPalette;

        [STAThread]
        private static void Main(string[] args)
        {
            CurrentPalette = Palette.Default;

            Render.Window = new RenderWindow(new VideoMode(Render.Width, Render.Height), "physics", Styles.Default,
                new ContextSettings { AntialiasingLevel = 4 });
            //Render.Window.SetVerticalSyncEnabled(true);
            //Window.SetFramerateLimit(240);


            Init();
            Simulation.Pause = false;
            Simulation.TogglePause();

            Camera.Center();
            Camera.CalculateWindow();

            Render.Statistics = new Text("", Ui.Font, 14);

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

                    var delta = (Simulation.TargetDt - sw.Elapsed.TotalSeconds) * 1000 * 0.975f;
                    if (delta > 0)
                        Thread.Sleep((int) delta);
                }
            });
            /*var tmrPhy = new System.Timers.Timer() {Interval = 10};
            tmrPhy.Elapsed += (s, e) => { Simulation.UpdatePhysics(); };*/

            var lastUpd = DateTime.Now;

            Task.Run(() =>
            {
                Scene.Load(Scene.LoadScript(args.Length > 0 ? args[0] : "scenes/boing.csx")).Wait();
                //tmrPhy.Start();
                if (!thrPhy.IsAlive)
                    thrPhy.Start();
            });

            var txl = new Text(L["Loading..."], Ui.Font, 32) { FillColor = Color.White };
            txl.Origin = txl.GetLocalBounds().Size() / 2;

            while (Render.Window.IsOpen)
            {
                Render.Window.DispatchEvents();

                if (Scene.Loaded)
                {
                    Render.WorldCache = Simulation.World.ToArrayLocked();

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

                    Gui.Draw();

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

                var dt = (float) (DateTime.Now - lastUpd).TotalSeconds;

                if (_cameraMoveVel != default)
                {
                    Camera.GameView.Center += _cameraMoveVel.InvertY() * dt / Camera.Zoom;
                    _cameraMoveVel *= (float) Math.Exp(-3 * dt);
                    if (_cameraMoveVel.Norm() < 0.001f)
                        _cameraMoveVel = default;
                }

                Simulation.Fps = 1 / dt;

                lastUpd = DateTime.Now;
            }
        }

        private static void Window_MouseMoved(object? sender, MouseMoveEventArgs e)
        {
            if (!Rotating && !Moving && Mouse.IsButtonPressed(Mouse.Button.Right) &&
                ObjectAtPosition(ClickPosition) is { } obj)
            {
                Drawing.SelectObject(obj);
                if (obj is PhysicalObject phy)
                    phy.UserFix = true;
                Rotating = true;
                _rotatingAngle = obj.Angle;
                RotCircle.Radius = 135 / Camera.Zoom;
                RotCircle.CenterOrigin();
                RotCircle.Position = obj.Position;
                RotText.Position = obj.Position.ToScreen().F();
                RotStartAngle = (ClickPosition.ToWorld() - obj.Position).Angle();
            }

            if (Rotating)
            {
                var rotObj = Drawing.SelectedObject;
                if (rotObj != null)
                {
                    var rotCur = e.Position().ToWorld() - rotObj.Position;
                    RotDeltaAngle = rotCur.Angle() - RotStartAngle;
                    var newAng = _rotatingAngle + RotDeltaAngle;
                    if (rotCur.Norm() < RotCircle.Radius)
                    {
                        newAng = ((float) (15 * Math.Round(newAng.Degrees() / 15))).Radians();
                        RotDeltaAngle = newAng - _rotatingAngle;
                    }

                    rotObj.Angle = newAng;
                    
                    Simulation.UpdatePhysicsInternal(0);
                }
            }

            if (!Rotating && Camera.CameraMoveOrigin != null &&
                (Mouse.IsButtonPressed(Mouse.Button.Right) || Mouse.IsButtonPressed(Mouse.Button.Middle)))
            {
                Moving = true;
                Camera.GameView.Center = Camera.CameraMoveOrigin.Value +
                                         (ClickPosition - e.Position()).F().InvertY() / Camera.Zoom;
                _lastMove = (DateTime.Now, e.Position());
            }

            if (Drawing.DragObject != null && Mouse.IsButtonPressed(Mouse.Button.Left))
            {
                var world = e.Position().ToWorld();
                
                if (Render.GridSnappingActive)
                {
                    var (f, r) = Render.CalculateRuler(Camera.Zoom);
                    world = world.RoundTo(f);
                }
                
                if (Drawing.DrawMode == DrawingType.Move)
                {
                    Drawing.DragObject.Position = world - Drawing.DragObjectRelPosDirect;
                    Simulation.UpdatePhysicsInternal(0);
                }
                else if (Drawing.DragSpring != null)
                {
                    Drawing.DragSpring.End2.RelPos = world;
                    if (Drawing.DrawMode == DrawingType.Spring)
                        Drawing.DragSpring.TargetLength = Drawing.DragSpring.Delta.Norm();
                }
            }

            Drawing.DragSpring?.UpdatePhysics(0);
        }

        private static void Window_MouseWheelScrolled(object? sender, MouseWheelScrollEventArgs e)
        {
            var factor = 1 + Camera.ZoomDelta;
            if (e.Delta < 0)
                factor = 1 / factor;

            var dz = 1 / Camera.Zoom - 1 / (Camera.Zoom * factor);

            Camera.SetZoom(factor, Camera.GameView.Center + dz * (e.Position().F() - Render.WindowF / 2).InvertY());
        }

        private static void Window_DoubleClick()
        {
            if (Drawing.SelectedObject != null)
                OpenProperties(Drawing.SelectedObject, LastClick.F());
        }

        public static void Window_MouseButtonReleased(Vector2i pos, Mouse.Button btn)
        {
            var moved = pos != ClickPosition;

            if (!moved && !Rotating)
                Drawing.SelectObject(ObjectAtPosition(pos));

            if (btn == Mouse.Button.Left)
            {
                if (pos == LastClick && (DateTime.Now - MouseDownTime).TotalMilliseconds < DoubleClickTime)
                    Window_DoubleClick();

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
                    FinishDrawing();
            }

            if (btn == Mouse.Button.Right)
            {
                if (Rotating)
                {
                    Rotating = false;
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
                        _cameraMoveVel = dp.F() / (float) dt.TotalSeconds;
                    }
                    else
                    {
                        var obj = ObjectAtPosition(pos);
                        if (obj != null)
                            OpenProperties(obj, pos.F());
                    }
                }
            }

            Camera.CameraMoveOrigin = null;
            _lastMove = default;

            ClickPosition = default;

            LastClick = pos;
        }

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
                        var obj = ObjectAtPosition(pos);

                        Drawing.DragObject = obj;

                        if (obj != null)
                        {
                            var posW = pos.ToWorld();
                            
                            if (Drawing.DrawMode == DrawingType.Spring && Render.GridSnappingActive)
                            {
                                var (f, r) = Render.CalculateRuler(Camera.Zoom);
                                posW = posW.RoundTo(f);
                            }
                            
                            Drawing.DragObjectRelPos = obj.MapInv(posW);
                            Drawing.DragObjectRelPosDirect = posW - obj.Position;

                            if (obj is PhysicalObject phy)
                            {
                                if (Drawing.DrawMode == DrawingType.Move)
                                    phy.IsMoving = true;
                                else
                                    Simulation.Add(Drawing.DragSpring =
                                        new Spring(Drawing.DragConstant, 0, DefaultSpringSize,
                                            phy, Drawing.DragObjectRelPos, null,
                                            pos.ToWorld(),
                                            ForceType.Drag) { Damping = 1 });
                            }
                        }
                    }

                    break;
                case Mouse.Button.Right:
                case Mouse.Button.Middle:
                    Camera.CameraMoveOrigin = Camera.GameView.Center;
                    _cameraMoveVel = default;
                    _lastMove = (DateTime.Now, pos);
                    Moving = false;
                    Rotating = false;
                    break;
            }
        }
        
        private static float MinDrawingArea => 16 / (Camera.Zoom * Camera.Zoom);
        private static float MinDrawingRadius => 4 / Camera.Zoom;

        private static void FinishDrawing()
        {
            var mouse = Mouse.GetPosition(Render.Window);
            var mouseW = mouse.ToWorld();
            if (Render.GridSnappingActive)
            {
                var (f, r) = Render.CalculateRuler(Camera.Zoom);
                mouseW = mouseW.RoundTo(f);
            }
            var moved = mouse != ClickPosition;
            BaseObject? added = null;
            switch (Drawing.DrawMode)
            {
                case DrawingType.Rectangle when moved:
                    if (Render.DrawRectangle.Size.Area() > MinDrawingArea)
                        added = Drawing.SelectObject(Simulation.Add(new Box(
                            Render.DrawRectangle.Position + Render.DrawRectangle.Size / 2,
                            new RectangleShape(Render.DrawRectangle))));
                    break;
                case DrawingType.Circle when moved:
                    if (Render.DrawCircle.Radius > MinDrawingRadius)
                        added = Drawing.SelectObject(Simulation.Add(new Circle(
                            Render.DrawCircle.Position + Render.DrawCircle.GetLocalBounds().Size() / 2,
                            new CircleShape(Render.DrawCircle))));
                    break;
                case DrawingType.Spring:
                {
                    if (moved && Drawing.DragSpring != null)
                    {
                        var obj = PhysObjectAtPosition(mouse);

                        if (obj != Drawing.DragObject)
                        {
                            PhysicalObject? obj2;
                            Vector2f obj2Pos;
                            if (obj != null)
                            {
                                obj2 = obj;
                                obj2Pos = obj.MapInv(mouseW);
                            }
                            else
                            {
                                obj2 = null;
                                obj2Pos = mouseW;
                            }

                            added = Simulation.Add(new Spring(Drawing.DragSpring.Constant,
                                Drawing.DragSpring.TargetLength, DefaultSpringSize,
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
                            added = Simulation.Add(new Fixate(obj, obj.MapInv(mouseW), DefaultObjectSize));
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
                            var obj2Pos = mouseW;
                            if (obj2 != null)
                                obj2Pos = obj2.MapInv(obj2Pos);
                            added = Simulation.Add(
                                new Hinge(DefaultSpringSize, obj, obj.MapInv(mouseW), obj2, obj2Pos));
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
                            added = Simulation.Add(new Tracer(obj, obj.MapInv(mouseW), DefaultObjectSize,
                                new Color(RandomColor()) { A = 255 }));
                    }

                    break;
                }
                case DrawingType.Thruster:
                {
                    if (!moved)
                    {
                        var obj = PhysObjectAtPosition(mouse);

                        if (obj != null)
                            added = Simulation.Add(new Thruster(obj, obj.MapInv(mouseW), DefaultObjectSize));
                    }

                    break;
                }
                case DrawingType.Laser:
                {
                    if (!moved)
                    {
                        var obj = PhysObjectAtPosition(mouse);
                        var pos = mouseW;

                        if (obj != null)
                            pos = obj.MapInv(pos);

                        added = Simulation.Add(new Laser(obj, pos, DefaultObjectSize));
                    }

                    break;
                }
            }

            added?.UpdatePhysics(0);
        }

        private static void Window_KeyReleased(object? sender, KeyEventArgs e)
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

        private static void Window_KeyPressed(object? sender, KeyEventArgs e)
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

                case Keyboard.Key.Add:
                    Simulation.TimeScale *= 2;
                    break;
                case Keyboard.Key.Subtract:
                    Simulation.TimeScale /= 2;
                    break;
            }

            if (e.Control)
                switch (e.Code)
                {
                    case Keyboard.Key.Delete:
                        if (Drawing.SelectedObject != null)
                        {
                            Drawing.SelectedObject.Delete();
                            Drawing.SelectObject(null);
                        }

                        break;
                    case Keyboard.Key.S:
                        Simulation.UpdatePhysics(true);
                        break;
                    case Keyboard.Key.U:
                        NumRays--;
                        break;
                    case Keyboard.Key.I:
                        NumRays++;
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
                    case Keyboard.Key.P:
                        Simulation.TogglePause();
                        break;
                    case Keyboard.Key.C:
                        Camera.Center();
                        break;
                    case Keyboard.Key.R:
                        Simulation.Player.Position = new Vector2f(0, 1);
                        break;
                }
        }

        private static void Window_Resized(object? sender, SizeEventArgs e)
        {
            //SetZoom(CameraZoom * e.Width / Width);
            Render.Width = e.Width;
            Render.Height = e.Height;
            Camera.CalculateWindow();
            Render.ResizeTextures();
        }

        private static void Window_Closed(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}