using Assistant;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RazorEx
{
    public static class WorldEx
    {
        public static UOEntity GetEntity(Serial serial) { return World.Items[serial] as UOEntity ?? World.Mobiles[serial] as UOEntity; }
        public static Item FindItem(Func<Item, bool> predicate) { return World.Items.Values.Cast<Item>().FirstOrDefault(predicate); }
        public static Mobile FindMobile(Func<Mobile, bool> predicate) { return World.Mobiles.Values.Cast<Mobile>().FirstOrDefault(predicate); }
        public static IEnumerable<Item> FindItems(Func<Item, bool> predicate) { return World.Items.Values.Cast<Item>().Where(predicate); }

        public static Item FindItem(this Item container, ItemID itemID, Predicate<Item> predicate = null, bool recurse = true, bool include = true)
        { return container.FindItems(itemID, predicate, recurse, include).FirstOrDefault(); }

        public static Item FindItem(this Item container, ItemID itemID, ushort hue, Predicate<Item> predicate = null, bool recurse = true, bool include = true)
        { return container.FindItems(itemID, hue, predicate, recurse, include).FirstOrDefault(); }

        public static Item FindItem(this Item container, Predicate<Item> predicate, bool recurse = true, bool include = true)
        { return container.FindItems(predicate, recurse, include).FirstOrDefault(); }

        public static IEnumerable<Item> FindItems(this Item container, ItemID itemID, Predicate<Item> predicate = null, bool recurse = true, bool include = true)
        { return container.FindItems(i => i.ItemID == itemID && (predicate == null || predicate(i)), recurse, include); }

        public static IEnumerable<Item> FindItems(this Item container, ItemID itemID, ushort hue, Predicate<Item> predicate = null, bool recurse = true, bool include = true)
        { return container.FindItems(i => i.ItemID == itemID && i.Hue == hue && (predicate == null || predicate(i)), recurse, include); }

        public static IEnumerable<Item> FindItems(this Item container, Predicate<Item> predicate, bool recurse = true, bool include = true)
        {
            foreach (Item item in container.Contains)
            {
                if (include && predicate(item))
                    yield return item;
                if (recurse)
                    foreach (Item i in item.FindItems(predicate))
                        yield return i;
            }
        }

        public static Item FindItemG(ItemID itemID, ushort hue, Predicate<Item> predicate = null)
        { return FindItemsG(itemID, hue, predicate).FirstOrDefault(); }

        public static Item FindItemG(Predicate<Item> predicate)
        { return FindItemsG(predicate).FirstOrDefault(); }

        public static IEnumerable<Item> FindItemsG(ItemID itemID, Predicate<Item> predicate = null)
        { return FindItemsG(i => i.ItemID == itemID && (predicate == null || predicate(i))); }

        public static IEnumerable<Item> FindItemsG(ItemID itemID, ushort hue, Predicate<Item> predicate = null)
        { return FindItemsG(i => i.ItemID == itemID && i.Hue == hue && (predicate == null || predicate(i))); }

        public static IEnumerable<Item> FindItemsG(Predicate<Item> predicate)
        { return World.Items.Values.Cast<Item>().Where(item => item.OnGround && predicate(item)); }

        public static void OverHeadMessage(string msg) { InternalMessage(World.Player, msg, proc: false); }
        public static void OverHeadMessage(string msg, int hue) { InternalMessage(World.Player, msg, hue, false); }
        public static void OverHeadMessage(string msg, Item item) { InternalMessage(item, msg, proc: false); }
        public static void SendMessage(string msg) { InternalMessage(null, msg); }
        private static void InternalMessage(UOEntity entity, string msg, int hue = 0x0017, bool proc = true)
        {
            Serial serial = entity != null ? entity.Serial : Serial.MinusOne;
            int graphic;
            Item item = entity as Item;
            Mobile mobile = entity as Mobile;
            if (item != null)
                graphic = item.ItemID;
            else if (mobile != null)
                graphic = mobile.Body;
            else
                graphic = -1;
            SendToClient(new UnicodeMessage(serial, graphic, MessageType.Regular, hue, 3, "CSY", "RazorEx", msg), proc);
        }

        public static void SendToClient(Packet packet, bool processHandlers = false)
        {
            if (processHandlers)
                PacketHandler.OnServerPacket(packet.PacketID, new PacketReader(packet.Compile(), packet.m_DynSize), packet);
            ClientCommunication.SendToClient(packet);
        }

        public static void SendToServer(Packet packet, bool processHandlers = false)
        {

            if (processHandlers)
                PacketHandler.OnClientPacket(packet.PacketID, new PacketReader(packet.Compile(), packet.m_DynSize), packet);
            ClientCommunication.SendToServer(packet);
        }

        public static bool IsShield(this Item item) { return (item.ItemID >= 0x1B72 && item.ItemID <= 0x1B7B) || (item.ItemID >= 0x1BC3 && item.ItemID <= 0x1BC5) || item.ItemID == 0x2B01 || item.ItemID == 0x0A0F; }
        public static bool IsQuiver(this Item item) { return item.ItemID == 0x2B02 || item.ItemID == 0x2B03 || item.ItemID == 0x2FB7 || item.ItemID == 0x3171; }
        public static bool IsMy(this Item item) { return item.RootContainer == World.Player && !item.IsInBank; }
        public static bool IsBlessed(this Item item) { return item.ObjPropList.m_Content.OfType<ObjectPropertyList.OPLEntry>().Any(p => p.Number == 1038021); }
        public static bool IsElementalBody(this Item item)
        { return item.ItemID == 0x2006 && item.Hue != 0x0835 && ((item.Amount >= 0x000D && item.Amount <= 0x0010) || (item.Amount >= 0x006B && item.Amount <= 0x0071) || (item.Amount >= 0x009E && item.Amount <= 0x00A3) || item.Amount == 0x00A6 || item.Amount == 0x02F0 || item.Amount == 0x0083); }
        public static bool IsPlayer(this Mobile mobile)
        { return mobile.Body == 0x0190 || mobile.Body == 0x0191 || mobile.Body == 0x025D || mobile.Body == 0x025E; }

        public static string GetName(this Mobile mobile)
        {
            string name = mobile.Name.Split(new[] { ",", "the", "from", "[" }, StringSplitOptions.None)[0];
            return name.Replace("Lord", string.Empty).Replace("Lady", string.Empty).Trim();
        }
    }
}