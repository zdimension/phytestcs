using System;
using System.Collections.Generic;
using System.Linq;
using phytestcs.Interface;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using static phytestcs.Global;

namespace phytestcs
{
    public static class Render
    {
        [ObjProp("Afficher les forces")]
        public static bool ShowForces { get; set; } = true;
        [ObjProp("Afficher les valeurs des forces")]
        public static bool ShowForcesValues { get; set; } = false;
        [ObjProp("Afficher les composantes des forces")]
        public static bool ShowForcesComponents { get; set; } = false;
        [ObjProp("Échelle des forces", "m/N")]
        public static float ForcesScale { get; set; } = 0.50f;

        private static void DrawGrid()
        {
            var (f, r) = CalculateRuler(10);
            var fd = (decimal) f;

            var start = new Vector2i(0, 0).ToWorld();
            var end = Window.Size.I().ToWorld();

            var lines = new List<Vertex>();

            (int, byte) thickness(decimal coord)
            {
                var w = 1;
                byte a = 0;
                if (Math.Abs(coord) < fd)
                {
                    w = (int)Math.Round(6 / Camera.CameraZoom * 200 / f);
                    a = 255;
                }
                else if (coord % (5 * fd) == 0)
                {
                    w = coord % (10 * fd) == 0 ? 3 : 2;
                }

                if (a == 0)
                {
                    a = (byte)(80 + w * 20);
                }

                return (w, (byte)(a / 2));
            }

            for (var x = Math.Round((decimal)start.X / fd) * fd; x < (decimal)end.X; x += fd / 100)
            {
                if (x % fd != 0)
                    continue;
                
                var (w, a) = thickness(x);
                lines.AddRange(Tools.VertexLine(new Vector2f((float)x, start.Y), new Vector2f((float)x, end.Y), new Color(255, 255, 255, a), w, false, f / 400));
            }

            for (var y = Math.Round((decimal)start.Y / fd) * fd; y > (decimal)end.Y; y -= fd / 100)
            {
                if (y % fd != 0)
                    continue;

                var (h, a) = thickness(y);
                lines.AddRange(Tools.VertexLine(new Vector2f(start.X, (float)y), new Vector2f(end.X, (float)y), new Color(255, 255, 255, a), h, true, f / 400));
            }

            Window.Draw(lines.ToArray(), PrimitiveType.Lines, new RenderStates(BlendMode.Alpha));
        }

        public static RectangleShape DrawRectangle = new RectangleShape();
        public static CircleShape DrawCircle = new CircleShape();
        public static Sprite DrawSprite = new Sprite {Scale=new Vector2f(0.5f, 0.5f)};
        public static void DrawDrawing()
        {
            var mouse = Mouse.GetPosition(Window);
            var offset = new Vector2f(13, 13);
            var press = Mouse.IsButtonPressed(Mouse.Button.Left) && UI.ClickPosition != default;

            DrawSprite.Position = mouse.F() + offset;
            Window.Draw(DrawSprite);

            if (press)
            {
                switch (Drawing.DrawMode)
                {
                    case DrawingType.Rectangle:
                    {
                        var corner1 = new Vector2i(Math.Min(UI.ClickPosition.X, mouse.X),
                            Math.Max(UI.ClickPosition.Y, mouse.Y)).ToWorld();
                        var corner2 = new Vector2i(Math.Max(UI.ClickPosition.X, mouse.X),
                            Math.Min(UI.ClickPosition.Y, mouse.Y)).ToWorld();
                        DrawRectangle.FillColor = Drawing.DrawColor;
                        DrawRectangle.Position = corner1;
                        DrawRectangle.Size = corner2 - corner1;
                        Window.SetView(Camera.GameView);
                        Window.Draw(DrawRectangle);
                        Window.SetView(Camera.MainView);

                        break;
                    }
                    case DrawingType.Circle:
                    {
                        var center = UI.ClickPosition.ToWorld();
                        DrawCircle.FillColor = Drawing.DrawColor;
                        DrawCircle.Radius = (mouse.ToWorld() - center).Norm();
                        DrawCircle.Position = center - new Vector2f(DrawCircle.Radius, DrawCircle.Radius);
                        Window.SetView(Camera.GameView);
                        Window.Draw(DrawCircle);
                        Window.SetView(Camera.MainView);

                        break;
                    }
                }
            }
        }

        public static void DrawGame()
        {
            Window.Clear(Background);

            foreach (var obj in Simulation.WorldCache)
            {
                obj.Draw();
            }

            DrawGrid();

            foreach (var obj in Simulation.WorldCache)
            {
                obj.DrawOverlay();
            }
        }

        public static Text Statistics;
        private static readonly Text PauseText = new Text("EN PAUSE", UI.Font, 20){FillColor = Color.Red};

        public static void DrawStatistics()
        {
            var ecin = Simulation.WorldCache.Sum(o => (o as PhysicalObject)?.LinearKineticEnergy ?? 0);
            var epes = Simulation.WorldCache.Sum(o => (o as PhysicalObject)?.GravityEnergy ?? 0);
            var eela = Simulation.WorldCache.Sum(o => (o as Spring)?.ElasticEnergy ?? 0);

            var epot = epes + eela;
            var etot = epot + ecin;

            var mpos = Mouse.GetPosition(Window).ToWorld();

            Statistics.DisplayedString =
                $@"
{Simulation.FPS,4:#} fps  (x{Simulation.TimeScale:F4}) {L["Zoom"]} {Camera.CameraZoom,5:F1}
{(Simulation.Pause ? "-" : Simulation.UPS.ToString("#")),4} Hz / {Simulation.TargetUPS,4:#} Hz ({L["physics"]}) - {L["simulation"]} : {(Simulation.PauseA == default ? "-" : TimeSpan.FromSeconds(Simulation.SimDuration).ToString())}
Caméra = ({Camera.GameView.Center.X,6:F2} ; {Camera.GameView.Center.Y,6:F2})
Souris = ({mpos.X,6:F2} ; {mpos.Y,6:F2})
{Simulation.WorldCache.Length,5} {L["objects"]}
";
            if (Drawing.SelectedObject == null)
            {
                Statistics.DisplayedString += L["No selected object"];
            }
            else
            {
                Statistics.DisplayedString +=
                    $@"
ID          = {Drawing.SelectedObject.ID}";

                switch (Drawing.SelectedObject)
                {
                    case PhysicalObject objPhy:
                    {
                        Statistics.DisplayedString +=
                            $@"
P = {objPhy.Position.DisplayPoint()}
V = {objPhy.Velocity.Display()}
θ = {objPhy.Angle,7:F2} rad
ω = {objPhy.AngularVelocity,7:F2} rad/s
m       = {objPhy.Mass,7:F2} kg
{L["Forces"]} :
R = {objPhy.NetForce.Display()}
";
                        foreach (var force in objPhy.Forces.ToArrayLocked())
                        {
                            Statistics.DisplayedString +=
                                $"  - {force.Value.Display()} (TTL={force.TimeToLive,4:F3}) {force.Type.Name}\n";
                        }

                        Statistics.DisplayedString +=
                            $@"
{L["Torques"]} :
R = {objPhy.NetTorque,7:F2} 
";

                        break;
                    }
                    default:
                        Statistics.DisplayedString += Drawing.SelectedObject.GetType().Name;
                        break;
                }
            }

            Window.Draw(Statistics);

            if (Simulation.Pause)
            {
                PauseText.Position = new Vector2f((Width - PauseText.GetLocalBounds().Width) / 2f, 20);
                Window.Draw(PauseText);
            }
        }

        private static (float factor, float ruler) CalculateRuler(int min=30, int max=300)
        {
            float factor = 1;
            float ruler;

            while (true)
            {
                ruler = Camera.CameraZoom * factor;

                if (ruler < min)
                    factor *= 10;
                else if (ruler > max)
                    factor /= 10;
                else
                    break;
            }

            return (factor, ruler);
        }

        [ObjProp("Show gravity field")]
        public static bool ShowGravityField { get; set; } = false;

        public static void DrawGravityField()
        {
            for (var x = 15; x < Width; x += 50)
            {
                for (var y = 15; y < Height; y += 50)
                {
                    var world = new Vector2i(x, y).ToWorld();
                    var gravity = Simulation.GravityField(world);
                    if (gravity == default)
                        continue;
                    var pos = new Vector2f(x, y);
                    var twhite = new Color(255, 255, 255, 0);
                    var white = new Color(255, 255, 255, 180);
                    var trans = Transform.Identity;
                    trans.Rotate(-gravity.Angle().Degrees());
                    Window.Draw(new []{
                        new Vertex(pos + trans.TransformPoint(new Vector2f(0, 2)), white),
                        new Vertex(pos + trans.TransformPoint(new Vector2f(30, 2)), twhite),
                        new Vertex(pos + trans.TransformPoint(new Vector2f(30, -2)), twhite),
                        new Vertex(pos + trans.TransformPoint(new Vector2f(0, -2)), white)
                    }, PrimitiveType.Quads);
                }
            }
        }

        private static readonly Text txtScale = new Text("", UI.Font, 15){FillColor = Color.White, OutlineColor = Color.Black, OutlineThickness = 1f};
        private static readonly Text txtXAxis = new Text("x", UI.Font, 15);
        private static readonly Text txtYAxis = new Text("y", UI.Font, 15);

        public static void DrawLegend()
        {
            var margin = 30;
            var (f, r) = CalculateRuler();
            var axis = 30;
            var tri = 4;

            var d = 1;
            foreach (var col in new[] { Color.Black, Color.White })
            {
                Window.Draw(new[]{
                    new Vertex(new Vector2f(d + Width - margin, d + Height - margin), col),
                    new Vertex(new Vector2f(d + Width - margin - r, d + Height - margin), col),
                    new Vertex(new Vector2f(d + Width - margin - r, d + Height - margin - 5), col),
                    new Vertex(new Vector2f(d + Width - margin - r, d + Height - margin + 5), col),
                    new Vertex(new Vector2f(d + Width - margin, d + Height - margin - 5), col),
                    new Vertex(new Vector2f(d + Width - margin, d + Height - margin + 5), col),

                    new Vertex(new Vector2f(d + margin, d + Height - margin), col),
                    new Vertex(new Vector2f(d + margin + axis, d + Height - margin), col), 

                    new Vertex(new Vector2f(d + margin, d + Height - margin), col), 
                    new Vertex(new Vector2f(d + margin, d + Height - margin - axis), col) 
                }, PrimitiveType.Lines);

                Window.Draw(new []
                {
                    new Vertex(new Vector2f(d + margin + axis, d + Height - margin - tri), col),
                    new Vertex(new Vector2f(d + margin + axis, d + Height - margin + tri), col),
                    new Vertex(new Vector2f(d + margin + axis + tri, d + Height - margin), col),

                    new Vertex(new Vector2f(d + margin - tri, d + Height - margin - axis), col),
                    new Vertex(new Vector2f(d + margin      , d + Height - margin - axis - tri), col),
                    new Vertex(new Vector2f(d + margin + tri, d + Height - margin - axis), col)
                }, PrimitiveType.Triangles);

                d--;
            }

            txtScale.Position = new Vector2f(Width - margin - 3, Height - margin - 25);
            txtScale.Origin = new Vector2f(txtScale.GetLocalBounds().Width, 0);
            txtScale.DisplayedString = $"{f,5:F1} m";

            Window.Draw(txtScale);

            txtXAxis.Position = new Vector2f(margin + axis - 5, Height - margin + 5);
            txtYAxis.Position = new Vector2f(margin - 5 - 7, Height - margin - axis);

            txtXAxis.FillColor = Color.Black;
            Window.Draw(txtXAxis);
            txtXAxis.Position -= new Vector2f(1, 1);
            txtXAxis.FillColor = Color.White;
            Window.Draw(txtXAxis);

            txtYAxis.FillColor = Color.Black;
            Window.Draw(txtYAxis);
            txtYAxis.Position -= new Vector2f(1, 1);
            txtYAxis.FillColor = Color.White;
            Window.Draw(txtYAxis);
        }

        public static uint Width = 900;
        public static uint Height = 550;
        public static Vector2f WindowF => new Vector2f(Width, Height);
        public static RenderWindow Window;
        public static Color Background = new Color(0x73, 0x8c, 0xff);
    }
}
