using Assistant;
using Assistant.Macros;
using RazorEx.Addons;
using System;
using System.Linq;

namespace RazorEx.Skills
{
    public static class FishingCollector
    {
        private static bool IsFish(ItemID id) { return (id >= 0x3AF9 && id <= 0x3B15 && id != 0x3B10 && id != 0x3B0C) || id == 0x0C93 || id == 0x0DD7; }
        public static void OnInit()
        {
            MacroEx macro = new MacroEx();
            macro.Insert(-1, new GumpWait());
            macro.Insert(-1, new Clean());
            Command.Register("sud", args => MacroManager.HotKeyPlay(macro));
        }

        private class Clean : MacroAction
        {
            public override bool Perform()
            {
                Item barrel = WorldEx.FindItemG(0x0E77, 0x0847, i => i.DistanceTo(World.Player) < 3);
                if (barrel == null)
                {
                    WorldEx.SendMessage("Sud nenalezen.");
                    Parent.Stop();
                }
                foreach (Item item in World.Player.Backpack.FindItems(i => IsFish(i.ItemID)))
                    if (!DragDropManager.HasDragFor(item.Serial))
                        DragDrop.Move(item, barrel);
                return true;
            }
        }

        private class GumpWait : WaitForGumpAction
        {
            private int current = 3;

            public GumpWait() { m_Timeout = TimeSpan.FromSeconds(30); }
            public override bool Perform()
            {
                if (World.Player.Backpack.FindItems(i => IsFish(i.ItemID)).Count() > 10)
                    return true;
                ClientCommunication.SendToClient(new CloseGump(World.Player.CurrentGumpI));
                ClientCommunication.SendToServer(new GumpResponse(World.Player.CurrentGumpS, World.Player.CurrentGumpI, current, new int[0], new GumpTextEntry[0]));
                World.Player.HasGump = false;
                if (current == 21)
                    current = 23;
                else if (current == 25)
                    current = 3;
                else
                    current++;
                return !PerformWait();
            }
        }
    }
}