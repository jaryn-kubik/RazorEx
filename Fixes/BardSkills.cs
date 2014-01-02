using Assistant;
using System;

namespace RazorEx.Fixes
{
    public static class BardSkills
    {
        public static void OnInit()
        {
            ConfigAgent.AddItem<uint>(0, "Instrument");
            Event.LocalizedMessage += Event_LocalizedMessage;
        }

        private static void Event_LocalizedMessage(Serial serial, int msg, string args)
        {
            if (msg != 500617)
                return;

            Item item = World.FindItem(ConfigEx.GetElement<uint>(0, "Instrument"));
            if (item != null && item.IsMy())
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
        }
    }
}