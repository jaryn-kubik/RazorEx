using Assistant;
using System.IO;
using System.Text;

namespace RazorEx.Skills
{
    public static class FishingTracking
    {
        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "FishingTracking"); }
        private static void OnChange(bool enabled)
        {
            if (enabled)
                PacketHandler.RegisterServerToClientViewer(0xDD, OnCompressedGump);
            else
                PacketHandler.RemoveServerToClientViewer(0xDD, OnCompressedGump);
        }

        private static void OnCompressedGump(PacketReader p, PacketHandlerEventArgs e)
        {
            p.Seek(7, SeekOrigin.Begin);
            if (p.ReadUInt32() != 0x776CCCC1)
                return;
            p.Seek(19, SeekOrigin.Begin);
            p.Seek(p.ReadInt32(), SeekOrigin.Current);
            int lines = p.ReadInt32(), cLen = p.ReadInt32(), dLen = p.ReadInt32();
            byte[] buffer = new byte[dLen];
            ZLib.uncompress(buffer, ref dLen, p.CopyBytes(p.Position, cLen - 4), cLen - 4);

            for (int i = 0, pos = 0; i < lines; i++)
            {
                int strLen = (buffer[pos++] << 8) | buffer[pos++];
                string str = Encoding.BigEndianUnicode.GetString(buffer, pos, strLen * 2);
                if (str.Trim() == "a sea horse")
                    WorldEx.SendMessage("Sea Horse!!! " + World.Player.Position);
                pos += strLen * 2;
            }
        }
    }
}