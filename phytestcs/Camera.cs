using System;
using System.Diagnostics;
using phytestcs.Interface;
using SFML.Graphics;
using SFML.System;

namespace phytestcs
{
    public static class Camera
    {
        public static Vector2f OldSize;
        public static Vector2f OldPos;
        public static bool ZoomTransition;
        public static Vector2f NewSize;
        public static Vector2f? NewPos;
        public static DateTime TransitionStart;
        public static float TransitionDuration = 0.1f;
        public static float ZoomDelta = 0.3f;

        public static Vector2f? CameraMoveOrigin = null;
        public static View GameView;
        public static View MainView;

        /// <summary>
        /// Pixels par mètre
        /// </summary>
        public static float Zoom => Render.Width / GameView.Size.X;

        public static void Center()
        {
            SetZoomAbsolute(25);
            GameView.Center = new Vector2f(0, 8);
        }

        public static void UpdateZoom()
        {
            if (!ZoomTransition) return;

            var dt = DateTime.Now - TransitionStart;

            if (dt.TotalSeconds > TransitionDuration)
            {
                GameView.Size = NewSize;

                if (NewPos.HasValue) GameView.Center = NewPos.Value;

                ZoomTransition = false;
            }
            else
            {
                GameView.Size = Tools.Transition(OldSize, NewSize, TransitionStart, TransitionDuration);

                if (NewPos.HasValue)
                    GameView.Center = Tools.Transition(OldPos, NewPos.Value, TransitionStart, TransitionDuration);
            }
        }

        public static void SetZoom(float val, Vector2f? pos = null, bool abs = false)
        {
            Debug.Assert(val > 0);
            OldSize = GameView.Size;
            OldPos = GameView.Center;
            ZoomTransition = true;
            NewSize = (abs ? Render.WindowF : GameView.Size) / val;
            NewPos = pos;
            TransitionStart = DateTime.Now;
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