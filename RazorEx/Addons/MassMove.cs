using Assistant;
using System;
using System.Linq;

namespace RazorEx.Addons
{
    public static class MassMove
    {
        private enum MoveType { IgnoreColor, UseColor, Except, NonExcept }

        public static void OnInit()
        {
            Command.Register("move", args => Move(args, MoveType.IgnoreColor));
            Command.Register("movec", args => Move(args, MoveType.UseColor));
            Command.Register("movee", args => Move(args, MoveType.Except));
            Command.Register("movene", args => Move(args, MoveType.NonExcept));
        }

        private static void Move(string[] args, MoveType type)
        {
            int count;
            if (args.Length < 1 || !int.TryParse(args[0], out count))
                count = int.MaxValue;
            Targeting.OneTimeTarget((l, s, p, g) => Timer.DelayedCallback(TimeSpan.FromMilliseconds(500), () => Targeting.OneTimeTarget((l2, s2, p2, g2) => Move(s, s2, type, count))).Start());
        }

        private static void Move(Serial source, Serial target, MoveType type, int count)
        {
            Item sItem = World.FindItem(source);
            Item tItem = World.FindItem(target);
            if (sItem == null || tItem == null || !tItem.IsContainer)
                return;
            Item container = sItem.Container as Item;
            if (container != null)
                foreach (Item item in container.FindItems(sItem.ItemID, i => Filter(i, sItem, type), false).Take(count))
                    DragDrop.Move(item, tItem);
        }

        private static bool Filter(Item item, Item source, MoveType type)
        {
            if (type == MoveType.UseColor)
                return item.Hue == source.Hue;
            if (type == MoveType.Except)
                return IsExcept(item);
            if (type == MoveType.NonExcept)
                return !IsExcept(item);
            return true;
        }

        private static bool IsExcept(Item item)
        { return item.ObjPropList.m_Content.Cast<ObjectPropertyList.OPLEntry>().Any(e => except.Contains(e.Number)); }

        private static readonly int[] except = { 1060636, 1053100, 1050040, 1063484 };
    }
}