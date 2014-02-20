using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assistant;

namespace RazorEx.Fixes
{
    public static class Maps
    {
        private static readonly List<Point2D> positions = new List<Point2D>();
        private static ushort x, y;

        public static void OnInit()
        {
            PacketHandler.RegisterServerToClientViewer(0x90, MapDetails);
            PacketHandler.RegisterServerToClientViewer(0x56, MapPlot);
            PacketHandler.RegisterClientToServerViewer(0xBF, CancelArrow);

            using (StreamReader stream = new StreamReader(typeof(Maps).Assembly.GetManifestResourceStream("RazorEx.treasure.cfg")))
            {
                string str;
                while ((str = stream.ReadLine()) != null)
                {
                    string[] data = str.Split(' ');
                    positions.Add(new Point2D(int.Parse(data[0]), int.Parse(data[1])));
                }
            }
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
                Point2D map = new Point2D(x, y);
                Point2D closest = positions.Aggregate((min, next) =>
                    Utility.Distance(map, min) < Utility.Distance(map, next) ? min : next);
                WorldEx.SendMessage(string.Format("Map opened to {0}, {1}. ({2})", x, y, positions.IndexOf(closest) + 1));
                WorldEx.SendToClient(new QuestArrow(true, x, y));
            }
        }
    }
}