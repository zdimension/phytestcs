using System.Diagnostics;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using static phytestcs.Tools;

namespace phytestcs
{
    public sealed class Force
    {
        public Force(ForceType type, Vector2f val, Vector2f pos, float ttl = float.PositiveInfinity)
        {
            Type = type;
            Value = val;
            Debug.Assert(!pos.IsNaN());
            Position = pos;
            TimeToLive = ttl;
        }

        public ForceType Type { get; set; }
        public Vector2f Value { get; set; }
        public Vector2f Position { get; set; }
        public float TimeToLive { get; set; }
        public BaseObject? Source { get; set; }

        public override string ToString()
        {
            return $"{Type} = {Value} N @ {Position} ({TimeToLive} s)";
        }
    }

    public sealed class ForceType
    {
        public static readonly ForceType Gravity = new ForceType(L["Gravity"], "P", Color.Black);
        public static readonly ForceType AirFriction = new ForceType(L["Air friction"], "f", Color.Red);
        public static readonly ForceType Buoyancy = new ForceType(L["Buoyancy"], "Φ", Color.Green);
        public static readonly ForceType Normal = new ForceType(L["Normal"], "N", Color.Black);
        public static readonly ForceType Friction = new ForceType(L["Friction"], "T", Color.Black);
        public static readonly ForceType Spring = new ForceType(L["Spring"], "s", Color.Magenta);
        public static readonly ForceType User = new ForceType(L["User"], "u", Color.Blue);
        public static readonly ForceType Drag = new ForceType(L["Drag"], "h", Color.Yellow);
        public static readonly ForceType Hinge = new ForceType(L["Hinge"], "o", Color.Cyan);
        public static readonly ForceType Thruster = new ForceType(L["Thruster"], "e", Color.Yellow);

        public ForceType(string name, string sname, Color color)
        {
            Name = name;
            ShortName = sname;
            Color = color;
        }

        public string Name { get; }
        public string ShortName { get; }
        public Color Color { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}