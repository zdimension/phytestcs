using System;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using TGUI;
using static phytestcs.Tools;
using Object = phytestcs.Objects.Object;

namespace phytestcs.Interface.Windows.Properties
{
    public class WndAppearance : WndBase<Object>
    {
        private const int margin = 4;
        private const int hueWidth = 20;
        private const int sqSize = 140;
        private const int offset = hueWidth + 10;
        private const int totalSize = offset + sqSize;
        private const float hueFac = 360f / sqSize;
        private const int absorbHeight = 80;
        private const float hueFacHoriz = 360f / hueWidthHoriz;
        public const int hueWidthHoriz = 250 - 2 * margin;
        private static readonly Color BackColor = new Color(30, 30, 30);
        private static readonly Image HueImg = new Image(hueWidth, sqSize);
        private static readonly Image HueImgHoriz = new Image(hueWidthHoriz, absorbHeight);

        public static readonly Color SelectorOutline = new Color(255, 255, 255, 192);

        private readonly Image _absorbanceImg;

        private readonly RectangleShape _colorSelector = new RectangleShape(new Vector2f(8, 8))
        {
            OutlineColor = SelectorOutline, OutlineThickness = 2f, FillColor = Color.Transparent,
            Origin = new Vector2f(4, 4)
        };

        private readonly RectangleShape _hueSelector = new RectangleShape(new Vector2f(hueWidth, 4))
        {
            OutlineColor = SelectorOutline, OutlineThickness = 2f, FillColor = Color.Transparent,
            Origin = new Vector2f(hueWidth / 2, 2)
        };

        private readonly Image RenderImg = new Image(totalSize + 2 * margin + 10, sqSize + 2 * margin, BackColor);

        static WndAppearance()
        {
            for (uint y = 0; y < sqSize; y++)
            {
                var col = new HSVA(y * hueFac, 1, 1, 1);
                for (uint x = 0; x < hueWidth; x++)
                    HueImg.SetPixel(x, y, col);
            }

            for (uint x = 0; x < hueWidthHoriz; x++)
            {
                var col = new HSVA(x * hueFacHoriz, 1, 1, 1);
                for (uint y = 0; y < absorbHeight; y++)
                    HueImgHoriz.SetPixel(x, y, col);
            }
        }

        public WndAppearance(Object obj, Vector2f pos)
            : base(obj, 250, pos)
        {
            var wrapper = new ColorWrapper(() => obj.Color);

            var selector = new Canvas();
            selector.SizeLayout = new Layout2d(RenderImg.Size.X, RenderImg.Size.Y);
            selector.Clear(BackColor);

            Add(selector);

            Color oldColor = default;
            IntPtr oldPointer = default;

            var userChanging = false;

            wrapper.ValueChanged += delegate { UpdateSelector(); };

            void DrawSelector()
            {
                oldColor = wrapper.Value;

                const float fac = 1f / sqSize;

                var tex = selector.RenderTexture();
                oldPointer = tex.CPointer;

                selector.Clear(BackColor);

                for (uint y = 0; y < RenderImg.Size.Y; y++)
                for (uint x = 0; x < RenderImg.Size.X; x++)
                    RenderImg.SetPixel(x, y, BackColor);

                RenderImg.Copy(HueImg, margin, margin);

                var hue = wrapper.H;

                for (uint y = 0; y < sqSize; y++)
                {
                    var v = y * fac;

                    for (uint x = 0; x < sqSize; x++)
                    {
                        var s = x * fac;

                        RenderImg.SetPixel(margin + offset + x, margin + y, new HSVA(hue, s, 1 - v, 1));
                    }
                }

                tex.Texture.Update(RenderImg);

                _hueSelector.Position = new Vector2f(
                    margin + hueWidth / 2,
                    margin + (float) ((360 - wrapper.H) / hueFac));
                tex.Draw(_hueSelector);

                _colorSelector.Position = new Vector2f(
                    (float) (margin + offset + wrapper.S * sqSize),
                    (float) (margin + wrapper.V * sqSize));
                tex.Draw(_colorSelector);
            }

            void UpdateSelector()
            {
                if (userChanging)
                    return;

                if (Mouse.IsButtonPressed(Mouse.Button.Left))
                {
                    userChanging = true;

                    var mpos = Mouse.GetPosition(Render.Window).F() - AbsolutePosition -
                               new Vector2f(0, Renderer.TitleBarHeight);
                    if (selector.MouseOnWidget(mpos))
                    {
                        var (x, y) = (mpos - selector.AbsolutePosition).I();
                        x -= margin;
                        y -= margin;
                        if (x >= 0 && x <= totalSize && y >= 0 && y < 140)
                        {
                            if (x <= hueWidth)
                                wrapper.H = y * hueFac;
                            else if (x >= offset)
                                wrapper.Value = new Color(RenderImg.GetPixel(margin + (uint) x, margin + (uint) y))
                                    { A = wrapper.A };
                        }
                    }

                    userChanging = false;
                }

                if (wrapper.Value != oldColor || selector.RenderTexture().CPointer != oldPointer)
                    DrawSelector();
            }

            DrawSelector();

            Ui.Drawn += UpdateSelector;

            Closed += delegate { Ui.Drawn -= UpdateSelector; };

            Add(new NumberField<double>(0, 360, unit: "°", bindProp: () => wrapper.H, inline: true, round: 0));
            Add(new NumberField<double>(0, 100, factor: 100, unit: "%", bindProp: () => wrapper.S, inline: true,
                round: 0));
            Add(new NumberField<double>(0, 100, factor: 100, unit: "%", bindProp: () => wrapper.V, inline: true,
                round: 0));

            Add(new NumberField<double>(0, 100, factor: 100, unit: "%", bindProp: () => wrapper.Ad, inline: true,
                round: 0));

            Add(new NumberField<byte>(0, 255, deci: false, bindProp: () => wrapper.R, inline: true));
            Add(new NumberField<byte>(0, 255, deci: false, bindProp: () => wrapper.G, inline: true));
            Add(new NumberField<byte>(0, 255, deci: false, bindProp: () => wrapper.B, inline: true));

            var btnRandom = new BitmapButton(L["Random color"]) { Image = new Texture("icons/small/random.png") };
            btnRandom.Clicked += delegate
            {
                wrapper.Value = new Color(Palette.Default.ColorRange.RandomColor()) { A = wrapper.A };
            };

            Add(btnRandom);

            if (obj is PhysicalObject phy)
            {
                Add(new NumberField<float>(30, 2000, bindProp: () => phy.ColorFilterWidth, log: true)
                    { RightValue = float.PositiveInfinity });

                _absorbanceImg = new Image(250, absorbHeight + 2 * margin, BackColor);

                var absorbance = new Canvas();
                absorbance.SizeLayout = new Layout2d(_absorbanceImg.Size.X, _absorbanceImg.Size.Y);
                absorbance.Clear(BackColor);

                Add(absorbance);

                IntPtr oldPointerAbs = default;

                var oldHue = -1d;
                var oldWidth = -1f;

                void DrawAbsorbance()
                {
                    oldHue = wrapper.H;
                    oldWidth = phy.ColorFilterWidth;

                    var tex = absorbance.RenderTexture();
                    oldPointerAbs = tex.CPointer;

                    absorbance.Clear(BackColor);

                    _absorbanceImg.Copy(HueImgHoriz, margin, margin);

                    var objHue = phy.ColorHsva.H;
                    for (uint x = 0; x < hueWidthHoriz; x++)
                    {
                        var transmittance = (int) ((1 - Transmittance(x * hueFacHoriz, objHue, phy.ColorFilterWidth)) *
                                                   absorbHeight) + 1;
                        for (uint y = 0; y < transmittance; y++)
                            _absorbanceImg.SetPixel(margin + x, margin + y, BackColor);
                    }

                    tex.Texture.Update(_absorbanceImg);
                }

                void UpdateAbsorbance()
                {
                    if (wrapper.H != oldHue || phy.ColorFilterWidth != oldWidth ||
                        absorbance.RenderTexture().CPointer != oldPointerAbs)
                        DrawAbsorbance();
                }

                DrawAbsorbance();

                Ui.Drawn += UpdateAbsorbance;

                Closed += delegate { Ui.Drawn -= UpdateAbsorbance; };

                if (phy.Shape is CircleShape circle)
                {
                    Add(new CheckField(() => phy.Protractor));
                }
            }

            Show();
        }
    }
}