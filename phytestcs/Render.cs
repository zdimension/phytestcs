using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using phytestcs.Interface;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using static phytestcs.Tools;

namespace phytestcs
{
    public static class Render
    {
        public const uint RotCirclePointCount = 360;
        public static RectangleShape DrawRectangle = new RectangleShape();
        public static CircleShape DrawCircle = new CircleShape(1, RotCirclePointCount);
        public static Sprite DrawSprite = new Sprite { Scale = new Vector2f(0.5f, 0.5f) };

        public static int NumRays;
        public static Text Statistics = null!;
        private static readonly Text PauseText = new Text("EN PAUSE", Ui.Font, 20) { FillColor = Color.Red };

        private static readonly Text TxtScale = new Text("", Ui.Font, 15)
            { FillColor = Color.White, OutlineColor = Color.Black, OutlineThickness = 1f };

        private static readonly Text TxtXAxis = new Text("x", Ui.Font, 15);
        private static readonly Text TxtYAxis = new Text("y", Ui.Font, 15);

        public static uint Width = 900;
        public static uint Height = 550;
        public static RenderWindow Window = null!;

        public static readonly Vector2f[] RotCirclePoints = (
            from i in Enumerable.Range(0, (int) RotCirclePointCount)
            let angle = i * 2 * Math.PI / RotCirclePointCount
            select new Vector2f((float) Math.Cos(angle), (float) Math.Sin(angle))
        ).ToArray();

        public static BaseObject[] WorldCache = null!;

        static Render()
        {
            ResizeTextures();
        }

        [ObjProp("Show forces")]
        public static bool ShowForces { get; set; } = true;

        [ObjProp("Show values")]
        public static bool ShowForcesValues { get; set; } = false;

        [ObjProp("Show components")]
        public static bool ShowForcesComponents { get; set; } = false;

        [ObjProp("Force arrows scale", "m/N")]
        public static float ForcesScale { get; set; } = 0.50f;

        public static bool ShowGrid { get; set; } = true;

        public static bool SnapToGrid { get; set; } = true;

        public static bool GridSnappingActive => ShowGrid && SnapToGrid;

        [ObjProp("Show gravity field")]
        public static bool ShowGravityField { get; set; } = false;

        public static Vector2f WindowF => new Vector2f(Width, Height);

        private static void DrawGrid()
        {
            var (f, _) = CalculateRuler(Camera.Zoom, 10);
            var fd = (decimal) f;

            var start = new Vector2i(0, 0).ToWorld();
            var end = Window.Size.I().ToWorld();

            var lines = new List<Vertex>();

            (int, byte) Thickness(decimal coord)
            {
                var w = 3;
                byte a = 40;
                if (Math.Abs(coord) < fd)
                {
                    w = (int) Math.Round(6 / Camera.Zoom * 45 / f);
                    a = 160;
                }
                else if (coord % (5 * fd) == 0)
                {
                    w = coord % (10 * fd) == 0 ? 9 : 6;
                    a = 80;
                }

                return (w, a);
            }

            for (var x = Math.Round((decimal) start.X / fd) * fd; x < (decimal) end.X; x += fd / 100)
            {
                if (x % fd != 0)
                    continue;

                var (w, a) = Thickness(x);
                lines.AddRange(VertexLine(new Vector2f((float) x, start.Y), new Vector2f((float) x, end.Y),
                    new Color(255, 255, 255, a), w * f / 100));
            }

            for (var y = Math.Round((decimal) start.Y / fd) * fd; y > (decimal) end.Y; y -= fd / 100)
            {
                if (y % fd != 0)
                    continue;

                var (h, a) = Thickness(y);
                lines.AddRange(VertexLine(new Vector2f(start.X, (float) y), new Vector2f(end.X, (float) y),
                    new Color(255, 255, 255, a), h * f / 100));
            }

            Window.Draw(lines.ToArray(), PrimitiveType.Quads, new RenderStates(BlendMode.Alpha));
        }

        public static void DrawDrawing()
        {
            var mouse = Mouse.GetPosition(Window);
            var offset = new Vector2f(13, 13);
            var press = Mouse.IsButtonPressed(Mouse.Button.Left) && Ui.ClickPosition != default;

            DrawSprite.Position = mouse.F() + offset;
            Window.Draw(DrawSprite);

            var clickPos = Ui.ClickPosition.ToWorld();
            var mousePos = mouse.ToWorld();

            if (GridSnappingActive)
            {
                var (f, r) = CalculateRuler(Camera.Zoom);
                clickPos = clickPos.RoundTo(f);
                mousePos = mousePos.RoundTo(f);
            }

            if (press)
                switch (Drawing.DrawMode)
                {
                    case DrawingType.Rectangle:
                    {
                        var corner1 = new Vector2f(Math.Min(clickPos.X, mousePos.X),
                            Math.Max(clickPos.Y, mousePos.Y));
                        var corner2 = new Vector2f(Math.Max(clickPos.X, mousePos.X),
                            Math.Min(clickPos.Y, mousePos.Y));
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
                        DrawCircle.FillColor = Drawing.DrawColor;
                        DrawCircle.Radius = (mousePos - clickPos).Norm();
                        DrawCircle.Position = clickPos - new Vector2f(DrawCircle.Radius, DrawCircle.Radius);
                        Window.SetView(Camera.GameView);
                        Window.Draw(DrawCircle);
                        Window.SetView(Camera.MainView);

                        break;
                    }
                }
        }

        public static void DrawGame()
        {
            Window.Clear(Program.CurrentPalette.SkyColor);
            NumRays = 0;

            foreach (var obj in WorldCache)
                obj.Draw();

            if (ShowGrid)
                DrawGrid();

            foreach (var obj in WorldCache)
                obj.DrawOverlay();
        }

        public static void DrawStatistics()
        {
            var ecin = Simulation.WorldCache.Sum(o => (o as PhysicalObject)?.LinearKineticEnergy ?? 0);
            var epes = Simulation.WorldCache.Sum(o => (o as PhysicalObject)?.GravityEnergy ?? 0);
            var eela = Simulation.WorldCache.Sum(o => (o as Spring)?.ElasticEnergy ?? 0);

            var epot = epes + eela;
            var etot = epot + ecin;

            var mpos = Mouse.GetPosition(Window).ToWorld();

            Statistics.FillColor = Program.CurrentPalette.SelectionColor;
            Statistics.DisplayedString =
                $@"
{Simulation.Fps,4:#} fps  (x{Simulation.TimeScale:F4}) {L["Zoom"]} {Camera.Zoom,5:F1}
{(Simulation.Pause ? "-" : Simulation.Ups.ToString("#", CultureInfo.CurrentCulture)),4} Hz / {Simulation.TargetUps,4:#} Hz ({L["physics"]}) - {L["Simulation"]} : {(Simulation.PauseA == default ? "-" : TimeSpan.FromSeconds(Simulation.SimDuration).ToString())}
Caméra = ({Camera.GameView.Center.X,6:F2} ; {Camera.GameView.Center.Y,6:F2})
Souris = ({mpos.X,6:F2} ; {mpos.Y,6:F2})
{WorldCache.Length,5} {L["objects"]}, {NumRays,5} / {Program.NumRays,5} {L["rays"]}
";
            if (Drawing.SelectedObject == null)
            {
                Statistics.DisplayedString += L["No selected object"];
            }
            else
            {
                Statistics.DisplayedString +=
                    $@"
ID          = {Drawing.SelectedObject.Id}
";

                switch (Drawing.SelectedObject)
                {
                    case PhysicalObject objPhy:
                    {
                        Statistics.DisplayedString +=
                            $@"
P = {objPhy.Position.DisplayPoint()}
V = {objPhy.Velocity.Display()}
P0 = {objPhy.GlobalPointsCache[0].DisplayPoint()}
Vp0 = {objPhy.SpeedAtPoint(objPhy.MapInv(objPhy.GlobalPointsCache[0])).Display()}
θ = {objPhy.Angle,7:F2} rad
ω = {objPhy.AngularVelocity,7:F2} rad/s
m = {objPhy.Mass,7:F2} kg
{L["Forces"]} :
R = {objPhy.NetForce.Display()}
";
                        foreach (var force in objPhy.Forces.ToArrayLocked())
                            Statistics.DisplayedString +=
                                $"  - {force.Value.Display()} (TTL={force.TimeToLive,4:F3}) {force.Type.Name}\n";

                        Statistics.DisplayedString +=
                            $@"
{L["Torques"]} :
R = {objPhy.NetTorque,7:F2} 
";

                        break;
                    }
                    case Laser laser:
                    {
                        Statistics.DisplayedString +=
                            @"
Laser
Rayons :
";
                        var rays = laser.Rays.ToArrayLocked();
                        for (var index = 0; index < rays.Length; index++)
                        {
                            var ray = rays[index];
                            Statistics.DisplayedString +=
                                $"  - {index,3} ({ray.Start.DisplayPoint()}) θ={ray.Angle.Degrees(),6:F1}° L={ray.Length,6:F3}m de {(ray.Source == null ? -1 : Array.IndexOf(rays, ray.Source))} {ray.DebugInfo ?? ""}\n";
                        }

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

        public static (float factor, float ruler) CalculateRuler(float zoom, int min = 30, int max = 300)
        {
            float factor = 1;
            float ruler;

            while (true)
            {
                ruler = zoom * factor;
                if (ruler < 0)
                    Debug.Assert(false);

                if (ruler < min)
                    factor *= 10;
                else if (ruler > max)
                    factor /= 10;
                else
                    break;
            }

            return (factor, ruler);
        }

        public static void DrawGravityField()
        {
            for (var x = 15; x < Width; x += 50)
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
                Window.Draw(new[]
                {
                    new Vertex(pos + trans.TransformPoint(new Vector2f(0, 2)), white),
                    new Vertex(pos + trans.TransformPoint(new Vector2f(30, 2)), twhite),
                    new Vertex(pos + trans.TransformPoint(new Vector2f(30, -2)), twhite),
                    new Vertex(pos + trans.TransformPoint(new Vector2f(0, -2)), white)
                }, PrimitiveType.Quads);
            }
        }

        private static void DrawAxes(Vector2f pos, float axis = 30, float tri = 4, float angle = 0)
        {
            var d = 1;
            var tr = Transform.Identity;
            tr.Rotate(-angle.Degrees());

            Vector2f V(float x, float y)
            {
                return tr.TransformPoint(new Vector2f(x, y)) + pos;
            }

            foreach (var col in new[] { Color.Black, Color.White })
            {
                Window.Draw(new[]
                {
                    new Vertex(V(d, d), col),
                    new Vertex(V(d + axis, d), col),

                    new Vertex(V(d, d), col),
                    new Vertex(V(d, d - axis), col)
                }, PrimitiveType.Lines);

                Window.Draw(new[]
                {
                    new Vertex(V(d + axis, d - tri), col),
                    new Vertex(V(d + axis, d + tri), col),
                    new Vertex(V(d + axis + tri, d), col),

                    new Vertex(V(d - tri, -axis), col),
                    new Vertex(V(d, -axis - tri), col),
                    new Vertex(V(d + tri, -axis), col)
                }, PrimitiveType.Triangles);

                d--;
            }
        }

        public static void DrawLegend()
        {
            var margin = 30;
            var (f, r) = CalculateRuler(Camera.Zoom);
            var axis = 30;

            var d = 1;
            foreach (var col in new[] { Color.Black, Color.White })
            {
                Window.Draw(new[]
                {
                    new Vertex(new Vector2f(d + Width - margin, d + Height - margin), col),
                    new Vertex(new Vector2f(d + Width - margin - r, d + Height - margin), col),
                    new Vertex(new Vector2f(d + Width - margin - r, d + Height - margin - 5), col),
                    new Vertex(new Vector2f(d + Width - margin - r, d + Height - margin + 5), col),
                    new Vertex(new Vector2f(d + Width - margin, d + Height - margin - 5), col),
                    new Vertex(new Vector2f(d + Width - margin, d + Height - margin + 5), col)
                }, PrimitiveType.Lines);

                d--;
            }

            DrawAxes(new Vector2f(margin, Height - margin));

            TxtScale.Position = new Vector2f(Width - margin - 3, Height - margin - 25);
            TxtScale.Origin = new Vector2f(TxtScale.GetLocalBounds().Width, 0);
            TxtScale.DisplayedString = $"{f,5:F1} m";

            Window.Draw(TxtScale);

            TxtXAxis.Position = new Vector2f(margin + axis - 5, Height - margin + 5);
            TxtYAxis.Position = new Vector2f(margin - 5 - 7, Height - margin - axis);

            TxtXAxis.FillColor = Color.Black;
            Window.Draw(TxtXAxis);
            TxtXAxis.Position -= new Vector2f(1, 1);
            TxtXAxis.FillColor = Color.White;
            Window.Draw(TxtXAxis);

            TxtYAxis.FillColor = Color.Black;
            Window.Draw(TxtYAxis);
            TxtYAxis.Position -= new Vector2f(1, 1);
            TxtYAxis.FillColor = Color.White;
            Window.Draw(TxtYAxis);
        }

        public static void ResizeTextures()
        {
            //
        }

        public static void DrawRotation()
        {
            if (Program.Rotating)
            {
                Program.RotCircle.OutlineThickness = 4 / Camera.Zoom;
                Window.Draw(Program.RotCircle);

                var curAngle = Drawing.SelectedObject!.Angle;
                var thick = 4 / Camera.Zoom;

                Window.Draw(
                    CircleOutline(
                        Program.RotCircle.Position,
                        Program.RotCircle.Radius + Program.RotCircle.OutlineThickness,
                        thick,
                        new Color(255, 255, 255, 100),
                        curAngle));

                Window.Draw(
                    CircleSector(
                        Program.RotCircle.Position,
                        (Mouse.GetPosition(Window).ToWorld() - Program.RotCircle.Position).Norm(),
                        new Color(255, 0, 255, 100),
                        Program.RotDeltaAngle,
                        Program.RotStartAngle));

                Window.SetView(Camera.MainView);
                DrawAxes(Program.RotCircle.Position.ToScreen().F(), 20, angle: curAngle);
                Program.RotText.DisplayedString = $"{curAngle.Degrees():0.#}°";
                Window.Draw(Program.RotText);
                Window.SetView(Camera.GameView);
            }
        }
    }
}