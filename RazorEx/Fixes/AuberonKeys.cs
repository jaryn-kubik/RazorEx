using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Assistant;
using Ultima;

namespace RazorEx.Fixes
{
    public static class AuberonKeys
    {
        public static void OnInit() { PacketHandler.RegisterServerToClientFilter(0xD6, OnProps); }
        private static void OnProps(Packet p, PacketHandlerEventArgs args)
        {
            p.Seek(5, SeekOrigin.Begin);
            Item item = World.FindItem(p.ReadUInt32());
            if (item == null || item.ItemID != 0x21FC || !keys.ContainsKey(item.Hue))
                return;

            if (item.ObjPropList.m_CustomContent.Count == 0)
            {
                string color = ColorTranslator.ToHtml(Hues.GetHue(item.Hue).GetColor(30));
                item.ObjPropList.Add("<basefont color={0}>{1}", color, keys[item.Hue]);
            }
            args.Block = true;
            WorldEx.SendToClient(item.ObjPropList.BuildPacket());
        }

        private static readonly Dictionary<ushort, string> keys = new Dictionary<ushort, string>
        {
            {0x0203, "Sorceror"},
            {0x0118, "Ankh"},
            {0x002D, "Exodus"},
            {0x02B0, "Hellrise"},
            {0x040E, "Unknown"}
        };
    }
}