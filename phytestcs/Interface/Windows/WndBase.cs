using phytestcs.Objects;
using SFML.System;
using static phytestcs.Interface.UI;

namespace phytestcs.Interface.Windows
{
    public class WndBase<T> : ChildWindowEx
        where T : Object
    {
        public WndBase(T obj, string name, int larg, Vector2f p)
            : base(name, larg)
        {
            Object = obj;
            PropertyWindows[obj].Add(this);
            GUI.Add(this);
            StartPosition = Position = p;
            Closed += delegate { PropertyWindows[obj].Remove(this); };
        }

        public T Object { get; }
    }

    public class WndBase : WndBase<Object>
    {
        public WndBase(Object obj, string name, int larg, Vector2f p)
            : base(obj, name, larg, p)
        {
        }
    }
}