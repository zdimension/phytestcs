using System;
using System.Linq.Expressions;
using phytestcs.Interface;

namespace phytestcs
{
    public sealed class BitFieldWrapper
    {
        private readonly Func<uint> _getter;
        private readonly Action<uint>? _setter;

        public BitFieldWrapper(Expression<Func<uint>> bindProp)
            : this(PropertyReference.FromExpression(bindProp))
        {
        }

        public BitFieldWrapper(PropertyReference<uint> bindProp)
        {
            if (bindProp == null)
                throw new ArgumentNullException(nameof(bindProp));
            
            (_getter, _setter) = bindProp.GetAccessors();
        }

        public bool this[int bit]
        {
            get => (_getter() & (1 << bit)) != 0;
            set
            {
                if (_setter == null)
                    throw new InvalidOperationException();
                
                var old = _getter();
                old ^= (uint) ((value ? -1 : 0) ^ old) & (uint) (1 << bit);
                _setter(old);
            }
        }
    }
}