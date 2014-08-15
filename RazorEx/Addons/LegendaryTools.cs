using Assistant;

namespace RazorEx.Addons
{
    public static class LegendaryTools
    {
        public static void OnInit()
        {
            PacketHandler.RegisterServerToClientViewer(0x1A, WorldItem);
            ConfigAgent.AddItem<uint>(0, "LegendaryPickaxe");
            ConfigAgent.AddItem<uint>(0, "LegendaryHatchet");
            Core.AddHotkey("LegendaryTool", OnHotkey);
        }

        private static void OnHotkey()
        {
            if (World.Player.GetItemOnLayer(Layer.Mount) != null || !World.Player.IsPlayer())
                return;

            Item item = WorldEx.FindItem(i => i.IsElementalBody() || i.IsSomeTreeishBody() && i.DistanceTo(World.Player) < 3);
            if (item != null)
                DoIt(item);
        }

        private static void WorldItem(PacketReader p, PacketHandlerEventArgs args)
        {
            Item item = World.FindItem(p.ReadUInt32() & 0x7FFFFFFF);
            if (item != null && item.DistanceTo(World.Player) < 3 && World.Player.GetItemOnLayer(Layer.Mount) == null && World.Player.IsPlayer())
                DoIt(item);
        }

        private static void DoIt(Item item)
        {
            Item tool = null;
            if (item.IsElementalBody())
                tool = World.FindItem(ConfigEx.GetElement<uint>(0, "LegendaryPickaxe"));
            else if (item.IsSomeTreeishBody())
                tool = World.FindItem(ConfigEx.GetElement<uint>(0, "LegendaryHatchet"));

            if (tool != null)
            {
                Targeting.CancelTarget();
                Targeting.QueuedTarget = () =>
                {
                    Targeting.Target(item);
                    return true;
                };
                WorldEx.SendToServer(new DoubleClick(tool.Serial));
            }
        }

        private static bool IsElementalBody(this Item item)
        {
            return item.ItemID == 0x2006 && item.Hue != 0x0835 &&
                ((item.Amount >= 0x000D && item.Amount <= 0x0010) ||
                (item.Amount >= 0x006B && item.Amount <= 0x0071) ||
                (item.Amount >= 0x009E && item.Amount <= 0x00A3) ||
                item.Amount == 0x00A6 || item.Amount == 0x02F0 || item.Amount == 0x0083);
        }

        private static bool IsSomeTreeishBody(this Item item)
        {
            return item.ItemID == 0x2006 && item.Hue != 0x0835 &&
                (item.Amount == 47 || item.Amount == 301 || item.Amount == 8 || item.Amount == 66);
        }
    }
}