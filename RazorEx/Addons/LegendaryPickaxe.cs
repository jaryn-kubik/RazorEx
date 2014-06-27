using Assistant;

namespace RazorEx.Addons
{
    public static class LegendaryPickaxe
    {
        public static void OnInit()
        {
            ConfigAgent.AddItem<uint>(0, OnChange, "LegendaryPickaxe");
            Core.AddHotkey("LegendaryPickaxe", OnHotkey);
        }

        private static void OnHotkey()
        {
            if (World.Player.GetItemOnLayer(Layer.Mount) != null || !World.Player.IsPlayer())
                return;

            Item item = WorldEx.FindItem(i => i.IsElementalBody() && i.DistanceTo(World.Player) < 3);
            if (item != null)
                Mine(item);
        }

        private static void OnChange(uint pickaxe)
        {
            if (pickaxe > 0)
                PacketHandler.RegisterServerToClientViewer(0x1A, WorldItem);
            else
                PacketHandler.RemoveServerToClientViewer(0x1A, WorldItem);
        }

        private static void WorldItem(PacketReader p, PacketHandlerEventArgs args)
        {
            Item item = World.FindItem(p.ReadUInt32() & 0x7FFFFFFF);
            if (item != null && item.IsElementalBody() && item.DistanceTo(World.Player) < 3 && World.Player.GetItemOnLayer(Layer.Mount) == null && World.Player.IsPlayer())
                Mine(item);
        }

        private static void Mine(Item item)
        {
            Item pickaxe = World.FindItem(ConfigEx.GetElement<uint>(0, "LegendaryPickaxe"));
            if (pickaxe != null)
            {
                Targeting.CancelTarget();
                Targeting.QueuedTarget = () =>
                {
                    Targeting.Target(item);
                    return true;
                };
                WorldEx.SendToServer(new DoubleClick(pickaxe.Serial));
            }
        }
    }
}