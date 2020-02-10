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
        Move
    }

    public sealed class Drawing
    {
        public static float DragConstant = 1e3f;
        public static DrawingType DrawMode;
        public static Color DrawColor;
        public static PhysicalObject DragObject;
        public static Spring DragSpring;
        public static Vector2f DragObjectRelPos;

        public static void SelectObject(Object obj)
        {
            if (SelectedObject != null && SelectedObject is PhysicalObject old)
            {
                old.Shape.OutlineThickness = 0;
            }

            if (obj != null && obj is PhysicalObject @new)
            {
                @new.Shape.OutlineThickness = -0.2f;
                @new.Shape.OutlineColor = Color.White;
            }

            SelectedObject = obj;
        }

        public static Object SelectedObject { get; set; }
    }
}