using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ComboBox = TGUI.ComboBox;
using Panel = TGUI.Panel;
using View = SFML.Graphics.View;

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

        private const float marge = 1.25f;
        private static readonly Color colCourbe = new Color(38, 188, 47);
        private static readonly Color colInteg = new Color(colCourbe) { A = 100 };
        private static readonly Color colGrille = new Color(255, 255, 255, 80);
        private static readonly Color colAxeX = new Color(255, 255, 255, 192);
        private float _plotStart;
        private readonly SynchronizedCollection<Vector2f> _points = new SynchronizedCollection<Vector2f>();
        private readonly Canvas _canvas = new Canvas(lGraphe, hauteur);
        private ComboBox drop;

        private static readonly ReadOnlyCollection<(string, ObjPropAttribute, Func<PhysicalObject, float>)> props;

        private Text _textInt = new Text("", UI.Font, 14)
        {
            FillColor = Color.White
        };

        private View _canvasView;

        static WndPlot()
        {
            var res = new List<(string, ObjPropAttribute, Func<PhysicalObject, float>)>();

            foreach (var p in ObjPhyProps)
            {
                var prop = typeof(PhysicalObject).GetProperty(p);
                var attr = prop.GetCustomAttribute<ObjPropAttribute>();
                var nom = attr.DisplayName;

                if (prop.PropertyType == typeof(float))
                {
                    res.Add((nom, attr, (o) => (float)prop.GetValue(o)));
                }
                else if (prop.PropertyType == typeof(Vector2f))
                {
                    res.Add((nom, attr, (o) => ((Vector2f)prop.GetValue(o)).Norm()));
                    res.Add((nom + " (X)", attr, (o) => ((Vector2f)prop.GetValue(o)).X));
                    res.Add((nom + " (Y)", attr, (o) => ((Vector2f)prop.GetValue(o)).Y));
                    res.Add((nom + " (θ)", attr, (o) => ((Vector2f)prop.GetValue(o)).Angle()));
                }
            }

            props = res.AsReadOnly();
        }

        private void ClearPlot()
        {
            _plotStart = Simulation.SimDuration;
            _points.Clear();
        }

        private void UpdatePlot()
        {
            _points.Add(new Vector2f(Simulation.SimDuration - _plotStart, -props[drop.GetSelectedItemIndex()].Item3((PhysicalObject)Object)));
        }

        private void DrawPlotGrid(float maxY)
        {
            var xFact = (float)Math.Pow(10, Math.Round(Math.Log10(_canvasView.Size.X * 0.2)));
            var xLines = (int)Math.Ceiling(_canvasView.Size.X / xFact);
            var xGrid = new Vertex[xLines * 2];

            for (var i = 0; i < xLines; i++)
            {
                var x = (i - 1) * xFact;
                xGrid[2 * i] = new Vertex(new Vector2f(x, -_canvasView.Size.Y + _canvasView.Center.Y / 2), colGrille);
                xGrid[2 * i + 1] = new Vertex(new Vector2f(x, _canvasView.Size.Y + _canvasView.Center.Y / 2), colGrille);
            }

            _canvas.Draw(xGrid, PrimitiveType.Lines);

            var yFact = (float)Math.Pow(10, Math.Round(Math.Log10(_canvasView.Size.Y * 0.1)));
            var yLines = (int)Math.Ceiling(_canvasView.Size.Y / yFact);
            var yGrid = new Vertex[yLines * 2];
            maxY *= marge;
            var offY = maxY % yFact;
            for (var i = 0; i < yLines; i++)
            {
                var y = (i - 1) * -yFact + maxY - offY;
                yGrid[2 * i] = new Vertex(new Vector2f(_canvasView.Center.X - _canvasView.Size.X, y), colGrille);
                yGrid[2 * i + 1] = new Vertex(new Vector2f(_canvasView.Size.X, y), colGrille);
            }

            _canvas.Draw(yGrid, PrimitiveType.Lines);
        }

        private void DrawPlot()
        {
            _canvas.Clear(Color.Black);
            //lock (_points.SyncRoot)
            {
                Vector2f[] cache;
                //var cache = _points;
                lock (_points.SyncRoot)
                {
                    cache = _points.ToArray();
                }

                if (cache.Length > 0)
                {
                    var minY = cache[0].Y;
                    var maxY = cache[0].Y;

                    for (var i = 1; i < cache.Length; i++)
                    {
                        if (cache[i].Y < minY)
                            minY = cache[i].Y;
                        if (cache[i].Y > maxY)
                            maxY = cache[i].Y;
                    }

                    var taille = new Vector2f(Simulation.SimDuration - _plotStart, maxY - minY);
                    _canvasView.Center = new Vector2f(taille.X / 2, taille.Y / 2 + minY);
                    _canvasView.Size = taille * marge;

                    _canvas.View = _canvasView;

                    DrawPlotGrid(maxY);

                    _canvas.Draw(new[]
                    {
                        new Vertex(new Vector2f(_canvasView.Center.X - _canvasView.Size.X / 2, 0), colAxeX),
                        new Vertex(new Vector2f(_canvasView.Center.X + _canvasView.Size.X / 2, 0), colAxeX),
                        new Vertex(new Vector2f(0, _canvasView.Center.Y - _canvasView.Size.Y / 2), colAxeX),
                        new Vertex(new Vector2f(0, _canvasView.Center.Y + _canvasView.Size.Y / 2), colAxeX)
                    }, PrimitiveType.Lines);

                    var mpos = Mouse.GetPosition(Render.Window).F();
                    if (MouseOnWidget(mpos))
                    {
                        var tex = _canvas.RenderTexture();
                        var rpos = tex.MapPixelToCoords(new Vector2f(mpos.X - Position.X - _canvas.Position.X,
                            mpos.Y - Position.Y).I());

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

                                _canvas.Draw(aire, PrimitiveType.Quads);

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

                                        new Vertex(new Vector2f(cache[k].X - 10, deriv * (cache[k].X - 10) + cB),
                                            colDeriv),
                                        new Vertex(new Vector2f(cache[k].X + 10, deriv * (cache[k].X + 10) + cB),
                                            colDeriv),
                                    };

                                    _canvas.Draw(lignes, PrimitiveType.Lines);

                                    _textInt.DisplayedString = $@"  x   = {rpos.X,6:F2} s
  y   = {-cache[k].Y,6:F2} {props[drop.GetSelectedItemIndex()].Item2.Unit}
 ∫dx  = {-integ,6:F2} {props[drop.GetSelectedItemIndex()].Item2.UnitInteg}
dy/dx = {-deriv,6:F2} {props[drop.GetSelectedItemIndex()].Item2.UnitDeriv}";
                                    _textInt.Position =
                                        (rpos - _canvasView.Center - _canvasView.Size / 2)
                                        .Prod(_canvas.DefaultView.Size)
                                        .Div(_canvasView.Size) +
                                        _canvas.Size + new Vector2f(30, -25);
                                    Console.WriteLine(_textInt.Position);
                                    _canvas.View = _canvas.DefaultView;
                                    _canvas.Draw(_textInt);
                                    _canvas.View = _canvasView;
                                }
                            }
                        }
                    }

                    _canvas.Draw(cache.Select(p => new Vertex(p, colCourbe)).ToArray(), PrimitiveType.LineStrip);

                    /*cercle.Position = (cache[cache.Length - 1] - v.Center - v.Size / 2).Prod(canvas.DefaultView.Size).Div(v.Size);
                    canvas.View = canvas.DefaultView;
                    canvas.Draw(cercle);*/
                }
            }

            _canvas.Display();
        }

        public WndPlot(PhysicalObject obj, Vector2f pos)
        : base(obj, obj.Name, lGauche + lGraphe, pos)
        {
            var hl = new Panel {SizeLayout = new Layout2d(Size.X, hauteur)};

            var btnClear = new BitmapButton("Effacer")
            {
                Image = new Texture("icones/clear.png"),
                PositionLayout = new Layout2d(margeX, 0),
                SizeLayout = new Layout2d(largeurBtn, hauteurBtn)
            };
            hl.Add(btnClear);

            drop = new TGUI.ComboBox();

            foreach (var p in props)
            {
                drop.AddItem(p.Item1);
            }

            drop.PositionLayout = new Layout2d(margeX, hauteurLigne);
            drop.SizeLayout = new Layout2d(largeurBtn, hauteurBtn);
            hl.Add(drop);

            drop.SetSelectedItemByIndex(2);

            hl.Add(_canvas);
            _canvas.PositionLayout = new Layout2d(lGauche, 0);
            _canvas.SizeLayout = new Layout2d(lGraphe, hauteur);
            _canvas.ParentGui = UI.GUI;

            _plotStart = Simulation.SimDuration;
            
            btnClear.Clicked += delegate { ClearPlot(); };

            drop.ItemSelected += delegate { ClearPlot(); };

            _canvasView = new SFML.Graphics.View(_canvas.View);

            Simulation.AfterUpdate += UpdatePlot;
            UI.Drawn += DrawPlot;

            var btnCSV = new BitmapButton("Exporter en CSV")
            {
                Image = new Texture("icones/csv.png"),
                SizeLayout = new Layout2d(largeurBtn, hauteurBtn),
                PositionLayout = new Layout2d(margeX, 2 * hauteurLigne)
            };
            hl.Add(btnCSV);

            btnCSV.Clicked += btnCSV_Clicked;

            AddEx(hl);

            Show();

            Closed += delegate
            {
                Simulation.AfterUpdate -= UpdatePlot;
                UI.Drawn -= DrawPlot;
            };
        }

        private void btnCSV_Clicked(object sender, SignalArgsVector2f e)
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

            if (sfd.ShowDialog() != DialogResult.OK) return;

            var sb = new StringBuilder();

            sb.AppendLine($"Temps (s);{props[drop.GetSelectedItemIndex()].Item1} ({props[drop.GetSelectedItemIndex()].Item2.Unit})");

            lock (_points.SyncRoot)
            {
                foreach (var (x, y) in _points)
                {
                    sb.AppendLine($"{x};{-y}");
                }
            }

            File.WriteAllText(sfd.FileName, sb.ToString());
        }
    }
}
