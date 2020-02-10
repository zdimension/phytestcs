using SFML.System;
using TGUI;

namespace phytestcs.Interface
{
    public class ChildWindowEx : ChildWindow
    {
        public Vector2f? StartPosition { get; set; }
        public bool WasMoved => StartPosition.HasValue && Position != StartPosition.Value;
        public bool IsMinimized { get; private set; }
        public VerticalLayout Container { get; private set; }
        public bool IsMain { get; set; }
        public bool IsClosing { get; private set; }
        public void Close()
        {
            if (IsClosing)
                return;

            IsClosing = true;
            CloseWindow();
            IsClosing = false;
        }

        public ChildWindowEx(string name, int width) : base(name, TitleButton.Close | TitleButton.Minimize)
        {
            Size = new Vector2f(width, 0);

            Container = new VerticalLayout();

            Add(Container);

            UpdateSize();

            Minimized += delegate
            {
                IsMinimized = !IsMinimized; UpdateSize();
            };

            Closed += delegate
            {
                UI.GUI.Remove(this);
                Dispose();
            };
        }

        void UpdateSize()
        {
            MaximumSize = Container.Size = MinimumSize = new Vector2f(Container.Size.X,
                IsMinimized
                    ? 0
                    : _height);
        }

        private float _height;

        public void AddEx(Widget w)
        {
            _height += w.Size.Y;
            Container.Add(w);
            
            UpdateSize();
        }

        public void Show()
        {
            ShowWithEffect(ShowAnimationType.Scale, Time.FromMilliseconds(50));
        }
    }
}