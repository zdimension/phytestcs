Program.CurrentPalette = Palette.Palettes[9].palette;
var laser = Simulation.Add(new Laser(null, new Vector2f(-1f, 6.5f), 0.73017f){Color=Color.White});

Simulation.Add(new Polygon(1f, 5.66666f, new[] {
    new Vector2f(0, 0),
    new Vector2f(2, 0),
    new Vector2f(1, 2)
}, new Color(255, 255, 255, 0))); 