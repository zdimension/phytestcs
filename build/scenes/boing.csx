Simulation.Add(PhysicalObject.Rectangle(0, -0.5f, 20, 1, Color.Green, true, "murBas"));
Simulation.Add(PhysicalObject.Rectangle(0, 16.5f, 20, 1, Color.Green, true, "murHaut"));
PhysicalObject mg, md;
Simulation.Add(mg = PhysicalObject.Rectangle(-9.5f, 7.5f, 1, 15, Color.Green, true, "murGauche"));
Simulation.Add(md = PhysicalObject.Rectangle(10.5f, 7.5f, 1, 15, Color.Green, true, "murDroite"));

mg.Restitution = md.Restitution = 1;

var testt = PhysicalObject.Rectangle(-1, 0.5f, 1, 1, Color.Blue);
testt.Restitution = 1;
testt.Friction = 0;
testt.Velocity = new Vector2f(9, 0);
Simulation.Add(testt);


var testt2 = PhysicalObject.Rectangle(5, 0.5f, 1, 1, Color.Red);
testt2.Restitution = 1;
testt2.Friction = 0;
Simulation.Add(testt2);