using phytestcs.Objects;
using SFML.System;
using static phytestcs.Interface.UI;

namespace phytestcs.Interface.Windows
{
    public class WndBase : ChildWindowEx
    {
        public Object Object { get; }

        public WndBase(Object obj, string name, int larg, Vector2f p)
        : base(name, larg)
        {
            Object = obj;
            PropertyWindows[obj].Add(this);
            GUI.Add(this);
            StartPosition = Position = p;
            Closed += delegate
            {
                PropertyWindows[obj].Remove(this);
            };
        }
    }
}
