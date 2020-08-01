﻿var obj1 = Simulation.Add(new Box(0, 0, 1, 1, Color.Green));
var obj2 = Simulation.Add(new Box(5, 0, 1, 1, Color.Red));

Simulation.Add(new Spring(100, 1, 0.1f, obj1, default, obj2, default)
{
    Damping = 0
});