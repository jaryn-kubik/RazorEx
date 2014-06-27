using Assistant;
using System.Collections.Generic;
using System.Globalization;

namespace RazorEx.Addons
{
    public static class Looting
    {
        public static IEnumerable<LootItem> Items { get { return items; } }
        private static readonly List<LootItem> items = new List<LootItem>();

        public static void OnInit()
        {
            foreach (string[] data in ConfigEx.LoadCfg("loot.cfg", 3))
            {
                ushort graphic, color;
                if (ushort.TryParse(data[0].Substring(2), NumberStyles.HexNumber, null, out graphic) && ushort.TryParse(data[1].Substring(2), NumberStyles.HexNumber, null, out color) && !string.IsNullOrEmpty(data[2]))
                    items.Add(new LootItem { Graphic = graphic, Color = color, Name = data[2] });
            }

            if (items.Count > 0)
                PacketHandler.RegisterServerToClientFilter(0x3C, ContainerContent);
        }

        private static void ContainerContent(Packet p, PacketHandlerEventArgs args)
        {
            int count = p.ReadUInt16();
            for (int i = 0; i < count; i++)
            {
                Item item = World.FindItem(p.ReadUInt32());
                Item container = item.Container as Item;
                if (container != null && container.ItemID == 0x2006)
                    foreach (LootItem loot in items)
                        if (item.ItemID == loot.Graphic && (item.Hue == loot.Color || loot.Color == 0xFFFF))
                        {
                            if (Fixes.LootBag.Bag != 0)
                                DragDrop.Move(item, Fixes.LootBag.Bag);
                            WorldEx.OverHeadMessage(loot.Name, container);
                        }
                p.Seek(15, System.IO.SeekOrigin.Current);
            }
        }

        public class LootItem
        {
            public ushort Graphic { get; set; }
            public ushort Color { get; set; }
            public string Name { get; set; }
        }
    }
}