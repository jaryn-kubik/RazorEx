using Assistant;
using System;

namespace RazorEx.Fixes
{
    public static class BardSkills
    {
        public static void OnInit() { Event.LocalizedMessage += Event_LocalizedMessage; }

        private static bool IsInstrument(Item item)
        {
            return item.ItemID == 0x2805 || item.ItemID == 0x2807 || item.ItemID == 0x315C ||
                   (item.ItemID >= 0x0EB1 && item.ItemID <= 0x0EB4) ||
                   (item.ItemID >= 0x0E9C && item.ItemID <= 0x0E9E);
        }

        private static bool? Event_LocalizedMessage(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, int num, string name, string args)
        {
            if (num != 500617)
                return null;

            Item item = World.Player.Backpack.FindItem(0x2AFA) ?? World.Player.Backpack.FindItem(IsInstrument);
            if (item != null)
            {
                Targeting.QueueTarget queue = Targeting.QueuedTarget;
                Targeting.CancelTarget();
                Targeting.QueuedTarget = () =>
                {
                    Targeting.Target(item);
                    Timer.DelayedCallback(TimeSpan.FromMilliseconds(10), () => Targeting.QueuedTarget = queue).Start();
                    return true;
                };
            }
            return null;
        }
    }
}