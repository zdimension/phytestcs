Simulation.Add(Simulation.Joueur = PhysicalObject.Rectangle(-3, 4, 1, 1, Color.Red, name: "Joueur"));
var rect = PhysicalObject.Rectangle(-3, 7, 1, 1, Color.Magenta, true);
rect.Attraction = 9.81f * 9f / 10f;
rect.Mass = 10f;
Simulation.Add(rect);
