using SFML.System;

namespace phytestcs
{
    public class Force
    {
        public string Name { get; }
        public Vector2f Value { get; set; }
        public float TimeToLive { get; set; }

        public Force(string name, Vector2f val, float ttl=float.PositiveInfinity)
        {
            Name = name;
            Value = val;
            TimeToLive = ttl;
        }
    }
}
