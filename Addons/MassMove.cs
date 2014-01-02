using Assistant;
using System;
using System.Linq;

namespace RazorEx.Addons
{
    public static class MassMove
    {
        public static void OnInit()
        {
            Command.Register("move", args => Targeting.OneTimeTarget((l, s, p, g) => Timer.DelayedCallback(TimeSpan.FromMilliseconds(500), () => Targeting.OneTimeTarget((l2, s2, p2, g2) => Move(s, s2, false))).Start()));
            Command.Register("movec", args => Targeting.OneTimeTarget((l, s, p, g) => Timer.DelayedCallback(TimeSpan.FromMilliseconds(500), () => Targeting.OneTimeTarget((l2, s2, p2, g2) => Move(s, s2, true))).Start()));
            Command.Register("movee", args => Targeting.OneTimeTarget((l, s, p, g) => Timer.DelayedCallback(TimeSpan.FromMilliseconds(500), () => Targeting.OneTimeTarget((l2, s2, p2, g2) => MoveE(s, s2, true))).Start()));
            Command.Register("movene", args => Targeting.OneTimeTarget((l, s, p, g) => Timer.DelayedCallback(TimeSpan.FromMilliseconds(500), () => Targeting.OneTimeTarget((l2, s2, p2, g2) => MoveE(s, s2, false))).Start()));
        }

        private static void Move(Serial source, Serial target, bool color)
        {
            Item sItem = World.FindItem(source);
            Item tItem = World.FindItem(target);
            if (sItem == null || tItem == null || !tItem.IsContainer)
                return;
            Item container = sItem.Container as Item;
            if (container != null)
                foreach (Item item in container.FindItems(sItem.ItemID, i => !color || i.Hue == sItem.Hue, recurse: false))
                    DragDrop.Move(item, tItem);
        }

        private static void MoveE(Serial source, Serial target, bool exceptional)
        {
            Item sItem = World.FindItem(source);
            Item tItem = World.FindItem(target);
            if (sItem == null || tItem == null || !tItem.IsContainer)
                return;
            Item container = sItem.Container as Item;
            if (container != null)
                foreach (Item item in container.FindItems(sItem.ItemID, i => exceptional ? IsExcept(i) : !IsExcept(i), recurse: false))
                    DragDrop.Move(item, tItem);
        }

        private static bool IsExcept(Item item)
        { return item.ObjPropList.m_Content.Cast<ObjectPropertyList.OPLEntry>().Any(e => except.Contains(e.Number)); }

        private static readonly int[] except = { 1060636, 1053100, 1050040, 1063484 };
    }
}