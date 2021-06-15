/*{
    var rect = Simulation.Add(new Box(1, 0, 4, 1, Color.Red));
    rect.Mass = 13;
    var hinge = Simulation.Add(new Hinge(0.2f, rect, new Vector2f(-1, 0)));
    hinge.MotorTorque = 20;
    hinge.MotorSpeed = 15;
    //hinge.Motor = true;
}
{
    var rect = Simulation.Add(new Box(7, 0, 4, 1, Color.Red));
    rect.Mass = 13;
    var hinge = Simulation.Add(new Hinge(0.2f, rect, new Vector2f(1, 0), null, new Vector2f(8, 0)));
    hinge.MotorTorque = 20;
    hinge.MotorSpeed = 15;
    //hinge.Motor = true;
}*/

var r1 = Simulation.Add(new Box(0, 0, 8, 1, Color.Red));
var r2 = Simulation.Add(new Box(7, 0, 8, 1, Color.Black));
var h1 = Simulation.Add(new Hinge(0.2f, r1, new Vector2f(-3.5f, 0), null, new Vector2f(-3.5f, 0)));
var h2 = Simulation.Add(new Hinge(0.2f, r2, new Vector2f(-3.5f, 0), r1, new Vector2f(3.5f, 0)));