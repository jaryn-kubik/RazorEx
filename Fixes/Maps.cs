using Assistant;

namespace RazorEx.Fixes
{
    public static class Maps
    {
        private static ushort x, y;

        public static void OnInit()
        {
            PacketHandler.RegisterServerToClientViewer(0x90, MapDetails);
            PacketHandler.RegisterServerToClientViewer(0x56, MapPlot);
            PacketHandler.RegisterClientToServerViewer(0xBF, CancelArrow);
        }

        private static void CancelArrow(PacketReader p, PacketHandlerEventArgs args)
        {
            if (p.Length == 6 && p.ReadUInt16() == 0x7 && p.ReadByte() == 1)
                WorldEx.SendToClient(new QuestArrow(false, ushort.MaxValue, ushort.MaxValue));
        }

        private static void MapDetails(PacketReader p, PacketHandlerEventArgs args)
        {
            p.ReadUInt32();
            p.ReadUInt16();
            x = p.ReadUInt16();
            y = p.ReadUInt16();
        }

        private static void MapPlot(PacketReader p, PacketHandlerEventArgs args)
        {
            p.ReadUInt32();
            if (p.ReadByte() == 1 && p.ReadByte() == 0)
            {
                x += (ushort)(p.ReadUInt16() * 2);
                y += (ushort)(p.ReadUInt16() * 2);
                WorldEx.SendMessage(string.Format("Map opened to {0}, {1}.", x, y));
                WorldEx.SendToClient(new QuestArrow(true, x, y));
            }
        }
    }
}