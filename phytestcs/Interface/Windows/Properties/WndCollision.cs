using System.Globalization;
using phytestcs.Objects;
using SFML.System;
using static phytestcs.Tools;

namespace phytestcs.Interface.Windows.Properties
{
    public sealed class WndCollision : WndBase<PhysicalObject>
    {
        public WndCollision(PhysicalObject obj, Vector2f pos)
            : base(obj, 250, pos)
        {
            var wrapper = new BitFieldWrapper(() => obj.CollideSet);
            for (var i = 0; i < 16; i++)
            {
                var bit = i;
                Add(new CheckField(
                    string.Format(CultureInfo.InvariantCulture, L["Collision layer {0}"], (char) ('A' + i)),
                    new PropertyReference<bool>(() => wrapper[bit], val => wrapper[bit] = val)));
            }

            Add(new CheckField(() => obj.HeteroCollide));

            Show();
        }
    }
}