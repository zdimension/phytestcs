Simulation.World.Add(PhysicalObject.Rectangle(-10, -1, 20, 1, Color.Green, true, "murBas"));
Simulation.World.Add(PhysicalObject.Rectangle(-10, 16, 20, 1, Color.Green, true, "murHaut"));
Simulation.World.Add(PhysicalObject.Rectangle(-10, 0, 1, 15, Color.Green, true, "murGauche"));
Simulation.World.Add(PhysicalObject.Rectangle(10, 0, 1, 15, Color.Green, true, "murDroite"));

Simulation.World.Add(Simulation.Player = PhysicalObject.Rectangle(0, 0, 1, 1, Color.Red, name: "Player"));

var testt = PhysicalObject.Rectangle(-1, 10, 1, 1, Color.Blue);
Simulation.World.Add(testt);
var testt2 = PhysicalObject.Rectangle(3, 2, 1, 1, Color.Magenta);

Simulation.World.Add(testt2);

Simulation.World.Add(new Spring(30, 10, testt, new Vector2f(0.5f, 0.5f), null, new Vector2f(-9.5f, 15.5f)) { Damping = 0 });
Simulation.World.Add(new Spring(30, 10, testt, new Vector2f(0.5f, 0.5f), null, new Vector2f(10.5f, -0.5f)) { Damping = 0 });
Simulation.World.Add(new Spring(30, 10, testt, new Vector2f(0.5f, 0.5f), null, new Vector2f(-9.5f, -0.5f)) { Damping = 0 });
Simulation.World.Add(new Spring(30, 10, testt, new Vector2f(0.5f, 0.5f), null, new Vector2f(10.5f, 15.5f)) { Damping = 0 });
Simulation.World.Add(new Spring(30, 5, testt, new Vector2f(0.5f, 0.5f), testt2, new Vector2f(0.5f, 0.5f)) { Damping = 0 });