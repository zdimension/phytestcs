var obj1 = PhysicalObject.Rectangle(0, 0, 1, 1, Color.Green);
var obj2 = PhysicalObject.Rectangle(5, 0, 1, 1, Color.Red);

Simulation.Add(obj1);
Simulation.Add(obj2);

Simulation.Add(new Spring(100, 1, 0.1f, obj1, default, obj2, default)
{
    Damping = 0
});