using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using phytestcs.Interface;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using static phytestcs.Global;
using Object = phytestcs.Objects.Object;

namespace phytestcs
{
    public static class Render
    {
        public static RectangleShape DrawRectangle = new RectangleShape();
        public static CircleShape DrawCircle = new CircleShape();
        public static Sprite DrawSprite = new Sprite {Scale=new Vector2f(0.5f, 0.5f)};

        public static int NumRays = 0;
        public static Text Statistics;
        private static readonly Text PauseText = new Text("EN PAUSE", UI.Font, 20){FillColor = Color.Red};

        private static readonly Text txtScale = new Text("", UI.Font, 15){FillColor = Color.White, OutlineColor = Color.Black, OutlineThickness = 1f};
        private static readonly Text txtXAxis = new Text("x", UI.Font, 15);
        private static readonly Text txtYAxis = new Text("y", UI.Font, 15);

        public static uint Width = 900;
        public static uint Height = 550;
        public static RenderWindow Window;

        public static uint _rotCirclePointCount = 360;

        public static readonly Vector2f[] _rotCirclePoints = (
            from i in Enumerable.Range(0, (int) _rotCirclePointCount)
            let angle = i * 2 * Math.PI / _rotCirclePointCount
            select new Vector2f((float)Math.Cos(angle), (float)Math.Sin(angle))
        ).ToArray();

        public static Object[] WorldCache = null;

        static Render()
        {
            ResizeTextures();
        }

        [ObjProp("Afficher les forces")]
        public static bool ShowForces { get; set; } = true;

        [ObjProp("Afficher les valeurs des forces")]
        public static bool ShowForcesValues { get; set; } = false;

        [ObjProp("Afficher les composantes des forces")]
        public static bool ShowForcesComponents { get; set; } = false;

        [ObjProp("Échelle des forces", "m/N")]
        public static float ForcesScale { get; set; } = 0.50f;

        public static bool ShowGrid { get; set; } = true;

        [ObjProp("Show gravity field")]
        public static bool ShowGravityField { get; set; } = false;

        public static Vector2f WindowF => new Vector2f(Width, Height);

        private static void DrawGrid()
        {
            var (f, r) = CalculateRuler(10);
            var fd = (decimal) f;

            var start = new Vector2i(0, 0).ToWorld();
            var end = Window.Size.I().ToWorld();

            var lines = new List<Vertex>();

            (int, byte) thickness(decimal coord)
            {
                var w = 3;
                byte a = 40;
                if (Math.Abs(coord) < fd)
                {
                    w = (int)Math.Round(6 / Camera.Zoom * 45 / f);
                    a = 160;
                }
                else if (coord % (5 * fd) == 0)
                {
                    w = coord % (10 * fd) == 0 ? 9 : 6;
                    a = 80;
                }

                return (w, a);
            }

            for (var x = Math.Round((decimal)start.X / fd) * fd; x < (decimal)end.X; x += fd / 100)
            {
                if (x % fd != 0)
                    continue;
                
                var (w, a) = thickness(x);
                lines.AddRange(Tools.VertexLine(new Vector2f((float)x, start.Y), new Vector2f((float)x, end.Y), new Color(255, 255, 255, a), w * f / 100, false));
            }

            for (var y = Math.Round((decimal)start.Y / fd) * fd; y > (decimal)end.Y; y -= fd / 100)
            {
                if (y % fd != 0)
                    continue;

                var (h, a) = thickness(y);
                lines.AddRange(Tools.VertexLine(new Vector2f(start.X, (float)y), new Vector2f(end.X, (float)y), new Color(255, 255, 255, a), h * f / 100, true));
            }

            Window.Draw(lines.ToArray(), PrimitiveType.Quads, new RenderStates(BlendMode.Alpha));
        }

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
            Window.Clear(Program.CurrentPalette.SkyColor);
            NumRays = 0;

            foreach (var obj in WorldCache)
            {
                obj.Draw();
            }

            if (ShowGrid)
                DrawGrid();

            foreach (var obj in WorldCache)
            {
                obj.DrawOverlay();
            }
        }

        public static void DrawStatistics()
        {
            var ecin = Enumerable.Sum<Object>(Simulation.WorldCache, o => (o as PhysicalObject)?.LinearKineticEnergy ?? 0);
            var epes = Enumerable.Sum<Object>(Simulation.WorldCache, o => (o as PhysicalObject)?.GravityEnergy ?? 0);
            var eela = Enumerable.Sum<Object>(Simulation.WorldCache, o => (o as Spring)?.ElasticEnergy ?? 0);

            var epot = epes + eela;
            var etot = epot + ecin;

            var mpos = Mouse.GetPosition(Window).ToWorld();

            Statistics.DisplayedString =
                $@"
{Simulation.FPS,4:#} fps  (x{Simulation.TimeScale:F4}) {L["Zoom"]} {Camera.Zoom,5:F1}
{(Simulation.Pause ? "-" : Simulation.UPS.ToString("#")),4} Hz / {Simulation.TargetUPS,4:#} Hz ({L["physics"]}) - {L["simulation"]} : {(Simulation.PauseA == default ? "-" : TimeSpan.FromSeconds(Simulation.SimDuration).ToString())}
Caméra = ({Camera.GameView.Center.X,6:F2} ; {Camera.GameView.Center.Y,6:F2})
Souris = ({mpos.X,6:F2} ; {mpos.Y,6:F2})
{WorldCache.Length,5} {L["objects"]}, {NumRays,5} {L["rays"]}
";
            if (Drawing.SelectedObject == null)
            {
                Statistics.DisplayedString += L["No selected object"];
            }
            else
            {
                Statistics.DisplayedString +=
                    $@"
ID          = {Drawing.SelectedObject.ID}
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
                    case Laser laser:
                    {
                        Statistics.DisplayedString +=
                            $@"
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

        private static (float factor, float ruler) CalculateRuler(int min=30, int max=300)
        {
            float factor = 1;
            float ruler;

            while (true)
            {
                ruler = Camera.Zoom * factor;
                if (ruler < 0)
                {
                    Debug.Assert(false);
                }
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

        public static void DrawAxes(Vector2f pos, float axis=30, float tri=4, float angle=0)
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
                Window.Draw(new[]{
                    new Vertex(V(d, d), col),
                    new Vertex(V(d + axis, d), col), 

                    new Vertex(V(d, d), col), 
                    new Vertex(V(d, d - axis), col) 
                }, PrimitiveType.Lines);

                Window.Draw(new []
                {
                    new Vertex(V(d + axis, d - tri), col),
                    new Vertex(V(d + axis, d + tri), col),
                    new Vertex(V(d + axis + tri, d), col),

                    new Vertex(V(d - tri, - axis), col),
                    new Vertex(V(d      , - axis - tri), col),
                    new Vertex(V(d + tri, - axis), col)
                }, PrimitiveType.Triangles);

                d--;
            }
        }

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
                }, PrimitiveType.Lines);

                d--;
            }
            
            DrawAxes(new Vector2f(margin, Height - margin), 30);

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

        public static void ResizeTextures()
        {
            //TracersTexture = new RenderTexture(Width, Height, Window.Settings);
        }

        public static void DrawRotation()
        {
            if (Program._rotating)
            {
                Program._rotCircle.OutlineThickness = 4 / Camera.Zoom;
                Window.Draw(Program._rotCircle);

                var curAngle = ((IRotatable) Drawing.SelectedObject).Angle;
                var thick = 4 / Camera.Zoom;

                Window.Draw(
                    Tools.CircleOutline(
                        Program._rotCircle.Position,
                        Program._rotCircle.Radius + Program._rotCircle.OutlineThickness,
                        thick,
                        new Color(255, 255, 255, 100),
                        curAngle));

                Window.Draw(
                    Tools.CircleSector(
                        Program._rotCircle.Position,
                        (Mouse.GetPosition(Window).ToWorld() - Program._rotCircle.Position).Norm(),
                        new Color(255, 0, 255, 100),
                        Program._rotDeltaAngle,
                        Program._rotStartAngle));
                
                Window.SetView(Camera.MainView);
                DrawAxes(Program._rotCircle.Position.ToScreen().F(), 20, angle: curAngle);
                Program._rotText.DisplayedString = $"{curAngle.Degrees():0.#}°";
                Window.Draw(Program._rotText);
                Window.SetView(Camera.GameView);
            }
        }
    }
}
