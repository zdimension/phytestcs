using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SFML.Graphics;
using SFML.System;
using static phytestcs.Global;

namespace phytestcs.Objects
{
    public abstract class Object : DispatchProxy, IDisposable
    {
        private static ulong _idCounter;

        private readonly Dictionary<MethodInfo, Func<object>> _bindings = new Dictionary<MethodInfo, Func<object>>();
        private readonly SynchronizedCollection<Object> dependents = new SynchronizedCollection<Object>();

        private readonly SynchronizedCollection<Object> parents = new SynchronizedCollection<Object>();
        private bool _selected;
        public ObjectAppearance Appearance = Program.CurrentPalette.Appearance;

        protected Object()
        {
            ID = _idCounter++;
        }

        public ulong ID { get; }

        public string Name { get; set; }

        public abstract IEnumerable<Shape> Shapes { get; }

        public virtual Color Color
        {
            get => Shapes.First().FillColor;
            set
            {
                foreach (var s in Shapes)
                    s.FillColor = value;
                UpdateOutline();
            }
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

        public IReadOnlyList<Object> Parents => parents.ToList().AsReadOnly();

        public IReadOnlyList<Object> Dependents => dependents.ToList().AsReadOnly();

        public virtual void Dispose()
        {
            //
        }

        private void UpdateOutline()
        {
            Color color;
            if (Selected)
                color = Color.White;
            else
            {
                color = Color.Multiply(0.5f);
                if (Appearance.OpaqueBorders)
                    color.A = 255;
            }

            OutlineColor = color;
        }

        public void DependsOn(Object other)
        {
            other.dependents.Add(this);
            parents.Add(other);
        }

        public void BothDepends(Object other)
        {
            DependsOn(other);
            other.DependsOn(this);
        }

        public event Action Deleted;

        public virtual void Delete(Object source = null)
        {
            while (dependents.Any())
            {
                var first = dependents.FirstOrDefault();
                if (first == source)
                    dependents.Remove(first);
                else
                    first.Delete(this);
            }

            lock (parents.SyncRoot)
            {
                foreach (var obj in parents)
                    obj.dependents.Remove(this);
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
            Deleted?.Invoke();
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

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (_bindings.TryGetValue(targetMethod, out var func))
                return func();
            return targetMethod.Invoke(this, args);
        }

        public void Bind<Tthis, T>(Expression<Func<Tthis, T>> prop, Func<T> value)
        {
            if (typeof(Tthis) != GetType())
                throw new ArgumentException(nameof(Tthis));

            var expr = ((MemberExpression) prop.Body);
            var BindPropInfo = (PropertyInfo) expr.Member;
            var getMethod = BindPropInfo.GetGetMethod()!;

            _bindings[getMethod] = () => value();
        }

        public void Unbind<Tthis, T>(Expression<Func<Tthis, T>> prop)
        {
            if (typeof(Tthis) != GetType())
                throw new ArgumentException(nameof(Tthis));

            var expr = ((MemberExpression) prop.Body);
            var BindPropInfo = (PropertyInfo) expr.Member;
            var getMethod = BindPropInfo.GetGetMethod()!;

            _bindings.Remove(getMethod);
        }
    }

    [AttributeUsageAttribute(AttributeTargets.All)]
    public class ObjPropAttribute : DisplayNameAttribute
    {
        public ObjPropAttribute(string displayName, string unit = "", string unitInteg = null, string unitDeriv = null)
            : base(L[displayName])
        {
            Unit = unit;
            UnitInteg = unitInteg ?? (unit + "⋅s");
            UnitDeriv = unitDeriv ?? (unit + "/s");
        }

        public string Unit { get; set; }
        public string UnitInteg { get; set; }
        public string UnitDeriv { get; set; }
    }

    public class Binding<T>
    {
        public Action<T> Setter { get; set; }
        public Expression<Func<T>> Value { get; set; }
    }
}