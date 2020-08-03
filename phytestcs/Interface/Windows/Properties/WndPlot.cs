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
using static phytestcs.Tools;
using Button = TGUI.Button;
using ComboBox = TGUI.ComboBox;
using Panel = TGUI.Panel;
using TextBox = TGUI.TextBox;
using View = SFML.Graphics.View;

namespace phytestcs.Interface.Windows.Properties
{
    public sealed class WndPlot : WndBase<PhysicalObject>
    {
        private const int LGauche = 200;
        private const int LGraphe = 400;
        private const int Hauteur = 300;
        private const int MargeX = 5;
        private const int MargeY = 5;
        private const int LargeurBtn = LGauche - 2 * MargeX;
        private const int HauteurBtn = 20;
        private const int HauteurLigne = HauteurBtn + MargeY;

        private const float Marge = 1.25f;

        private static readonly string[] ObjPhyProps =
        {
            nameof(PhysicalObject.Position),
            nameof(PhysicalObject.Angle),
            nameof(PhysicalObject.Velocity),
            nameof(PhysicalObject.AngularVelocity),
            nameof(PhysicalObject.Momentum),
            nameof(PhysicalObject.Acceleration),
            nameof(PhysicalObject.NetForce),
            nameof(PhysicalObject.AttractionEnergy),
            nameof(PhysicalObject.LinearKineticEnergy),
            nameof(PhysicalObject.GravityEnergy),
            nameof(PhysicalObject.TotalEnergy)
        };

        private static readonly Color ColCourbe = new Color(38, 188, 47);
        private static readonly Color ColInteg = new Color(ColCourbe) { A = 100 };
        private static readonly Color ColGrille = new Color(255, 255, 255, 80);
        private static readonly Color ColAxeX = new Color(255, 255, 255, 192);

        private static readonly ReadOnlyCollection<(string, ObjPropAttribute, Func<PhysicalObject, float>)> Props;
        private readonly Canvas _canvas = new Canvas(LGraphe, Hauteur);

        private readonly View _canvasView;

        private readonly ComboBox _drop;
        private readonly SynchronizedCollection<Vector2f> _points = new SynchronizedCollection<Vector2f>();

        private readonly Text _textInt = new Text("", Ui.FontMono, 14)
        {
            FillColor = Color.White
        };

        private Func<PhysicalObject, float>? _customExpr;
        private float _plotStart;

        static WndPlot()
        {
            var res = new List<(string, ObjPropAttribute, Func<PhysicalObject, float>)>();

            foreach (var p in ObjPhyProps)
            {
                var prop = typeof(PhysicalObject).GetProperty(p)!;
                var attr = prop.GetCustomAttribute<ObjPropAttribute>()!;
                var nom = attr.DisplayName;

                if (prop.PropertyType == typeof(float))
                {
                    res.Add((nom, attr, o => (float) prop.GetValue(o)!));
                }
                else if (prop.PropertyType == typeof(Vector2f))
                {
                    res.Add((nom, attr, o => ((Vector2f) prop.GetValue(o)!).Norm()));
                    res.Add((nom + " (X)", attr, o => ((Vector2f) prop.GetValue(o)!).X));
                    res.Add((nom + " (Y)", attr, o => ((Vector2f) prop.GetValue(o)!).Y));
                    res.Add((nom + " (θ)", attr, o => ((Vector2f) prop.GetValue(o)!).Angle()));
                }
            }

            Props = res.AsReadOnly();
        }

        public WndPlot(PhysicalObject obj, Vector2f pos)
            : base(obj, LGauche + LGraphe, pos)
        {
            var hl = new Panel { SizeLayout = new Layout2d(Size.X, Hauteur) };

            var btnClear = new BitmapButton(L["Clear"])
            {
                Image = new Texture("icons/small/clear.png"),
                PositionLayout = new Layout2d(MargeX, 0),
                SizeLayout = new Layout2d(LargeurBtn, HauteurBtn)
            };
            hl.Add(btnClear);

            _drop = new ComboBox();

            _drop.AddItem($"** {L["Custom"]} **");

            foreach (var p in Props)
                _drop.AddItem(p.Item1);

            _drop.PositionLayout = new Layout2d(MargeX, HauteurLigne);
            _drop.SizeLayout = new Layout2d(LargeurBtn, HauteurBtn);
            hl.Add(_drop);

            _drop.SetSelectedItemByIndex(3);

            hl.Add(_canvas);
            _canvas.PositionLayout = new Layout2d(LGauche, 0);
            _canvas.SizeLayout = new Layout2d(LGraphe, Hauteur);
            _canvas.ParentGui = Ui.Gui;

            _plotStart = Simulation.SimDuration;

            btnClear.Clicked += delegate { ClearPlot(); };

            _canvasView = new View(_canvas.View);

            Simulation.AfterUpdate += UpdatePlot;
            Ui.Drawn += DrawPlot;

            var btnCsv = new BitmapButton(L["Export to CSV"])
            {
                Image = new Texture("icons/small/csv.png"),
                SizeLayout = new Layout2d(LargeurBtn, HauteurBtn),
                PositionLayout = new Layout2d(MargeX, 2 * HauteurLigne)
            };
            hl.Add(btnCsv);

            var txtCustom = new TextBox
            {
                SizeLayout = new Layout2d(LargeurBtn, HauteurBtn),
                PositionLayout = new Layout2d(MargeX, 3 * HauteurLigne),
                Visible = false
            };
            hl.Add(txtCustom);

            var btnApplyCustom = new Button(L["Apply"])
            {
                SizeLayout = new Layout2d(LargeurBtn, HauteurBtn),
                PositionLayout = new Layout2d(MargeX, 4 * HauteurLigne),
                Visible = false
            };
            hl.Add(btnApplyCustom);

            btnApplyCustom.Clicked += delegate
            {
                _customExpr = null;
                try
                {
                    _customExpr = $"o=>({txtCustom.Text})".Eval<Func<PhysicalObject, float>>(globals: Object).Result;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                ClearPlot();
            };

            _drop.ItemSelected += delegate
            {
                txtCustom.Visible = btnApplyCustom.Visible = true;
                ClearPlot();
            };

            btnCsv.Clicked += btnCSV_Clicked;

            Add(hl);

            Show();

            Closed += delegate
            {
                Simulation.AfterUpdate -= UpdatePlot;
                Ui.Drawn -= DrawPlot;
            };
        }

        private (string, ObjPropAttribute?, Func<PhysicalObject, float>?) CurrentLine
        {
            get
            {
                var idx = _drop.GetSelectedItemIndex();
                if (idx == 0)
                    return ("y", null, _customExpr);
                return Props[idx - 1];
            }
        }

        private void ClearPlot()
        {
            _plotStart = Simulation.SimDuration;
            _points.Clear();
        }

        private void UpdatePlot()
        {
            var getter = CurrentLine.Item3;
            if (getter == null)
                return;
            _points.Add(new Vector2f(Simulation.SimDuration - _plotStart,
                -getter(Object)));
        }

        private void DrawPlotGrid(float minY, float maxY)
        {
            var xFact = (float) Math.Pow(10, Math.Round(Math.Log10(_canvasView.Size.X * 0.2)));
            var xLines = (int) Math.Ceiling(_canvasView.Size.X / xFact);
            var xGrid = new Vertex[xLines * 2];

            for (var i = 0; i < xLines; i++)
            {
                var x = (i - 1) * xFact;
                xGrid[2 * i + 0] = new Vertex(new Vector2f(x, -_canvasView.Size.Y + _canvasView.Center.Y / 2),
                    ColGrille);
                xGrid[2 * i + 1] =
                    new Vertex(new Vector2f(x, _canvasView.Size.Y + _canvasView.Center.Y / 2), ColGrille);
            }

            _canvas.Draw(xGrid, PrimitiveType.Lines);

            var yFact = (float) Math.Pow(10, Math.Round(Math.Log10(_canvasView.Size.Y * 0.1)));
            var yLines = (int) Math.Ceiling(_canvasView.Size.Y / yFact);
            var yGrid = new Vertex[yLines * 2];
            var ySpan = maxY - minY;
            ySpan *= Marge;
            var offY = ySpan % yFact;
            for (var i = 0; i < yLines; i++)
            {
                var y = minY + (i - 1) * -yFact + ySpan - offY;
                yGrid[2 * i + 0] =
                    new Vertex(new Vector2f(_canvasView.Center.X - _canvasView.Size.X / 2, y), ColGrille);
                yGrid[2 * i + 1] =
                    new Vertex(new Vector2f(_canvasView.Center.X + _canvasView.Size.X / 2, y), ColGrille);
            }

            _canvas.Draw(yGrid, PrimitiveType.Lines);
        }

        private void DrawPlot()
        {
            _canvas.Clear(Color.Black);
            var cache = _points.ToArrayLocked();

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
                _canvasView.Size = taille * Marge;

                _canvas.View = _canvasView;

                {
                    var zoomY = _canvas.Size.Y / Marge / taille.Y;
                    var (fY, _) = Render.CalculateRuler(zoomY, 10);
                    var zoomX = _canvas.Size.X / Marge / taille.X;
                    var (fX, _) = Render.CalculateRuler(zoomX, 10);

                    var fdX = (decimal) fX;
                    var fdY = (decimal) fY;

                    var start = new Vector2f(0, maxY);
                    var end = new Vector2f(taille.X, minY);

                    var lines = new List<Vertex>();

                    (int, byte) Thickness(float zoom, decimal factor, decimal coord)
                    {
                        var w = 3;
                        byte a = 40;
                        if (Math.Abs(coord) < factor)
                        {
                            w = (int) Math.Round(6 / zoom * 45 / (float) factor);
                            a = 160;
                        }
                        else if (coord % (5 * factor) == 0)
                        {
                            w = coord % (10 * factor) == 0 ? 9 : 6;
                            a = 80;
                        }

                        return (w, a);
                    }

                    if (fdX != 0)
                        for (var x = Math.Round((decimal) start.X / fdX) * fdX; x < (decimal) end.X; x += fdX / 100)
                        {
                            if (x % fdX != 0)
                                continue;

                            var (w, a) = Thickness(zoomX, fdX, x);
                            lines.AddRange(VertexLine(new Vector2f((float) x, start.Y), new Vector2f((float) x, end.Y),
                                new Color(255, 255, 255, a), w * fX / 100));
                        }

                    if (fdY != 0)
                        for (var y = Math.Round((decimal) start.Y / fdY) * fdY; y > (decimal) end.Y; y -= fdY / 100)
                        {
                            if (y % fdY != 0)
                                continue;

                            var (h, a) = Thickness(zoomY, fdY, y);
                            lines.AddRange(VertexLine(new Vector2f(start.X, (float) y), new Vector2f(end.X, (float) y),
                                new Color(255, 255, 255, a), h * fY / 100));
                        }

                    _canvas.Draw(lines.ToArray(), PrimitiveType.Quads, new RenderStates(BlendMode.Alpha));
                }

                //DrawPlotGrid(minY, maxY);

                _canvas.Draw(new[]
                {
                    new Vertex(new Vector2f(_canvasView.Center.X - _canvasView.Size.X / 2, 0), ColAxeX),
                    new Vertex(new Vector2f(_canvasView.Center.X + _canvasView.Size.X / 2, 0), ColAxeX),
                    new Vertex(new Vector2f(0, _canvasView.Center.Y - _canvasView.Size.Y / 2), ColAxeX),
                    new Vertex(new Vector2f(0, _canvasView.Center.Y + _canvasView.Size.Y / 2), ColAxeX)
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
                            if (cache[k].X > rpos.X)
                                break;

                        if (k > 0)
                        {
                            var aire = new Vertex[k * 4];
                            var integ = 0f;

                            for (var i = 1; i < k; i++)
                            {
                                var j = i * 4;

                                aire[j + 0] = new Vertex(cache[i - 0], ColInteg);
                                aire[j + 1] = new Vertex(cache[i - 1], ColInteg);
                                aire[j + 2] = new Vertex(new Vector2f(cache[i - 1].X, 0), ColInteg);
                                aire[j + 3] = new Vertex(new Vector2f(cache[i - 0].X, 0), ColInteg);

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
                                        colDeriv)
                                };

                                _canvas.Draw(lignes, PrimitiveType.Lines);
                                var name = CurrentLine.Item2?.ShortName ?? "y";
                                _textInt.DisplayedString = $@"  t   = {rpos.X,6:F2} s
  {name}   = {-cache[k].Y,6:F2} {CurrentLine.Item2?.Unit ?? ""}
 ∫dt  = {-integ,6:F2} {CurrentLine.Item2?.UnitInteg ?? ""}
d{name}/dt = {-deriv,6:F2} {CurrentLine.Item2?.UnitDeriv ?? ""}";
                                _textInt.Position =
                                    (rpos - _canvasView.Center - _canvasView.Size / 2)
                                    .Prod(_canvas.DefaultView.Size)
                                    .Div(_canvasView.Size) +
                                    _canvas.Size + new Vector2f(30, -25);

                                _canvas.View = _canvas.DefaultView;
                                _canvas.Draw(_textInt);
                                _canvas.View = _canvasView;
                            }
                        }
                    }
                }

                _canvas.Draw(cache.Select(p => new Vertex(p, ColCourbe)).ToArray(), PrimitiveType.LineStrip);

                /*cercle.Position = (cache[cache.Length - 1] - v.Center - v.Size / 2).Prod(canvas.DefaultView.Size).Div(v.Size);
                canvas.View = canvas.DefaultView;
                canvas.Draw(cercle);*/
            }

            _canvas.Display();
        }

        private void btnCSV_Clicked(object? sender, SignalArgsVector2f e)
        {
            var sfd = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                AutoUpgradeEnabled = true,
                DefaultExt = "csv",
                Filter = $"{L["CSV file"]} (*.csv)|*.csv",
                InitialDirectory = Environment.CurrentDirectory
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            var sb = new StringBuilder();

            sb.AppendLine(
                $"{L["Time"]} (s);{CurrentLine.Item1}{(CurrentLine.Item2 == null ? "" : $" ({CurrentLine.Item2.Unit})")}");

            lock (_points.SyncRoot)
            {
                foreach (var (x, y) in _points)
                    sb.AppendLine($"{x};{-y}");
            }

            File.WriteAllText(sfd.FileName, sb.ToString());
        }
    }
}