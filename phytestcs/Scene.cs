﻿using System;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using phytestcs.Interface;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using TGUI;
using static phytestcs.Tools;

namespace phytestcs
{
    public sealed class Scene
    {
        public static volatile bool Loaded;
        public static Script<object>? Script;
        public static ExpandoObject My;

        public static readonly ScriptOptions DefaultScriptOptions = ScriptOptions.Default
            .AddReferences(
                typeof(Scene).Assembly,
                typeof(Color).Assembly,
                typeof(Vector2f).Assembly,
                typeof(Console).GetTypeInfo().Assembly
            )
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).GetAssemblyLoadPath()),
                MetadataReference.CreateFromFile(Extensions.GetSystemAssemblyPathByName("System.Runtime.dll")),
                MetadataReference.CreateFromFile(Extensions.GetSystemAssemblyPathByName("System.Private.CoreLib.dll"))
            )
            .AddImports(
                "phytestcs",
                "phytestcs.Objects",
                "phytestcs.Tools",
                "SFML.Graphics",
                "SFML.System",
                "System");

        public static async Task Restart()
        {
            await Load(Script).ConfigureAwait(true);
        }

        public static Script<object> LoadScript(string file)
        {
            var scr = CSharpScript.Create(
                File.ReadAllText(file),
                DefaultScriptOptions);
            scr.Compile();
            return scr;
        }

        public static async Task New()
        {
            await Load(null).ConfigureAwait(true);
        }

        public static async Task Load(Script<object>? scr)
        {
            Simulation.Pause = true;
            Simulation.GravityEnabled = true;

            Simulation.SimDuration = 0;
            Ui.ClearPropertyWindows();
            Simulation.Clear();
            Render.WorldCache = Array.Empty<BaseObject>();
            Simulation.AttractorsCache = Array.Empty<PhysicalObject>();
            Simulation.Player = null;
            My = new ExpandoObject();

            Program.CurrentPalette = Palette.Default;

            Simulation.Add(new Box(-5000, -5100, 10000, 100, Color.Black, true, "murBas", true));
            Simulation.Add(new Box(-5000, 5000, 10000, 100, Color.Black, true, "murHaut", true));
            Simulation.Add(new Box(-5100, -5000, 100, 10000, Color.Black, true, "murGauche", true));
            Simulation.Add(new Box(5000, -5000, 100, 10000, Color.Black, true, "murDroite", true));

            if (scr != null)
            {
                Console.WriteLine("Début compilation");
                Script = scr;
                Console.WriteLine("Fin compilation et début exécution");
                try
                {
                    await Script.RunAsync().ConfigureAwait(true);
                    //Script.CreateDelegate()();
                }
                catch (Exception e)
                {
                    var text = L["Error while loading script:"] + "\n" + e;
                    Console.WriteLine(text);
                    var msgbox = new MessageBox(L["Error"], text, new[] { "OK" });
                    Ui.Gui.Add(msgbox);
                    msgbox.SizeLayout = new Layout2d("800", "200");
                    msgbox.PositionLayout = new Layout2d("&.w / 2 - w / 2", "&.h / 2 - h / 2");
                    msgbox.ButtonPressed += delegate
                    {
                        msgbox.CloseWindow();
                        Ui.Gui.Remove(msgbox);
                    };
                }

                Console.WriteLine(L["Script finished"]);

                Simulation.Player?.Forces.Add(Program.MoveForce);
            }


            Simulation.UpdatePhysicsInternal(0);

            Loaded = true;
            Simulation.Pause = false;
            Simulation.TogglePause();
        }

        public static void SoftbodyStaggered(int n = 6)
        {
            var square = new Box[n + 1][];
            var spring = 500;
            var dist = 1.5f;
            var distY = (float) (Math.Sqrt(3) / 2 * dist);
            var diago = (float) Math.Sqrt(2) * dist;

            void R(int x1, int y1, int x2, int y2)
            {
                Simulation.Add(new Spring(spring, dist, 0.1f, square[y1][x1], default, square[y2][x2])
                    { ShowInfos = false });
            }

            for (var i = 0; i < n + 1; i++)
            {
                square[i] = new Box[n];
                for (var j = 0; j < n - i % 2; j++)
                {
                    square[i][j] = new Box(i % 2 * 0.75f + j * dist, 18 + i * distY, 1, 1, Color.Cyan,
                        name: "Softbody");
                    Simulation.Add(square[i][j]);

                    if (j > 0)
                        R(j - 1, i, j, i);

                    if (i > 0)
                    {
                        if (i % 2 == 1)
                        {
                            if (j < n - 1)
                                R(j, i, j, i - 1);
                            R(j, i, j + 1, i - 1);
                        }
                        else
                        {
                            if (j < n - 1)
                                R(j, i, j, i - 1);

                            if (j > 0)
                                R(j, i, j - 1, i - 1);
                        }
                    }
                }
            }
        }

        public static void SoftbodySquare(int n = 6)
        {
            var square = new Box[n][];
            var spring = 500;
            var dist = 1.5f;
            var diago = (float) Math.Sqrt(2) * dist;
            for (var i = 0; i < n; i++)
            {
                square[i] = new Box[n];
                for (var j = 0; j < n; j++)
                {
                    square[i][j] =
                        new Box(j * dist, 18 + i * dist, 1, 1, Color.Cyan, name: "Softbody");
                    Simulation.Add(square[i][j]);

                    if (j > 0)
                        Simulation.Add(new Spring(spring, dist, 0.1f,
                                square[i][j - 1], default,
                                square[i][j])
                            { ShowInfos = false });

                    if (i > 0)
                    {
                        Simulation.Add(new Spring(spring, dist, 0.1f,
                                square[i - 1][j], default,
                                square[i][j])
                            { ShowInfos = false });

                        if (j > 0)
                        {
                            Simulation.Add(new Spring(spring, diago, 0.1f,
                                    square[i - 1][j - 1], default,
                                    square[i][j])
                                { ShowInfos = false });

                            Simulation.Add(new Spring(spring, diago, 0.1f,
                                    square[i - 1][j], default,
                                    square[i][j - 1])
                                { ShowInfos = false });
                        }
                    }
                }
            }
        }
    }
}