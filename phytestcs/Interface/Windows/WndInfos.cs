using phytestcs.Objects;
using SFML.System;
using TGUI;

namespace phytestcs.Interface.Windows
{
    public class WndInfos : WndBase
    {
        public WndInfos(Object obj, Vector2f pos)
            : base(obj, obj.Name, 410, pos)
        {
            var header = new Label();
            if (obj is PhysicalObject)
                header.Text += @"Aire :
Masse :
Moment d'inertie :
Position :
Vitesse :
Moment linéaire :
";

            header.Text += @"Énergie :
- cinétique :
- potentielle :
  - gravité :
  - attraction :
  - élastique :";
            header.SizeLayout = new Layout2d(130, header.Size.Y);
            var val = new Label {SizeLayout = new Layout2d(280, header.Size.Y), PositionLayout = new Layout2d(130, 0)};

            void updateInfos()
            {
                var text = "";
                var epes = 0f;
                var eela = 0f;
                var ecin = 0f;
                var eatt = 0f;
                switch (obj)
                {
                    case PhysicalObject objPhy:
                        ecin += objPhy.KineticEnergy;
                        epes += objPhy.GravityEnergy;
                        eatt += objPhy.AttractionEnergy;
                        text +=
                            $@"{objPhy.Shape.Area(),7:F3} m²
{objPhy.Mass,8:F3} kg
{objPhy.MomentOfInertia,7:F3} kg.m²
{objPhy.Position.DisplayPoint()} m
{objPhy.Velocity.Display()} m/s
{objPhy.Momentum.DisplayPoint()} N.s
";
                        break;
                    case Spring ress:
                        eela += ress.ElasticEnergy;
                        break;
                }

                var epot = epes + eela;
                var etot = epot + ecin;

                text += $@"{etot,10:F3} J
{ecin,10:F3} J
{epot,10:F3} J
{epes,10:F3} J
{eatt,10:F3} J
{eela,10:F3} J";
                val.Text = text;
            }

            UI.Drawn += updateInfos;

            Closed += delegate
            {
                UI.Drawn -= updateInfos;
            };

            updateInfos();

            var pnl = new Panel {SizeLayout = new Layout2d(Size.X, header.Size.Y)};
            pnl.Add(header);
            pnl.Add(val);
            Add(pnl);
            Show();
        }
    }
}
