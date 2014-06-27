using System;
using Assistant;
using Assistant.Macros;

namespace RazorEx.Addons
{
    public static class BoneCarving
    {
        public static void OnInit()
        {
            MacroEx neira = new MacroEx();
            neira.Insert(-1, new WaitForBone(GetNeiraBones));
            neira.Insert(-1, new DoubleClickExAction());
            neira.Insert(-1, new WaitForTargetAction(new[] { "", "1" }));
            neira.Insert(-1, new TargetBone(GetNeiraBones));
            neira.Insert(-1, new PauseAction(250));

            MacroEx df = new MacroEx();
            df.Insert(-1, new WaitForBone(GetDFBones));
            df.Insert(-1, new DoubleClickExAction());
            df.Insert(-1, new WaitForTargetAction(new[] { "", "1" }));
            df.Insert(-1, new TargetBone(GetDFBones));
            df.Insert(-1, new PauseAction(250));

            Core.AddHotkey("Neira", () => MacroManager.HotKeyPlay(neira));
            Core.AddHotkey("Dark Father", () => MacroManager.HotKeyPlay(df));
            ConfigAgent.AddItem<uint>(0, "BoneCarver");
        }

        private static Item GetNeiraBones() { return WorldEx.FindItem(i => i.ItemID == 0x0F7E && i.Hue == 0x0497 && i.DistanceTo(World.Player) < 3); }
        private static Item GetDFBones() { return WorldEx.FindItem(i => i.ItemID >= 0x0ECA && i.ItemID <= 0x0ED2 && i.DistanceTo(World.Player) < 3); }

        private class DoubleClickExAction : MacroAction
        {
            public override bool Perform()
            {
                Item item = World.FindItem(ConfigEx.GetElement<uint>(0, "BoneCarver"));
                if (item != null)
                {
                    Targeting.CancelTarget();
                    WorldEx.SendToServer(new DoubleClick(item.Serial));
                }
                return true;
            }
        }

        private class TargetBone : MacroAction
        {
            private readonly Func<Item> getBones;
            public TargetBone(Func<Item> getBones) { this.getBones = getBones; }
            public override bool Perform()
            {
                Item item = getBones();
                if (item != null)
                    Targeting.Target(item);
                return true;
            }
        }

        private class WaitForBone : MacroWaitAction
        {
            private readonly Func<Item> getBones;
            public WaitForBone(Func<Item> getBones) { this.getBones = getBones; }
            public override bool Perform() { return !PerformWait(); }
            public override bool PerformWait() { return getBones() == null; }
        }
    }
}
