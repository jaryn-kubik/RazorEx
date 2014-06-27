using Assistant;

namespace RazorEx.Fixes
{
    public static class Stealth
    {
        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "StealthFix"); }
        private static void OnChange(bool shringDmg)
        {
            if (shringDmg)
                PacketHandler.RegisterClientToServerFilter(0x02, OnMoveReq);
            else
                PacketHandler.RemoveClientToServerFilter(0x02, OnMoveReq);
        }

        private static void OnMoveReq(Packet p, PacketHandlerEventArgs args)
        {
            if (!World.Player.Visible && (PositionCheck.IsAuberon || PositionCheck.InPanda))
            {
                Direction dir = (Direction)p.ReadByte();
                if (dir.HasFlag(Direction.Running))
                    return;
                p.MoveToData();
                p.Write((byte)(dir | Direction.Running));
            }
        }
    }
}
