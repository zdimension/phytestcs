using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public sealed class Fixate : PinnedShapedVirtualObject
    {
        private readonly RectangleShape _rect1 = new RectangleShape(new Vector2f(5, 1)) { FillColor = Color.Black }
            .CenterOrigin();

        private readonly RectangleShape _rect2 = new RectangleShape(new Vector2f(5, 1)) { FillColor = Color.Black }
            .CenterOrigin();

        public Fixate(PhysicalObject @object, Vector2f relPos, float size)
            : base(@object, relPos)
        {
            Object.HasFixate = true;
            Size = size;
        }

        public float Size
        {
            get => _rect1.Scale.X;
            set
            {
                _rect1.Scale = _rect2.Scale = new Vector2f(value, value) / 10f;
                _rect1.CenterOrigin();
                _rect2.CenterOrigin();
            }
        }

        protected override IEnumerable<Shape> Shapes => new[] { _rect1, _rect2 };

        public override Shape Shape => _rect1;


        public override void Delete(Object source = null)
        {
            Object.HasFixate = false;

            base.Delete(source);
        }

        public override void Draw()
        {
        }

        public override void DrawOverlay()
        {
            base.DrawOverlay();

            _rect1.Position = _rect2.Position = Position;
            _rect1.Rotation = +45;
            _rect2.Rotation = -45;
            
            Render.Window.Draw(_rect1);
            Render.Window.Draw(_rect2);
        }
    }
}