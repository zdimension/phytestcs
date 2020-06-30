using SFML.Graphics;
using SFML.System;

namespace phytestcs
{
    public class Force
    {
        public ForceType Type { get; }
        public Vector2f Value { get; set; }
        public Vector2f Position { get; set; }
        public float TimeToLive { get; set; }

        public Force(ForceType type, Vector2f val, Vector2f pos, float ttl=float.PositiveInfinity)
        {
            Type = type;
            Value = val;
            Position = pos;
            TimeToLive = ttl;
        }
    }

    public class ForceType
    {
        public string Name { get; }
        public string ShortName { get; }
        public Color Color { get; }

        public ForceType(string name, string sname, Color color)
        {
            Name = name;
            ShortName = sname;
            Color = color;
        }

        public static readonly ForceType Gravity = new ForceType("Gravité", "P", Color.Black);
        public static readonly ForceType AirFriction = new ForceType("Frottements de l'air", "f", Color.Red);
        public static readonly ForceType Buoyance = new ForceType("Poussée d'Archimède", "Φ", Color.Green);
        public static readonly ForceType Normal = new ForceType("Normale", "N", Color.Black);
        public static readonly ForceType Friction = new ForceType("Frottements", "T", Color.Black);
        public static readonly ForceType Spring = new ForceType("Ressort", "s", Color.Magenta);
        public static readonly ForceType External = new ForceType("Utilisateur", "u", Color.Blue);
        public static readonly ForceType Drag = new ForceType("Main", "h", Color.Yellow);
        public static readonly ForceType Hinge = new ForceType("Pivot", "o", Color.Cyan);
    }
}
