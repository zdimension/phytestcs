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
        [ObjProp("Gravity strength", "m/s²")]
        public static float Gravity
        {
            get => _gravity;
            set { _gravity = value; UpdateGravity(); }
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
        [ObjProp("Angle du vent", "rad")]
        public static float WindAngle { get; set; } = 0;

        public static Vector2f WindVector => Tools.FromPolar(WindSpeed, WindAngle);

        public static Vector2f GravityVector { get; private set; }
        private static Transform GravityTransform = Transform.Identity;
        [ObjProp("Gravity angle", "rad")]
        public static float GravityAngle
        {
            get => _gravityAngle;
            set
            {
                _gravityAngle = value;
                UpdateGravity();
            }
        }


        private static void UpdateGravity()
        {
            GravityTransform = Transform.Identity;
            GravityTransform.Rotate(_gravityAngle);
            GravityVector = GravityEnabled ? GravityTransform.TransformPoint(new Vector2f(_gravity, 0)) : default;
        }

        static Simulation()
        {
            UpdateGravity();
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

        public static float ActualGravity;
        public static float EscapeVelocity = 55;
        public static float Jump = 40;
        public static float Walk = 15;
        [ObjProp("Simulation speed", "x")]
        public static float TimeScale { get; set; } = 1;
        public static bool Pause = true;
        public static readonly SynchronizedCollection<Object> World = new SynchronizedCollection<Object>();
        public static Object[] WorldCache = null;
        public static PhysicalObject[] AttractorsCache = Array.Empty<PhysicalObject>();
        public static PhysicalObject Player;
        public static float FPS;
        public static DateTime PauseA;
        public static volatile float SimDuration;

        public const float TargetUPS = 100;
        public const float TargetDT = 1 / TargetUPS;
        private static float ActualDT => TargetDT * TimeScale;

        public static void Add(Object obj)
        {
            World.Add(obj);

            foreach (var par in obj.Parents)
                if (!World.Contains(par))
                    World.Add(par);
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

            UI.btnPlay.Image = Pause ? UI.imgPlay : UI.imgPause;
            UI.btnPlay.SetRenderer(!Pause ? UI.brRed : UI.brGreen);
        }

        private static float _gravityAngle = -90;
        private static float _gravity = 9.81f;
        private static bool _gravityEnabled = true;
        public static event Action AfterUpdate;
        public static DateTime LastUpdate = DateTime.Now;
        public static volatile float UPS;

        public static void UpdatePhysics(bool force=false)
        {
            if (Pause && !force) return;

            var dt = (float)(DateTime.Now - LastUpdate).TotalSeconds;

            LastUpdate = DateTime.Now;

            UPS = 1 / dt;

            UpdatePhysicsInternal(ActualDT);

            AfterUpdate?.Invoke();
        }

        public static void UpdatePhysicsInternal(float dt)
        {
            var _cache = World.ToArrayLocked();
            var phy = _cache.OfType<PhysicalObject>().ToArray();

            AttractorsCache = phy.Where(o => o.Attraction != 0f).ToArray();

            SimDuration += dt;

            foreach (var o in _cache)
                o.UpdatePhysics(dt);

            if (dt != 0)
                PhysicalObject.ProcessPairs(dt, phy);
        }

        public static float AttractionEnergy(Vector2f pos, float mass = 1f, PhysicalObject excl = null)
        {
            return AttractorsCache.Where(o => o != excl).Sum(o => o.AttractionEnergyCaused(pos, mass));
        }

        public static Vector2f AttractionField(Vector2f pos, float mass=1f, PhysicalObject excl = null)
        {
            return AttractorsCache.Where(o => o != excl).Sum(o => o.AttractionField(pos, mass));
        }

        public static Vector2f GravityField(Vector2f pos, float mass=1f, PhysicalObject excl=null)
        {
            return GravityVector * mass + AttractionField(pos, mass, excl);
        }
    }
}
