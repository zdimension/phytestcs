using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public abstract class PinnedVirtualObject : VirtualObject, IMoveable
    {
        public PhysicalObject Object { get; }
        public virtual Vector2f RelPos { get; set; }
        public virtual float Angle { get; set; }
        public virtual Vector2f Position
        {
            get => Object?.Map(RelPos) ?? RelPos;
            set => RelPos = Object?.MapInv(value) ?? value;
        }

        public PinnedVirtualObject(PhysicalObject @object, Vector2f relPos)
        {
            Object = @object;
            RelPos = relPos;

            if (@object != null)
                DependsOn(@object);
        }
        
        public float ActualAngle => (Object?.Angle ?? 0) + Angle;
        public abstract Vector2f Map(Vector2f local);
        public abstract Vector2f MapInv(Vector2f global);
    }

    public abstract class PinnedShapedVirtualObject : PinnedVirtualObject, IHasShape
    {
        public abstract Shape Shape { get; }

        protected PinnedShapedVirtualObject(PhysicalObject @object, Vector2f relPos)
            : base(@object, relPos)
        {
            
        }
        
        public override void Draw()
        {
            base.Draw();
            
            Shape.Rotation = ActualAngle.Degrees();
            Shape.Position = Position;
            
            Render.Window.Draw(Shape);
        }
        
        public override Vector2f Map(Vector2f local)
        {
            return Shape.Transform.TransformPoint(Shape.Origin + local);
        }

        public override Vector2f MapInv(Vector2f @global)
        {
            return Shape.InverseTransform.TransformPoint(global) - Shape.Origin;
        }
    }
}