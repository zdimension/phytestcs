using System;
using System.Collections.Generic;
using System.Linq;
using phytestcs.Interface;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using Object = phytestcs.Objects.Object;

namespace phytestcs
{
    public static class Simulation
    {
        public const float TargetUPS = 100;
        public const float TargetDT = 1 / TargetUPS;
        private static Transform GravityTransform = Transform.Identity;

        public static float ActualGravity;
        public static float EscapeVelocity = 55;
        public static float Jump = 40;
        public static float Walk = 15;
        public static bool Pause = true;
        public static readonly SynchronizedCollection<Object> World = new SynchronizedCollection<Object>();
        public static PhysicalObject[] AttractorsCache = Array.Empty<PhysicalObject>();
        public static PhysicalObject Player;
        public static float FPS;
        public static DateTime PauseA;
        public static volatile float SimDuration;

        private static float _gravityAngle = -90;
        private static float _gravity = 9.81f;
        private static bool _gravityEnabled = true;
        public static DateTime LastUpdate = DateTime.Now;
        public static volatile float UPS;

        public static Object[] WorldCache = null;
        public static PhysicalObject[] WorldCachePhy = null;

        static Simulation()
        {
            UpdateGravity();
        }

        [ObjProp("Gravity strength", "m/s²")]
        public static float Gravity
        {
            get => _gravity;
            set
            {
                _gravity = value;
                UpdateGravity();
            }
        }

        public static bool AirFriction { get; set; } = false;

        [ObjProp("Multiplier", "x")]
        public static float AirFrictionMultiplier { get; set; } = 1;

        [ObjProp("Linear term", "N/(m²/s)")]
        public static float AirFrictionLinear { get; set; } = 0.1f;

        [ObjProp("Quadratic term", "N/(m³/s²)")]
        public static float AirFrictionQuadratic { get; set; } = 0.01f;

        [ObjProp("Air density", "kg/m²")]
        public static float AirDensity { get; set; } = 0.01f;

        [ObjProp("Amortissement de rotation", "s⁻¹")]
        public static float RotFrictionLinear { get; set; } = 0.0314f;

        [ObjProp("Wind speed", "m/s")]
        public static float WindSpeed { get; set; } = 0;

        [ObjProp("Wind angle", "rad")]
        public static float WindAngle { get; set; } = 0;

        public static Vector2f WindVector => Tools.FromPolar(WindSpeed, WindAngle);

        public static Vector2f GravityVector { get; private set; }

        [ObjProp("Gravity angle", "°")]
        public static float GravityAngle
        {
            get => _gravityAngle;
            set
            {
                _gravityAngle = value;
                UpdateGravity();
            }
        }

        public static bool GravityEnabled
        {
            get => _gravityEnabled;
            set
            {
                _gravityEnabled = value;
                ActualGravity = value ? Gravity : 0;
                UpdateGravity();
            }
        }

        [ObjProp("Simulation speed", "x")]
        public static float TimeScale { get; set; } = 1;

        public static float ActualDT => TargetDT * TimeScale;


        private static void UpdateGravity()
        {
            GravityTransform = Transform.Identity;
            GravityTransform.Rotate(_gravityAngle);
            GravityVector = GravityEnabled ? GravityTransform.TransformPoint(new Vector2f(_gravity, 0)) : default;
        }

        private static void AddInternal(Object obj)
        {
            World.Add(obj);
            obj.OnSpawn.Invoke(new BaseEventArgs(obj));
        }

        public static T Add<T>(T obj)
            where T : Object
        {
            AddInternal(obj);

            foreach (var par in obj.Parents)
                if (!World.Contains(par))
                    AddInternal(par);

            return obj;
        }

        public static void Clear()
        {
            World.Clear();
        }

        public static void Remove(Object obj)
        {
            World.Remove(obj);
        }


        public static void TogglePause()
        {
            SetPause(!Pause);
        }

        public static void SetPause(bool val)
        {
            if (!(Pause = val))
            {
                PauseA = DateTime.Now;
            }

            Ui.BtnPlay.Image = Pause ? Ui.ImgPlay : Ui.ImgPause;
            Ui.BtnPlay.SetRenderer(!Pause ? Ui.BrRed : Ui.BrGreen);
        }

        public static event Action AfterUpdate;

        public static void UpdatePhysics(bool force = false)
        {
            if (Pause && !force) return;

            var dt = (float) (DateTime.Now - LastUpdate).TotalSeconds;

            LastUpdate = DateTime.Now;

            UPS = 1 / dt;

            UpdatePhysicsInternal(ActualDT);

            AfterUpdate?.Invoke();
        }

        public static void UpdatePhysicsInternal(float dt)
        {
            WorldCache = World.ToArrayLocked();
            WorldCachePhy = WorldCache.OfType<PhysicalObject>().ToArray();

            AttractorsCache = WorldCachePhy.Where(o => o.Attraction != 0f).ToArray();

            SimDuration += dt;

            foreach (var o in WorldCache)
                o.UpdatePhysics(dt);

            if (dt != 0)
            {
                PhysicalObject.ProcessPairs(dt, WorldCachePhy);

                foreach (var o in WorldCache)
                    o.PostStep.Invoke(new PostStepEventArgs(o, dt));
            }
        }

        public static float AttractionEnergy(Vector2f pos, float mass = 1f, PhysicalObject excl = null)
        {
            return AttractorsCache.Where(o => o != excl).Sum(o => o.AttractionEnergyCaused(pos, mass));
        }

        public static Vector2f AttractionField(Vector2f pos, float mass = 1f, PhysicalObject excl = null)
        {
            return AttractorsCache.Where(o => o != excl).Sum(o => o.AttractionField(pos, mass));
        }

        public static Vector2f GravityField(Vector2f pos, float mass = 1f, PhysicalObject excl = null)
        {
            return GravityVector * mass + AttractionField(pos, mass, excl);
        }
    }
}