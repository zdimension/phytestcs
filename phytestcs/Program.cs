using System;
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

namespace phytestcs
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Render.Window = new RenderWindow(new VideoMode(Render.Width, Render.Height), "jeu", Styles.Default, new ContextSettings(){AntialiasingLevel = 4});
            Render.Window.SetVerticalSyncEnabled(true);
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

            const float maxFPS = 240;
            var minDT = 1000f / maxFPS;
            var thrPhy = new Thread(() =>
            {
                while (true)
                {
                    var avant = DateTime.Now;
                    Simulation.UpdatePhysics();
                    
                    var delta = minDT - (float) (DateTime.Now - avant).TotalMilliseconds;
                    if (delta > 0)
                    {
                        Thread.Sleep((int) delta);
                    }
                }
            });

            var lastUpd = DateTime.Now;

            Task.Run(() =>
            {
                Scene.Load();
                if (!thrPhy.IsAlive)
                    thrPhy.Start();
            });

            var txl = new Text("Chargement...", UI.Font, 32);
            txl.FillColor = Color.White;
            txl.Origin = txl.GetLocalBounds().Size() / 2;

            while (Render.Window.IsOpen)
            {
                Render.Window.DispatchEvents();

                if (Scene.Loaded)
                {
                    lock (Simulation.World.SyncRoot)
                    {
                        Simulation.WorldCache = Simulation.World.ToList();
                    }

                    Camera.UpdateZoom();

                    Render.Window.SetView(Camera.GameView);

                    Render.DrawGame();

                    Render.Window.SetView(Camera.MainView);

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

                Simulation.FPS = (float) (1 / (DateTime.Now - lastUpd).TotalSeconds);

                lastUpd = DateTime.Now;
            }
        }

        private static void Window_MouseMoved(object sender, MouseMoveEventArgs e)
        {
            if (Camera.CameraMoveOrigin != null &&
                (Mouse.IsButtonPressed(Mouse.Button.Right) || Mouse.IsButtonPressed(Mouse.Button.Middle)))
            {
                Camera.GameView.Center = Camera.CameraMoveOrigin.Value +
                                       (ClickPosition - e.Position()).F().InvertY() / Camera.CameraZoom;
            }

            if (Drawing.DragObject != null && Mouse.IsButtonPressed(Mouse.Button.Left))
            {
                if (Drawing.DrawMode == DrawingType.Move)
                {
                    Drawing.DragObject.Position = e.Position().ToWorld() - Drawing.DragObjectRelPos;
                }
                else
                {
                    Drawing.DragSpring.Object2RelPos = e.Position().ToWorld();
                }
            }
        }

        private static void Window_MouseWheelScrolled(object sender, MouseWheelScrollEventArgs e)
        {
            var factor = 1 + Camera.ZoomDelta;
            if (e.Delta < 0)
                factor = 1 / factor;

            var dz = 1 / Camera.CameraZoom - 1 / (Camera.CameraZoom * factor);

            Camera.SetZoom(factor, Camera.GameView.Center + dz * (e.Position().F() - Render.WindowF / 2).InvertY());
        }

        public const float DoubleClickTime = 500; // millisecondes

        public static void Window_DoubleClick()
        {
            if (Drawing.SelectedObject != null)
            {
                OpenProperties(Drawing.SelectedObject, LastClick.F());
            }
        }

        public static void Window_MouseButtonReleased(Vector2i pos, Mouse.Button btn)
        {
            if (pos == ClickPosition)
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
            else if (btn == Mouse.Button.Right && pos == ClickPosition)
            {
                var obj = ObjectAtPosition(pos);
                if (obj != null)
                {
                    OpenProperties(obj, pos.F());
                }
            }

            Camera.CameraMoveOrigin = null;

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
                        var obj = PhysObjectAtPosition(pos);

                        if (obj != null)
                        {
                            Drawing.DragObject = obj;
                            Drawing.DragObjectRelPos = pos.ToWorld() - obj.Position;

                            if (Drawing.DrawMode == DrawingType.Move)
                            {
                                Drawing.DragObject.IsMoving = true;
                            }
                            else
                            {
                                Simulation.World.Add(Drawing.DragSpring =
                                    new Spring(Drawing.DragConstant, 0, Drawing.DragObject, Drawing.DragObjectRelPos, null,
                                        pos.ToWorld(),
                                        "Main"){Damping = 1});
                            }
                        }
                    }

                    break;
                case Mouse.Button.Right:
                case Mouse.Button.Middle:
                    Camera.CameraMoveOrigin = Camera.GameView.Center;
                    break;
            }
        }

        static void FinishDrawing()
        {
            var mouse = Mouse.GetPosition(Render.Window);
            var moved = mouse != ClickPosition;

            if (Drawing.DrawMode == DrawingType.Rectangle && moved)
            {
                Simulation.World.Add(new PhysicalObject(Render.DrawRectangle.Position, new RectangleShape(Render.DrawRectangle)));
            }
            else if (Drawing.DrawMode == DrawingType.Circle && moved)
            {
                Simulation.World.Add(new PhysicalObject(Render.DrawCircle.Position, new CircleShape(Render.DrawCircle)));
            }
            else if (Drawing.DrawMode == DrawingType.Spring)
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
                            obj2Pos = mouse.ToWorld() - obj.Position;
                        }
                        else
                        {
                            obj2 = null;
                            obj2Pos = mouse.ToWorld();
                        }

                        Simulation.World.Add(new Spring(Drawing.DragSpring.Constant,
                            Drawing.DragSpring.TargetLength,
                            Drawing.DragSpring.Object1, Drawing.DragSpring.Object1RelPos, obj2, obj2Pos));
                    }
                }

                Drawing.DragObject = null;
                Drawing.DragObjectRelPos = default;
                Drawing.DragSpring = null;
            }
            else if (Drawing.DrawMode == DrawingType.Fixate)
            {
                if (!moved)
                {
                    var obj = PhysObjectAtPosition(mouse);

                    if (obj != null && !obj.HasFixate)
                    {
                        Simulation.World.Add(new Fixate(obj, mouse.ToWorld() - obj.Position));
                    }
                }
            }
            else if (Drawing.DrawMode == DrawingType.Move)
            {
                if (Drawing.DragObject != null)
                {
                    Drawing.DragObject.IsMoving = false;
                    Drawing.DragObject = null;
                }

                Drawing.DragObjectRelPos = default;
            }
        }

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

        public static Force MoveForce = new Force("Dépl", new Vector2f(0, 0));

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
                    Simulation.Player.Speed = new Vector2f(5, 0);
                    break;
                case Keyboard.Key.T:
                    Simulation.Player.Speed = new Vector2f(-5, 0);
                    break;
                case Keyboard.Key.G:
                    var debut = DateTime.Now;
                    Simulation.TogglePause();
                    while ((DateTime.Now - debut).TotalSeconds < 1)
                        Render.Window.DispatchEvents();
                    Simulation.TogglePause();
                    break;
            }
        }

        private static void Window_Resized(object sender, SizeEventArgs e)
        {
            //SetZoom(CameraZoom * e.Width / Width);
            Render.Width = e.Width;
            Render.Height = e.Height;
            Camera.CalculateWindow();
        }

        private static void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
