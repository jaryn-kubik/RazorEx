using System;
using System.Drawing;
using System.Windows.Forms;
using Ultima;

namespace RazorEx.UI
{
    public class ArtBox : Control
    {
        private readonly int size;
        private readonly bool isGump;
        private bool vertical;
        public ushort ArtID { get; private set; }
        public ushort Hue { get; private set; }

        public ArtBox(ushort artID, int margin = 1, int size = -1, bool isGump = false, ushort hue = 0, bool vertical = true)
        {
            Enabled = false;
            Margin = new Padding(margin);
            Size = new Size(1, 1);
            Set(artID, hue);
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.size = size;
            this.isGump = isGump;
            this.vertical = vertical;
        }

        public void Set(ushort artID, ushort hueID = 0)
        {
            bool changed = artID != ArtID || hueID != Hue;
            if (changed)
            {
                ArtID = artID;
                Hue = hueID;
                Invalidate();
            }
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

        public bool Vertical
        {
            set
            {
                if (vertical != value)
                {
                    vertical = value;
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Bitmap bitmap;
            if (ArtID == 0xFFFF)
            {
                bitmap = new Bitmap(20, 20);
                Graphics g = Graphics.FromImage(bitmap);
                g.DrawLine(Pens.DeepPink, 0, 0, 19, 19);
                g.DrawLine(Pens.DeepPink, 19, 0, 0, 19);
            }
            else
                bitmap = isGump ? Core.GetGump(ArtID) : Art.GetStatic(ArtID);

            if (size != -1)
                bitmap = new Bitmap(bitmap, size, size);
            else if (Hue != 0)
                bitmap = new Bitmap(bitmap);
            if (Hue != 0)
                Hues.GetHue(Hue).ApplyTo(bitmap, false);

            Point position = new Point();
            Size bSize;
            if (isGump)
                bSize = bitmap.Size;
            else
            {
                int xMin, yMin, xMax, yMax;
                Art.Measure(bitmap, out xMin, out yMin, out xMax, out yMax);
                bSize = new Size(xMax - xMin + 1, yMax - yMin + 1);
                position = new Point(-xMin, -yMin);
            }

            if (!string.IsNullOrEmpty(Text))
            {
                if (vertical)
                {
                    StringFormat format = new StringFormat(StringFormatFlags.DirectionVertical);
                    SizeF sizeF = e.Graphics.MeasureString(Text, Font, -1, format);
                    Size = new Size(bSize.Width + Font.Height, Math.Max(bSize.Height, (int)sizeF.Height));
                    e.Graphics.DrawString(Text, Font, Brushes.DeepPink, Size.Width - (Font.Height - Font.Size) / 2 - Font.Height, (Size.Height - sizeF.Height) / 2, format);
                }
                else
                {
                    SizeF sizeF = e.Graphics.MeasureString(Text, Font, -1);
                    Size = new Size(Math.Max(bSize.Width, (int)sizeF.Width), bSize.Height + Font.Height);
                    e.Graphics.DrawString(Text, Font, Brushes.DeepPink, (Size.Width - sizeF.Width) / 2, Size.Height - Font.Height);
                }
            }
            else
                Size = bSize;

            if (vertical)
                e.Graphics.DrawImage(bitmap, position.X, position.Y + (Size.Height - bSize.Height) / 2F);
            else
                e.Graphics.DrawImage(bitmap, position.X + (Size.Width - bSize.Width) / 2F, position.Y);
        }
    }
}