using Assistant;
using Assistant.Macros;

namespace RazorEx.Addons
{
    public static class Summoning
    {
        public static void OnInit()
        {
            MacroEx mirroring = new MacroEx();
            mirroring.Insert(-1, new WaitForSlot(2));
            mirroring.Insert(-1, new MacroCastSpellAction(508));
            mirroring.Insert(-1, new PauseAction(500));

            MacroEx naturesking = new MacroEx();
            naturesking.Insert(-1, new WaitForSlot(1));
            naturesking.Insert(-1, new MacroCastSpellAction(606));
            naturesking.Insert(-1, new TargetSelf());

            Core.AddHotkey("Naturesking", () => MacroManager.HotKeyPlay(naturesking));
            Core.AddHotkey("Mirroring", () =>
                                            {
                                                if (World.Player != null && World.Player.GetItemOnLayer(Layer.Mount) != null)
                                                    WorldEx.SendToServer(new DoubleClick(World.Player.Serial));
                                                MacroManager.HotKeyPlay(mirroring);
                                            });
        }

        private class WaitForSlot : MacroWaitAction
        {
            private readonly int value;
            public WaitForSlot(int value) { this.value = value; }

            public override bool Perform() { return !PerformWait(); }
            public override bool PerformWait() { return World.Player.FollowersMax - World.Player.Followers < value; }
        }
    }
}