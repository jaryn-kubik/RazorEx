using Assistant;
using Assistant.Macros;
using RazorEx.Fixes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RazorEx.Addons
{
    public static class Other
    {
        private static readonly MacroEx macro = new MacroEx();
        private static readonly List<Serial> list = new List<Serial>();
        private static readonly Timer timer = Timer.DelayedCallback(TimeSpan.FromSeconds(1), OnTimer);
        private static Item door;

        public static void OnInit()
        {
            Core.AddHotkey("Honor", Honor);
            Core.AddHotkey("Honor/Attack closest non-friendly", HonorAttack);
            Core.AddHotkey("Attack closest non-friendly", Attack);
            Core.AddHotkey("Grab", () => new SpeechAction(MessageType.Regular, World.Player.SpeechHue, 3, null, null, ".grab").Perform());
            Command.Register("key", args => UseKey());
            Core.AddHotkey("UseKey", UseKey);
            PacketHandler.RegisterServerToClientViewer(0x6C, OnTarget);
            PacketHandler.RegisterServerToClientViewer(0x2C, OnDeath);

            Core.AddHotkey("Drink LifeSaving Potion", Drink);
            Core.AddHotkey("Use AntiDmg Ball", () => UseItem(0x0E73, 0x0629));
            Core.AddHotkey("Use AntiSpell Ball", () => UseItem(0x0E73, 0x0702));
            Core.AddHotkey("Use Smoke Bomb", () => UseItem(0x2808, 0));
            Command.Register("blast", FireBlast);
        }

        private static void Honor() { WorldEx.SendToServer(new InvokeVirtue(0x31)); }
        private static void HonorAttack()
        {
            TargetingEx.ClosestTarget(3, 4, 5, 6);
            Targeting.CancelTarget();
            Targeting.LastTarget(true);
            Honor();
            BloodOath.AttackLast();
        }

        private static void Attack()
        {
            TargetingEx.ClosestTarget(3, 4, 5, 6);
            BloodOath.AttackLast();
        }

        private static void Drink()
        {
            Item item = !PositionCheck.InKhaldun && !PositionCheck.InFire && !PositionCheck.InWisp ?
                        World.Player.Backpack.FindItem(0x0F06, 0x000C) :
                        WorldEx.FindItemG(0x37B9, 0x098F, i => i.DistanceTo(World.Player) <= GetOrbDistance() + 1) ??
                        WorldEx.FindItemG(0x3728, 0x0A58, i => i.DistanceTo(World.Player) <= GetOrbDistance() + 1) ??
                        World.Player.Backpack.FindItem((ItemID)(World.Player.Poisoned ? 0x0F07 : 0x0F0C), 0x0000);

            if (item != null)
                WorldEx.SendToServer(new DoubleClick(item.Serial));
        }

        private static byte GetOrbDistance()
        {
            Item item = World.Player.GetItemOnLayer(Layer.Unused_x9);
            if (item != null)
            {
                ArrayList content = item.ObjPropList.m_Content;
                if (content.Count > 4 && ((ObjectPropertyList.OPLEntry)content[0]).Args.Contains("Khal Ankur Amulet"))
                {
                    string args = ((ObjectPropertyList.OPLEntry)content[content.Count - 4]).Args;
                    return byte.Parse(args.Split('\t')[1][1].ToString());
                }
            }
            return 0;
        }

        private static void UseItem(ItemID itemID, ushort hue)
        {
            Item item = World.Player.Backpack.FindItem(itemID, hue);
            if (item != null)
                WorldEx.SendToServer(new DoubleClick(item.Serial));
        }

        private static void OnDeath(PacketReader p, PacketHandlerEventArgs args) { MacroManager.HotKeyPlay(macro); }
        private static void OnTimer() { door = null; }
        private static void OnTarget(PacketReader p, PacketHandlerEventArgs args)
        {
            if (door != null)
            {
                WorldEx.SendToServer(new TargetResponse(new TargetInfo { Serial = door.Serial, X = door.Position.X, Y = door.Position.Y, Z = door.Position.Z, Gfx = door.ItemID }));
                WorldEx.SendToServer(new DoubleClick(door.Serial));
                args.Block = true;
            }
            door = null;
        }

        private static bool IsKey(Item i) { return i.ItemID == 0x1013 || i.ItemID == 0x1B12 || i.ItemID == 0x14FD; }
        private static bool IsLockpick(Item i) { return i.ItemID == 0x14FD && i.Hue == 0x0000; }
        private static bool IsSwitch(Item i) { return i.ItemID >= 0x108C && i.ItemID <= 0x1095; }
        private static void UseKey()
        {
            lock (list)
            {
                Item item = World.Player.Backpack.FindItem(i => IsKey(i) && !IsLockpick(i) && !list.Contains(i.Serial));
                if (item == null && list.Count > 0)
                {
                    list.Clear();
                    item = World.Player.Backpack.FindItem(i => IsKey(i) && !IsLockpick(i) && !list.Contains(i.Serial));
                }
                if (item == null)
                    item = World.Player.Backpack.FindItem(i => IsKey(i) && !list.Contains(i.Serial));

                if (item != null)
                {
                    door = WorldEx.FindItem(i => i.IsDoor && i.DistanceTo(World.Player) <= 1);
                    if (door != null)
                        timer.Start();
                    else
                    {
                        Item item2 = WorldEx.FindItemsG(i => IsSwitch(i) && i.DistanceTo(World.Player) < 3)
                            .OrderBy(i => Utility.DistanceSqrt(i.Position, World.Player.Position))
                            .FirstOrDefault();
                        if (item2 != null)
                            item = item2;
                    }

                    if (!IsSwitch(item))
                    {
                        list.Add(item.Serial);
                        list.RemoveAll(i => !World.Items.ContainsKey(i));
                    }

                    WorldEx.SendToServer(new DoubleClick(item.Serial));
                }
                else
                {
                    item = WorldEx.FindItemsG(i => IsSwitch(i) && i.DistanceTo(World.Player) < 3)
                        .OrderBy(i => Utility.DistanceSqrt(i.Position, World.Player.Position))
                        .FirstOrDefault();

                    if (item != null)
                        WorldEx.SendToServer(new DoubleClick(item.Serial));
                    else
                        WorldEx.SendMessage("No key found.");
                }
            }
        }

        private static void FireBlast(string[] args)
        {
            Item spellBook = World.Player.Backpack.FindItem(0x2253, 0x0872);
            if (spellBook != null)
            {
                Targeting.CancelTarget();
                new ContextMenuAction(spellBook, 0, 0).Perform();
            }
            else
                WorldEx.SendMessage("Arcanum Ignis not found.");
        }
    }
}
