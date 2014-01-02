using System;
using Assistant;

namespace RazorEx.Addons
{
    public static class GiftOfLife
    {
        private static Timer timer;
        public static void OnInit()
        {
            BuffIcons.Added += BuffIcons_Added;
            BuffIcons.Removed += BuffIcons_Removed;
        }

        static void BuffIcons_Removed(BuffIcon buffID)
        {
            if (buffID == BuffIcon.GiftOfLife && timer != null)
            {
                timer.Stop();
                timer = null;
            }
        }

        private static void BuffIcons_Added(BuffIcon buffID, BuffInfo buff)
        {
            if (buffID == BuffIcon.GiftOfLife && buff.Duration > 15)
            {
                if (timer != null)
                    timer.Stop();
                timer = Timer.DelayedCallback(TimeSpan.FromSeconds(buff.Duration - 15), OnTimer);
                timer.Start();
            }
        }

        private static void OnTimer()
        {
            timer = null;
            WorldEx.OverHeadMessage("Gift of Life ends in 15 seconds!!!");
        }
    }
}