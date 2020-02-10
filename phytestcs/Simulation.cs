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
    public class Simulation
    {
        public static float Gravity
        {
            get => _gravity;
            set { _gravity = value; UpdateGravity(); }
        }

        public static bool AirFriction { get; set; } = false;
        public static float AirFrictionMultiplier { get; set; } = 1;
        public static float AirFrictionLinear { get; set; } = 0.1f;
        public static float AirFrictionQuadratic { get; set; } = 0.01f;
        public static float AirDensity { get; set; } = 0.01f;

        public static Vector2f GravityVector { get; private set; }
        public static Transform GravityTransform = Transform.Identity;
        public static float GravityAngle
        {
            get => _gravityAngle;
            set
            {
                _gravityAngle = value;
                UpdateGravity();
            }
        } // 0 = vers le bas


        private static void UpdateGravity()
        {
            GravityTransform = Transform.Identity;
            GravityTransform.Rotate(_gravityAngle);
            GravityVector = GravityEnabled ? GravityTransform.TransformPoint(new Vector2f(0, -_gravity)) : default;
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
        public static float TimeScale = 1;
        public static bool Pause = true;
        public static readonly SynchronizedCollection<Object> World = new SynchronizedCollection<Object>();
        public static List<Object> WorldCache = null;
        public static PhysicalObject[] AttractorsCache = new PhysicalObject[0];
        public static PhysicalObject Player;
        public static float FPS;
        public static DateTime PauseA;
        public static volatile float SimDuration;

        public const float TargetUPS = 120;
        public const float TargetDT = 1 / TargetUPS;


        public static void TogglePause()
        {
            if (!(Pause = !Pause))
            {
                PauseA = DateTime.Now;
            }

            UI.btnPlay.Image = Pause ? UI.imgPlay : UI.imgPause;
            UI.btnPlay.SetRenderer(!Pause ? UI.brRed : UI.brGreen);
        }

        private static float _gravityAngle;
        private static float _gravity = 9.81f;
        private static bool _gravityEnabled = true;
        public static event Action AfterUpdate;

        public static void UpdatePhysics()
        {
            if (Pause) return;

            UpdatePhysicsInternal(TargetDT * TimeScale);

            AfterUpdate?.Invoke();
        }

        public static void UpdatePhysicsInternal(float dt)
        {
            lock (World.SyncRoot)
            {
                var _cache = World.ToArray();

                AttractorsCache = _cache.OfType<PhysicalObject>().Where(o => o.Attraction != 0f).ToArray();

                SimDuration += dt;

                foreach (var o in _cache)
                    o.UpdatePhysics(dt);
            }
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
