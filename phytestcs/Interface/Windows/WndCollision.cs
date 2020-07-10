using System;
using System.Globalization;
using System.Linq.Expressions;
using phytestcs.Objects;
using SFML.System;
using static phytestcs.Global;

namespace phytestcs.Interface.Windows
{
    public class WndCollision : WndBase<PhysicalObject>
    {
        public WndCollision(PhysicalObject obj, Vector2f pos)
            : base(obj, obj.Name, 250, pos)
        {
            var wrapper = new BitFieldWrapper(() => obj.CollideSet);
            for (var i = 0; i < 16; i++)
            {
                var bit = i;
                Add(new CheckField(string.Format(CultureInfo.InvariantCulture, L["Collision layer {0}"], (char)('A' + i)), 
                    new PropertyReference<bool>(() => wrapper[bit], val => wrapper[bit] = val)));
            }
            Show();
        }
    }
}