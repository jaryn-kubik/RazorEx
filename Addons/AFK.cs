using Assistant;
using System.IO;
using System.Text;

namespace RazorEx.Addons
{
    public static class AFK
    {
        private const uint compressedID = 0x18471015;
        private const uint responseID = 0x05D3C7DF;
        private static uint _responseSender;

        public static void OnInit() { PacketHandler.RegisterServerToClientViewer(0xDD, OnCompressedGump); }
        private static void OnCompressedGump(PacketReader p, PacketHandlerEventArgs e)
        {
            p.MoveToData();
            uint sender = p.ReadUInt32();
            uint id = p.ReadUInt32();
            if (id == responseID)
                _responseSender = sender;
            if (id != compressedID)
                return;
            p.Seek(19, SeekOrigin.Begin);
            p.Seek(p.ReadInt32(), SeekOrigin.Current);
            int lines = p.ReadInt32(), cLen = p.ReadInt32(), dLen = p.ReadInt32();
            if (cLen < 5)
                return;
            byte[] buffer = new byte[dLen];
            ZLib.uncompress(buffer, ref dLen, p.CopyBytes(p.Position, cLen - 4), cLen - 4);
            string afk = string.Empty;
            for (int i = 0, pos = 0; i < lines; i++)
            {
                int strLen = (buffer[pos++] << 8) | buffer[pos++];
                string str = Encoding.BigEndianUnicode.GetString(buffer, pos, strLen * 2);
                int index = str.IndexOf('>');
                if (index != -1 && index < str.Length - 1)
                    afk += str[index + 1].ToString().ToUpper().Normalize(NormalizationForm.FormD)[0];
                pos += strLen * 2;
            }
            afk = afk.Trim();
            if (afk.Length == 5 && _responseSender != 0)
            {
                /*ClientCommunication.SendToClient(new CloseGump(responseID));
                WorldEx.SendToServer(new GumpResponse(responseSender, responseID, 0x310, new int[0], new[] { new GumpTextEntry(0x310, afk) }));
                responseSender = 0;*/
                WorldEx.OverHeadMessage(afk);
            }
        }
    }
}