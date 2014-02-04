using System.IO;
using Assistant;

namespace RazorEx.Fixes
{
    public static class RuneBook
    {
        private const uint gumpID = 0x098F2406;
        private enum TeleportType { Default, Recall = 3, GateTravel, SacredJourney }

        public static void OnInit()
        {
            ConfigAgent.AddItem(TeleportType.Default, "RuneBook");
            PacketHandler.RegisterClientToServerFilter(0xB1, OnGumpResponse);
        }

        private static void OnGumpResponse(Packet p, PacketHandlerEventArgs args)
        {
            p.ReadUInt32();
            if (p.ReadUInt32() != gumpID)
                return;
            uint buttonID = p.ReadUInt32();
            if ((buttonID - 2) % 6 == 0)
            {
                p.Seek(-4, SeekOrigin.Current);
                p.Write(buttonID + (uint)ConfigEx.GetElement(TeleportType.Default, "RuneBook"));
            }
        }
    }
}