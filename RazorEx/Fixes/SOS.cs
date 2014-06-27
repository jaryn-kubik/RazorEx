using Assistant;
using System;
using System.IO;
using System.Text;

namespace RazorEx.Fixes
{
    public static class SOS
    {
        public static void OnInit() { PacketHandler.RegisterServerToClientViewer(0xDD, OnCompressedGump); }
        private static void OnCompressedGump(PacketReader p, PacketHandlerEventArgs e)
        {
            p.Seek(7, SeekOrigin.Begin);
            if (p.ReadUInt32() != 0x1105B263)
                return;

            p.Seek(19, SeekOrigin.Begin);
            p.Seek(p.ReadInt32() + 4, SeekOrigin.Current);
            int cLen = p.ReadInt32(), dLen = p.ReadInt32();
            byte[] buffer = new byte[dLen];
            ZLib.uncompress(buffer, ref dLen, p.CopyBytes(p.Position, cLen - 4), cLen - 4);
            int strLen = (buffer[0] << 8) | buffer[1];
            string[] str = Encoding.BigEndianUnicode.GetString(buffer, 2, strLen * 2).Split(',');

            string[] lat = str[0].Split('°');
            int yLat = int.Parse(lat[0]);
            int yMins = int.Parse(lat[1].Split('\'')[0]);
            bool ySouth = lat[1][lat[1].Length - 1] == 'S';

            string[] lon = str[1].Split('°');
            int xLong = int.Parse(lon[0]);
            int xMins = int.Parse(lon[1].Split('\'')[0]);
            bool xEast = lon[1][lon[1].Length - 1] == 'E';

            const int xWidth = 5120;
            const int yHeight = 4096;
            const int xCenter = 1323;
            const int yCenter = 1624;

            double absLong = xLong + ((double)xMins / 60);
            double absLat = yLat + ((double)yMins / 60);

            if (!xEast)
                absLong = 360.0 - absLong;

            if (!ySouth)
                absLat = 360.0 - absLat;

            int x = xCenter + (int)((absLong * xWidth) / 360);
            int y = yCenter + (int)((absLat * yHeight) / 360);

            if (x < 0)
                x += xWidth;
            else if (x >= xWidth)
                x -= xWidth;

            if (y < 0)
                y += yHeight;
            else if (y >= yHeight)
                y -= yHeight;

            onGump(x, y);
        }

        private static void ShowPosition(int x, int y)
        {
            WorldEx.SendMessage(string.Format("SOS position: {0}, {1} ({2})", x, y, Utility.Distance(World.Player.Position.X, World.Player.Position.Y, x, y)));
            WorldEx.SendToClient(new QuestArrow(true, (ushort)x, (ushort)y));
        }

        private static Action<int, int> onGump = ShowPosition;
        public static event Action<int, int> OnGump
        {
            add { onGump += value; }
            remove { onGump -= value; }
        }
    }
}