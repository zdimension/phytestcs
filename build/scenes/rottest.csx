var rect = Simulation.Add(Simulation.Player = new Box(-8, 0, 18, 2, Color.Red));
rect.Mass = 72;
Simulation.Add(new Thruster(rect, new Vector2f(8.75f, 0), 0.3f){Force=50,Angle=(float)Math.PI/2});
Simulation.Add(new Thruster(rect, new Vector2f(7.25f, 0), 0.3f){Force=50,Angle=(float)Math.PI/-2});