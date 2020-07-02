using System;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using phytestcs.Interface;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using TGUI;
using Object = phytestcs.Objects.Object;

namespace phytestcs
{
    public sealed class Scene
    {
        public static volatile bool Loaded;
        public static Script<object> Script;

        public static void Restart()
        {
            Load(Script);
        }

        public static Script<object> LoadScript(string file="scenes/energie.csx")
        {
            return CSharpScript.Create(
                File.ReadAllText(file),
                ScriptOptions.Default
                    .AddReferences(typeof(Scene).Assembly, typeof(Color).Assembly,
                        typeof(Vector2f).Assembly)
                    .AddImports("phytestcs", "phytestcs.Objects", "SFML.Graphics", "SFML.System",
                        "System"));
        }

        public static void Load(Script<object> scr =null)
        {
            Simulation.Pause = true;
            Simulation.GravityEnabled = true;

            Simulation.SimDuration = 0;
            UI.ClearPropertyWindows();
            Simulation.World.Clear();
            Simulation.WorldCache = Array.Empty<Object>();
            Simulation.AttractorsCache = Array.Empty<PhysicalObject>();
            Simulation.Player = null;

            Simulation.World.Add(PhysicalObject.Rectangle(-5000, -5100, 10000, 100, Color.Black, true, "murBas", true));
            Simulation.World.Add(PhysicalObject.Rectangle(-5000, 5000, 10000, 100, Color.Black, true, "murHaut", true));
            Simulation.World.Add(PhysicalObject.Rectangle(-5100, -5000, 100, 10000, Color.Black, true, "murGauche", true));
            Simulation.World.Add(PhysicalObject.Rectangle(5000, -5000, 100, 10000, Color.Black, true, "murDroite", true));

            Console.WriteLine("Début compilation");
            Script = scr ?? LoadScript();
            Console.WriteLine("Fin compilation et début exécution");
            try
            {
                Script.CreateDelegate()();
            }
            catch (Exception e)
            {
                var text = "Erreur de chargement :\n" + e;
                Console.WriteLine(text);
                var msgbox = new MessageBox("Erreur", text, new[] {"OK"});
                UI.GUI.Add(msgbox);
                msgbox.SizeLayout = new Layout2d("800", "200");
                msgbox.PositionLayout = new Layout2d("&.w / 2 - w / 2", "&.h / 2 - h / 2");
                msgbox.ButtonPressed += delegate
                {
                    msgbox.CloseWindow();
                    UI.GUI.Remove(msgbox);
                };
            }

            Console.WriteLine("Fin exécution");

            Simulation.Player?.Forces.Add(Program.MoveForce);
            Simulation.UpdatePhysicsInternal(0);

            Loaded = true;
            Simulation.Pause = false;
            Simulation.TogglePause();
        }

        public static void SoftbodyStaggered(int N=6)
        {
            var square = new PhysicalObject[N + 1][];
            var spring = 500;
            var dist = 1.5f;
            var distY = (float) (Math.Sqrt(3) / 2 * dist);
            var diago = (float)Math.Sqrt(2) * dist;

            void r(int x1, int y1, int x2, int y2)
            {
                Simulation.World.Add(new Spring(spring, dist, square[y1][x1], new Vector2f(0.5f, 0.5f), square[y2][x2],
                        new Vector2f(0.5f, 0.5f))
                    { ShowInfos = false });
            }

            for (var i = 0; i < N + 1; i++)
            {
                square[i] = new PhysicalObject[N];
                for (var j = 0; j < N - i % 2; j++)
                {
                    square[i][j] = PhysicalObject.Rectangle(i % 2 * 0.75f + j * dist, 18 + i * distY, 1, 1, Color.Cyan, name: "Softbody");
                    Simulation.World.Add(square[i][j]);

                    if (j > 0)
                    {
                        r(j - 1, i, j, i);
                    }

                    if (i > 0)
                    {
                        if (i % 2 == 1)
                        {
                            if (j < N - 1)
                                r(j, i, j, i - 1);
                            r(j, i, j + 1, i - 1);
                        }
                        else
                        {
                            if (j < N - 1)
                                r(j, i, j, i - 1);

                            if (j > 0)
                                r(j, i, j - 1, i - 1);
                        }
                    }
                }
            }
        }

        public static void SoftbodySquare(int N=6)
        {
            var square = new PhysicalObject[N][];
            var spring = 500;
            var dist = 1.5f;
            var diago = (float)Math.Sqrt(2) * dist;
            for (var i = 0; i < N; i++)
            {
                square[i] = new PhysicalObject[N];
                for (var j = 0; j < N; j++)
                {
                    square[i][j] = PhysicalObject.Rectangle(j * dist, 18 + i * dist, 1, 1, Color.Cyan, name: "Softbody");
                    Simulation.World.Add(square[i][j]);

                    if (j > 0)
                    {
                        Simulation.World.Add(new Spring(spring, dist, square[i][j - 1], new Vector2f(0.5f, 0.5f), square[i][j],
                                new Vector2f(0.5f, 0.5f))
                            { ShowInfos = false });
                    }

                    if (i > 0)
                    {
                        Simulation.World.Add(new Spring(spring, dist, square[i - 1][j], new Vector2f(0.5f, 0.5f), square[i][j],
                                new Vector2f(0.5f, 0.5f))
                            { ShowInfos = false });

                        if (j > 0)
                        {
                            Simulation.World.Add(new Spring(spring, diago, square[i - 1][j - 1], new Vector2f(0.5f, 0.5f),
                                    square[i][j],
                                    new Vector2f(0.5f, 0.5f))
                                { ShowInfos = false });

                            Simulation.World.Add(new Spring(spring, diago, square[i - 1][j], new Vector2f(0.5f, 0.5f),
                                    square[i][j - 1],
                                    new Vector2f(0.5f, 0.5f))
                                { ShowInfos = false });
                        }
                    }
                }
            }
        }
    }
}
