using System.Collections.Generic;
using phytestcs.Interface;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public sealed class Fixate : PinnedShapedVirtualObject
    {
        public float Size
        {
            get => rect1.Scale.X;
            set => rect1.Scale = rect2.Scale = new Vector2f(value, value) / 10f;
        }

        public Fixate(PhysicalObject @object, Vector2f relPos, float size)
            : base(@object, relPos)
        {
            Object.HasFixate = true;
            Size = size;
        }


        public override void Delete(Object source=null)
        {
            Object.HasFixate = false;

            base.Delete(source);
        }

        private readonly RectangleShape rect1 = new RectangleShape(new Vector2f(5, 1)){FillColor = Color.Black}.CenterOrigin().With(o => o.Rotation = +45);
        private readonly RectangleShape rect2 = new RectangleShape(new Vector2f(5, 1)){FillColor = Color.Black}.CenterOrigin().With(o => o.Rotation = -45);
        public override IEnumerable<Shape> Shapes => new[] {rect1, rect2};

        public override void Draw()
        {
            
        }

        public override void DrawOverlay()
        {
            base.DrawOverlay();

            rect1.Position = rect2.Position = Position;
            
            using var view = new View(Camera.GameView);
            view.Rotate(Object.Angle.Degrees());
            Render.Window.SetView(view);
            Render.Window.Draw(rect1);
            Render.Window.Draw(rect2);
            Render.Window.SetView(Camera.GameView);
        }

        public override Shape Shape => rect1;
    }
}
