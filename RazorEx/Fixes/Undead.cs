using System.IO;
using Assistant;

namespace RazorEx.Fixes
{
    public static class Undead
    {
        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "Undead"); }
        private static void OnChange(bool undead)
        {
            if (undead)
            {
                PacketHandler.RegisterServerToClientFilter(0x20, OnMobileUpdate);
                PacketHandler.RegisterServerToClientFilter(0x77, OnMobileUpdate);
                PacketHandler.RegisterServerToClientFilter(0x78, OnMobileUpdate);
                PacketHandler.RegisterServerToClientFilter(0xA3, OnStamina);
                PacketHandler.RegisterServerToClientFilter(0x11, OnMobileStatus);
            }
            else
            {
                PacketHandler.RemoveServerToClientFilter(0x20, OnMobileUpdate);
                PacketHandler.RemoveServerToClientFilter(0x77, OnMobileUpdate);
                PacketHandler.RemoveServerToClientFilter(0x78, OnMobileUpdate);
                PacketHandler.RemoveServerToClientFilter(0xA3, OnStamina);
                PacketHandler.RemoveServerToClientFilter(0x11, OnMobileStatus);
            }
        }

        private static void OnMobileStatus(Packet p, PacketHandlerEventArgs args)
        {
            if (p.ReadUInt32() == World.Player.Serial && World.Player.IsGhost)
            {
                p.Seek(50, SeekOrigin.Begin);
                p.Write(World.Player.StamMax);
            }
        }

        private static void OnStamina(Packet p, PacketHandlerEventArgs args)
        {
            if (p.ReadUInt32() == World.Player.Serial && World.Player.IsGhost)
                p.Write(p.ReadUInt16());
        }

        private static void OnMobileUpdate(Packet p, PacketHandlerEventArgs args)
        {
            if (p.ReadUInt32() == World.Player.Serial)
            {
                ushort body = p.ReadUInt16();
                if (body == 0x0192 || body == 0x0193 || body == 0x025F || body == 0x0260)
                    body -= 2;
                p.Seek(-2, SeekOrigin.Current);
                p.Write(body);
            }
        }
    }
}