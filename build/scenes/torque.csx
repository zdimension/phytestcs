var obj = new Box(0, 0, 5, 1, Color.Red);
Simulation.Add(obj);

Simulation.Add(new Spring(100, 3, 0.1f, obj, new Vector2f(-2, 0), null, new Vector2f(-2, 2))
{
	Damping = 0
});

Simulation.Add(new Spring(50, 3, 0.1f, obj, new Vector2f(2, 0), null, new Vector2f(2, 2))
{
	Damping = 0
});