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

            SelectedObject = obj;

            if (obj != null && obj is PhysicalObject @new)
            {
                UpdateThickness();
                @new.Shape.OutlineColor = Color.White;
            }
        }

        static Drawing()
        {
            Camera.ZoomChanged += zoom =>
            {
                UpdateThickness();
            };
        }

        private static float GetThickness(float zoom)
        {
            return -7 / zoom;
        }

        private static void UpdateThickness()
        {
            if (SelectedObject is PhysicalObject @new)
                @new.Shape.OutlineThickness = GetThickness(Camera.CameraZoom);
        }

        public static Object SelectedObject { get; set; }
    }
}