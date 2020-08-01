using phytestcs.Objects;
using SFML.System;
using TGUI;
using static phytestcs.Tools;

namespace phytestcs.Interface.Windows.Properties
{
    public class WndInfos : WndBase<Object>
    {
        public WndInfos(Object obj, Vector2f pos)
            : base(obj, 440, pos)
        {
            var header = new Label();
            if (obj is PhysicalObject)
                header.Text += $@"{L["Area"]} :
{L["Mass"]} :
{L["Moment of inertia"]} :
{L["Position"]} :
{L["Velocity"]} :
{L["Momentum"]} :
";

            header.Text += $@"{L["Energy"]} :
- {L["Kinetic"]} :
  - {L["Linear"]} :
  - {L["Angular"]}
- {L["Potential"]} :
  - {L["Gravity"]} :
  - {L["Attraction"]} :
  - {L["Spring"]} :";
            header.SizeLayout = new Layout2d(130, header.Size.Y);
            var val = new Label
                { SizeLayout = new Layout2d(280, header.Size.Y), PositionLayout = new Layout2d(130, 0) };

            void UpdateInfos()
            {
                var text = "";
                var epes = 0f;
                var eela = 0f;
                var ecinl = 0f;
                var ecina = 0f;
                var eatt = 0f;
                switch (obj)
                {
                    case PhysicalObject objPhy:
                        ecinl += objPhy.LinearKineticEnergy;
                        ecina += objPhy.AngularKineticEnergy;
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
                var ecin = ecinl + ecina;
                var etot = epot + ecin;


                text += $@"{etot,10:F3} J
{ecin,10:F3} J
{ecinl,10:F3} J
{ecina,10:F3} J
{epot,10:F3} J
{epes,10:F3} J
{eatt,10:F3} J
{eela,10:F3} J";
                val.Text = text;
            }

            Ui.Drawn += UpdateInfos;

            Closed += delegate { Ui.Drawn -= UpdateInfos; };

            UpdateInfos();

            var pnl = new Panel { SizeLayout = new Layout2d(Size.X, header.Size.Y) };
            pnl.Add(header);
            pnl.Add(val);
            Add(pnl);
            Show();
        }
    }
}