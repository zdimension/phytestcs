using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using phytestcs.Interface;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    [Guid("28DA2FA2-87F9-4748-A1C2-F43675AB8069")]
    public class Spring : VirtualObject
    {
        protected readonly Force _force1;
        protected readonly Force _force2;

        private readonly Text _legende = new Text("", UI.Font, 13) { FillColor = Color.Black };

        public Spring(float constant, float targetLength, float size, PhysicalObject object1, Vector2f object1RelPos,
            PhysicalObject object2 = null, Vector2f object2RelPos = default, ForceType type = null)
        {
            Constant = constant;
            TargetLength = targetLength;

            type ??= ForceType.Spring;

            _force1 = new Force(type, new Vector2f(0, 0), object1RelPos) { Source = this };
            End1 = new SpringEnd(object1, object1RelPos, size);
            End1.Object.Forces.Add(_force1);
            BothDepends(End1);

            End2 = new SpringEnd(object2, object2RelPos, size);

            if (object2 != null)
            {
                _force2 = new Force(type, new Vector2f(0, 0), object2RelPos) { Source = this };
                End2.Object.Forces.Add(_force2);
            }

            BothDepends(End2);

            Size = size;
            Color = Color.Black;

            UpdateForce();
        }

        [ObjProp("Spring constant", "N/m")] public float Constant { get; set; }

        [ObjProp("Target length", "m")] public float TargetLength { get; set; }

        [ObjProp("Damping")] public float Damping { get; set; } = 0.10f;

        public SpringEnd End1 { get; }
        public SpringEnd End2 { get; }

        public Vector2f Delta => End1.Position - End2.Position;

        public bool ShowInfos { get; set; } = true;

        public float Size
        {
            get => End1.Size;
            set => End1.Size = End2.Size = value;
        }

        public float DeltaLength => TargetLength - Delta.Norm();

        public float ElasticEnergy =>
            (float) (Constant * Math.Pow(DeltaLength + Speed * Simulation.TargetDT / 2, 2) / 2);

        public virtual float Force
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

                var dhdt = End1.Object.Velocity.Dot(unit);

                if (End2.Object != null)
                    dhdt += End2.Object.Velocity.Dot(-unit);

                return dhdt;
            }
        }

        public override IEnumerable<Shape> Shapes => new[] { End1.Shape, End2.Shape };

        public override void UpdatePhysics(float dt)
        {
            UpdateForce();
        }

        private void UpdateForce()
        {
            var unit = UnitVector;
            var force = Force;

            if (End2.Object != null)
            {
                if (!End1.Object.Fixed && !End2.Object.Fixed)
                    force /= 2;
                if (!End1.Object.Fixed)
                    _force1.Value = unit * force;
                if (!End2.Object.Fixed)
                    _force2.Value = -unit * force;
            }
            else
            {
                _force1.Value = unit * force;
            }
        }

        public override void Delete(Object source = null)
        {
            End1.Object.Forces.Remove(_force1);

            End2.Object?.Forces.Remove(_force2);

            base.Delete(source);
        }

        public override void DrawOverlay()
        {
            base.DrawOverlay();

            if (TargetLength == 0)
            {
                Render.Window.Draw(new[]
                {
                    new Vertex(End1.Position, Color.Black),
                    new Vertex(End2.Position, Color.Black)
                }, PrimitiveType.Lines);
            }
            else
            {
                var angle = -Delta.Angle();

                var transform = Transform.Identity;
                transform.Rotate(180 - angle.Degrees(), End1.Position);

                var d = 2 * (int) Math.Ceiling(2 * Math.Round(TargetLength / 2, MidpointRounding.AwayFromZero));
                var dx = Delta.Norm() / d;
                var dy = 0.5f;

                var p = End1.Position;
                var hd = new Vector2f(dx / 2, -dy / 2);

                var color = Color;

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
            }

            if (ShowInfos)
            {
                Render.Window.SetView(Camera.MainView);

                _legende.DisplayedString = $"{Force,8:F2} N\n{Delta.Norm(),8:F2} m\n{Speed,8:F2} m/s";
                _legende.Origin = _legende.GetGlobalBounds().Size() / 2;
                _legende.Position = Tools.Average(End1.Position, End2.Position).ToScreen().F();
                Render.Window.Draw(_legende);

                Render.Window.SetView(Camera.GameView);
            }
        }
    }

    public class SpringEnd : PinnedShapedVirtualObject
    {
        private readonly CircleShape _shape = new CircleShape();

        public SpringEnd(PhysicalObject @object, Vector2f relPos, float size)
            : base(@object, relPos)
        {
            Size = size;
        }

        public float Size
        {
            get => _shape.Radius * 2;
            set
            {
                _shape.Radius = value / 2;
                _shape.CenterOrigin();
            }
        }

        public override Shape Shape => _shape;
        public override IEnumerable<Shape> Shapes => new[] { _shape };
    }
}