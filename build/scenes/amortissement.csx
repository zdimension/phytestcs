var x = 0f;
var am = 0f;
var N = 1;

for (var i = 0; i < N; i++, x += 2, am += 0.1f)
{
    PhysicalObject obj;
    Simulation.Add(obj = new Box(x, 0, 1, 1, Color.Red));

    Simulation.Add(new Spring(100, 3, 0.4f, obj, default, null, new Vector2f(x, 2f))
    {
        Damping = am
    });
}

x -= 2;
am = 1;

for (var i = 0; i < N; i++, x -= 2, am /= 2f)
{
    PhysicalObject obj;
    Simulation.Add(obj = new Box(x, 10, 1, 1, Color.Red));

    Simulation.Add(new Spring(100, 3, 0.4f, obj, default, null, new Vector2f(x, 12f))
    {
        Damping = am
    });
}