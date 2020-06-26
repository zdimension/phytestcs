var x = 0f;
var am = 0f;

for (var i = 0; i < 11; i++, x += 2, am += 0.1f)
{
    PhysicalObject obj;
    Simulation.World.Add(obj = PhysicalObject.Rectangle(x, 0, 1, 1, Color.Red));

    Simulation.World.Add(new Spring(100, 3, obj, new Vector2f(0.5f, 0.5f), null, new Vector2f(x + 0.5f, 2.5f))
    {
        Damping = am
    });
}