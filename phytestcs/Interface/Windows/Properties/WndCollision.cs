using System.Globalization;
using phytestcs.Internal;
using phytestcs.Objects;
using SFML.System;
using static phytestcs.Tools;

namespace phytestcs.Interface.Windows.Properties
{
    public sealed class WndCollision : WndBase<ICollides>
    {
        public WndCollision(ICollides obj, Vector2f pos)
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

            if (obj is PhysicalObject phy)
                Add(new CheckField(() => phy.HeteroCollide));

            Show();
        }
    }
}