using Assistant;
using System;
using System.IO;

namespace RazorEx
{
    public static class Event
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
            PacketHandler.RegisterServerToClientViewer(0xC1, OnLocalizedMessage);
            PacketHandler.RegisterServerToClientViewer(0xCC, OnLocalizedMessage2);
            PacketHandler.RegisterServerToClientViewer(0x1D, OnRemoveObject);
            PacketHandler.RegisterServerToClientViewer(0x22, OnMove);
        }

        private static void OnMobileUpdate1(PacketReader p, PacketHandlerEventArgs args) { OnMobileUpdate(p.ReadUInt32()); }
        private static void OnMobileUpdate2(Packet p, PacketHandlerEventArgs args) { OnMobileUpdate(p.ReadUInt32()); }
        private static void OnMobileUpdate(uint serial)
        {
            if (mobileUpdated != null)
                mobileUpdated(serial);
        }

        private static Action<Serial> mobileUpdated;
        public static event Action<Serial> MobileUpdated
        {
            add { mobileUpdated += value; }
            remove { mobileUpdated -= value; }
        }

        private static void OnLocalizedMessage(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            p.Seek(14, SeekOrigin.Begin);
            int num = p.ReadInt32();
            p.Seek(30, SeekOrigin.Current);
            if (localizedMessage != null)
                localizedMessage(serial, num, p.ReadUnicodeStringBE(((p.Length - 1) - p.Position) / 2));
        }

        private static void OnLocalizedMessage2(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            p.Seek(14, SeekOrigin.Begin);
            int num = p.ReadInt32();
            p.Seek(30, SeekOrigin.Current);
            if (localizedMessage != null)
                localizedMessage(serial, num, string.Empty);
        }

        private static Action<Serial, int, string> localizedMessage;
        public static event Action<Serial, int, string> LocalizedMessage
        {
            add { localizedMessage += value; }
            remove { localizedMessage -= value; }
        }

        private static void OnRemoveObject(PacketReader p, PacketHandlerEventArgs args)
        {
            if (removeObject != null)
                removeObject(p.ReadUInt32());
        }

        private static Action<Serial> removeObject;
        public static event Action<Serial> RemoveObject
        {
            add { removeObject += value; }
            remove { removeObject -= value; }
        }

        private static void OnMove(PacketReader p, PacketHandlerEventArgs args)
        {
            if (playerMoved != null)
                playerMoved();
        }

        private static Action playerMoved;
        public static event Action PlayerMoved
        {
            add { playerMoved += value; }
            remove { playerMoved -= value; }
        }
    }
}
