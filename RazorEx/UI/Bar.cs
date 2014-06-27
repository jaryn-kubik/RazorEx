using System.Drawing;
using System.Windows.Forms;

namespace RazorEx.UI
{
    public class Bar : Control
    {
        public Bar(Color color)
        {
            Enabled = false;
            BackColor = Color.Black;
            Anchor = AnchorStyles.None;
            Size = new Size(100, 15);
            Margin = new Padding(0);
            this.color = color;
        }

        private ushort maximum, value;

        private Color color;
        public Color Color
        {
            get { return color; }
            set
            {
                if (value != color)
                {
                    color = value;
                    Invalidate();
                }
            }
        }

        private bool unknown;
        public bool Unknown
        {
            get { return unknown; }
            set
            {
                if (value != unknown)
                {
                    unknown = value;
                    Invalidate();
                }
            }
        }

        public void Set(ushort max, ushort val)
        {
            if (max != maximum || val != value)
            {
                maximum = max;
                value = val;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(unknown ? Color.Gray : color))
            {
                Rectangle rect = ClientRectangle;
                double current = value > maximum ? maximum : value;
                rect.Width = (int)(rect.Width / (maximum / current));
                e.Graphics.FillRectangle(brush, rect);
                string text = string.IsNullOrEmpty(Text) ? string.Format("{0}/{1}", value, maximum) : Text;
                SizeF size = e.Graphics.MeasureString(text, Font);
                float x = (ClientRectangle.Width - size.Width) / 2;
                float y = (ClientRectangle.Height - size.Height) / 2;
                e.Graphics.DrawString(text, Font, Brushes.White, x, y);
            }
        }
    }
}