using Assistant;
using System;

namespace RazorEx.Addons
{
    public static class Evasion
    {
        private class EvasionTimer : Timer
        {
            private int i;
            public EvasionTimer() : base(TimeSpan.Zero, TimeSpan.FromSeconds(1)) { }
            protected override void OnTick()
            {
                if (World.Player.IsGhost || !World.Player.Visible)
                {
                    WorldEx.SendToClient(new ToggleMove(403, false));
                    Stop();
                }
                else if (!enabled || i++ > 5)
                {
                    WorldEx.SendToServer(new CastSpellFromMacro(403));
                    i = 0;
                }
            }
        }

        private static bool enabled;
        private static readonly EvasionTimer timer = new EvasionTimer();
        public static void OnInit()
        {
            Core.AddHotkey("Evasion", OnCommand);
            PacketHandler.RegisterServerToClientViewer(0xBF, OnServer);
        }

        private static void OnServer(PacketReader p, PacketHandlerEventArgs args)
        {
            if (p.ReadInt16() == 0x25 && p.ReadInt16() == 403)
                enabled = p.ReadBoolean();
        }

        private static void OnCommand()
        {
            if (timer.Running)
            {
                WorldEx.SendToClient(new ToggleMove(403, false));
                timer.Stop();
            }
            else
                timer.Start();
        }
    }
}