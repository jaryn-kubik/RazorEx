using Assistant;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RazorEx.Addons
{
    public static class PotionStack
    {
        private sealed class FakeItem : Item
        {
            private static uint fakeSerial = 0x60000000;
            public List<Serial> List { get; private set; }
            public ItemID OrigID { get; private set; }
            public ushort OrigHue { get; private set; }

            public FakeItem(string name, ItemID origID, ushort origHue, ItemID newID, ushort newHue)
                : base(++fakeSerial)
            {
                List = new List<Serial>();
                Name = name;
                ItemID = newID;
                Hue = newHue;
                OrigID = origID;
                OrigHue = origHue;
                Position = new Point3D(ConfigEx.GetAttribute(100, "X", "FakeItems", Name),
                                        ConfigEx.GetAttribute(100, "Y", "FakeItems", Name), 0);
            }
        }

        private static readonly Dictionary<Serial, FakeItem> items = new Dictionary<Serial, FakeItem>();
        private static void AddFakeItem(string name, ItemID origID, ushort origHue, ItemID newID, ushort newHue)
        {
            FakeItem item = new FakeItem(name, origID, origHue, newID, newHue);
            items.Add(item.Serial, item);
        }

        public static void AfterInit()
        {
            ConfigAgent.AddItem(false, "PotionStack");
            if (!ConfigEx.GetElement(false, "PotionStack"))
                return;

            AddFakeItem("Revitalize", 0x0F06, 0x000C, 0x0F0E, 0x000C);
            AddFakeItem("TMR", 0x0F0B, 0x012E, 0x0F0E, 0x012E);
            AddFakeItem("MR", 0x0F0B, 0x0130, 0x0F0E, 0x005F);
            AddFakeItem("GH", 0x0F0C, 0x0000, 0x0F0E, 0x0035);

            liftRequest = (ArrayList)PacketHandler.m_ClientViewers[7];
            PacketHandler.m_ClientViewers[7] = new ArrayList(new PacketViewerCallback[] { LiftRequest });
            dropRequest = (ArrayList)PacketHandler.m_ClientViewers[8];
            PacketHandler.m_ClientViewers[8] = new ArrayList(new PacketViewerCallback[] { DropRequest });
            clientDoubleClick = (ArrayList)PacketHandler.m_ClientViewers[6];
            PacketHandler.m_ClientViewers[6] = new ArrayList(new PacketViewerCallback[] { ClientDoubleClick });
            PacketHandler.RegisterClientToServerViewer(9, ClientSingleClick);
            PacketHandler.RegisterServerToClientFilter(0x25, ContainerContentUpdate);
            PacketHandler.RegisterServerToClientFilter(0x3C, ContainerContent);
            PacketHandler.RegisterClientToServerViewer(0x6C, TargetResponse);
            Event.RemoveObject += Event_RemoveObject;
        }

        private static void TargetResponse(PacketReader p, PacketHandlerEventArgs args)
        {
            p.Seek(7, SeekOrigin.Begin);
            args.Block = items.ContainsKey(p.ReadUInt32());
        }

        private static void ClientSingleClick(PacketReader p, PacketHandlerEventArgs args)
        { args.Block = items.ContainsKey(p.ReadUInt32()); }

        private static ushort lifting;
        private static ArrayList liftRequest;
        private static void LiftRequest(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            if (items.ContainsKey(serial))
            {
                lifting = p.ReadUInt16();
                args.Block = true;
                WorldEx.SendToClient(new RemoveObject(serial));
            }
            else
                args.Block = PacketHandler.ProcessViewers(liftRequest, p);
        }

        private static ArrayList dropRequest;
        private static void DropRequest(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            int x = p.ReadInt16();
            int y = p.ReadInt16();
            int z = p.ReadSByte();
            Item container = World.FindItem(p.ReadUInt32());

            if (items.ContainsKey(serial))
            {
                args.Block = true;
                FakeItem fake = items[serial];
                if (container == World.Player.Backpack)
                    fake.Position = new Point3D(x, y, z);
                else
                    foreach (Serial s in fake.List.Take(lifting))
                        DragDrop.Move(World.FindItem(s), container);
                lifting = 0;
                Resend();
            }
            else
                args.Block = PacketHandler.ProcessViewers(dropRequest, p);
        }

        private static ArrayList clientDoubleClick;
        private static void ClientDoubleClick(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            if (items.ContainsKey(serial))
            {
                args.Block = true;
                WorldEx.SendToServer(new DoubleClick(items[serial].List.Last()));
            }
            else
                args.Block = PacketHandler.ProcessViewers(clientDoubleClick, p);
        }

        private static void ContainerContentUpdate(Packet p, PacketHandlerEventArgs args)
        {
            Item item = World.FindItem(p.ReadUInt32());
            if (item != null && item.Container == World.Player.Backpack)
                foreach (FakeItem fake in items.Values)
                    if (item.ItemID == fake.OrigID && item.Hue == fake.OrigHue)
                    {
                        if (!fake.List.Contains(item.Serial))
                            fake.List.Add(item.Serial);
                        args.Block = true;
                        Resend();
                    }
        }

        private static void ContainerContent(Packet p, PacketHandlerEventArgs args)
        {
            List<Serial> toRemove = new List<Serial>();
            for (ushort count = p.ReadUInt16(); count > 0; count--)
            {
                Item item = World.FindItem(p.ReadUInt32());
                if (item != null && item.Container == World.Player.Backpack)
                    foreach (FakeItem fake in items.Values)
                        if (item.ItemID == fake.OrigID && item.Hue == fake.OrigHue)
                        {
                            if (!fake.List.Contains(item.Serial))
                                fake.List.Add(item.Serial);
                            toRemove.Add(item.Serial);
                        }
                p.Seek(15, SeekOrigin.Current);
            }

            if (toRemove.Count > 0)
                Resend();
            toRemove.ForEach(s => WorldEx.SendToClient(new RemoveObject(s)));
        }

        private static void Event_RemoveObject(Serial serial)
        {
            foreach (FakeItem fake in items.Values)
                if (fake.List.Remove(serial))
                {
                    if (fake.List.Count == 0)
                        WorldEx.SendToClient(new RemoveObject(fake.Serial));
                    else
                        Resend();
                    return;
                }
        }

        private static void Resend()
        {
            foreach (FakeItem fake in items.Values)
            {
                fake.Amount = (ushort)fake.List.Count;
                fake.Container = World.Player.Backpack;
                fake.ObjPropList.Remove(1050039);
                fake.ObjPropList.Remove(1072789);
                fake.ObjPropList.Add(1050039, "{0}\t{1}", fake.List.Count, fake.Name);
                fake.ObjPropList.Add(1072789, "{0}", fake.List.Count);
                if (fake.Amount > 0)
                {
                    WorldEx.SendToClient(new ContainerItem(fake));
                    WorldEx.SendToClient(fake.ObjPropList.BuildPacket());
                }
                ConfigEx.SetAttribute(fake.Position.X, "X", "FakeItems", fake.Name);
                ConfigEx.SetAttribute(fake.Position.Y, "Y", "FakeItems", fake.Name);
            }
        }
    }
}