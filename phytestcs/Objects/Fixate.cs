using phytestcs.Interface;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public sealed class Fixate : VirtualObject
    {
        public PhysicalObject Object { get; }
        public Vector2f ObjectRelPos { get; }

        public Fixate(PhysicalObject @object, Vector2f objectRelPos)
        {
            Object = @object;
            Object.HasFixate = true;
            ObjectRelPos = objectRelPos;

            DependsOn(@object);
        }

        public Vector2f ObjetPosRel => Object.Position + ObjectRelPos;

        public override void Delete()
        {
            Object.HasFixate = false;

            base.Delete();
        }

        private Sprite _sprite = new Sprite(UI.actions[4].Item4.Value){Scale=new Vector2f(0.5f, 0.5f), Origin = new Vector2f(25, 25)};

        public override void DrawOverlay()
        {
            base.DrawOverlay();

            Render.Window.SetView(Camera.MainView);
            _sprite.Position = ObjetPosRel.ToScreen().F();
            Render.Window.Draw(_sprite);
            Render.Window.SetView(Camera.GameView);
        }
    }
}
