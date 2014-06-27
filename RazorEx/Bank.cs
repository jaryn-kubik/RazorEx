using Assistant;
using System.IO;

namespace RazorEx
{
    public static class Bank
    {
        public static bool Opened { get; private set; }
        public static void OnInit()
        {
            PacketHandler.RegisterServerToClientFilter(0x2E, EquipUpdate);
            Event.PlayerMoved += Event_PlayerMoved;
        }

        private static void Event_PlayerMoved()
        {
            if (Opened)
                Opened = false;
        }

        private static void EquipUpdate(Packet p, PacketHandlerEventArgs args)
        {
            p.Seek(8, SeekOrigin.Begin);
            if (p.ReadByte() == 0x1D)
                Opened = true;
        }
    }
}