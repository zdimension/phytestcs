﻿using System;
using System.Data;
using phytestcs.Objects;
using SFML.System;
using static phytestcs.Interface.Ui;
using static phytestcs.Tools;

namespace phytestcs.Interface.Windows.Properties
{
    public class WndBase<T> : ChildWindowEx
    {
        protected WndBase(T obj1, int larg, Vector2f p)
            : base("", larg)
        {
            if (!(obj1 is BaseObject obj))
                throw new InvalidConstraintException(nameof(obj1));
            
            Object = obj1;
            Title = obj.Name ?? L[obj.GetType().Name] ?? "";
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

    public sealed class WndBase : WndBase<BaseObject>
    {
        public WndBase(BaseObject obj, int larg, Vector2f p)
            : base(obj, larg, p)
        {
        }

        public WndBase(BaseObject obj, string name, int larg, Vector2f p)
            : base(obj, name, larg, p)
        {
        }
    }
}