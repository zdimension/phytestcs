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
        private static readonly Texture _spring = new Texture("images/spring.png"){Smooth = true};
        
        protected readonly Force Force1;
        protected readonly Force? Force2;

        private readonly Text _legende = new Text("", Ui.FontMono, 13) { FillColor = Color.Black };

        public Spring(float constant, float targetLength, float size, PhysicalObject object1, Vector2f object1RelPos,
            PhysicalObject? object2 = null, Vector2f object2RelPos = default, ForceType? type = null)
        {
            if (object1 == null)
                throw new ArgumentNullException(nameof(object1));
            
            Constant = constant;
            TargetLength = targetLength;

            type ??= ForceType.Spring;

            Force1 = new Force(type, new Vector2f(0, 0), object1RelPos) { Source = this };
            End1 = new SpringEnd(object1, object1RelPos, size, this);
            End1.Object.Forces.Add(Force1);
            BothDepends(End1);

            End2 = new SpringEnd(object2, object2RelPos, size, this);

            if (object2 != null)
            {
                Force2 = new Force(type, new Vector2f(0, 0), object2RelPos) { Source = this };
                End2.Object.Forces.Add(Force2);
            }

            BothDepends(End2);
            
            Color = Tools.RandomColor();

            UpdateForce();
        }

        [ObjProp("Spring constant", "N/m", shortName: "k")]
        public float Constant { get; set; }

        [ObjProp("Target length", "m", shortName: "l")]
        public float TargetLength { get; set; }

        [ObjProp("Damping", shortName: "ζ")]
        public float Damping { get; set; } = 0.10f;

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
            (float) (Constant * Math.Pow(DeltaLength + Speed * Simulation.TargetDt / 2, 2) / 2);

        public virtual float Force
        {
            get
            {
                var force = Constant * DeltaLength;

                if (Damping != 0)
                    force += -Speed * Damping * 20;

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

        protected override IEnumerable<Shape> Shapes => new[] { End1.Shape, End2.Shape };
        public override Vector2f Position { get; set; }
        public override float Angle { get; set; }

        public override void UpdatePhysics(float dt)
        {
            UpdateForce();
        }

        public override Color Color
        {
            get;
            set;
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
                    Force1.Value = unit * force;
                if (!End2.Object.Fixed)
                    Force2!.Value = -unit * force;
            }
            else
            {
                Force1.Value = unit * force;
            }
        }

        public override void Delete(BaseObject? source = null)
        {
            End1.Object.Forces.Remove(Force1);

            End2.Object?.Forces.Remove(Force2!);

            base.Delete(source);
        }

        public override void Draw()
        {
            base.Draw();

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
                var color = Color;
                var angle = 180 + Delta.Angle().Degrees();
                
                //var d = (int)Math.Round(2 * (int) Math.Ceiling(2 * Math.Round(TargetLength / 2, MidpointRounding.AwayFromZero)) / (2 * Size), MidpointRounding.AwayFromZero);
                var d = (int)Math.Round(3 * TargetLength / (2 * Size), MidpointRounding.AwayFromZero);
                // number of images

                var dx = Delta.Norm() / d;
                if (d < 1)
                    d = 1;
                
                var p = End1.Position;
                
                if (true)
                {
                    var img = new Sprite(_spring);
                    img.Color = color;
                    img.Rotation = angle;
                    var delta = img.Transform.TransformPoint(dx, 0);
                    img.Origin = new Vector2f(0, _spring.Size.Y / 2f);
                    img.Scale = new Vector2f(dx / _spring.Size.X, 0.6f * -Size / _spring.Size.Y);
                    img.Position = p;

                    for (var i = 0; i < d; i++)
                    {
                        Render.Window.Draw(img);
                        img.Position += delta;
                    }
                }
                else
                {
                    var dy = 0.5f;
                    var hd = new Vector2f(dx / 2, -dy / 2);
                    var lines = new Vertex[2 + d];
                    lines[0] = new Vertex(p, color);
                    lines[1] = new Vertex(p += hd, color);

                    for (var i = 0; i < d - 1; i++)
                    {
                        lines[2 + i] = new Vertex(p += new Vector2f(dx, dy), color);
                        dy = -dy;
                    }

                    lines[2 + d - 1] = new Vertex(p + hd, color);

                    var transform = Transform.Identity;
                    transform.Rotate(angle, End1.Position);

                    for (var i = 0; i < lines.Length; i++)
                        lines[i].Position = transform.TransformPoint(lines[i].Position);

                    Render.Window.Draw(lines, PrimitiveType.LineStrip);
                }
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

        public override Vector2f Map(Vector2f local)
        {
            throw new NotImplementedException();
        }

        public override Vector2f MapInv(Vector2f global)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class SpringEnd : PinnedShapedVirtualObject
    {
        private readonly CircleShape _shape = new CircleShape();
        private readonly CircleShape _shape2 = new CircleShape();

        public SpringEnd(PhysicalObject? @object, Vector2f relPos, float size, Spring parent)
            : base(@object, relPos)
        {
            Size = size;
            Parent = parent;
            Shape.FillColor = Color.Black;
        }

        public float Size
        {
            get => _shape.Radius * 2;
            set
            {
                _shape.Radius = value / 2f;
                _shape2.Radius = 0.75f * _shape.Radius;
                _shape.CenterOrigin();
                _shape2.CenterOrigin();
            }
        }

        public override Shape Shape => _shape;
        protected override IEnumerable<Shape> Shapes => new[] { _shape };
        public override Color Color { get; set; }
        public Spring Parent { get; }
        
        public override void Draw()
        {
            UpdatePosition();

            _shape.FillColor = Color.Black;
            _shape.OutlineThickness = (Selected ? 5 : 0) / Camera.Zoom;
            
            Render.Window.Draw(_shape);
            
            _shape2.Position = _shape.Position;
            _shape2.FillColor = Object?.Color ?? Program.CurrentPalette.SkyColor;
            Render.Window.Draw(_shape2);
        }
    }
}