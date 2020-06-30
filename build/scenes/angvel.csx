var obj = PhysicalObject.Rectangle(0, 0, 0.75f, 0.25f, Color.Red);
obj.Mass=0.375f;
Simulation.World.Add(obj);

obj.AngularVelocity = 10;

var obj2 = PhysicalObject.Rectangle(0.25f, 0.25f, 0.125f, 0.125f, Color.Blue);
Simulation.world.Add(obj2);
obj2.Mass = 0.0325f;