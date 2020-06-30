var obj = PhysicalObject.Rectangle(0, 0, 5, 1, Color.Red);
Simulation.World.Add(obj);

Simulation.World.Add(new Spring(100, 3, obj, new Vector2f(-2, 0), null, new Vector2f(-2, 2))
{
	Damping = 0
});

Simulation.World.Add(new Spring(50, 3, obj, new Vector2f(2, 0), null, new Vector2f(2, 2))
{
	Damping = 0
});