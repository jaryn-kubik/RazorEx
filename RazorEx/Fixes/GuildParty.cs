using Assistant;

namespace RazorEx.Fixes
{
    public static class GuildParty
    {
        private const uint gumpID = 0xF6E2284F;
        private static uint responseID;
        private static Packet packet;

        public static void OnInit() { ConfigAgent.AddItem(false, OnChanged, "AutoGuildParty"); }
        private static void OnChanged(bool value)
        {
            if (value)
            {
                PacketHandler.RegisterServerToClientFilter(0xBF, OnParty);
                PacketHandler.RegisterServerToClientFilter(0xDD, OnCompressedGump);
            }
            else
            {
                PacketHandler.RemoveServerToClientFilter(0xBF, OnParty);
                PacketHandler.RemoveServerToClientFilter(0xDD, OnCompressedGump);
            }
        }

        private static void OnCompressedGump(Packet p, PacketHandlerEventArgs e)
        {
            p.MoveToData();
            uint sender = p.ReadUInt32();
            if (p.ReadUInt32() == gumpID)
            {
                responseID = sender;
                packet = new Packet();
                packet.Copy(p);
                e.Block = true;
            }
        }

        private static void OnParty(Packet p, PacketHandlerEventArgs args)
        {
            p.MoveToData();
            if (p.ReadUInt16() != 6 || p.ReadByte() != 7)
                return;
            Mobile mobile = World.FindMobile(p.ReadUInt32());
            if (responseID != 0 && mobile != null && mobile.Notoriety == 2)
                WorldEx.SendToServer(new GumpResponse(responseID, gumpID, 1, new int[0], new GumpTextEntry[0]));
            else
                WorldEx.SendToClient(packet);
            responseID = 0;
            packet = null;
        }
    }
}