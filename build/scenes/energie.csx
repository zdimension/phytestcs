PhysicalObject obj;
Simulation.World.Add(obj = PhysicalObject.Rectangle(0, 0, 1, 1, Color.Red));

Simulation.World.Add(new Spring(100, 3, obj, new Vector2f(0.5f, 0.5f), null, new Vector2f(0.5f, 2.5f))
{
    Damping = 0
});