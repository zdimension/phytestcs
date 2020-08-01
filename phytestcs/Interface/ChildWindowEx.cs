﻿using System;
using System.Collections;
using System.Collections.Generic;
using SFML.System;
using TGUI;

namespace phytestcs.Interface
{
    public class ChildWindowEx : ChildWindow, IEnumerable<Widget>
    {
        protected readonly List<Widget> Children = new List<Widget>();

        protected float Height;

        public ChildWindowEx(string name, int width, bool hide = false, bool minimize = true) : base(name,
            TitleButton.Close | (minimize ? TitleButton.Minimize : 0))
        {
            Size = new Vector2f(width, 0);

            Container = new VerticalLayout();

            ((Container) this).Add(Container);

            UpdateSize();

            Minimized += delegate
            {
                IsMinimized = !IsMinimized;
                UpdateSize();
            };

            if (hide)
                Closed += delegate { Visible = false; };
            else
                Closed += delegate
                {
                    Ui.Gui.Remove(this);
                    Dispose();
                };
        }

        public Vector2f? StartPosition { get; set; }
        public bool WasMoved => StartPosition.HasValue && Position != StartPosition.Value;
        public bool IsMinimized { get; private set; }
        public VerticalLayout Container { get; }
        public bool IsMain { get; set; }
        public bool IsClosing { get; private set; }

        public IEnumerator<Widget> GetEnumerator()
        {
            return Widgets.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Close()
        {
            if (IsClosing)
                return;

            IsClosing = true;
            CloseWindow();
            IsClosing = false;
        }

        protected virtual void UpdateSize()
        {
            MaximumSize = Container.Size = MinimumSize = new Vector2f(Container.Size.X,
                IsMinimized
                    ? 0
                    : Height);
        }

        public T Add<T>(T w)
            where T : Widget
        {
            if (w == null) throw new ArgumentNullException(nameof(w));

            Height += w.Size.Y;
            Container.Add(w, w.Size.Y);
            Children.Add(w);

            UpdateSize();

            return w;
        }

        public void Show()
        {
            ShowWithEffect(ShowAnimationType.Scale, Time.FromMilliseconds(50));
        }
    }
}