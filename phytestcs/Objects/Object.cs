using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SFML.System;

namespace phytestcs.Objects
{
    public abstract class Object
    {
        private readonly SynchronizedCollection<Object> parents = new SynchronizedCollection<Object>();
        private readonly SynchronizedCollection<Object> dependents = new SynchronizedCollection<Object>();

        public string Name { get; set; }

        public IReadOnlyList<Object> Parents => parents.ToList().AsReadOnly();

        public IReadOnlyList<Object> Dependents => dependents.ToList().AsReadOnly();

        public void DependsOn(Object other)
        {
            other.dependents.Add(this);
            parents.Add(other);
        }

        public event Action Deleted;

        public virtual void Delete()
        {
            while (dependents.Any())
                dependents.FirstOrDefault()?.Delete();

            lock (parents.SyncRoot)
            {
                foreach (var obj in parents)
                    obj.dependents.Remove(this);
            }

            Simulation.World.Remove(this);
            Simulation.UpdatePhysicsInternal(0);

            InvokeDeleted();

            if (Drawing.SelectedObject == this)
                Drawing.SelectedObject = null;
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
            return false;
        }
    }

    public class ObjPropAttribute : DisplayNameAttribute
    {
        public string Unit { get; set; }
        public string UnitInteg { get; set; }
        public string UnitDeriv { get; set; }

        public ObjPropAttribute(string displayName, string unit, string unitInteg=null, string unitDeriv=null)
        : base(displayName)
        {
            Unit = unit;
            UnitInteg = unitInteg ?? (unit + "⋅s");
            UnitDeriv = unitDeriv ?? (unit + "/s");
        }
    }
}
