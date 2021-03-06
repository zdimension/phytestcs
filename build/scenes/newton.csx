﻿var N = 5;
var objs = new PhysicalObject[N];

for (var i = 0; i < N; i++)
{
    Simulation.Add(objs[i] = new Box(-2 + i, 2, 1, 1, Color.Red));
    objs[i].Restitution = 1;
    objs[i].Friction = 0;
    objs[i].LockAngle = true;
    Simulation.Add(new Spring(3000, 3, 0.5f, objs[i], default, null, new Vector2f(-2f + i, 5f)) { ShowInfos = false });
}

objs[0].Position = new Vector2f(-5f, 5f);

