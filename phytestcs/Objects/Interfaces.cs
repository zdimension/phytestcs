using SFML.Graphics;

namespace phytestcs.Objects
{
    public interface IHasShape
    {
        public Shape Shape { get; }
    }

    public interface ICollides
    {
        public uint CollideSet { get; }
    }
}