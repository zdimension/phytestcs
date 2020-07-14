using System;
using phytestcs.Objects;
using SFML.System;
using TGUI;
using static phytestcs.Global;

namespace phytestcs.Interface.Windows
{
    public class WndMaterial : WndBase<PhysicalObject>
    {
        public static readonly (string, Action<PhysicalObject>)[] Materials =
        {
            (L["Default"], o =>
            {
                
            }),
            (L["Glass"], o =>
            {
                
            }),
            (L["Gold"], o =>
            {
                
            }),
            (L["Helium"], o =>
            {
                
            }),
            (L["Ice"], o =>
            {
                
            }),
            (L["Rubber"], o =>
            {
                
            }),
            (L["Steel"], o =>
            {
                
            }),
            (L["Stone"], o =>
            {
                
            }),
            (L["Wood"], o =>
            {
                
            }),
        };
        
        public WndMaterial(PhysicalObject obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            HorizontalLayout cur = null;
            var wrap = new HorizontalWrap();
            for (var i = 0; i < Materials.Length; i++)
            {
                if (i % 2 == 0)
                {
                    cur = new HorizontalLayout();
                    //Add(cur);
                }

                var (text, action) = Materials[i];
                var btn = new Button(text);
                btn.Clicked += delegate { action(obj); };
                wrap.Add(btn);
            }

            Add(wrap);
            
            Add(new NumberField<float>(0.001f, 100f, bindProp: () => obj.Density, log: true));
            Add(new NumberField<float>(0.001f, 1000f, bindProp: () => obj.Mass, log: true));
            Add(new NumberField<float>(0, 2, bindProp: () => obj.Friction) { RightValue = float.PositiveInfinity });
            Add(new NumberField<float>(0, 1, bindProp: () => obj.Restitution));
            Add(new NumberField<float>(1, 100, bindProp: () => obj.RefractiveIndex, log: true){RightValue = float.PositiveInfinity});
            Add(new NumberField<float>(0.01f, 100, bindProp: () => obj.Attraction, log: true) { LeftValue = 0 });
            Show();
        }
    }
}
