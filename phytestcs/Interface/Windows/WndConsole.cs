using System;
using SFML.Graphics;
using SFML.System;
using TGUI;
using static phytestcs.Tools;

namespace phytestcs.Interface.Windows
{
    public class WndConsole : ChildWindowEx
    {
        public WndConsole() : base(L["Console"], 400, useLayout:true)
        {
            var cb = new ChatBox() { SizeLayout = new Layout2d("parent.w", "400")};
            Add(cb, "cb");
            var w = new Group();
            var txt = new EditBox();
            var btn = new BitmapButton() { Image = new Texture("icons/small/accept.png") };
            w.Add(txt, "txt");
            w.Add(btn, "btn");
            btn.Size = new Vector2f(20, 20);
            w.SizeLayout = new Layout2d("parent.w", "btn.h + 10");
            txt.SizeLayout = new Layout2d("btn.left - 10", "btn.h");
            btn.PositionLayout = new Layout2d("parent.w - w - 10", "0");
            w.PositionLayout = new Layout2d("5", "cb.h + 5");
            Add(w, "cont");
            
            void ProcessCommand()
            {
                cb.AddLine("> " + txt.Text);
                try
                {
                    var res = txt.Text.Eval<object?>().Result;
                    if (res != null)
                    {
                        cb.AddLine(res.Repr(), Color.Blue);
                    }
                }
                catch (Exception e)
                {
                    cb.AddLine(e.Message, Color.Red);
                }

                txt.Text = "";
                txt.Focus = true;
            }

            txt.ReturnKeyPressed += delegate { ProcessCommand(); };
            btn.Clicked += delegate { ProcessCommand(); };
        }
    }
}