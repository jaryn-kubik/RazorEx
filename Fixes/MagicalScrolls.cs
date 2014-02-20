using System.Collections.Generic;
using System.IO;
using Assistant;

namespace RazorEx.Fixes
{
    public static class MagicalScrolls
    {
        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "DyeMagicalScrolls"); }
        private static void OnChange(bool enabled)
        {
            if (enabled)
            {
                PacketHandler.RegisterServerToClientFilter(0xD6, OnProps);
                PacketHandler.RegisterServerToClientFilter(0x25, ContainerContentUpdate);
            }
            else
            {
                PacketHandler.RemoveServerToClientFilter(0xD6, OnProps);
                PacketHandler.RemoveServerToClientFilter(0x25, ContainerContentUpdate);
            }
        }

        private static void OnProps(Packet p, PacketHandlerEventArgs args)
        {
            p.ReadUInt16();
            Item item = World.FindItem(p.ReadUInt32());
            ushort hue = GetHue(item);
            if (hue != 0)
            {
                Packet packet = new ContainerItem(item);
                packet.Seek(-2, SeekOrigin.End);
                packet.Write(hue);
                WorldEx.SendToClient(packet);
            }
        }

        private static void ContainerContentUpdate(Packet p, PacketHandlerEventArgs args)
        {
            Item item = World.FindItem(p.ReadUInt32());
            ushort hue = GetHue(item);
            if (hue != 0)
            {
                p.Seek(-2, SeekOrigin.End);
                p.Write(hue);
            }
        }

        private static ushort GetHue(Item item)
        {
            if (item != null && item.ItemID == 0x14F0 && item.Hue == 0x0625 && item.ObjPropList.m_Content.Count > 1)
            {
                ObjectPropertyList.OPLEntry entry = (ObjectPropertyList.OPLEntry) item.ObjPropList.m_Content[1];
                ushort hue;
                if (scrolls.TryGetValue(entry.Number, out hue))
                    return hue;
            }
            return 0;
        }

        private static readonly Dictionary<int, ushort> scrolls = new Dictionary<int, ushort>
        {
            {1060485, 37},//strength bonus
            {1060431, 39},//hit point increase
            {1060444, 41},//hit point regeneration

            {1060432, 2},//intelligence bonus
            {1060439, 4},//mana increase
            {1060440, 6},//mana regeneration

            {1060409, 67},//dexterity bonus
            {1060484, 69},//stamina increase
            {1060443, 71},//stamina regeneration

            {1060483, 0x0448},//spell damage increase
            {1060415, 0x04AC},//hit chance increase
            {1060401, 0x0444},//damage increase

            {1060413, 1102},//faster casting
            {1060412, 1109},//faster cast recovery
            {1060408, 0x04A8},//defense chance increase
            {1060486, 0x04AB},//swing speed increase
            {1060435, 0x05C5},//lower requirements
            {1060436, 0x047E},//luck
        };
    }
}