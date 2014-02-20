using System;
using Assistant;

namespace RazorEx
{
    public static partial class Event
    {
        private static Action<Serial> mobileUpdated;
        public static event Action<Serial> MobileUpdated
        {
            add { mobileUpdated += value; }
            remove { mobileUpdated -= value; }
        }

        private static Action playerMoved;
        public static event Action PlayerMoved
        {
            add { playerMoved += value; }
            remove { playerMoved -= value; }
        }

        private static void OnMobileUpdate1(PacketReader p, PacketHandlerEventArgs args) { OnMobileUpdate(p.ReadUInt32()); }
        private static void OnMobileUpdate2(Packet p, PacketHandlerEventArgs args) { OnMobileUpdate(p.ReadUInt32()); }
        private static void OnMobileUpdate(uint serial)
        {
            if (mobileUpdated != null)
                mobileUpdated(serial);
        }

        private static void OnMove(PacketReader p, PacketHandlerEventArgs args)
        {
            if (playerMoved != null)
                playerMoved();
        }
    }
}