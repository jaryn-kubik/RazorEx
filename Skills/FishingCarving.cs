using Assistant;
using System.Collections.Generic;

namespace RazorEx.Skills
{
    public static class FishingCarving
    {
        private static readonly List<Serial> carved = new List<Serial>();
        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "CarveSeaSerpent"); }
        private static void OnChange(bool enabled)
        {
            if (enabled)
                PacketHandler.RegisterServerToClientViewer(0x1A, WorldItem);
            else
                PacketHandler.RemoveServerToClientViewer(0x1A, WorldItem);
        }

        private static void WorldItem(PacketReader p, PacketHandlerEventArgs args)
        {
            Item item = World.FindItem(p.ReadUInt32() & 0x7FFFFFFF);
            if (item != null && item.ItemID == 0x2006 && item.Hue >= 0x0530 && item.Hue <= 0x0539 && item.Amount == 0x0096 && item.DistanceTo(World.Player) < 3)
                Carve(item);
        }

        private static void Carve(Item item)
        {
            Item carver = World.FindItem(ConfigEx.GetElement<uint>(0, "BoneCarver"));
            carved.RemoveAll(i => !World.Items.ContainsKey(i));
            if (carver != null && !carved.Contains(item.Serial))
            {
                Targeting.CancelTarget();
                Targeting.QueuedTarget = () =>
                {
                    Targeting.Target(item);
                    carved.Add(item.Serial);
                    return true;
                };
                WorldEx.SendToServer(new DoubleClick(carver.Serial));
            }
        }
    }
}