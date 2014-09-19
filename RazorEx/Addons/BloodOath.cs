using Assistant;

namespace RazorEx.Addons
{
    public static class BloodOath
    {
        private static uint lastSerial, lastOath;
        public static void OnInit()
        {
            ConfigAgent.AddItem(false, OnChange, "BloodOath");
            Core.AddHotkey("Attack Last", AttackLast);
        }

        private static void OnChange(bool enabled)
        {
            if (enabled)
            {
                Event.HuedEffect += Event_HuedEffect;
                BuffIcons.Added += BuffIcons_Added;
                BuffIcons.Removed += BuffIcons_Removed;
            }
            else
            {
                Event.HuedEffect -= Event_HuedEffect;
                BuffIcons.Added -= BuffIcons_Added;
                BuffIcons.Removed -= BuffIcons_Removed;
            }
        }

        private static bool? Event_HuedEffect(byte type, Serial src, Serial dest, ItemID itemID, byte speed, byte count, uint hue, uint mode)
        {
            if (type == 3 && src != World.Player.Serial && dest == 0 && itemID == 0x375A)
                lastSerial = src;
            return null;
        }

        private static void BuffIcons_Added(BuffIcon buffId, BuffInfo info)
        {
            if (buffId == BuffIcon.BloodOathCurse)
            {
                Mobile mobile = World.FindMobile(lastSerial);
                if (mobile == null)
                    return;
                lastOath = lastSerial;
                if (Utility.Distance(mobile.Position, World.Player.Position) < 2)
                {
                    WorldEx.SendToServer(new SetWarMode(true));
                    WorldEx.SendToServer(new SetWarMode(false));
                    WorldEx.OverHeadMessage("!Blood Oath!", 0x0017);
                }
            }
        }

        private static void BuffIcons_Removed(BuffIcon buffId)
        {
            if (buffId == BuffIcon.BloodOathCurse)
                lastSerial = lastOath = 0;
        }

        public static void AttackLast()
        {
            if (Targeting.m_LastTarget != null && Targeting.m_LastTarget.Serial != lastOath)
            {
                Mobile mobile = World.FindMobile(Targeting.m_LastTarget.Serial);
                if (mobile != null && !FriendsAgent.IsFriend(mobile) && mobile.Notoriety != 1 && mobile.Notoriety != 2 && !mobile.Renamable)
                    ClientCommunication.SendToServer(new AttackReq(Targeting.m_LastTarget.Serial));
            }
        }
    }
}
