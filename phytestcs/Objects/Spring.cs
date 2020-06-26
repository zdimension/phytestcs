using System;
using phytestcs.Interface;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    [System.Runtime.InteropServices.Guid("28DA2FA2-87F9-4748-A1C2-F43675AB8069")]
    public sealed class Spring : VirtualObject
    {
        public float Constant { get; set; }
        public float TargetLength { get; set; }
        public float Damping { get; set; } = 0.10f;
        public PhysicalObject Object1 { get; }
        public Vector2f Object1RelPos { get; set; }
        public PhysicalObject Object2 { get; }
        public Vector2f Object2RelPos { get; set; }
        private readonly Force _force1;
        private readonly Force _force2;

        public Vector2f Object1AbsPos => Object1.Position + Object1RelPos;
        public Vector2f Object2AbsPos => (Object2?.Position ?? default) + Object2RelPos;

        public Vector2f Delta => Object1AbsPos - Object2AbsPos;

        public bool ShowInfos { get; set; } = true;

        public Spring(float constant, float targetLength, PhysicalObject object1, Vector2f object1RelPos,
            PhysicalObject object2 = null, Vector2f object2RelPos = default, string name = "Ressort")
        {
            Constant = constant;
            TargetLength = targetLength;
            Object1 = object1;
            Object1RelPos = object1RelPos;
            Object2 = object2;
            Object2RelPos = object2RelPos;

            _force1 = new Force(name, new Vector2f(0, 0));
            Object1.Forces.Add(_force1);
            DependsOn(Object1);

            if (Object2 != null)
            {
                _force2 = new Force(name, new Vector2f(0, 0));
                Object2.Forces.Add(_force2);
                DependsOn(Object2);
            }

            UpdatePhysics(0);
        }

        public float DeltaLength => TargetLength - Delta.Norm();

        public float ElasticEnergy => (float) (Constant * Math.Pow(DeltaLength + Speed * Simulation.TargetDT / 2, 2) / 2);

        public float Force
        {
            get
            {
                var force = Constant * DeltaLength;

                if (Damping != 0)
                {
                    force += -Speed * Damping * 20;
                }

                return force;
            }
        }

        public Vector2f UnitVector => Tools.FromPolar(1, Delta.Angle());

        public float Speed
        {
            get
            {
                var unit = UnitVector;

                var dhdt = Object1.Speed.Dot(unit);

                if (Object2 != null)
                    dhdt += Object2.Speed.Dot(-unit);

                return dhdt;
            }
        }

        public override void UpdatePhysics(float dt)
        {
            var unit = UnitVector;
            var force = Force;

            if (Object2 != null)
            {
                if (!Object1.Fixed && !Object2.Fixed)
                    force /= 2;
                if (!Object1.Fixed)
                    _force1.Value = unit * force;
                if (!Object2.Fixed)
                    _force2.Value = -unit * force;
            }
            else
            {
                _force1.Value = unit * force;
            }
        }

        public override void Delete()
        {
            Object1.Forces.Remove(_force1);

            Object2?.Forces.Remove(_force2);

            base.Delete();
        }

        private const float CircleSize = 0.1f;

        private readonly CircleShape circle1 = new CircleShape(CircleSize)
            {FillColor = Color.Black, Origin = new Vector2f(CircleSize, CircleSize) };

        private readonly CircleShape circle2 = new CircleShape(CircleSize)
            {FillColor = Color.Black, Origin = new Vector2f(CircleSize, CircleSize) };
        private readonly Text _legende = new Text("", UI.Font, 13){FillColor = Color.Black};

        public override void DrawOverlay()
        {
            base.DrawOverlay();

            if (TargetLength == 0)
            {
                Render.Window.Draw(new[]
                {
                    new Vertex(Object1AbsPos, Color.Black),
                    new Vertex(Object2AbsPos, Color.Black)
                }, PrimitiveType.Lines);
            }
            else
            {
                var local = new View(Camera.GameView);
                var angle = -Delta.Angle();
                local.Rotate(angle.Degrees());
                local.Center += Object1AbsPos.Rotate(angle);

                var transform = Transform.Identity;
                transform.Rotate(180 - angle.Degrees(), Object1AbsPos);

                var d = 2 * (int) Math.Ceiling(2 * Math.Round(TargetLength / 2, MidpointRounding.AwayFromZero));
                var dx = Delta.Norm() / d;
                var dy = 0.5f;

                var p = Object1AbsPos;
                var hd = new Vector2f(dx / 2, -dy / 2);

                var color = Color.Black;

                var lines = new Vertex[2 + d];
                lines[0] = new Vertex(p, color);
                lines[1] = new Vertex(p += hd, color);

                for (var i = 0; i < d - 1; i++)
                {
                    lines[2 + i] = new Vertex(p += new Vector2f(dx, dy), color);
                    dy = -dy;
                }

                lines[2 + d - 1] = new Vertex(p + hd, color);

                for (var i = 0; i < lines.Length; i++)
                {
                    lines[i].Position = transform.TransformPoint(lines[i].Position);
                }

                Render.Window.Draw(lines, PrimitiveType.LineStrip);

                Render.Window.SetView(Camera.GameView);
            }

            circle1.Position = Object1AbsPos;
            circle2.Position = Object2AbsPos;

            Render.Window.Draw(circle1);
            Render.Window.Draw(circle2);

            if (ShowInfos)
            {
                Render.Window.SetView(Camera.MainView);

                _legende.DisplayedString = $"{Force,8:F2} N\n{Delta.Norm(),8:F2} m\n{Speed,8:F2} m/s";
                _legende.Origin = _legende.GetGlobalBounds().Size() / 2;
                _legende.Position = Tools.Average(Object1AbsPos, Object2AbsPos).ToScreen().F();
                Render.Window.Draw(_legende);

                Render.Window.SetView(Camera.GameView);
            }
        }

        public override bool Contains(Vector2f point)
        {
            return circle1.Contains(point) || circle2.Contains(point);
        }
    }
}
