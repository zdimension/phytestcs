using SFML.Graphics;
using SFML.System;

namespace phytestcs
{
    public class Force
    {
        public string Name { get; }
        public Vector2f Value { get; set; }
        public Vector2f Position { get; set; }
        public float TimeToLive { get; set; }
        public Color Color { get; set; } = Color.Black;
        public string ShortName { get; set; }

        public Force(string name, Vector2f val, Vector2f pos, float ttl=float.PositiveInfinity, string sname=null)
        {
            Name = name;
            Value = val;
            Position = pos;
            TimeToLive = ttl;
            ShortName = sname;
        }
    }
}
