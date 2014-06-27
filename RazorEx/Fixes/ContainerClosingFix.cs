using System.Collections.Generic;
using Assistant;

namespace RazorEx.Fixes
{
    public static class ContainerClosingFix
    {
        private static readonly Dictionary<Serial, Item> containers = new Dictionary<Serial, Item>();

        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "ContainerClosingFix"); }
        private static void OnChange(bool fix)
        {
            if (fix)
            {
                PacketHandler.RegisterServerToClientViewer(0x25, ContainerContentUpdate1);
                PacketHandler.RegisterServerToClientFilter(0x25, ContainerContentUpdate2);
                Event.RemoveObject += Event_RemoveObject;
                if (World.Player != null)
                    foreach (Item item in World.Player.Backpack.FindItems(i => i.IsContainer, false))
                        containers.Add(item.Serial, item);
            }
            else
            {
                PacketHandler.RemoveServerToClientViewer(0x25, ContainerContentUpdate1);
                PacketHandler.RemoveServerToClientFilter(0x25, ContainerContentUpdate2);
                Event.RemoveObject -= Event_RemoveObject;
                containers.Clear();
            }
        }

        private static void ContainerContentUpdate1(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            if (containers.ContainsKey(serial))
            {
                Item item = World.FindItem(serial);
                if (item == null || item.Deleted || item != containers[serial])
                    args.Block = true;
                else
                {
                    ushort itemID = (ushort)(p.ReadUInt16() + p.ReadSByte());
                    ushort amount = p.ReadUInt16();
                    if (amount == 0)
                        amount = 1;
                    Point3D position = new Point3D(p.ReadUInt16(), p.ReadUInt16(), 0);
                    p.ReadUInt32();
                    ushort hue = p.ReadUInt16();
                    args.Block = item.ItemID == itemID && item.Amount == amount && item.Position == position && item.Hue == hue;
                }
            }
        }

        private static void ContainerContentUpdate2(Packet p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            Item item = World.FindItem(serial);
            if (item != null && item.Container == World.Player.Backpack && item.IsContainer)
            {
                if (containers.ContainsKey(serial))
                    containers[serial] = item;
                else
                    containers.Add(serial, item);
            }
        }

        private static void Event_RemoveObject(Serial serial) { containers.Remove(serial); }
    }
}