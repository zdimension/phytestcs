var obj1 = Simulation.Add(PhysicalObject.Rectangle(2, -0.3f, 1, 1, Color.Green));
obj1.Angle=(float)Math.PI / 5;
obj1.Color = new Color(0, 255, 0, 10);

var obj2 = Simulation.Add(PhysicalObject.Rectangle(2, 2.3f, 1, 1, Color.Green));
obj2.Angle=(float)Math.PI / 4;
obj2.Color = new Color(0, 255, 0, 10);


var laser = Simulation.Add(new Laser(null, new Vector2f(0, 0), 0.1f){Color=Color.Red});