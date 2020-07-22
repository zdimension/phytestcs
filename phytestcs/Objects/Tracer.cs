using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public class Tracer : PinnedShapedVirtualObject
    {
        private readonly SynchronizedCollection<(float, Vector2f)> _points =
            new SynchronizedCollection<(float, Vector2f)>();

        private readonly CircleShape _shape = new CircleShape();

        public Tracer(PhysicalObject @object, Vector2f relPos, float size, Color clr, float fadeTime = 1.5f)
            : base(@object, relPos)
        {
            Size = size;
            Color = clr;
            FadeTime = fadeTime;
        }

        [ObjProp("Fade time", "s")]
        public float FadeTime { get; set; }

        [ObjProp("Size", "m")]
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
        protected override IEnumerable<Shape> Shapes => new[] { _shape };

        public override void Draw()
        {
            lock (_points.SyncRoot)
            {
                Render.Window.Draw(Tools.VertexLineTri(_points.Select(p => p.Item2).ToArray(), Color, Size, true));
            }

            base.Draw();
        }

        public override void UpdatePhysics(float dt)
        {
            base.UpdatePhysics(dt);

            if (dt == 0)
                return;

            lock (_points.SyncRoot)
            {
                _points.Add((Simulation.SimDuration, Position));

                var beginning = Simulation.SimDuration - FadeTime;

                while (_points[0].Item1 < beginning)
                    _points.RemoveAt(0);
            }
        }
    }
}