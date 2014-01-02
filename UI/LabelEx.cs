using System.Drawing;
using System.Windows.Forms;

namespace RazorEx.UI
{
    public class LabelEx : Control
    {
        public LabelEx(string text, float size = 8, AnchorStyles anchor = AnchorStyles.None, FontStyle style = FontStyle.Regular, int margin = 3)
        {
            Text = text;
            Anchor = anchor;
            Size = TextRenderer.MeasureText(Text, Font);
            Font = new Font(Font.FontFamily, size, style);
            Margin = new Padding(margin);
            Enabled = false;
            ForeColor = Color.DeepPink;
        }

        public override string Text
        {
            get { return base.Text; }
            set
            {
                if (value != base.Text)
                {
                    base.Text = value;
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Size = TextRenderer.MeasureText(Text, Font);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor);
        }
    }
}