Simulation.World.Add(PhysicalObject.Rectangle(-10, -1, 20, 1, Color.Green, true, "murBas"));
Simulation.World.Add(PhysicalObject.Rectangle(-10, 16, 20, 1, Color.Green, true, "murHaut"));
PhysicalObject mg, md;
Simulation.World.Add(mg = PhysicalObject.Rectangle(-10, 0, 1, 15, Color.Green, true, "murGauche"));
Simulation.World.Add(md = PhysicalObject.Rectangle(10, 0, 1, 15, Color.Green, true, "murDroite"));

mg.Restitution = md.Restitution = 1;

var testt = PhysicalObject.Rectangle(-1, 0, 1, 1, Color.Blue);
testt.Restitution = 1;
testt.Friction = 0;
testt.Speed = new Vector2f(9, 0);
Simulation.World.Add(testt);


var testt2 = PhysicalObject.Rectangle(5, 0, 1, 1, Color.Blue);
testt2.Restitution = 1;
testt2.Friction = 0;
Simulation.World.Add(testt2);