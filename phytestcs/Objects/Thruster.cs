﻿using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;

namespace phytestcs.Objects
{
    public sealed class Thruster : PinnedShapedVirtualObject
    {
        private readonly Force _force = new Force(ForceType.Thruster, default, default);
        private readonly CircleShape _shape = new CircleShape();

        public Thruster(PhysicalObject @object, Vector2f relPos, float size, float force = 5, ForceType? type = null)
            : base(@object, relPos)
        {
            Size = size;
            Force = force;

            _force.Source = this;
            if (type != null)
                _force.Type = type;
            Object.Forces.Add(_force);

            UpdatePhysics(0);
        }

        public override Vector2f RelPos
        {
            get => _force.Position;
            set => _force.Position = value;
        }

        public float Size
        {
            get => _shape.Radius * 2;
            set
            {
                _shape.Radius = value / 2;
                _shape.CenterOrigin();
            }
        }

        [ObjProp("Force", "N", shortName: "F")]
        public float Force { get; set; }

        //public bool FollowGeometry { get; set; } = true;
        public override Shape Shape => _shape;
        protected override IEnumerable<Shape> Shapes => new[] { _shape };

        public override void Delete(BaseObject? source = null)
        {
            Object.Forces.Remove(_force);

            base.Delete(source);
        }

        public override void UpdatePhysics(float dt)
        {
            base.UpdatePhysics(dt);

            _force.Value = Tools.FromPolar(Force, ActualAngle);
        }
    }
}