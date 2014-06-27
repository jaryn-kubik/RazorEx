using System;
using Assistant;

namespace RazorEx.Addons
{
    public static class NPCAbilities
    {
        private class BanishTimer : Timer
        {
            public BanishTimer() : base(TimeSpan.Zero, TimeSpan.FromSeconds(1), 5) { }
            protected override void OnTick() { WorldEx.OverHeadMessage((5 - m_Index).ToString(), 0x0017); }
        }

        private static readonly BanishTimer timer = new BanishTimer();

        public static void OnInit() { Event.ASCIIMessage += Event_ASCIIMessage; }
        private static bool? Event_ASCIIMessage(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, string lang, string name, string msg)
        {
            if (msg == "Banish!" || msg == "Decimate!" || msg == "Pool of Poison!")
            {
                msg = string.Format("!!!{0}!!", msg);
                WorldEx.OverHeadMessage(msg, 0x0017);
                if (msg == "!!!Banish!!!")
                    timer.Start();
            }
            return null;
        }
    }
}