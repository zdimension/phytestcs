PhysicalObject obj;
Simulation.World.Add(obj = PhysicalObject.Rectangle(0, 0, 1, 1, Color.Red));

Simulation.World.Add(new Spring(100, 3, obj, default, null, new Vector2f(0f, 2f))
{
    Damping = 0
});