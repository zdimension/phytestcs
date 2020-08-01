Simulation.Add(new Box(0, -0.5f, 20, 1, Color.Green, true, "murBas"));
Simulation.Add(new Box(0, 16.5f, 20, 1, Color.Green, true, "murHaut"));
PhysicalObject mg, md;
Simulation.Add(mg = new Box(-9.5f, 7.5f, 1, 15, Color.Green, true, "murGauche"));
Simulation.Add(md = new Box(10.5f, 7.5f, 1, 15, Color.Green, true, "murDroite"));

mg.Restitution = md.Restitution = 1;

var testt = new Box(-1, 0.5f, 1, 1, Color.Blue);
testt.Restitution = 1;
testt.Friction = 0;
testt.Velocity = new Vector2f(9, 0);
testt.Name = "B";
Simulation.Add(testt);


var testt2 = new Box(5, 0.5f, 1, 1, Color.Red);
testt2.Restitution = 1;
testt2.Friction = 0;
testt2.Name = "R";
Simulation.Add(testt2);