using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using TGUI;
using Panel = TGUI.Panel;

namespace phytestcs.Interface.Windows
{
    public class WndPlot : WndBase
    {
        private const int lGauche = 200;
        private const int lGraphe = 400;
        private const int hauteur = 300;
        private const int margeX = 5;
        private const int margeY = 5;
        private const int largeurBtn = lGauche - 2 * margeX;
        private const int hauteurBtn = 20;
        private const int hauteurLigne = hauteurBtn + margeY;

        private static readonly string[] ObjPhyProps =
        {
            nameof(PhysicalObject.Position),
            nameof(PhysicalObject.Speed),
            nameof(PhysicalObject.Momentum),
            nameof(PhysicalObject.Acceleration),
            nameof(PhysicalObject.NetForce),
            nameof(PhysicalObject.AttractionEnergy),
            nameof(PhysicalObject.KineticEnergy),
            nameof(PhysicalObject.GravityEnergy),
            nameof(PhysicalObject.TotalEnergy)
        };

        private static readonly float marge = 1.25f;
        private static readonly Color colCourbe = new Color(38, 188, 47);
        private static readonly Color colInteg = new Color(colCourbe) { A = 100 };
        private static readonly Color colGrille = new Color(255, 255, 255, 80);
        private static readonly Color colAxeX = new Color(255, 255, 255, 192);

        private IEnumerable<(string, ObjPropAttribute, Func<float>)> Properties()
        {
            foreach (var p in ObjPhyProps)
            {
                var prop = Object.GetType().GetProperty(p);
                var attr = prop.GetCustomAttribute<ObjPropAttribute>();
                var nom = attr.DisplayName;

                if (prop.PropertyType == typeof(float))
                {
                    yield return (nom, attr, () => (float) prop.GetValue(Object));
                }
                else if (prop.PropertyType == typeof(Vector2f))
                {
                    yield return (nom, attr, () => ((Vector2f) prop.GetValue(Object)).Norm());
                    yield return (nom + " (X)", attr, () => ((Vector2f) prop.GetValue(Object)).X);
                    yield return (nom + " (Y)", attr, () => ((Vector2f) prop.GetValue(Object)).Y);
                    yield return (nom + " (θ)", attr, () => ((Vector2f) prop.GetValue(Object)).Angle());
                }
            }
        }

        public WndPlot(PhysicalObject obj, Vector2f pos)
        : base(obj, obj.Name, lGauche + lGraphe, pos)
        {
            var hl = new Panel();
            hl.SizeLayout = new Layout2d(Size.X, hauteur);

            var btnClear = new BitmapButton("Effacer") { Image = new Texture("icones/clear.png") };
            btnClear.PositionLayout = new Layout2d(margeX, 0);
            btnClear.SizeLayout = new Layout2d(largeurBtn, hauteurBtn);
            hl.Add(btnClear);

            var props = Properties().ToList();

            var drop = new TGUI.ComboBox();

            foreach (var p in props)
            {
                drop.AddItem(p.Item1);
            }

            drop.PositionLayout = new Layout2d(margeX, hauteurLigne);
            drop.SizeLayout = new Layout2d(largeurBtn, hauteurBtn);
            hl.Add(drop);

            drop.SetSelectedItemByIndex(2);

            var canvas = new Canvas(lGraphe, hauteur);
            hl.Add(canvas);
            canvas.PositionLayout = new Layout2d(lGauche, 0);
            canvas.SizeLayout = new Layout2d(lGraphe, hauteur);
            canvas.ParentGui = UI.GUI;


            var points = new SynchronizedCollection<Vector2f>();
            var textInt = new Text("", UI.Font, 14)
            {
                FillColor = Color.White
            };
            var debut = Simulation.SimDuration;
            void Effacer()
            {
                debut = Simulation.SimDuration;
                points.Clear();
            }
            btnClear.Clicked += delegate { Effacer(); };

            drop.ItemSelected += delegate { Effacer(); };

            void MajGraphe()
            {
                points.Add(new Vector2f(Simulation.SimDuration - debut, -props[drop.GetSelectedItemIndex()].Item3()));
            }

            var v = new SFML.Graphics.View(canvas.View);

            void DessineGraphe()
            {
                canvas.Clear(Color.Black);

                lock (points.SyncRoot)
                {
                    var cache = points.ToArray();

                    if (cache.Length > 0)
                    {
                        var minY = cache[0].Y;
                        var maxY = cache[0].Y;

                        for (int i = 1; i < cache.Length; i++)
                        {
                            if (cache[i].Y < minY)
                                minY = cache[i].Y;
                            if (cache[i].Y > maxY)
                                maxY = cache[i].Y;
                        }

                        var taille = new Vector2f(Simulation.SimDuration - debut, maxY - minY);
                        v.Center = new Vector2f(taille.X / 2, taille.Y / 2 + minY);
                        v.Size = taille * marge;

                        canvas.View = v;

                        

                        var xFact = (float)Math.Pow(10, Math.Round(Math.Log10(v.Size.X * 0.2)));
                        var xLines = (int)Math.Ceiling(v.Size.X / xFact);
                        var xGrid = new Vertex[xLines * 2];

                        for (var i = 0; i < xLines; i++)
                        {
                            var x = (i - 1) * xFact;
                            xGrid[2 * i] = new Vertex(new Vector2f(x, -v.Size.Y + v.Center.Y / 2), colGrille);
                            xGrid[2 * i + 1] = new Vertex(new Vector2f(x, v.Size.Y + v.Center.Y / 2), colGrille);
                        }

                        canvas.Draw(xGrid, PrimitiveType.Lines);

                        var yFact = (float)Math.Pow(10, Math.Round(Math.Log10(v.Size.Y * 0.1)));
                        var yLines = (int)Math.Ceiling(v.Size.Y / yFact);
                        var yGrid = new Vertex[yLines * 2];
                        maxY *= marge;
                        var offY = maxY % yFact;
                        for (var i = 0; i < yLines; i++)
                        {
                            var y = (i - 1) * -yFact + maxY - offY;
                            yGrid[2 * i] = new Vertex(new Vector2f(v.Center.X - v.Size.X, y), colGrille);
                            yGrid[2 * i + 1] = new Vertex(new Vector2f(v.Size.X, y), colGrille);
                        }

                        canvas.Draw(yGrid, PrimitiveType.Lines);

                        
                        canvas.Draw(new[]
                        {
                                new Vertex(new Vector2f(v.Center.X - v.Size.X / 2, 0), colAxeX),
                                new Vertex(new Vector2f(v.Center.X + v.Size.X / 2, 0), colAxeX),
                                new Vertex(new Vector2f(0, v.Center.Y - v.Size.Y / 2), colAxeX),
                                new Vertex(new Vector2f(0, v.Center.Y + v.Size.Y / 2), colAxeX)
                            }, PrimitiveType.Lines);

                        var mpos = Mouse.GetPosition(Render.Window).F();
                        if (MouseOnWidget(mpos))
                        {
                            var tex = canvas.RenderTexture();
                            var rpos = tex.MapPixelToCoords(new Vector2f(mpos.X - Position.X - canvas.Position.X, mpos.Y - Position.Y).I());

                            if (cache.Length > 1)
                            {

                                int k;
                                for (k = 0; k < cache.Length; k++)
                                {
                                    if (cache[k].X > rpos.X)
                                        break;
                                }

                                if (k > 0)
                                {
                                    var aire = new Vertex[k * 4];
                                    var integ = 0f;

                                    for (int i = 1; i < k; i++)
                                    {
                                        var j = i * 4;

                                        aire[j + 0] = new Vertex(cache[i - 0], colInteg);
                                        aire[j + 1] = new Vertex(cache[i - 1], colInteg);
                                        aire[j + 2] = new Vertex(new Vector2f(cache[i - 1].X, 0), colInteg);
                                        aire[j + 3] = new Vertex(new Vector2f(cache[i - 0].X, 0), colInteg);

                                        integ += (cache[i - 0].Y + cache[i - 1].Y)
                                                 * (cache[i - 0].X - cache[i - 1].X)
                                                 / 2;

                                    }

                                    canvas.Draw(aire, PrimitiveType.Quads);

                                    if (k != cache.Length && k > 5)
                                    {
                                        var deriv = (cache[k].Y - cache[k - 5].Y) / (cache[k].X - cache[k - 5].X);
                                        var cB = cache[k].Y - cache[k].X * deriv;

                                        var colDeriv = new Color(255, 255, 255, 110);
                                        var lignes = new[]
                                        {
                                                // intercept Y
                                                new Vertex(new Vector2f(0, cache[k].Y), colDeriv),
                                                new Vertex(cache[k], colDeriv),

                                                new Vertex(new Vector2f(cache[k].X - 10, deriv * (cache[k].X - 10) + cB), colDeriv),
                                                new Vertex(new Vector2f(cache[k].X + 10, deriv * (cache[k].X + 10) + cB), colDeriv),
                                            };

                                        canvas.Draw(lignes, PrimitiveType.Lines);

                                        textInt.DisplayedString = $@"  x   = {rpos.X,6:F2} s
  y   = {-cache[k].Y,6:F2} {props[drop.GetSelectedItemIndex()].Item2.Unit}
  ∫   = {-integ,6:F2} {props[drop.GetSelectedItemIndex()].Item2.UnitInteg}
dy/dx = {-deriv,6:F2} {props[drop.GetSelectedItemIndex()].Item2.UnitDeriv}";
                                        textInt.Position =
                                            (rpos - v.Center - v.Size / 2).Prod(canvas.DefaultView.Size)
                                            .Div(v.Size) +
                                            canvas.Size + new Vector2f(30, -25);
                                        Console.WriteLine(textInt.Position);
                                        canvas.View = canvas.DefaultView;
                                        canvas.Draw(textInt);
                                        canvas.View = v;
                                    }
                                }
                            }
                        }


                        canvas.Draw(cache.Select(p => new Vertex(p, colCourbe)).ToArray(), PrimitiveType.LineStrip);

                        /*cercle.Position = (cache[cache.Length - 1] - v.Center - v.Size / 2).Prod(canvas.DefaultView.Size).Div(v.Size);
                        canvas.View = canvas.DefaultView;
                        canvas.Draw(cercle);*/
                    }
                }

                canvas.Display();
            }

            Simulation.AfterUpdate += MajGraphe;
            UI.Drawn += DessineGraphe;

            var btnCSV = new BitmapButton("Exporter en CSV") { Image = new Texture("icones/csv.png") };
            btnCSV.SizeLayout = new Layout2d(largeurBtn, hauteurBtn);
            btnCSV.PositionLayout = new Layout2d(margeX, 2 * hauteurLigne);
            hl.Add(btnCSV);

            btnCSV.Clicked += delegate
            {
                var sfd = new SaveFileDialog()
                {
                    AddExtension = true,
                    CheckPathExists = true,
                    AutoUpgradeEnabled = true,
                    DefaultExt = "csv",
                    Filter = "Fichier CSV (*.csv)|*.csv",
                    InitialDirectory = Environment.CurrentDirectory
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var sb = new StringBuilder();

                    sb.AppendLine($"Temps (s);{props[drop.GetSelectedItemIndex()].Item1} ({props[drop.GetSelectedItemIndex()].Item2.Unit})");

                    lock (points.SyncRoot)
                    {
                        foreach (var (x, y) in points)
                        {
                            sb.AppendLine($"{x};{-y}");
                        }
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString());
                }
            };

            AddEx(hl);

            Show();

            Closed += delegate
            {
                Simulation.AfterUpdate -= MajGraphe;
                UI.Drawn -= DessineGraphe;
            };
        }
    }
}
