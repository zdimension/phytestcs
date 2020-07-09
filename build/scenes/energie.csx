PhysicalObject obj;
Simulation.Add(obj = PhysicalObject.Rectangle(0, 0, 1, 1, Color.Red));

Simulation.Add(new Spring(100, 3, 0.1f, obj, default, null, new Vector2f(0f, 2f))
{
    Damping = 0
});