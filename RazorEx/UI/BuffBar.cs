using Assistant;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RazorEx.UI
{
    public class BuffBar : InGameWindow
    {
        private readonly ToolTip toolTip = new ToolTip();

        private BuffBar()
        {
            table.GrowStyle = TableLayoutPanelGrowStyle.AddColumns;
            table.RowCount = ConfigEx.GetAttribute(0, "orientation", "BuffBar");
            MinimumSize = new Size(30, 30);
            Location = new Point(ConfigEx.GetAttribute(Location.X, "locX", "BuffBar"),
                                 ConfigEx.GetAttribute(Location.Y, "locY", "BuffBar"));
            ConfigEx.SetAttribute(true, "enabled", "BuffBar");
            BuffIcons.Added += BuffIcons_Added;
            BuffIcons.Removed += BuffIcons_Removed;
            Honor.Start += Honor_Start;
            Honor.End += Honor_End;
            Honor.Change += Honor_Change;
        }

        private void Honor_Change()
        {
            LabelEx label = table.Controls.OfType<LabelEx>().FirstOrDefault();
            if (label != null)
                label.Text = Honor.Perfection.ToString();
        }

        private void Honor_End()
        {
            LabelEx label = table.Controls.OfType<LabelEx>().FirstOrDefault();
            if (label != null)
                table.Controls.Remove(label);
        }

        private void Honor_Start()
        {
            foreach (LabelEx control in table.Controls.OfType<LabelEx>())
                table.Controls.Remove(control);
            LabelEx label = new LabelEx("0", 9) { Margin = new Padding(0, 1, 0, 1) };
            Mobile mobile = World.FindMobile(Honor.Current);
            if (mobile != null)
                label.Tag = mobile.Name;
            table.Controls.Add(label);
        }

        private void BuffIcons_Removed(BuffIcon buffID)
        {
            ArtBox gump = table.Controls.OfType<ArtBox>().FirstOrDefault(g => g.ArtID == BuffIcons.GetGumpID(buffID));
            if (gump != null)
            {
                BuffTimer timer = gump.Tag as BuffTimer;
                if (timer != null)
                    timer.Stop();
                table.Controls.Remove(gump);
            }
        }

        private void BuffIcons_Added(BuffIcon buffID, BuffInfo buff)
        {
            ArtBox gump = new ArtBox(BuffIcons.GetGumpID(buffID), isGump: true, vertical: table.RowCount == 0);
            if (buff.Duration != 0)
            {
                BuffTimer timer = new BuffTimer(gump, buff);
                gump.Tag = timer;
                timer.Start();
            }
            table.Controls.Add(gump);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Control control = table.GetChildAtPoint(e.Location);
                string text = null, title = null;
                if (control is ArtBox)
                {
                    BuffInfo buff = BuffIcons.GetBuffByGump(((ArtBox)control).ArtID);
                    if (buff != null)
                    {
                        title = buff.Title;
                        text = buff.Info;
                    }
                }
                else if (control is LabelEx)
                    text = control.Tag as string;
                if (text != null)
                {
                    toolTip.ToolTipTitle = title;
                    toolTip.Show(text, this, e.Location, 5000);
                }
            }
            base.OnMouseClick(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (!locked && e.Button == MouseButtons.Left)
            {
                table.RowCount = table.RowCount == 0 ? 1 : 0;
                foreach (ArtBox box in table.Controls.OfType<ArtBox>())
                    box.Vertical = table.RowCount == 0;
            }
            else
                base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseRightClick()
        {
            ConfigEx.SetAttribute(false, "enabled", "BuffBar");
            base.OnMouseRightClick();
        }

        protected override void Dispose(bool disposing)
        {
            BuffIcons.Added -= BuffIcons_Added;
            BuffIcons.Removed -= BuffIcons_Removed;
            base.Dispose(disposing);
        }

        protected override void Save()
        {
            ConfigEx.SetAttribute(Location.X, "locX", "BuffBar");
            ConfigEx.SetAttribute(Location.Y, "locY", "BuffBar");
            ConfigEx.SetAttribute(table.RowCount, "orientation", "BuffBar");
        }

        public new static void OnInit()
        {
            Command.Register("buffbar", args => new BuffBar().ShowUnlocked());
            MainFormEx.Connected += MainFormEx_Connected;
        }

        private static void MainFormEx_Connected()
        {
            if (ConfigEx.GetAttribute(false, "enabled", "BuffBar"))
                new BuffBar().Show();
        }

        private class BuffTimer : Assistant.Timer
        {
            private readonly ArtBox gump;
            private readonly BuffInfo info;

            public BuffTimer(ArtBox gump, BuffInfo info)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1))
            {
                this.gump = gump;
                this.info = info;
            }

            protected override void OnTick() { gump.Text = info.Duration.ToString(); }
        }
    }
}