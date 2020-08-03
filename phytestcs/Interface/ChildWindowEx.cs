using System;
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

        public ChildWindowEx(string name, int width, bool hide = false, bool minimize = true, bool useLayout=false) : base(name,
            TitleButton.Close | (minimize ? TitleButton.Minimize : 0))
        {
            UseLayout = useLayout;
            Size = new Vector2f(width, 0);

            if (!UseLayout)
            {
                Container = new VerticalLayout();
                Container.SizeLayout = new Layout2d("parent.w", "parent.h");

                ((Container) this).Add(Container);
            }

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
        public VerticalLayout? Container { get; }
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

        public bool UseLayout { get;}

        protected virtual void UpdateSize()
        {
            if (UseLayout)
            {
                SizeLayout = new Layout2d($"{Size.X}", _yLayout);
            }
            else
            {
                MaximumSize = Container.Size = MinimumSize = new Vector2f(Container.Size.X,
                    IsMinimized
                        ? 0
                        : Height);
            }
        }

        private string _yLayout = "0";

        public T Add<T>(T w, string widgetName="")
            where T : Widget
        {
            if (w == null) throw new ArgumentNullException(nameof(w));
            
            if (UseLayout)
            {
                _yLayout += $"+{widgetName}.h";
                base.Add(w, widgetName);
            }
            else
            {
                Height += w.Size.Y;
                Container.Add(w, w.Size.Y, widgetName);
            }

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