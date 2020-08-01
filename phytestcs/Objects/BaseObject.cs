using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SFML.Graphics;
using SFML.System;
using static phytestcs.Tools;

namespace phytestcs.Objects
{
    public abstract class BaseObject : DispatchProxy, IDisposable
    {
        private static ulong _idCounter;

        private readonly Dictionary<MethodInfo, (Binding binding, MethodInfo? setter)> _bindings =
            new Dictionary<MethodInfo, (Binding, MethodInfo?)>();

        private readonly SynchronizedCollection<BaseObject> _dependents = new SynchronizedCollection<BaseObject>();

        private readonly SynchronizedCollection<BaseObject> _parents = new SynchronizedCollection<BaseObject>();
        private bool _selected;

        private bool _updating;
        private float _zDepth = 1;
        public ObjectAppearance Appearance = Program.CurrentPalette.Appearance;

        protected BaseObject()
        {
            Id = _idCounter++;
        }

        public ulong Id { get; }

        public string? Name { get; set; }

        protected abstract IEnumerable<Shape> Shapes { get; }

        [ObjProp("Position", "m", "m\u22c5s", "m/s", "x")]
        public abstract Vector2f Position { get; set; }

        [ObjProp("Angle", "rad", "rad\u22c5s", "rad/s", "θ")]
        public abstract float Angle { get; set; }

        public virtual Color Color
        {
            get => Shapes.First().FillColor;
            set
            {
                foreach (var s in Shapes)
                    s.FillColor = value;
                UpdateOutline();
                UpdatePhysics(0);
            }
        }

        public Hsva ColorHsva
        {
            get => Color;
            set => Color = value;
        }
        
        [Hidden]
        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                UpdateOutline();
            }
        }

        public virtual Color OutlineColor
        {
            get => Shapes.First().OutlineColor;
            set
            {
                foreach (var s in Shapes)
                    s.OutlineColor = value;
            }
        }

        public float ZDepth
        {
            get => _zDepth;
            set
            {
                _zDepth = value;
                Simulation.SortZDepth();
            }
        }

        public IReadOnlyList<BaseObject> Parents => _parents.ToList().AsReadOnly();

        public IReadOnlyList<BaseObject> Dependents => _dependents.ToList().AsReadOnly();

        public EventWrapper<ClickedEventArgs> OnClick { get; } = new EventWrapper<ClickedEventArgs>();
        public EventWrapper<BaseEventArgs> OnDie { get; } = new EventWrapper<BaseEventArgs>();
        public EventWrapper<ClickedEventArgs> OnKey { get; } = new EventWrapper<ClickedEventArgs>();
        public EventWrapper<BaseEventArgs> OnSpawn { get; } = new EventWrapper<BaseEventArgs>();
        public EventWrapper<PostStepEventArgs> PostStep { get; } = new EventWrapper<PostStepEventArgs>();

        public virtual void Dispose()
        {
            //
        }

        public void UpdateOutline()
        {
            Color color;
            if (Selected)
            {
                color = Program.CurrentPalette.SelectionColor;
            }
            else
            {
                color = Color.Multiply(1f - 0.5f * Color.A / 255f);
                color.A = Appearance.OpaqueBorders ? (byte) 255 : Color.A;
            }

            OutlineColor = color;
        }

        protected void DependsOn(BaseObject other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            other._dependents.Add(this);
            _parents.Add(other);
        }

        protected void BothDepends(BaseObject other)
        {
            DependsOn(other);
            other.DependsOn(this);
        }

        public event Action Deleted = () => { };

        public virtual void Delete(BaseObject? source = null)
        {
            OnDie.Invoke(new BaseEventArgs(this));

            while (_dependents.Any())
            {
                var first = _dependents.FirstOrDefault();
                if (first == source)
                    _dependents.Remove(first);
                else
                    first.Delete(this);
            }

            lock (_parents.SyncRoot)
            {
                foreach (var obj in _parents)
                    obj._dependents.Remove(this);
            }

            Simulation.Remove(this);
            Simulation.UpdatePhysicsInternal(0);

            InvokeDeleted();

            if (Drawing.SelectedObject == this)
                Drawing.SelectObject(null);

            Dispose();
        }

        public void InvokeDeleted()
        {
            Deleted();
        }

        public virtual void UpdatePhysics(float dt)
        {
            if (_updating)
                return;

            _updating = true;

            foreach (var (_, (binding, setter)) in _bindings)
            {
                if (setter == null)
                    continue;

                try
                {
                    setter.Invoke(this, new[] { binding.Value() });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            _updating = false;
        }

        public virtual void Draw()
        {
        }

        public virtual void DrawOverlay()
        {
        }

        public virtual bool Contains(Vector2f point)
        {
            return Shapes.Any(s => s.Contains(point));
        }

        protected override object? Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod == null) throw new ArgumentNullException(nameof(targetMethod));

            return _bindings.TryGetValue(targetMethod, out var binding)
                ? binding.binding.Value()
                : targetMethod.Invoke(this, args);
        }

        public void Bind<TThis, T>(Expression<Func<TThis, T>> prop, Func<T> value)
        {
            if (prop == null) throw new ArgumentNullException(nameof(prop));

            if (typeof(TThis) != GetType())
                throw new ArgumentException(nameof(TThis));

            var expr = (MemberExpression) prop.Body;
            var bindPropInfo = (PropertyInfo) expr.Member;
            var getMethod = bindPropInfo.GetGetMethod()!;

            Bind(getMethod, () => value);
        }

        public Binding Bind(MethodInfo getMethod, Func<object?> value, MethodInfo? setMethod = null)
        {
            return Bind(getMethod, new Binding(value), setMethod);
        }

        public T Bind<T>(MethodInfo getMethod, T binding, MethodInfo? setMethod = null)
            where T : Binding
        {
            _bindings[getMethod] = (binding, setMethod);
            return binding;
        }

        public void Unbind<TThis, T>(Expression<Func<TThis, T>> prop)
        {
            if (prop == null) throw new ArgumentNullException(nameof(prop));

            if (typeof(TThis) != GetType())
                throw new ArgumentException(nameof(TThis));

            var expr = (MemberExpression) prop.Body;
            var bindPropInfo = (PropertyInfo) expr.Member;
            var getMethod = bindPropInfo.GetGetMethod()!;

            Unbind(getMethod);
        }

        public void Unbind(MethodInfo getMethod)
        {
            if (_bindings.TryGetValue(getMethod, out var binding))
                binding.binding.Remove();
            _bindings.Remove(getMethod);
        }

        public bool IsBound(MethodInfo getMethod, out (Binding, MethodInfo?) res)
        {
            return _bindings.TryGetValue(getMethod, out res);
        }

        public abstract Vector2f Map(Vector2f local);

        public abstract Vector2f MapInv(Vector2f global);
        //public EventWrapper<ClickedEventArgs> Update { get; } = new EventWrapper<ClickedEventArgs>();
    }

    [AttributeUsageAttribute(AttributeTargets.All)]
    public sealed class ObjPropAttribute : DisplayNameAttribute
    {
        public ObjPropAttribute(string displayName, string unit = "", string? unitInteg = null, string? unitDeriv = null,
            string? shortName = null)
            : base(L[displayName])
        {
            Unit = unit;
            UnitInteg = unitInteg ?? Objects.Unit.IncreasePower(unit);
            UnitDeriv = unitDeriv ?? Objects.Unit.DecreasePower(unit);
            ShortName = shortName ?? displayName;
        }

        public string Unit { get; set; }
        public string UnitInteg { get; set; }
        public string UnitDeriv { get; set; }
        public string ShortName { get; set; }
    }

    public class Binding
    {
        public Binding(Func<object?> value)
        {
            Value = value;
        }

        public virtual Func<object?> Value { get; }

        public void Remove()
        {
            Removed();
        }

        public event Action Removed = () => { };
    }

    public sealed class UserBinding : Binding
    {
        public UserBinding(string code, object? target)
            : base(null!)
        {
            Wrapper = new LambdaStringWrapper<Func<object?>>(code, target);
        }

        public LambdaStringWrapper<Func<object?>> Wrapper { get; }

        public override Func<object?> Value => Wrapper.Value;

        public string Code
        {
            get => Wrapper.Code;
            set => Wrapper.Code = value;
        }
    }

    public class BaseEventArgs : HandledEventArgs
    {
        public BaseEventArgs(object @this)
        {
            This = @this;
        }

        public dynamic This { get; }
    }

    public sealed class ClickedEventArgs : BaseEventArgs
    {
        public ClickedEventArgs(object @this, Vector2f position) : base(@this)
        {
            Position = position;
        }

        public Vector2f Position { get; }
    }

    public sealed class PostStepEventArgs : BaseEventArgs
    {
        public PostStepEventArgs(object @this, float deltaTime) : base(@this)
        {
            DeltaTime = deltaTime;
        }

        public float DeltaTime { get; }
    }

    public sealed class Unit
    {
        public static readonly Dictionary<string, Unit> Units = new Dictionary<string, Unit>();

        private static readonly (string, string)[] Powers =
        {
            ("⋅s³", "⋅s²"),
            ("⋅s²", "⋅s"),
            ("/s", "/s²"),
            ("/s²", "/s³")
        };

        public static readonly Unit Length = FromString("m");
        public static readonly Unit Velocity = Length.Derivative;

        private Lazy<Unit> _antiderivative;
        private Lazy<Unit> _derivative;

        private Unit(string name, Lazy<Unit> deriv, Lazy<Unit> integ)
        {
            Name = name;
            _derivative = deriv;
            _antiderivative = integ;
        }

        public string Name { get; }
        public Unit Derivative => _derivative.Value;
        public Unit Antiderivative => _antiderivative.Value;

        public override string ToString()
        {
            return Name;
        }

        public static string IncreasePower(string suffix)
        {
            if (suffix == null)
                throw new ArgumentNullException(nameof(suffix));
            
            if (suffix.EndsWith("/s", StringComparison.InvariantCulture))
                return suffix[..^2];

            foreach (var (bef, aft) in Powers)
                if (suffix.EndsWith(aft, StringComparison.InvariantCulture))
                    return suffix[..^aft.Length] + bef;

            return suffix + "⋅s";
        }

        public static string DecreasePower(string suffix)
        {
            if (suffix == null)
                throw new ArgumentNullException(nameof(suffix));
            
            if (suffix.EndsWith("⋅s", StringComparison.InvariantCulture))
                return suffix[..^2];

            foreach (var (bef, aft) in Powers)
                if (suffix.EndsWith(bef, StringComparison.InvariantCulture))
                    return suffix[..^bef.Length] + aft;

            return suffix + "/s";
        }

        public static Unit FromString(string name, Unit? deriv = null, Unit? integ = null)
        {
            var lDeriv = new Lazy<Unit>(() => deriv ?? FromString(DecreasePower(name)));
            var lInteg = new Lazy<Unit>(() => integ ?? FromString(IncreasePower(name)));

            if (!Units.TryGetValue(name, out var res))
                res = Units[name] = new Unit(name,
                    lDeriv,
                    lInteg);

            var lRes = new Lazy<Unit>(() => res);

            if (deriv != null)
                deriv._antiderivative = lRes;

            if (integ != null)
                integ._derivative = lRes;

            return res;
        }

        public static implicit operator Unit(string name)
        {
            return FromString(name);
        }

        public Unit Differentiate(int degree = 1)
        {
            var unit = this;
            for (var i = 0; i < degree; i++)
                unit = unit.Derivative;
            return unit;
        }

        public Unit Integrate(int degree = 1)
        {
            var unit = this;
            for (var i = 0; i < degree; i++)
                unit = unit.Antiderivative;
            return unit;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class HiddenAttribute : Attribute
    {
        
    }
}