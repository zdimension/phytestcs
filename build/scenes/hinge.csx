var rect = Simulation.Add(PhysicalObject.Rectangle(0, 0, 4, 1, Color.Red));
rect.Mass = 13;
var hinge = Simulation.Add(new Hinge(rect, default, 0.2f));
hinge.MotorTorque = 20;
hinge.MotorSpeed = 15;
hinge.Motor = true;