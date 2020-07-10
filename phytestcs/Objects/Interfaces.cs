using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public interface IRotatable
    {
        public float Angle { get; set; }
    }

    public interface IHasPosition
    {
        public Vector2f Position { get; }
    }

    public interface IRotHasPos : IRotatable, IHasPosition
    {
        
    }

    public interface IHasLocalGeom
    {
        public Vector2f Map(Vector2f local);

        public Vector2f MapInv(Vector2f @global);
    }

    public interface IMoveable : IRotHasPos, IHasLocalGeom
    {
        public new Vector2f Position { get; set; }
    }

    public interface IHasShape : IHasLocalGeom
    {
        public Shape Shape { get; }
    }

    public interface ICollides
    {
        public uint CollideSet { get;  }
    }
}