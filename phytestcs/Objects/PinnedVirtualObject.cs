﻿using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public abstract class PinnedVirtualObject : VirtualObject
    {
        public PinnedVirtualObject(PhysicalObject @object, Vector2f relPos)
        {
            Object = @object;
            RelPos = relPos;

            if (@object != null)
                DependsOn(@object);
        }

        public PhysicalObject Object { get; }
        public virtual Vector2f RelPos { get; set; }

        public float ActualAngle => (Object?.Angle ?? 0) + Angle;
        public override float Angle { get; set; }

        public override Vector2f Position
        {
            get => Object?.Map(RelPos) ?? RelPos;
            set => RelPos = Object?.MapInv(value) ?? value;
        }
    }

    public abstract class PinnedShapedVirtualObject : PinnedVirtualObject, IHasShape
    {
        protected PinnedShapedVirtualObject(PhysicalObject @object, Vector2f relPos)
            : base(@object, relPos)
        {
            UpdatePosition();
        }

        public abstract Shape Shape { get; }

        public override Vector2f Map(Vector2f local)
        {
            return Shape.Transform.TransformPoint(Shape.Origin + local);
        }

        public override Vector2f MapInv(Vector2f @global)
        {
            return Shape.InverseTransform.TransformPoint(global) - Shape.Origin;
        }

        protected void UpdatePosition()
        {
            Shape.Rotation = ActualAngle.Degrees();
            Shape.Position = Position;
        }

        public override void Draw()
        {
            base.Draw();

            UpdatePosition();

            Render.Window.Draw(Shape);
        }
    }
}