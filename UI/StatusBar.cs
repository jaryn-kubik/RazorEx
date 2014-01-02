using Assistant;
using System.Drawing;
using System.Windows.Forms;

namespace RazorEx.UI
{
    public class StatusBar : InGameWindow
    {
        private readonly Bar barHP, barMana, barStamina;
        private readonly LabelEx labelFoll, labelWeight, labelBands;
        private readonly Serial serial;

        private StatusBar(Serial serial)
        {
            this.serial = serial;
            table.Padding = new Padding(3);
            table.Controls.Add(barHP = new Bar(Color.Red));
            if (serial == Serial.MinusOne || PacketHandlers.Party.Contains(serial))
            {
                table.Controls.Add(barMana = new Bar(Color.Blue));
                table.Controls.Add(barStamina = new Bar(Color.DarkGoldenrod));
                if (PacketHandlers.Party.Contains(serial))
                    barMana.Text = barStamina.Text = "-";
            }

            if (serial == Serial.MinusOne)
            {
                if (ConfigEx.GetAttribute(false, "ex", "Status"))
                {
                    table.ColumnCount = 2;
                    table.Controls.Add(labelFoll = new LabelEx("-", margin: 0) { ForeColor = Color.White });
                    table.Controls.Add(labelWeight = new LabelEx("-", margin: 0) { ForeColor = Color.White });
                    table.Controls.Add(labelBands = new LabelEx("-", margin: 0) { ForeColor = Color.White });
                    table.Controls.SetChildIndex(labelFoll, 1);
                    table.Controls.SetChildIndex(labelWeight, 3);
                    table.Controls.SetChildIndex(labelBands, 5);
                    Bandages.Changed += Bandages_Changed;
                }
                ConfigEx.SetAttribute(true, "enabled", "Status");
                BackColor = Color.Purple;
                Location = new Point(ConfigEx.GetAttribute(Location.X, "locX", "Status"),
                                     ConfigEx.GetAttribute(Location.Y, "locY", "Status"));
                UpdateStats();
            }
            else
            {
                Mobile mobile = World.FindMobile(serial);
                if (mobile != null)
                    WorldEx.SendToServer(new StatusQuery(mobile));
            }
            Event.MobileUpdated += WorldEx_MobileUpdated;
        }

        private void Bandages_Changed()
        {
            if (labelBands != null)
                labelBands.Text = Bandages.Amount.ToString();
        }

        private void WorldEx_MobileUpdated(Serial mobileSerial)
        {
            if (serial == mobileSerial || (serial == Serial.MinusOne && mobileSerial == World.Player.Serial))
                UpdateStats();
        }

        private void UpdateStats()
        {
            Mobile mobile = serial == Serial.MinusOne ? World.Player : World.FindMobile(serial);
            if (mobile == null)
                return;

            if (serial != Serial.MinusOne)
            {
                barHP.Text = mobile.GetName();
                BackColor = ColorTranslator.FromHtml("#" + mobile.GetNotorietyColor().ToString("X6"));
            }

            if (mobile.Blessed)
                barHP.Color = Color.Purple;
            else if (mobile.Poisoned)
                barHP.Color = Color.Green;
            else
                barHP.Color = Color.Red;
            barHP.Set(mobile.HitsMax, mobile.Hits);
            if (barMana != null)
                barMana.Set(mobile.ManaMax, mobile.Mana);
            if (barStamina != null)
                barStamina.Set(mobile.StamMax, mobile.Stam);
            if (labelFoll != null)
                labelFoll.Text = string.Format("{0}/{1}", World.Player.Followers, World.Player.FollowersMax);
            if (labelWeight != null)
                labelWeight.Text = string.Format("{0}/{1}", World.Player.Weight, World.Player.MaxWeight);
            Bandages_Changed();
        }

        protected override void Save() { }
        protected override void Dispose(bool disposing)
        {
            if (serial == Serial.MinusOne)
            {
                ConfigEx.SetAttribute(Location.X, "locX", "Status");
                ConfigEx.SetAttribute(Location.Y, "locY", "Status");
            }
            Bandages.Changed -= Bandages_Changed;
            Event.MobileUpdated -= WorldEx_MobileUpdated;
            base.Dispose(disposing);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (serial != Serial.MinusOne)
            {
                if (World.Player.Warmode)
                    WorldEx.SendToServer(new AttackReq(serial));
                else
                    WorldEx.SendToServer(new DoubleClick(serial));
            }
            else
            {
                ConfigEx.SetAttribute(!ConfigEx.GetAttribute(false, "ex", "Status"), "ex", "Status");
                Close();
                new StatusBar(Serial.MinusOne).Show();
            }
            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseRightClick()
        {
            if (serial == Serial.MinusOne)
                ConfigEx.SetAttribute(false, "enabled", "Status");
            base.OnMouseRightClick();
        }

        protected override bool Borderless { get { return true; } }
        protected override bool Lockable { get { return false; } }
        protected override bool Targetable { get { return true; } }
        protected override void OnTarget()
        {
            if (serial == Serial.MinusOne)
                Targeting.TargetSelf();
            else
                Targeting.Target(serial);
        }

        public new static void OnInit()
        {
            Command.Register("status", args => Targeting.OneTimeTarget(OnCommand));
            MainFormEx.Connected += MainFormEx_Connected;
        }

        private static void OnCommand(bool location, Serial serial, Point3D p, ushort gfxid)
        { new StatusBar(serial == World.Player.Serial || !serial.IsValid || !serial.IsMobile ? Serial.MinusOne : serial).Show(); }

        private static void MainFormEx_Connected()
        {
            if (ConfigEx.GetAttribute(false, "enabled", "Status"))
                new StatusBar(Serial.MinusOne).Show();
        }
    }
}