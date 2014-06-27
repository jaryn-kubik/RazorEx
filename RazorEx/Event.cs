using Assistant;
using System;

namespace RazorEx
{
    public static partial class Event
    {
        public static void OnInit()
        {
            PacketHandler.RegisterServerToClientViewer(0xA1, OnMobileUpdate1); // HitsUpdate
            PacketHandler.RegisterServerToClientViewer(0xA2, OnMobileUpdate1); // ManaUpdate
            PacketHandler.RegisterServerToClientViewer(0xA3, OnMobileUpdate1); // StamUpdate
            PacketHandler.RegisterServerToClientViewer(0x2D, OnMobileUpdate1); // MobileStatInfo
            PacketHandler.RegisterServerToClientViewer(0x11, OnMobileUpdate1); // MobileStatus
            PacketHandler.RegisterServerToClientViewer(0x17, OnMobileUpdate1); // NewMobileStatus
            PacketHandler.RegisterServerToClientFilter(0x77, OnMobileUpdate2); // MobileMoving
            PacketHandler.RegisterServerToClientFilter(0x20, OnMobileUpdate2); // MobileUpdate
            PacketHandler.RegisterServerToClientViewer(0x1D, OnRemoveObject);
            PacketHandler.RegisterServerToClientViewer(0x22, OnMove);

            PacketHandler.RegisterServerToClientViewer(0x1C, OnASCIIMessage);
            PacketHandler.RegisterServerToClientViewer(0xAE, OnUnicodeMessage);
            PacketHandler.RegisterServerToClientViewer(0xC1, OnLocalizedMessage);
            PacketHandler.RegisterServerToClientViewer(0xCC, OnMessageLocalizedAffix);

            PacketHandler.RegisterServerToClientViewer(0xC0, OnHuedEffect);
        }

        private static Action<Serial> removeObject;
        public static event Action<Serial> RemoveObject
        {
            add { removeObject += value; }
            remove { removeObject -= value; }
        }

        private static void OnRemoveObject(PacketReader p, PacketHandlerEventArgs args)
        {
            if (removeObject != null)
                removeObject(p.ReadUInt32());
        }
    }
}
