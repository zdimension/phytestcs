using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using SFML.System;
using TGUI;

namespace phytestcs.Interface
{
    public class ChildWindowEx : ChildWindow, IEnumerable<Widget>
    {
        public readonly List<Widget> Children = new List<Widget>();

        protected float ContentHeight;

        private const int MarginTop = 1;
        private const int MarginOther = 3;
        private const int MarginY = MarginTop + MarginOther;
        
        public ChildWindowEx(string name, int width, bool hide = false, bool minimize = true, bool useLayout=false) : base(name,
            TitleButton.Close | (minimize ? TitleButton.Minimize : 0))
        {
            TitleAlignment = HorizontalAlignment.Left;
            UseLayout = useLayout;
            Size = new Vector2f(width, 0);

            if (!UseLayout)
            {
                Container = new VerticalLayout();
                Container.SizeLayout = new Layout2d($"parent.iw - {2 * MarginOther}", $"parent.ih - {MarginOther + MarginTop}");
                Container.Position = new Vector2f(MarginOther,MarginTop);

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
                SizeLayout = new Layout2d($"{Size.X}", IsMinimized ? $"{MarginY}" : $"{MarginY} + {_yLayout}");
            }
            else
            {
                MaximumSize = MinimumSize = new Vector2f(Size.X,
                    MarginY + (IsMinimized
                        ? 0
                        : ContentHeight));
            }
        }

        private string _yLayout = "0";

        public T Add<T>(T w, string widgetName="")
            where T : Widget
        {
            if (w == null) throw new ArgumentNullException(nameof(w));
            
            if (UseLayout)
            {
                if (widgetName == "")
                {
                    widgetName = $"wg{w.CPointer.ToInt64()}";
                }
                _yLayout = $"max({widgetName}.bottom, {_yLayout})";
                base.Add(w, widgetName);
            }
            else
            {
                ContentHeight += w.Size.Y;
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