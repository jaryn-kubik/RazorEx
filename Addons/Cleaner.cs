using System.Xml.Linq;
using Assistant;
using RazorEx.Fixes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RazorEx.Addons
{
    public static class Cleaner
    {
        public static void OnInit()
        {
            CleanerAgent agent = new CleanerAgent();
            Agent.Add(agent);
            Command.Register("clean", args => Clean());
            Command.Register("loot", args => Loot());
            Core.AddHotkey("Cleaner", Clean);
            Core.AddHotkey("Loot", Loot);
            agent.Add<uint>(0, "GPBag");
            agent.Add<byte>(30, "CleanerTalismans");
            agent.Add(true, "CleanerResources");
            agent.Add(true, "CleanerFood");
            agent.Add(true, "CleanerRunics");
            agent.Add(false, "CleanerSOS");
            agent.Add(false, "LootBigDiamonds");
        }

        public static void Clean() { Loot(World.Player.Backpack); }
        private static void Loot() { Targeting.OneTimeTarget((l, s, p, g) => Loot(World.FindItem(s))); }
        private static void Loot(Item bag)
        {
            if (bag == null)
                return;
            Item backpack;
            if (bag != World.Player.Backpack)
            {
                backpack = World.FindItem(LootBag.Bag);
                Clean(backpack, bag.FindItems(i => Looting.Items.Any(l => i.ItemID == l.Graphic && (i.Hue == l.Color || l.Color == 0xFFFF))));
                Clean(backpack, bag.FindItems(0x0E34));//kousky map
                Clean(backpack, bag.FindItems(0x26B8));//dusty
                Clean(backpack, bag.FindItems(0x0E76));//bagly
                Clean(backpack, bag.FindItems(0x14F0));//pska
                Clean(backpack, bag.FindItems(0x0FAB));//barvy
                Clean(backpack, bag.FindItems(0x0DCA, 0x0481));//bila sit
                Clean(backpack, bag.FindItems(0x226E, 0x0065));//kousky sos
                Clean(backpack, bag.FindItems(0x0F8B, 0x043D));//moony
                Clean(backpack, bag.FindItems(0x3155, 0x048D));//slzy
                Clean(backpack, bag.FindItems(0x1BC4, 0x0482));//holy shield
                Clean(backpack, bag.FindItems(0x0F61, 0x0482));//holy sword
                Clean(backpack, bag.FindItems(0x0F87, 0x0000));//eye of newt
                Clean(backpack, bag.FindItems(0x1422, 0x0481));//token
                Clean(backpack, bag.FindItems(0x1006, 0x0973));//durab picovina
                Clean(backpack, bag.FindItems(0x1869, 0x04AE));//sb
                Clean(backpack, bag.FindItems(0x0F0E, 0x099F));//redidlo
                Clean(backpack, bag.FindItems(0x0EF0, 0x09F0));//cechy
                Clean(backpack, bag.FindItems(0x097C, 0x09E2));//fragmenty
                Clean(backpack, bag.FindItems(0x26B4, 0x0A11));//charmy
                Clean(backpack, bag.FindItems(0x1877, i => i.Hue != 0));//draty
                Clean(backpack, bag.FindItems(0x1F3D, i => i.Hue != 0));//svitky
                Clean(backpack, bag.FindItems(IsLevel));
                Clean(backpack, bag.FindItems(i => IsShitRunic(i) == null));

                if (ConfigEx.GetElement(false, "LootBigDiamonds"))
                    Clean(backpack, bag.FindItems(IsBigDiamonds));

                foreach (Item item in bag.FindItems(IsSOSCrap))
                    WorldEx.SendToClient(new RemoveObject(item.Serial));
            }

            backpack = World.Player.Backpack;
            Clean(backpack.FindItem(0x0E3B, 0x0489), bag.FindItems(i => Array.IndexOf(regs, i.ItemID) != -1)); //reagents
            Clean(backpack.FindItem(0x0E3B, 0x0489), bag.FindItems(0x0E76, i => i.Contains.Count == 0 && !i.IsBlessed())); //empty bags
            Clean(backpack.FindItem(0x0E79, 0x001A), bag.FindItems(0x0F0E, 0x0000)); //empty bottles
            Clean(backpack.FindItem(0x0E79, 0x001A), bag.FindItems(0x0C64, 0x0490)); //zoogi vejce
            Clean(backpack.FindItem(0x0E79, 0x001A), bag.FindItems(0x09EC, 0x002C)); //medovina
            Clean(backpack.FindItem(0x0E79, 0x001A), bag.FindItems(0x2808, 0)); //smoke bomb
            Clean(backpack.FindItem(0x0E79, 0x001A), bag.FindItems(0x0F0B, 0x0367)); //pet res
            Clean(backpack.FindItem(0x0E79, 0x001A), bag.FindItems(0x0F0B, 0x0774)); //repair
            Clean(backpack.FindItem(0x2252, 0x08AC), bag.FindItems(0x0EF0, 0x0000)); //silver
            Item quiver = World.Player.GetItemOnLayer(Layer.MiddleTorso);
            if (quiver == null || (!quiver.IsQuiver() && !IsQuiverSash(quiver)))
                quiver = backpack.FindItem(i => i.IsQuiver() || IsQuiverSash(i));
            Clean(quiver, bag.FindItems(i => i.ItemID == 0x0F3F || i.ItemID == 0x1BFB)); //ammo

            Item gpBag = World.FindItem(ConfigEx.GetElement<uint>(0, "GPBag"));
            Clean(gpBag, bag.FindItems(0x0EED, 0x0000, i => i.Container != gpBag)); //gp

            Item whiteBall = backpack.FindItem(0x0E73, 0x0702, recurse: false);
            if (whiteBall != null)
                Clean(backpack, bag.FindItems(0x0E73, 0x0702, i => i != whiteBall));
            else
                Clean(backpack.FindItem(0x0E79, 0x001A), bag.FindItems(0x0E73, 0x0702));

            Item brownBall = backpack.FindItem(0x0E73, 0x0629, recurse: false);
            if (brownBall != null)
                Clean(backpack, bag.FindItems(0x0E73, 0x0629, i => i != brownBall));
            else
                Clean(backpack.FindItem(0x0E79, 0x001A), bag.FindItems(0x0E73, 0x0629));

            Item medovina = backpack.FindItem(0x09EC, 0x001A, recurse: false);
            if (medovina != null)
                Clean(backpack, bag.FindItems(0x09EC, 0x001A, i => i != medovina));
            else
                Clean(backpack.FindItem(0x0E79, 0x001A), bag.FindItems(0x09EC, 0x001A));

            Clean(backpack.FindItem(0x0E79, 0x0488), bag.FindItems(i => Array.IndexOf(petPlant, i.ItemID) != -1)); //petplant
            Clean(backpack.FindItem(0x24D7, 0x0556), bag.FindItems(i => (IsFish(i.ItemID) && !IsBigFish(i)) || (i.ItemID == 0x0DCA && i.Hue == 0x08A0)));//fishbag + site
            Clean(backpack.FindItem(0x09B0, 0x048E), bag.FindItems(IsShitResource));//food+resources

            Clean(backpack.FindItem(0x2252, 0x0492), bag.FindItems(0x14EC, 0));//mapky
            if (!ConfigEx.GetElement(false, "CleanerSOS"))
                Clean(backpack.FindItem(0x22C5, IsSOSBook), bag.FindItems(0x14ED, recurse: false)); //sosky vsechny
            else
            {
                Clean(backpack.FindItem(0x09B0, 0x048E), bag.FindItems(0x14ED, 0, recurse: false)); //trash obyc
                Clean(backpack.FindItem(0x22C5, IsSOSBook), bag.FindItems(0x14ED, i => i.Hue != 0, recurse: false)); //uklid ostatni
            }

            foreach (Item item in bag.FindItems(0x099F))//flasky se soskama
                ActionQueue.DoubleClick(true, item.Serial);

            foreach (Item item in backpack.FindItems(0x0ECF, 0)) //kosti
            {
                Clean(backpack.FindItem(0x09B0, 0x048E), item.FindItems(0x1EFD, 0x04D1));
                Clean(backpack.FindItem(0x09B0, 0x048E), item.FindItems(0x170D, 0x0000));
                Clean(backpack.FindItem(0x09B0, 0x048E), item.FindItems(0x1539, 0x045E));
                Clean(backpack.FindItem(0x09B0, 0x048E), item.FindItems(0x171B, 0x0006));
                Clean(backpack.FindItem(0x09B0, 0x048E), item.FindItems(IsSextant));
            }
        }

        private static void Clean(Item container, IEnumerable<Item> items)
        {
            if (container != null)
                foreach (Item item in items.Where(i => i.Container != container))
                    DragDrop.Move(item, container);
        }

        private static bool IsQuiverSash(Item item)
        {
            return item.ItemID == 0x1541 &&
                   item.ObjPropList.m_Content.Cast<ObjectPropertyList.OPLEntry>()
                       .Any(e => e.Args != null && e.Args.Contains("Arrows: "));
        }

        private static bool IsSextant(Item item)
        {
            return item.ItemID == 0x1058 && item.Hue == 0 &&
                   item.ObjPropList.m_Content.Cast<ObjectPropertyList.OPLEntry>()
                       .Any(e => e.Number == 1024184);
        }

        private static bool IsLevel(Item item)
        {
            return item.ObjPropList.m_Content.Cast<ObjectPropertyList.OPLEntry>()
                       .Any(e => e.Args != null && e.Args.Contains("Level\t1"));
        }

        private static bool IsBigDiamonds(Item item) { return item.ItemID >= 0x3192 && item.ItemID <= 0x3199 && item.Hue == 0; }
        private static bool IsSOSCrap(Item item)
        {
            if (item.ItemID >= 0x1F2D && item.ItemID <= 0x1F72 && item.Hue == 0)//svitky
                return true;
            if (item.ItemID >= 0x0F0F && item.ItemID <= 0x0F30 && item.Hue == 0)//small diamonds
                return true;
            return !ConfigEx.GetElement(false, "LootBigDiamonds") && IsBigDiamonds(item);
        }

        private static bool IsShitResource(Item item)
        {
            if (ConfigEx.GetElement(true, "CleanerResources") &&
                (Array.IndexOf(resources, item.ItemID) != -1 ||
                (item.ItemID == 0x26B4 && item.Hue != 0x08B0 && item.Hue != 0x0A11) ||
                (item.ItemID == 0x1BF2 && item.Hue != 0x0578) ||
                (item.ItemID == 0x19B9 && item.Hue != 0x0A54 && item.Hue != 0x047E)))//chromit a honor
                return true;
            if (ConfigEx.GetElement(true, "CleanerFood") &&
                (IsFood(item.ItemID) && (item.ItemID != 0x09B9 || item.Hue != 0x09B7) && (item.ItemID != 0x09C1 || item.Hue != 0x0A3E)))
                return true;
            if (ConfigEx.GetElement(true, "CleanerRunics") && IsShitRunic(item) == true)
                return true;

            byte min = ConfigEx.GetElement<byte>(30, "CleanerTalismans");
            if (item.Hue != 0 || item.ItemID < 0x2F58 || item.ItemID > 0x2F5B || min == 0)
                return false;
            foreach (ObjectPropertyList.OPLEntry prop in item.ObjPropList.m_Content)
                if ((prop.Number == 1072394 || prop.Number == 1072395) && int.Parse(prop.Args.Split('\t')[1]) >= min)
                    return false;
            return true;
        }

        private static bool? IsShitRunic(Item item)
        {
            switch (item.ItemID)
            {
                case 0x0F9D://tailor
                    return item.Hue != 0x03DB ? (bool?)true : null;//demon
                case 0x1034://carp
                    return item.Hue != 0x04AB ? (bool?)true : null;//frost
                case 0x1022://bowcraft
                    return item.Hue != 0x04AB ? (bool?)true : null;//frost
                case 0x13E3://bs
                    return item.Hue != 0x0485 ? (bool?)true : null;//blood
            }
            return false;
        }

        private static bool IsSOSBook(Item item)
        {
            ArrayList content = item.ObjPropList.m_Content;
            return content.Count > 0 && ((ObjectPropertyList.OPLEntry)content[0]).Args == "Kniha na SOS zpravy";
        }

        private static bool IsBigFish(Item item) { return item.ItemID == 0x09CC && item.Hue == 0x0847; }
        private static bool IsFish(ItemID id)
        { return (id >= 0x3AF9 && id <= 0x3B15) || id == 0x0C93 || id == 0x0DD7 || (id >= 0x09CC && id <= 0x09CF); }

        private static bool IsFood(ItemID id)
        { return id != 0x097C && ((id >= 0x0976 && id <= 0x097E) || (id >= 0x09B5 && id <= 0x09C9) || (id >= 0x09D0 && id <= 0x09D3) || id == 0x09F1 || id == 0x09F2 || id == 0x0C66); }

        private static readonly ItemID[] petPlant = { 0x18EC, 0x18E5, 0x18DD };
        private static readonly ItemID[] resources = { 0x1BDD, 0x1BD7, 0x1BD4, 0x0F7E, 0x11EA, 0x26B7, 0x1079 };
        private static readonly ItemID[] regs = { 0x0F78, 0x0F7A, 0x0F7B, 0x0F7D, 0x0F84, 0x0F85, 0x0F86, 0x0F88, 0x0F8A, 0x0F8C, 0x0F8D, 0x0F8E, 0x0F8F };
    }

    public class CleanerAgent : ConfigAgent
    {
        public override string Name { get { return "Cleaner"; } }
        public void Add<T>(T defaultValue, params XName[] nodes) where T : IConvertible
        { items.Add(new CleanerItem(defaultValue, null, nodes)); }

        private class CleanerItem : ConfigItem
        {
            public CleanerItem(object defaultValue, Action<object> onChange, params XName[] nodes) : base(defaultValue, onChange, nodes) {}
            public override string ToString()
            {
                string str = base.ToString();
                return str.StartsWith("Cleaner") ? str.Substring(7) : str;
            }
        }
    }
}