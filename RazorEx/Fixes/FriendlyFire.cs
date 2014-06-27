using Assistant;

namespace RazorEx.Fixes
{
    public static class FriendlyFire
    {
        public static void OnInit()
        {
            ConfigAgent.AddItem(false, OnChange, "BlockFriendlyFire");
            Command.Register("friendlyfire", OnCommand);
        }

        private static void OnCommand(string[] args)
        {
            bool enabled = !ConfigEx.GetElement(false, "BlockFriendlyFire");
            ConfigEx.SetElement(enabled, "BlockFriendlyFire");
            WorldEx.SendMessage("Friendly fire " + (enabled ? "blocked." : "allowed."));
            OnChange(enabled);
        }

        private static void OnChange(bool enabled)
        {
            if (enabled)
                PacketHandler.RegisterClientToServerViewer(0x05, OnAttack);
            else
                PacketHandler.RemoveClientToServerViewer(0x05, OnAttack);
        }

        private static void OnAttack(PacketReader p, PacketHandlerEventArgs args)
        {
            Mobile mobile = World.FindMobile(p.ReadUInt32());
            if (mobile != null && (FriendsAgent.IsFriend(mobile) || mobile.Notoriety == 1 || mobile.Notoriety == 2 || mobile.Renamable))
            {
                WorldEx.SendMessage("Attack blocked.");
                args.Block = true;
            }
        }
    }
}