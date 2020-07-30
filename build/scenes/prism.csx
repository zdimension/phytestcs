Program.CurrentPalette = Palette.Palettes[9].palette;
var laser = Simulation.Add(new Laser(null, new Vector2f(-1f, 6.5f), 0.73017f){Color=Color.White});

var shape = new ConvexShape(3);
shape.SetPoint(0, new Vector2f(0, 0));
shape.SetPoint(1, new Vector2f(2, 0));
shape.SetPoint(2, new Vector2f(1, 2));
shape.FillColor = new Color(0, 0, 0, 0);
Simulation.Add(new PhysicalObject(new Vector2f(1f, 5.66666f), shape));