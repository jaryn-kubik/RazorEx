using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Assistant;

namespace RazorEx.Skills
{
    public class CarvingAgent : Agent
    {
        private ListBox listBox;
        private Button button;
        public override string Name { get { return "Carving"; } }

        private static bool enabled;
        private static readonly List<CorpseItem> items = new List<CorpseItem>();
        private static readonly List<Serial> carved = new List<Serial>();
        public static void OnInit()
        {
            CarvingAgent agent = new CarvingAgent();
            Add(agent);

            enabled = ConfigEx.GetAttribute(false, "enabled", "Carving");
            onChange();

            XElement parent = ConfigEx.GetXElement(false, "Carving");
            foreach (XElement element in parent.Elements())
            {
                string name = element.Value;
                ushort itemID = ushort.Parse(element.Attribute("itemID").Value, System.Globalization.NumberStyles.HexNumber);
                items.Add(new CorpseItem(itemID, name));
            }
        }

        private static void onChange()
        {
            if (enabled)
                PacketHandler.RegisterServerToClientViewer(0x1A, WorldItem);
            else
                PacketHandler.RemoveServerToClientViewer(0x1A, WorldItem);
        }

        private static void WorldItem(PacketReader p, PacketHandlerEventArgs args)
        {
            Item item = World.FindItem(p.ReadUInt32() & 0x7FFFFFFF);
            if (item == null || item.ItemID != 0x2006 || item.DistanceTo(World.Player) > 2 ||
                items.Find(c => c.ItemID == item.Amount) == null)
                return;

            Item carver = World.FindItem(ConfigEx.GetElement<uint>(0, "BoneCarver"));
            carved.RemoveAll(i => !World.Items.ContainsKey(i));
            if (carver != null && !carved.Contains(item.Serial))
            {
                Targeting.CancelTarget();
                Targeting.QueuedTarget = () =>
                {
                    Targeting.Target(item);
                    carved.Add(item.Serial);
                    return true;
                };
                WorldEx.SendToServer(new DoubleClick(carver.Serial));
            }
        }

        public override void Save(XmlTextWriter xml) { }
        public override void Load(XmlElement node) { }
        public override void Clear() { }
        public override void OnSelected(ListBox subList, params Button[] buttons)
        {
            buttons[0].Text = "Add";
            buttons[0].Visible = true;
            buttons[1].Text = "Remove";
            buttons[1].Visible = true;
            button = buttons[2];
            button.Text = enabled ? "Enabled" : "Disabled";
            button.Visible = true;

            listBox = subList;
            subList.BeginUpdate();
            subList.Sorted = true;
            subList.Items.Clear();
            foreach (CorpseItem item in items)
                subList.Items.Add(item);
            subList.EndUpdate();
        }

        public override void OnButtonPress(int num)
        {
            if (num == 1)
                Targeting.OneTimeTarget(addTarget);
            else if (num == 2)
            {
                CorpseItem corpse = listBox.SelectedItem as CorpseItem;
                items.Remove(corpse);
                listBox.Items.Remove(listBox.SelectedItem);
            }
            else if (num == 3)
            {
                enabled = !enabled;
                button.Text = enabled ? "Enabled" : "Disabled";
                onChange();
            }
        }

        private void addTarget(bool location, Serial serial, Point3D p, ushort gfxid)
        {
            Mobile mobile = World.FindMobile(serial);
            if (mobile != null && !string.IsNullOrEmpty(mobile.Name))
            {
                CorpseItem corpse = new CorpseItem(mobile.Body, mobile.Name);
                if (items.Find(c => c.ItemID == corpse.ItemID) == null)
                {
                    items.Add(corpse);
                    listBox.Items.Add(corpse);

                    XElement parent = ConfigEx.GetXElement(true, "Carving");
                    parent.RemoveAll();
                    parent.SetAttributeValue("enabled", enabled);
                    foreach (CorpseItem item in items)
                        parent.Add(new XElement("Corpse", new XAttribute("itemID", item.ItemID.Value.ToString("X4")), item.Name));
                }
            }
            else
                WorldEx.SendMessage("Invalid target");
        }

        protected class CorpseItem
        {
            public ItemID ItemID { get; private set; }
            public string Name { get; private set; }

            public CorpseItem(ItemID itemID, string name)
            {
                ItemID = itemID;
                Name = name;
            }

            public override string ToString() { return string.Format("{0}: {1}", Name, ItemID.Value.ToString("X4")); }
        }
    }
}