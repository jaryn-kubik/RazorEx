using Assistant;
using RazorEx.Skills;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RazorEx.UI
{
    public class MapControl : Control
    {
        private Point3D position;
        private readonly Font bigFont;
        private readonly Dictionary<Serial, Point3D> party = new Dictionary<Serial, Point3D>();

        private Mobile current;
        public Mobile Current
        {
            private get { return current ?? World.Player; }
            set
            {
                current = value;
                Invalidate();
            }
        }

        private IEnumerable<Serial> Party
        {
            get
            {
                if (World.Player != null)
                    yield return World.Player.Serial;
                foreach (Serial serial in PacketHandlers.Party)
                    yield return serial;
            }
        }

        public MapControl()
        {
            Enabled = false;
            Margin = new Padding(0);
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Font = new Font(Font.FontFamily, 6);
            bigFont = new Font(Font.FontFamily, 10, FontStyle.Bold);
        }

        public void Redraw()
        {
            if (position == World.Player.Position)
                foreach (Serial serial in PacketHandlers.Party)
                {
                    Point3D pos;
                    if (!party.TryGetValue(serial, out pos) || (World.Mobiles.ContainsKey(serial) && World.FindMobile(serial).Position != pos))
                    {
                        Invalidate();
                        return;
                    }
                }
            else
                Invalidate();
        }

        private Brush GetBrush(Mobile mobile) { return mobile == World.Player ? Brushes.DeepPink : Brushes.Red; }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Current == null)
                return;

            position = Current.Position;
            Point point = new Point(position.X % 8, position.Y % 8);
            Point start = new Point((position.X / 8) - (Width / 16), (position.Y / 8) - (Height / 16));
            Point center = new Point((position.X - (start.X * 8)) - point.X, (position.Y - (start.Y * 8)) - point.Y);

            e.Graphics.TranslateTransform(-Width / 2F, -Height / 2F, MatrixOrder.Append);
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.ScaleTransform(1.5F, 1.5F, MatrixOrder.Append);
            e.Graphics.RotateTransform(45F, MatrixOrder.Append);
            e.Graphics.TranslateTransform(Width / 2F, Height / 2F, MatrixOrder.Append);
            Ultima.Map map = Map.GetMap(Current.Map) ?? Ultima.Map.Trammel;
            e.Graphics.DrawImage(map.GetImage(start.X, start.Y, Width / 8 + point.X, Height / 8 + point.Y, true), -point.X, -point.Y);
            e.Graphics.ScaleTransform(1F, 1F, MatrixOrder.Append);
            e.Graphics.FillRectangle(GetBrush(Current), center.X - 1, center.Y - 1, 2, 2);
            if (Current != World.Player)
                e.Graphics.DrawString(Current.GetName(), Font, Brushes.Silver, center.X, center.Y);

            party.Clear();
            foreach (Serial serial in Party)
            {
                Mobile mobile = World.FindMobile(serial);
                Point3D pos = mobile != null ? mobile.Position : Point3D.Zero;
                party.Add(serial, pos);
                if (pos != Point3D.Zero && mobile != Current)
                    DrawPoint(e.Graphics, pos, mobile != null ? mobile.GetName() : "(Unknown)", GetBrush(mobile), center);
            }

            if ((PositionCheck.InFire || PositionCheck.InKhaldun) && ConfigEx.GetElement(true, "RadarArcheologyGrid"))
            {
                for (int x = center.X - position.X + (position.X - Width) + 40 - (position.X - Width) % 40; x < Width; x += 40)
                    e.Graphics.DrawLine(Pens.Gray, x, center.Y - Height, x, center.Y + Height);

                for (int y = center.Y - position.Y + (position.Y - Height) + 40 - (position.Y - Height) % 40; y < Height; y += 40)
                    e.Graphics.DrawLine(Pens.Gray, center.X - Width, y, center.X + Width, y);
            }

            foreach (Item sos in World.Player.Backpack.FindItems(0x14ED))
            {
                FishingSOS.SOSInfo info;
                if (FishingSOS.List.TryGetValue(sos.Serial, out info) && info.Felucca.HasValue)
                    DrawPoint(e.Graphics, info.Location, null, Brushes.Gold, center);
            }

            e.Graphics.ResetTransform();
            e.Graphics.DrawString("W", bigFont, Brushes.DeepPink, 0, 0);
            e.Graphics.DrawString("S", bigFont, Brushes.DeepPink, 0, Height - bigFont.Height);
            e.Graphics.DrawString("N", bigFont, Brushes.DeepPink, Width - bigFont.Height, 0);
            e.Graphics.DrawString("E", bigFont, Brushes.DeepPink, Width - bigFont.Height, Height - bigFont.Height);

            position = World.Player.Position;
        }

        private void DrawPoint(Graphics graphics, Point3D pos, string name, Brush brush, Point center)
        {
            Point3D offset = position - pos;
            if (Math.Abs(offset.X) < Width && Math.Abs(offset.Y) < Height)
            {
                graphics.FillRectangle(brush, center.X - offset.X - 1, center.Y - offset.Y - 1, 2, 2);
                if (!string.IsNullOrEmpty(name))
                    graphics.DrawString(name, Font, Brushes.Silver, center.X - offset.X, center.Y - offset.Y);
            }
        }
    }
}