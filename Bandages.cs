using System;
using System.IO;
using Assistant;

namespace RazorEx
{
    public static class Bandages
    {
        public static void OnInit()
        {
            PacketHandler.RegisterServerToClientFilter(0x25, ContainerContentUpdate);
            PacketHandler.RegisterServerToClientFilter(0x3C, ContainerContent);
            Event.RemoveObject += Event_RemoveObject;
        }

        private static void ContainerContentUpdate(Packet p, PacketHandlerEventArgs args) { Current = p.ReadUInt32(); }
        private static void ContainerContent(Packet p, PacketHandlerEventArgs args)
        {
            ushort count = p.ReadUInt16();
            for (; count > 0; count--)
            {
                ContainerContentUpdate(p, args);
                p.Seek(15, SeekOrigin.Current);
            }
        }

        private static void Event_RemoveObject(Serial serial)
        {
            if (serial == bandages)
                Current = Serial.Zero;
        }

        private static Serial bandages = Serial.Zero;
        public static ushort Amount { get { return Valid ? World.FindItem(bandages).Amount : (ushort)0; } }
        public static bool Valid { get { return Current != Serial.Zero; } }
        public static bool Enchanced { get { return Valid && World.FindItem(bandages).Hue == 0x0480; } }
        private static Serial Current
        {
            get
            {
                Item item = World.FindItem(bandages);
                return item == null ? Serial.Zero : bandages;
            }
            set
            {
                Item item = World.FindItem(value);
                if (item == null)
                {
                    item = World.Player.Backpack.FindItem(0x0E21, 0x0480, i => i.Serial != bandages) ?? World.Player.Backpack.FindItem(0x0E21, i => i.Serial != bandages);
                    bandages = item == null ? Serial.Zero : item.Serial;
                }
                else if (bandages != value && item.ItemID == 0x0E21 && item.IsMy())
                {
                    Item old = World.FindItem(bandages);
                    if (old == null || (old.Hue != 0x0480 && item.Hue == 0x0480))
                        bandages = value;
                }
                if (changed != null)
                    changed();
            }
        }

        public static void Use()
        {
            if (Valid)
                WorldEx.SendToServer(new DoubleClick(Current));
        }

        private static Action changed;
        public static event Action Changed
        {
            add { changed += value; }
            remove { changed -= value; }
        }
    }
}