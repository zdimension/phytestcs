using System;
using phytestcs.Objects;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using TGUI;
using static phytestcs.Tools;

namespace phytestcs.Interface.Windows.Properties
{
    public sealed class WndAppearance : WndBase<BaseObject>
    {
        private const int WndWidth = 250;
        private const int Margin = 4;
        private const int HueWidth = 20;
        private const int SqSize = 140;
        private const int Offset = HueWidth + 10;
        private const int TotalSize = Offset + SqSize;
        private const int PreviewMargin = 30;
        private const int PreviewOffset = TotalSize + PreviewMargin;
        private const int PreviewWidth = AbsorbWidth - PreviewOffset;
        private const float HueFac = 360f / SqSize;
        private const int AbsorbHeight = 80;
        private const float HueFacHoriz = 360f / HueWidthHoriz;
        public const int AbsorbWidth = WndWidth - 2 * Margin;
        public const int HueWidthHoriz = AbsorbWidth - AbsorbTextSize;
        public const int HueHeightHoriz = AbsorbHeight - AbsorbTextSize;
        private static readonly Color BackColor = new Color(30, 30, 30);
        private static readonly Image HueImg = new Image(HueWidth, SqSize);
        private static readonly Image HueImgHoriz = new Image(HueWidthHoriz, HueHeightHoriz);
        public const int AbsorbTextSize = 16;
        public static readonly Color SelectorOutline = new Color(255, 255, 255, 192);


        static WndAppearance()
        {
            for (uint y = 0; y < SqSize; y++)
            {
                var col = new Hsva(y * HueFac, 1, 1, 1);
                for (uint x = 0; x < HueWidth; x++)
                    HueImg.SetPixel(x, y, col);
            }

            for (uint x = 0; x < HueWidthHoriz; x++)
            {
                var col = new Hsva(x * HueFacHoriz, 1, 1, 1);
                for (uint y = 0; y < HueHeightHoriz; y++)
                    HueImgHoriz.SetPixel(x, y, col);
            }
        }

        public WndAppearance(BaseObject obj, Vector2f pos)
            : base(obj, WndWidth, pos)
        {
            var wrapper = new ColorWrapper(() => obj.Color);
            
            var renderImg = new Image(WndWidth, SqSize + 2 * Margin, BackColor);

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
                new Vector2f(Margin + PreviewOffset, Margin),
                new Vector2f(Margin + PreviewOffset + PreviewWidth, Margin),
                new Vector2f(Margin + PreviewOffset + PreviewWidth, Margin + SqSize),
                new Vector2f(Margin + PreviewOffset, Margin + SqSize),
            };
            
            var hueSelector = new RectangleShape(new Vector2f(HueWidth, 4))
            {
                OutlineColor = SelectorOutline, OutlineThickness = 2f, FillColor = Color.Transparent,
                Origin = new Vector2f(HueWidth / 2, 2)
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

                const float fac = 1f / SqSize;

                var tex = selector.RenderTexture();
                oldPointer = tex.CPointer;

                selector.Clear(BackColor);

                for (uint y = 0; y < renderImg.Size.Y; y++)
                for (uint x = 0; x < renderImg.Size.X; x++)
                    renderImg.SetPixel(x, y, BackColor);

                renderImg.Copy(HueImg, Margin, Margin);

                var hue = wrapper.H;

                for (uint y = 0; y < SqSize; y++)
                {
                    var v = y * fac;

                    for (uint x = 0; x < SqSize; x++)
                    {
                        var s = x * fac;

                        renderImg.SetPixel(Margin + Offset + x, Margin + y, new Hsva(hue, s, 1 - v, 1));
                    }
                }

                tex.Texture.Update(renderImg);

                hueSelector.Position = new Vector2f(
                    Margin + HueWidth / 2,
                    Margin + (float) ((360 - wrapper.H) / HueFac));
                tex.Draw(hueSelector);

                colorSelector.Position = new Vector2f(
                    (float) (Margin + Offset + wrapper.S * SqSize),
                    (float) (Margin + wrapper.V * SqSize));
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
                        x -= Margin;
                        y -= Margin;
                        if (x >= 0 && x <= TotalSize && y >= 0 && y < 140)
                        {
                            if (x <= HueWidth)
                                wrapper.H = y * HueFac;
                            else if (x >= Offset)
                                wrapper.Value = new Color(renderImg.GetPixel(Margin + (uint) x, Margin + (uint) y))
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

            Add(new NumberField<double>(0, 360, () => wrapper.H, unit: "°", inline: true, round: 0));
            Add(new NumberField<double>(0, 100, () => wrapper.S, unit: "%",
                factor: 100, inline: true, round: 0));
            Add(new NumberField<double>(0, 100, () => wrapper.V, unit: "%",
                factor: 100, inline: true, round: 0));

            Add(new NumberField<double>(0, 100, () => wrapper.Ad, unit: "%",
                factor: 100, inline: true, round: 0));

            Add(new NumberField<byte>(0, 255, () => wrapper.R, deci: false, inline: true));
            Add(new NumberField<byte>(0, 255, () => wrapper.G, deci: false, inline: true));
            Add(new NumberField<byte>(0, 255, () => wrapper.B, deci: false, inline: true));

            var btnRandom = new BitmapButton(L["Random color"]) { Image = new Texture("icons/small/random.png") };
            btnRandom.Clicked += delegate
            {
                wrapper.Value = new Color(Palette.Default.ColorRange.RandomColor()) { A = wrapper.A };
            };

            Add(btnRandom);

            if (obj is PhysicalObject phy)
            {
                Add(new NumberField<float>(30, 2000, () => phy.ColorFilterWidth, log: true)
                    { RightValue = float.PositiveInfinity });

                var absorbanceImg = new Image(WndWidth, AbsorbHeight + 2 * Margin, BackColor);

                var absorbance = new Canvas();
                absorbance.SizeLayout = new Layout2d(absorbanceImg.Size.X, absorbanceImg.Size.Y);
                absorbance.Clear(BackColor);

                var txtA = new Text("A", Ui.Font, AbsorbTextSize) { FillColor = Color.White, Scale = new Vector2f(1, -1) }.CenterOriginText();
                var txtH = new Text("H", Ui.Font, AbsorbTextSize) { FillColor = Color.White, Scale = new Vector2f(1, -1) }.CenterOriginText();

                Add(absorbance);

                IntPtr oldPointerAbs = default;
                
                var oldCol = new Hsva(-1, -1, -1, -1);
                var oldWidth = -1f;

                void DrawAbsorbance()
                {
                    oldCol = wrapper.ValueHsv;
                    oldWidth = phy.ColorFilterWidth;

                    var tex = absorbance.RenderTexture();
                    oldPointerAbs = tex.CPointer;

                    absorbance.Clear(BackColor);

                    absorbanceImg.Copy(HueImgHoriz, Margin + AbsorbTextSize, Margin + AbsorbTextSize);

                    var hsva = phy.ColorHsva;
                    var objHue = hsva.H;
                    var objSat = hsva.S;
                    var alphaD = 1 - hsva.A;
                    for (uint x = 0; x < HueWidthHoriz; x++)
                    {
                        var transmittance = (int) ((1 - alphaD * Transmittance(x * HueFacHoriz, objHue, phy.ColorFilterWidth, objSat)) *
                                                   HueHeightHoriz) + 1;
                        for (uint y = 0; y < transmittance; y++)
                            absorbanceImg.SetPixel(Margin + AbsorbTextSize + x, Margin + AbsorbTextSize + y, BackColor);
                    }

                    tex.Texture.Update(absorbanceImg);

                    var hx = (float)(Margin + AbsorbTextSize + objHue / HueFacHoriz);
                    var ay = (float)(Margin - 1 + alphaD * HueHeightHoriz);
                    tex.Draw(new[]
                    {
                        new Vertex(new Vector2f(hx, Margin), Color.White),
                        new Vertex(new Vector2f(hx, Margin + HueHeightHoriz + 2), Color.White),
                        
                        new Vertex(new Vector2f(Margin, ay), Color.White),
                        new Vertex(new Vector2f(WndWidth - Margin, ay), Color.White),
                    }, PrimitiveType.Lines);
                    
                    txtA.Position = new Vector2f(Margin + AbsorbTextSize / 2, ay + AbsorbTextSize);
                    tex.Draw(txtA);
                    txtH.Position = new Vector2f(hx, AbsorbHeight - Margin + AbsorbTextSize / 2);
                    tex.Draw(txtH);
                }

                void UpdateAbsorbance()
                {
                    if (wrapper.ValueHsv != oldCol || phy.ColorFilterWidth != oldWidth ||
                        absorbance.RenderTexture().CPointer != oldPointerAbs)
                        DrawAbsorbance();
                }

                Ui.Drawn += UpdateAbsorbance;

                Closed += delegate { Ui.Drawn -= UpdateAbsorbance; };
                
                Add(new CheckField(() => phy.Appearance.Borders, onChanged: () => { phy.UpdateOutline();}));
                Add(new CheckField(() => phy.Appearance.OpaqueBorders, onChanged: () => { phy.UpdateOutline();}));

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