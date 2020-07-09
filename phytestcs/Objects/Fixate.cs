using System.Collections.Generic;
using phytestcs.Interface;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public sealed class Fixate : PinnedShapedVirtualObject
    {
        public float Size { get; }

        public Fixate(PhysicalObject @object, Vector2f relPos, float size)
            : base(@object, relPos)
        {
            Object.HasFixate = true;
            Size = size;
            rect1.Scale = rect2.Scale = new Vector2f(size, size);
        }


        public override void Delete(Object source=null)
        {
            Object.HasFixate = false;

            base.Delete(source);
        }

        private readonly RectangleShape rect1 = new RectangleShape(new Vector2f(5, 1)){Rotation = +45, FillColor = Color.Black}.CenterOrigin();
        private readonly RectangleShape rect2 = new RectangleShape(new Vector2f(5, 1)){Rotation = -45, FillColor = Color.Black}.CenterOrigin();
        public override IEnumerable<Shape> Shapes => new[] {rect1, rect2};

        public override void DrawOverlay()
        {
            base.DrawOverlay();
            
            using var view = new View(Camera.GameView);
            view.Rotate(Object.Angle);
            Render.Window.SetView(view);
            Render.Window.Draw(rect1);
            Render.Window.Draw(rect2);
            Render.Window.SetView(Camera.GameView);
        }

        public override Shape Shape => rect1;
    }
}
