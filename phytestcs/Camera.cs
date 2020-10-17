using System;
using System.Diagnostics;
using phytestcs.Interface;
using SFML.Graphics;
using SFML.System;

namespace phytestcs
{
    public static class Camera
    {
        private static Vector2f _oldSize;
        private static Vector2f _oldPos;
        private static bool _zoomTransition;
        private static Vector2f _newSize;
        private static Vector2f? _newPos;
        private static DateTime _transitionStart;
        public static float TransitionDuration = 0.1f;
        public static float ZoomDelta = 0.3f;

        public static Vector2f? CameraMoveOrigin;
        public static readonly View GameView = new View();
        public static readonly View MainView = new View();

        /// <summary>
        ///     Pixels par mètre
        /// </summary>
        public static float Zoom => Render.Width / GameView.Size.X;

        public static void Center()
        {
            SetZoomAbsolute(25);
            GameView.Center = new Vector2f(0, 8);
        }

        public static void UpdateZoom()
        {
            if (!_zoomTransition) return;

            var dt = DateTime.Now - _transitionStart;

            if (dt.TotalSeconds > TransitionDuration)
            {
                GameView.Size = _newSize;

                if (_newPos.HasValue) GameView.Center = _newPos.Value;

                _zoomTransition = false;
            }
            else
            {
                GameView.Size = Tools.Transition(_oldSize, _newSize, _transitionStart, TransitionDuration);

                if (_newPos.HasValue)
                    GameView.Center = Tools.Transition(_oldPos, _newPos.Value, _transitionStart, TransitionDuration);
            }
        }

        public static void SetZoom(float val, Vector2f? pos = null, bool abs = false)
        {
            Debug.Assert(val > 0);
            _oldSize = GameView.Size;
            _oldPos = GameView.Center;
            _zoomTransition = true;
            _newSize = (abs ? Render.WindowF : GameView.Size) / val;
            _newPos = pos;
            _transitionStart = DateTime.Now;
            ZoomChanged(val);
        }

        public static event Action<float> ZoomChanged = delegate { };

        private static void SetZoomAbsolute(float val)
        {
            GameView.Size = (Render.WindowF / val).InvertY();
        }

        public static void CalculateWindow()
        {
            Render.Window.Size = new Vector2u(Render.Width, Render.Height);
            SetZoomAbsolute(Zoom);
            MainView.Size = Render.WindowF;
            MainView.Center = Render.WindowF / 2;
            Ui.Gui.View = MainView;
        }
    }
}