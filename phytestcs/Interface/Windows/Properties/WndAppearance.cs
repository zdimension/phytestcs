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
        private const int wndWidth = 250;
        private const int margin = 4;
        private const int hueWidth = 20;
        private const int sqSize = 140;
        private const int offset = hueWidth + 10;
        private const int totalSize = offset + sqSize;
        private const int previewOffset = totalSize + 20;
        private const int previewWidth = hueWidthHoriz - previewOffset;
        private const float hueFac = 360f / sqSize;
        private const int absorbHeight = 80;
        private const float hueFacHoriz = 360f / hueWidthHoriz;
        public const int hueWidthHoriz = wndWidth - 2 * margin;
        private static readonly Color BackColor = new Color(30, 30, 30);
        private static readonly Image HueImg = new Image(hueWidth, sqSize);
        private static readonly Image HueImgHoriz = new Image(hueWidthHoriz, absorbHeight);

        public static readonly Color SelectorOutline = new Color(255, 255, 255, 192);


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
            : base(obj, wndWidth, pos)
        {
            var wrapper = new ColorWrapper(() => obj.Color);
            
            var renderImg = new Image(wndWidth, sqSize + 2 * margin, BackColor);

            var selector = new Canvas();
            selector.SizeLayout = new Layout2d(renderImg.Size.X, renderImg.Size.Y);
            selector.Clear(BackColor);

            Add(selector);

            Color oldColor = default;
            IntPtr oldPointer = default;

            var userChanging = false;
            
            var previewRect = new VertexArray(PrimitiveType.Quads, 4);
            var previewOutline = new VertexArray(PrimitiveType.LineStrip, 5);

            Vector2f[] previewCorners =
            {
                new Vector2f(margin + previewOffset, margin),
                new Vector2f(margin + previewOffset + previewWidth, margin),
                new Vector2f(margin + previewOffset + previewWidth, margin + sqSize),
                new Vector2f(margin + previewOffset, margin + sqSize),
            };
            
            var hueSelector = new RectangleShape(new Vector2f(hueWidth, 4))
            {
                OutlineColor = SelectorOutline, OutlineThickness = 2f, FillColor = Color.Transparent,
                Origin = new Vector2f(hueWidth / 2, 2)
            };
            
            var colorSelector = new RectangleShape(new Vector2f(8, 8))
            {
                OutlineColor = SelectorOutline, OutlineThickness = 2f, FillColor = Color.Transparent,
                Origin = new Vector2f(4, 4)
            };
            
            wrapper.ValueChanged += delegate { UpdateSelector(); };
            
            void DrawSelector()
            {
                oldColor = wrapper.Value;

                const float fac = 1f / sqSize;

                var tex = selector.RenderTexture();
                oldPointer = tex.CPointer;

                selector.Clear(BackColor);

                for (uint y = 0; y < renderImg.Size.Y; y++)
                for (uint x = 0; x < renderImg.Size.X; x++)
                    renderImg.SetPixel(x, y, BackColor);

                renderImg.Copy(HueImg, margin, margin);

                var hue = wrapper.H;

                for (uint y = 0; y < sqSize; y++)
                {
                    var v = y * fac;

                    for (uint x = 0; x < sqSize; x++)
                    {
                        var s = x * fac;

                        renderImg.SetPixel(margin + offset + x, margin + y, new HSVA(hue, s, 1 - v, 1));
                    }
                }

                tex.Texture.Update(renderImg);

                hueSelector.Position = new Vector2f(
                    margin + hueWidth / 2,
                    margin + (float) ((360 - wrapper.H) / hueFac));
                tex.Draw(hueSelector);

                colorSelector.Position = new Vector2f(
                    (float) (margin + offset + wrapper.S * sqSize),
                    (float) (margin + wrapper.V * sqSize));
                tex.Draw(colorSelector);
                
                var col = wrapper.Value;
                previewRect[0] = new Vertex(previewCorners[0], col);
                previewRect[1] = new Vertex(previewCorners[1], col);
                previewRect[2] = new Vertex(previewCorners[2], col);
                previewRect[3] = new Vertex(previewCorners[3], col);
                tex.Draw(previewRect);
                
                col.A = 255;
                previewOutline[0] = new Vertex(previewCorners[0], col);
                previewOutline[1] = new Vertex(previewCorners[1], col);
                previewOutline[2] = new Vertex(previewCorners[2], col);
                previewOutline[3] = new Vertex(previewCorners[3], col);
                previewOutline[4] = new Vertex(previewCorners[0], col);
                tex.Draw(previewOutline);
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
                                wrapper.Value = new Color(renderImg.GetPixel(margin + (uint) x, margin + (uint) y))
                                    { A = wrapper.A };
                        }
                    }

                    userChanging = false;
                }

                if (wrapper.Value != oldColor || selector.RenderTexture().CPointer != oldPointer)
                    DrawSelector();
            }

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

                var _absorbanceImg = new Image(250, absorbHeight + 2 * margin, BackColor);

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

                Ui.Drawn += UpdateAbsorbance;

                Closed += delegate { Ui.Drawn -= UpdateAbsorbance; };
                
                Add(new CheckField(() => phy.Appearance.Borders));
                Add(new CheckField(() => phy.Appearance.OpaqueBorders));

                if (phy is Circle circle)
                {
                    Add(new CheckField(() => circle.Appearance.DrawCircleCakes));
                    Add(new CheckField(() => circle.Appearance.Protractor));
                }

                if (phy is Box box)
                {
                    Add(new CheckField(() => box.Appearance.Ruler));
                }
            }

            Show();
        }
    }
}