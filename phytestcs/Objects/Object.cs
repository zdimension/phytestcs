﻿using System;
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
    public abstract class Object : DispatchProxy, IDisposable
    {
        private static ulong _idCounter;

        private readonly Dictionary<MethodInfo, Func<object?>> _bindings = new Dictionary<MethodInfo, Func<object?>>();
        private readonly SynchronizedCollection<Object> _dependents = new SynchronizedCollection<Object>();

        private readonly SynchronizedCollection<Object> _parents = new SynchronizedCollection<Object>();
        private bool _selected;
        public ObjectAppearance Appearance = Program.CurrentPalette.Appearance;

        protected Object()
        {
            Id = _idCounter++;
        }

        public ulong Id { get; }

        public string? Name { get; set; }

        protected abstract IEnumerable<Shape> Shapes { get; }
        
        [ObjProp("Position", "m", "m\u22c5s", "m/s", shortName:"x")]
        public abstract Vector2f Position { get; set; }
        
        [ObjProp("Angle", "rad", "rad\u22c5s", "rad/s", shortName:"θ")]
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

        public HSVA ColorHsva
        {
            get => Color;
            set => Color = value;
        }

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

        public IReadOnlyList<Object> Parents => _parents.ToList().AsReadOnly();

        public IReadOnlyList<Object> Dependents => _dependents.ToList().AsReadOnly();

        public virtual void Dispose()
        {
            //
        }

        private void UpdateOutline()
        {
            Color color;
            if (Selected)
                color = Program.CurrentPalette.SelectionColor;
            else
            {
                color = Color.Multiply(1f - 0.5f * Color.A / 255f);
                color.A = Appearance.OpaqueBorders ? (byte) 255 : Color.A;
            }

            OutlineColor = color;
        }

        protected void DependsOn(Object other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            other._dependents.Add(this);
            _parents.Add(other);
        }

        protected void BothDepends(Object other)
        {
            DependsOn(other);
            other.DependsOn(this);
        }

        public event Action Deleted = () => { };

        public virtual void Delete(Object source = null)
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

            return _bindings.TryGetValue(targetMethod, out var func) ? func() : targetMethod.Invoke(this, args);
        }

        public void Bind<TThis, T>(Expression<Func<TThis, T>> prop, Func<T> value)
        {
            if (prop == null) throw new ArgumentNullException(nameof(prop));

            if (typeof(TThis) != GetType())
                throw new ArgumentException(nameof(TThis));

            var expr = ((MemberExpression) prop.Body);
            var bindPropInfo = (PropertyInfo) expr.Member;
            var getMethod = bindPropInfo.GetGetMethod()!;

            _bindings[getMethod] = () => value();
        }

        public void Unbind<TThis, T>(Expression<Func<TThis, T>> prop)
        {
            if (prop == null) throw new ArgumentNullException(nameof(prop));

            if (typeof(TThis) != GetType())
                throw new ArgumentException(nameof(TThis));

            var expr = ((MemberExpression) prop.Body);
            var bindPropInfo = (PropertyInfo) expr.Member;
            var getMethod = bindPropInfo.GetGetMethod()!;

            _bindings.Remove(getMethod);
        }
        
        public abstract Vector2f Map(Vector2f local);

        public abstract Vector2f MapInv(Vector2f @global);
        
        public EventWrapper<ClickedEventArgs> OnClick { get; } = new EventWrapper<ClickedEventArgs>();
        public EventWrapper<BaseEventArgs> OnDie { get; } = new EventWrapper<BaseEventArgs>();
        public EventWrapper<ClickedEventArgs> OnKey { get; } = new EventWrapper<ClickedEventArgs>();
        public EventWrapper<BaseEventArgs> OnSpawn { get; } = new EventWrapper<BaseEventArgs>();
        public EventWrapper<PostStepEventArgs> PostStep { get; } = new EventWrapper<PostStepEventArgs>();
        //public EventWrapper<ClickedEventArgs> Update { get; } = new EventWrapper<ClickedEventArgs>();
    }

    [AttributeUsageAttribute(AttributeTargets.All)]
    public class ObjPropAttribute : DisplayNameAttribute
    {
        public ObjPropAttribute(string displayName, string unit = "", string unitInteg = null, string unitDeriv = null, string shortName = null)
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

    public class Binding<T>
    {
        public Action<T> Setter { get; set; }
        public Expression<Func<T>> Value { get; set; }
    }
    
    public class BaseEventArgs : HandledEventArgs
    {
        public dynamic This { get; }

        public BaseEventArgs(object @this)
        {
            This = @this;
        }
    }

    public class ClickedEventArgs : BaseEventArgs
    {
        public Vector2f Position { get; }

        public ClickedEventArgs(object @this, Vector2f position) : base(@this)
        {
            Position = position;
        }
    }
    
    public class PostStepEventArgs : BaseEventArgs
    {
        public float DeltaTime { get; }

        public PostStepEventArgs(object @this, float deltaTime) : base(@this)
        {
            DeltaTime = deltaTime;
        }
    }
    
    public class Unit
    {
        public string Name { get; }
        public Lazy<Unit> _derivative;
        public Unit Derivative => _derivative.Value;
        
        private Lazy<Unit> _antiderivative;
        public Unit Antiderivative => _antiderivative.Value;
        
        public static readonly Dictionary<string, Unit> _units = new Dictionary<string, Unit>();
		
		public override string ToString()
		{
			return Name;
		}

        private Unit(string name, Lazy<Unit> deriv, Lazy<Unit> integ)
        {
            Name = name;
            _derivative = deriv;
            _antiderivative = integ;
        }

        private static readonly (string, string)[] Powers =
        {
            ("⋅s³", "⋅s²"),
            ("⋅s²", "⋅s"),
            ( "/s", "/s²" ),
            ( "/s²", "/s³" ),
        };
        
        public static string IncreasePower(string suffix)
        {
			if (suffix.EndsWith("/s"))
				return suffix[..^2];
		
			foreach (var (bef, aft) in Powers)
            {
                if (suffix.EndsWith(aft, StringComparison.InvariantCulture))
                    return suffix[..^aft.Length] + bef;
            }

            return suffix + "⋅s";
        }
        
        public static string DecreasePower(string suffix)
        {
			if (suffix.EndsWith("⋅s"))
				return suffix[..^2];
		
            foreach (var (bef, aft) in Powers)
            {
                if (suffix.EndsWith(bef, StringComparison.InvariantCulture))
                    return suffix[..^bef.Length] + aft;
            }

            return suffix + "/s";
        }

        public static Unit FromString(string name, Unit? deriv = null, Unit? integ = null)
        {
            Unit res;

            var lDeriv = new Lazy<Unit>(() => deriv ?? FromString(DecreasePower(name)));
            var lInteg = new Lazy<Unit>(() => integ ?? FromString(IncreasePower(name)));

            if (!_units.TryGetValue(name, out res!))
            {
                res = _units[name] = new Unit(name,
                    lDeriv,
                lInteg);
            }

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

        public static readonly Unit Length = FromString("m");
		public static readonly Unit Velocity = Length.Derivative;

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
}