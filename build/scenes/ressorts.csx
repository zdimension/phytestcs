﻿Simulation.Add(PhysicalObject.Rectangle(-10, -1, 20, 1, Color.Green, true, "murBas"));
Simulation.Add(PhysicalObject.Rectangle(-10, 16, 20, 1, Color.Green, true, "murHaut"));
Simulation.Add(PhysicalObject.Rectangle(-10, 0, 1, 15, Color.Green, true, "murGauche"));
Simulation.Add(PhysicalObject.Rectangle(10, 0, 1, 15, Color.Green, true, "murDroite"));

Simulation.Add(Simulation.Player = PhysicalObject.Rectangle(0, 0, 1, 1, Color.Red, name: "Player"));

var testt = PhysicalObject.Rectangle(-1, 10, 1, 1, Color.Blue);
Simulation.Add(testt);
var testt2 = PhysicalObject.Rectangle(3, 2, 1, 1, Color.Magenta);

Simulation.Add(testt2);

Simulation.Add(new Spring(30, 10, 0.1f, testt, new Vector2f(0.5f, 0.5f), null, new Vector2f(-9.5f, 15.5f)) { Damping = 0 });
Simulation.Add(new Spring(30, 10, 0.1f, testt, new Vector2f(0.5f, 0.5f), null, new Vector2f(10.5f, -0.5f)) { Damping = 0 });
Simulation.Add(new Spring(30, 10, 0.1f, testt, new Vector2f(0.5f, 0.5f), null, new Vector2f(-9.5f, -0.5f)) { Damping = 0 });
Simulation.Add(new Spring(30, 10, 0.1f, testt, new Vector2f(0.5f, 0.5f), null, new Vector2f(10.5f, 15.5f)) { Damping = 0 });
Simulation.Add(new Spring(30, 5, 0.1f, testt, new Vector2f(0.5f, 0.5f), testt2, new Vector2f(0.5f, 0.5f)) { Damping = 0 });