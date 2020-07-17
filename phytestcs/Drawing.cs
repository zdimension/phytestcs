using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;

namespace phytestcs
{
    public enum DrawingType
    {
        Off,
        Rectangle,
        Circle,
        Spring,
        Fixate,
        Hinge,
        Move,
        Tracer,
        Thruster,
        Laser
    }

    public sealed class Drawing
    {
        public static float DragConstant = 1e3f;
        public static DrawingType DrawMode;
        public static Color DrawColor;
        public static IMoveable DragObject;
        public static Spring DragSpring;
        public static Vector2f DragObjectRelPos;

        public static Object SelectedObject { get; private set; }
        public static Vector2f DragObjectRelPosDirect { get; set; }

        public static Object SelectObject(Object obj)
        {
            if (SelectedObject != null)
                SelectedObject.Selected = false;

            SelectedObject = obj;

            if (SelectedObject != null)
                SelectedObject.Selected = true;

            return obj;
        }
    }
}