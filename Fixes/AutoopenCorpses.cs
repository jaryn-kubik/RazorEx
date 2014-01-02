using System.Collections.Generic;
using Assistant;

namespace RazorEx.Fixes
{
    public static class AutoopenCorpses
    {
        private static readonly List<Serial> list = new List<Serial>();
        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "AutoopenCorpses"); }
        private static void OnChange(bool autoopen)
        {
            if (autoopen)
                Event.PlayerMoved += Event_PlayerMoved;
            else
                Event.PlayerMoved -= Event_PlayerMoved;
        }

        private static void Event_PlayerMoved()
        {
            foreach (Item item in WorldEx.FindItems(i => i.ItemID == 0x2006 && i.DistanceTo(World.Player) < 3 && !list.Contains(i.Serial)))
            {
                list.Add(item.Serial);
                list.RemoveAll(i => !World.Items.ContainsKey(i));
                WorldEx.SendToServer(new DoubleClick(item.Serial));
            }
        }
    }
}
