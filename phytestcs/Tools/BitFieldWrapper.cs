using System;
using System.Linq.Expressions;
using phytestcs.Interface;

namespace phytestcs
{
    public class BitFieldWrapper
    {
        private readonly Func<uint> getter;
        private readonly Action<uint> setter;

        public BitFieldWrapper(Expression<Func<uint>> bindProp)
            : this(PropertyReference.FromExpression(bindProp))
        {
        }

        public BitFieldWrapper(PropertyReference<uint> bindProp)
        {
            (getter, setter) = bindProp.GetAccessors();
        }

        public bool this[int bit]
        {
            get => (getter() & (1 << bit)) != 0;
            set
            {
                var old = getter();
                old ^= (uint) ((value ? -1 : 0) ^ old) & (uint) (1 << bit);
                setter(old);
            }
        }
    }
}