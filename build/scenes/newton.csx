var N = 5;
var objs = new PhysicalObject[N];

for (var i = 0; i < N; i++)
{
    Simulation.World.Add(objs[i] = PhysicalObject.Rectangle(-3 + i, 2, 1, 1, Color.Red));
    objs[i].Restitution = 1;
    objs[i].Friction = 0;
    Simulation.World.Add(new Spring(3000, 3, objs[i], new Vector2f(0.5f, 0.5f), null, new Vector2f(-2.5f + i, 5.5f)) { ShowInfos = false });
}

objs[0].Position = new Vector2f(-6f, 5f);

