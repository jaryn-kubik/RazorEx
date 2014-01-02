using Assistant;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace RazorEx.Addons
{
    public static class PotionStack
    {
        private static readonly List<Serial> revit = new List<Serial>();
        private static readonly Item fakeRevit = new Item(0x60000001) { ItemID = 0x0F0E, Hue = 0x000C };//0x0F06
        private static readonly List<Serial> tmr = new List<Serial>();
        private static readonly Item fakeTMR = new Item(0x60000002) { ItemID = 0xF0E, Hue = 0x012E };//0x0F0B

        public static void OnInit()
        {
            ConfigAgent.AddItem(false, "PotionStack");
            if (!ConfigEx.GetElement(false, "PotionStack"))
                return;

            fakeRevit.Position = new Point3D(ConfigEx.GetAttribute(50, "revitX", "PotionStack"),
                                             ConfigEx.GetAttribute(100, "revitY", "PotionStack"), 0);
            fakeTMR.Position = new Point3D(ConfigEx.GetAttribute(100, "tmrX", "PotionStack"),
                                           ConfigEx.GetAttribute(100, "tmrY", "PotionStack"), 0);

            liftRequest = (PacketViewerCallback)((ArrayList)PacketHandler.m_ClientViewers[7])[0];
            PacketHandler.m_ClientViewers[7] = new ArrayList(new PacketViewerCallback[] { LiftRequest });
            dropRequest = (PacketViewerCallback)((ArrayList)PacketHandler.m_ClientViewers[8])[0];
            PacketHandler.m_ClientViewers[8] = new ArrayList(new PacketViewerCallback[] { DropRequest });
            clientDoubleClick = (PacketViewerCallback)((ArrayList)PacketHandler.m_ClientViewers[6])[0];
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
            Serial serial = p.ReadUInt32();
            args.Block = serial == fakeRevit.Serial || serial == fakeTMR.Serial;
        }

        private static ushort lifting;
        private static PacketViewerCallback liftRequest;
        private static void LiftRequest(PacketReader p, PacketHandlerEventArgs args)
        {
            args.Block = true;
            Serial serial = p.ReadUInt32();
            lifting = p.ReadUInt16();
            if (serial == fakeRevit.Serial)
                WorldEx.SendToClient(new RemoveObject(fakeRevit));
            else if (serial == fakeTMR.Serial)
                WorldEx.SendToClient(new RemoveObject(fakeTMR));
            else
            {
                lifting = 0;
                args.Block = false;
                p.MoveToData();
                liftRequest(p, args);
            }
        }

        private static PacketViewerCallback dropRequest;
        private static void DropRequest(PacketReader p, PacketHandlerEventArgs args)
        {
            args.Block = true;
            Serial serial = p.ReadUInt32();
            int x = p.ReadInt16();
            int y = p.ReadInt16();
            int z = p.ReadSByte();
            Item container = World.FindItem(p.ReadUInt32());
            if (serial == fakeRevit.Serial)
            {
                if (container == World.Player.Backpack)
                    fakeRevit.Position = new Point3D(x, y, z);
                else
                    for (int i = 0; i < lifting; i++)
                        DragDrop.Move(World.FindItem(revit[revit.Count - 1 - i]), container);
            }
            else if (serial == fakeTMR.Serial)
            {
                if (container == World.Player.Backpack)
                    fakeTMR.Position = new Point3D(x, y, z);
                else
                    for (int i = 0; i < lifting; i++)
                        DragDrop.Move(World.FindItem(tmr[tmr.Count - 1 - i]), container);
            }
            else
            {
                p.MoveToData();
                dropRequest(p, args);
                return;
            }
            lifting = 0;
            Resend();
        }

        private static void ClientSingleClick(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            args.Block = serial == fakeRevit.Serial || serial == fakeTMR.Serial;
        }

        private static PacketViewerCallback clientDoubleClick;
        private static void ClientDoubleClick(PacketReader p, PacketHandlerEventArgs args)
        {
            args.Block = true;
            Serial serial = p.ReadUInt32();
            if (serial == fakeRevit.Serial)
                WorldEx.SendToServer(new DoubleClick(revit[revit.Count - 1]));
            else if (serial == fakeTMR.Serial)
                WorldEx.SendToServer(new DoubleClick(tmr[tmr.Count - 1]));
            else
            {
                args.Block = false;
                p.MoveToData();
                clientDoubleClick(p, args);
            }
        }

        private static void Resend()
        {
            fakeRevit.Amount = (ushort)revit.Count;
            fakeRevit.Container = World.Player.Backpack;
            if (fakeRevit.Amount > 0)
                WorldEx.SendToClient(new ContainerItem(fakeRevit));
            ConfigEx.SetAttribute(fakeRevit.Position.X, "revitX", "PotionStack");
            ConfigEx.SetAttribute(fakeRevit.Position.Y, "revitY", "PotionStack");

            fakeTMR.Amount = (ushort)tmr.Count;
            fakeTMR.Container = World.Player.Backpack;
            if (fakeTMR.Amount > 0)
                WorldEx.SendToClient(new ContainerItem(fakeTMR));
            ConfigEx.SetAttribute(fakeTMR.Position.X, "tmrX", "PotionStack");
            ConfigEx.SetAttribute(fakeTMR.Position.Y, "tmrY", "PotionStack");
        }

        private static void ContainerContentUpdate(Packet p, PacketHandlerEventArgs args)
        {
            Item item = World.FindItem(p.ReadUInt32());
            if (item != null && item.Container == World.Player.Backpack)
            {
                if (item.ItemID == 0x0F06 && item.Hue == 0x000C)
                {
                    if (!revit.Contains(item.Serial))
                        revit.Add(item.Serial);
                }
                else if (item.ItemID == 0x0F0B && item.Hue == 0x012E)
                {
                    if (!tmr.Contains(item.Serial))
                        tmr.Add(item.Serial);
                }
                else
                    return;
                args.Block = true;
                Resend();
            }
        }

        private static void ContainerContent(Packet p, PacketHandlerEventArgs args)
        {
            List<Serial> toRemove = new List<Serial>();
            ushort count = p.ReadUInt16();
            for (; count > 0; count--)
            {
                Item item = World.FindItem(p.ReadUInt32());
                if (item != null && item.Container == World.Player.Backpack)
                {
                    if (item.ItemID == 0x0F06 && item.Hue == 0x000C)
                    {
                        if (!revit.Contains(item.Serial))
                            revit.Add(item.Serial);
                        toRemove.Add(item.Serial);
                    }
                    else if (item.ItemID == 0x0F0B && item.Hue == 0x012E)
                    {
                        if (!tmr.Contains(item.Serial))
                            tmr.Add(item.Serial);
                        toRemove.Add(item.Serial);
                    }
                }
                p.Seek(15, SeekOrigin.Current);
            }

            if (toRemove.Count > 0)
                Resend();
            toRemove.ForEach(s => WorldEx.SendToClient(new RemoveObject(s)));
        }

        private static void Event_RemoveObject(Serial serial)
        {
            if (revit.Remove(serial))
            {
                if (revit.Count == 0)
                    WorldEx.SendToClient(new RemoveObject(fakeRevit));
                else
                    Resend();
            }
            else if (tmr.Remove(serial))
            {
                if (tmr.Count == 0)
                    WorldEx.SendToClient(new RemoveObject(fakeTMR));
                else
                    Resend();
            }
        }
    }
}