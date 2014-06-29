using Assistant;
using System.IO;

namespace RazorEx.Fixes
{
    public static class LootBag
    {
        private static Serial bag;
        public static Serial Bag
        {
            get
            {
                if (!World.Items.ContainsKey(bag))
                {
                    Item item = World.Player.Backpack.FindItem(0x0E75, i => i.Hue == 0x050F || i.Hue == 0x044C, false);
                    bag = item == null ? Serial.Zero : item.Serial;
                }
                return bag;
            }
        }

        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "OpenGrabbedBags"); }
        private static void OnChange(bool open)
        {
            if (open)
                PacketHandler.RegisterServerToClientViewer(0x25, ContainerContentUpdate);
            else
                PacketHandler.RemoveServerToClientViewer(0x25, ContainerContentUpdate);
        }

        private static void ContainerContentUpdate(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            ushort graphic = p.ReadUInt16();
            p.Seek(7, SeekOrigin.Current);
            Serial container = p.ReadUInt32();
            if (serial == DragDropManager.m_Holding)
                return;
            if (Bag != 0 && container == bag && (graphic == 0x0E76 || graphic == 0x0ECF))
                WorldEx.SendToServer(new DoubleClick(serial));
        }
    }
}
