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

        public static void OnInit() { PacketHandler.RegisterServerToClientViewer(0x1C, OnAsciiMessage); }
        private static void OnAsciiMessage(PacketReader p, PacketHandlerEventArgs args)
        {
            p.Position = 0x2C;
            string message = p.ReadStringSafe();
            if (message == "Banish!" || message == "Decimate!" || message == "Pool of Poison!")
            {
                message = string.Format("!!!{0}!!", message);
                WorldEx.OverHeadMessage(message, 0x0017);
                if (message == "!!!Banish!!!")
                    timer.Start();
            }
        }
    }
}