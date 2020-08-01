Simulation.Add(Simulation.Joueur = new Box(-3, 4, 1, 1, Color.Red, name: "Joueur"));
var rect = new Box(-3, 7, 1, 1, Color.Magenta, true);
rect.Attraction = 9.81f * 9f / 10f;
rect.Mass = 10f;
Simulation.Add(rect);
