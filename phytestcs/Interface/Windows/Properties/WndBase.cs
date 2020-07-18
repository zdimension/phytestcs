using System;
using SFML.System;
using static phytestcs.Interface.Ui;
using Object = phytestcs.Objects.Object;

namespace phytestcs.Interface.Windows.Properties
{
    public class WndBase<T> : ChildWindowEx
        where T : Object
    {
        protected WndBase(T obj, int larg, Vector2f p)
            : base("", larg)
        {
            Object = obj ?? throw new ArgumentNullException(nameof(obj));
            Title = obj.Name;
            PropertyWindows[obj].Add(this);
            Gui.Add(this);
            StartPosition = Position = p;
            Closed += delegate { PropertyWindows[obj].Remove(this); };
        }

        protected WndBase(T obj, string name, int larg, Vector2f p)
            : this(obj, larg, p)
        {
            Title = name;
        }

        protected T Object { get; }
    }

    public class WndBase : WndBase<Object>
    {
        public WndBase(Object obj, int larg, Vector2f p)
            : base(obj, larg, p)
        {
        }

        public WndBase(Object obj, string name, int larg, Vector2f p)
            : base(obj, name, larg, p)
        {
        }
    }
}