using Assistant;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RazorEx.UI
{
    public class Radar : InGameWindow
    {
        protected override bool Borderless { get { return true; } }
        private readonly MapControl map = new MapControl();
        private readonly ReqPartyLocTimer timer = new ReqPartyLocTimer();

        private Radar()
        {
            AutoSizeMode = AutoSizeMode.GrowOnly;
            ConfigEx.SetAttribute(true, "enabled", "Radar");
            Location = new Point(ConfigEx.GetAttribute(Location.X, "locX", "Radar"),
                                 ConfigEx.GetAttribute(Location.Y, "locY", "Radar"));
            Size = new Size(ConfigEx.GetAttribute(200, "sizeW", "Radar"),
                            ConfigEx.GetAttribute(200, "sizeH", "Radar"));
            table.Controls.Add(map);
            Event.MobileUpdated += Event_MobileUpdated;
            Event.PlayerMoved += Event_PlayerMoved;
            PacketHandler.RegisterServerToClientFilter(0xBF, OnMapChange);
            PacketHandler.RegisterServerToClientFilter(0xF0, OnPartyPosition);
            timer.Start();
        }

        private void OnPartyPosition(Packet p, PacketHandlerEventArgs args) { map.Redraw(); }
        private void OnMapChange(Packet p, PacketHandlerEventArgs args)
        {
            ushort id = p.ReadUInt16();
            if (id == 8 || id == 6)
                map.Redraw();
        }

        private void Event_PlayerMoved() { map.Redraw(); }
        private void Event_MobileUpdated(Serial serial)
        {
            if (serial == World.Player.Serial || (World.Mobiles.ContainsKey(serial) && World.FindMobile(serial).InParty))
                map.Redraw();
        }

        protected override void Dispose(bool disposing)
        {
            Event.MobileUpdated -= Event_MobileUpdated;
            Event.PlayerMoved -= Event_PlayerMoved;
            PacketHandler.RemoveServerToClientFilter(0xBF, OnMapChange);
            base.Dispose(disposing);
        }

        protected override void Save()
        {
            ConfigEx.SetAttribute(Location.X, "locX", "Radar");
            ConfigEx.SetAttribute(Location.Y, "locY", "Radar");
            ConfigEx.SetAttribute(Width, "sizeW", "Radar");
            ConfigEx.SetAttribute(Height, "sizeH", "Radar");
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            map.Invalidate();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                FormBorderStyle = FormBorderStyle == FormBorderStyle.None ? FormBorderStyle.SizableToolWindow : FormBorderStyle.None;
            else
                base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseRightClick()
        {
            ConfigEx.SetAttribute(false, "enabled", "Radar");
            base.OnMouseRightClick();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (locked && e.Button == MouseButtons.Right && World.Player != null)
            {
                ContextMenu menu = new ContextMenu();
                menu.MenuItems.Add(new MenuItem(World.Player.GetName(), (s, a) => map.Current = null));
                foreach (Mobile mobile in PacketHandlers.Party.Cast<Serial>().Select(World.FindMobile).Where(mobile => mobile != null))
                    menu.MenuItems.Add(new MenuItem(mobile.GetName(), (s, a) => map.Current = mobile));
                menu.Show(this, e.Location);
            }
        }

        public new static void OnInit()
        {
            ConfigAgent.AddItem(true, "RadarArcheologyGrid");
            Command.Register("radar", args => new Radar().ShowUnlocked());
            MainFormEx.Connected += MainFormEx_Connected;
        }

        private static void MainFormEx_Connected()
        {
            if (ConfigEx.GetAttribute(false, "enabled", "Radar"))
                new Radar().Show();
        }

        private class ReqPartyLocTimer : Assistant.Timer
        {
            public ReqPartyLocTimer() : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0)) { }
            protected override void OnTick()
            {
                if (PacketHandlers.SpecialPartySent > PacketHandlers.SpecialPartyReceived)
                {
                    PacketHandlers.SpecialPartySent = PacketHandlers.SpecialPartyReceived = 0;
                    Interval = TimeSpan.FromSeconds(5.0);
                }
                else
                {
                    Interval = TimeSpan.FromSeconds(1.0);
                    if (PacketHandlers.Party.Cast<Serial>().Select(World.FindMobile)
                        .Any(mobile => mobile != World.Player && (mobile == null || Utility.Distance(World.Player.Position, mobile.Position) > World.Player.VisRange || !mobile.Visible)))
                    {
                        PacketHandlers.SpecialPartySent++;
                        ClientCommunication.SendToServer(new QueryPartyLocs());
                    }
                }
            }
        }
    }
}