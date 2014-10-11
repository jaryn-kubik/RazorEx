using Assistant;
using System;
using System.Windows.Forms;

namespace RazorEx.Fixes
{
    public static class TargetingEx
    {
        public static void OnInit()
        {
            TreeNode node = Core.AddHotkeyNode("Targeting");
            Core.AddHotkey(node, "Closest Grey", () => ClosestTarget(3, 4));
            Core.AddHotkey(node, "Closest Red/Enemy", () => ClosestTarget(5, 6));
            Core.AddHotkey(node, "Closest Non-Friendly", () => ClosestTarget(3, 4, 5, 6));
        }

        public static void ClosestTarget(params byte[] notoriety)
        {
            Mobile closest = GetClosest(notoriety);
            if (closest != null)
                Targeting.SetLastTargetTo(closest);
        }

        public static Mobile GetClosest(params byte[] notoriety)
        {
            Mobile result = null;
            foreach (Mobile mobile in World.MobilesInRange(Config.GetInt("LTRange")))
            {
                if (FriendsAgent.IsFriend(mobile) || PacketHandlers.Party.Contains(mobile.Serial) || mobile.Serial == World.Player.Serial || Array.IndexOf(notoriety, mobile.Notoriety) == -1 || mobile.Renamable)
                    continue;
                if (mobile.Notoriety == 6)
                {
                    if (mobile.Body == 0x0009 && mobile.Hue == 0x0000) // daemon
                        continue;

                    if (mobile.Body == 0x02F4) // picovina golema
                        continue;

                    if (mobile.Body == 0x0033 && mobile.Hue == 0x4001 &&
                        (mobile.Name.StartsWith("a nature's fury") ||
                         (mobile.Name.StartsWith("a fire fury") && !PositionCheck.InFire)))
                        continue;

                    if (mobile.Body == 0x02EC && mobile.Hue == 0x4001) // wraith form
                        continue;

                    if (mobile.Body == 0x000E && mobile.Hue == 0x0000) // mental z krumpu
                        continue;

                    if (mobile.Body == 0x008A && mobile.Hue == 0x4001) // ork z talismanu
                        continue;
                }

                if (result == null || Utility.DistanceSqrt(World.Player.Position, mobile.Position) < Utility.DistanceSqrt(World.Player.Position, result.Position))
                    result = mobile;
            }
            return result;
        }
    }
}