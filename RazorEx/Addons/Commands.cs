using Assistant;
using Assistant.HotKeys;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace RazorEx.Addons
{
    public static class Commands
    {
        public static void OnInit()
        {
            Command.Register("resync", Resync);
            Command.Register("hide", args => Targeting.OneTimeTarget(Hide));
            Command.Register("info", args => Targeting.OneTimeTarget(Info));
            Command.Register("dye", Dye);
            Command.Register("bank", Bank);
            Command.Register("morph", Morph);
        }

        private static void Morph(string[] args)
        {
            ushort body;
            if (args.Length > 0 && ushort.TryParse(args[0], out body))
            {
                World.Player.Body = body;
                WorldEx.SendToClient(new MobileUpdate(World.Player));
            }
        }

        private static void Bank(string[] args)
        {
            Item item = World.Player.Backpack.FindItem(0x1F1C, 0x0489) ??
                        WorldEx.FindItemG(0x1F1C, 0x0489, i => i.DistanceTo(World.Player) < 5);
            if (item != null)
                WorldEx.SendToServer(new DoubleClick(item.Serial));
            else
                WorldEx.SendMessage("No bank crystal found.");
        }

        private static void Hide(bool location, Serial serial, Point3D p, ushort gfxid) { WorldEx.SendToClient(new RemoveObject(serial)); }
        private static void Resync(string[] args)
        {
            DragDropManager.DropCurrent();
            ActionQueue.Stop();
            UseHotKeys.Resync();
        }

        private static void Info(bool location, Serial serial, Point3D p, ushort gfxid)
        {
            StringBuilder str = new StringBuilder();
            UOEntity entity = WorldEx.GetEntity(serial);
            if (entity == null)
                return;

            str.AppendLine("Serial: " + serial);
            str.AppendLine("Graphic: 0x" + gfxid.ToString("X4"));
            str.AppendLine("Color: 0x" + entity.Hue.ToString("X4"));
            str.AppendLine("Position: " + entity.Position);
            str.AppendLine("Distance: " + Utility.Distance(entity.Position, World.Player.Position));
            str.AppendLine();

            foreach (PropertyInfo property in entity.GetType().GetProperties())
                str.AppendLine(string.Format("{0}: {1}\n", property.Name, property.GetValue(entity, null)));

            new MessageDialog("Info", str.ToString()) { TopMost = true }.Show(Engine.MainWindow);
        }

        private static void Dye(string[] args)
        {
            ushort hue;
            if (args.Length == 1 && ushort.TryParse(args[0], out hue))
                Timer.DelayedCallback(TimeSpan.FromMilliseconds(500), () => Targeting.OneTimeTarget((l, s, p2, g) => DyeTarget(s, hue))).Start();
            else
                Targeting.OneTimeTarget((l, s, p, g) => Timer.DelayedCallback(TimeSpan.FromMilliseconds(500), () => Targeting.OneTimeTarget((l2, s2, p2, g2) => OnDye(s, s2))).Start());
        }

        private static void OnDye(Serial source, Serial target)
        {
            UOEntity sourceObject = WorldEx.GetEntity(source);
            if (sourceObject != null)
            {
                ushort hue = sourceObject.Hue;
                Mobile mobile = sourceObject as Mobile;
                if (mobile != null)
                {
                    Item mount = mobile.GetItemOnLayer(Layer.Mount);
                    if (mount != null)
                        hue = mount.Hue;
                }
                DyeTarget(target, hue);
            }
        }

        private static void DyeTarget(Serial target, ushort hue)
        {
            UOEntity targetObject = WorldEx.GetEntity(target);
            if (targetObject == null)
                return;

            Packet packet;
            if (targetObject is Item)
            {
                if (((Item)targetObject).Container is Mobile)
                    packet = new EquipmentItem((Item)targetObject, hue, ((Mobile)((Item)targetObject).Container).Serial);
                else
                {
                    packet = new ContainerItem((Item)targetObject);
                    packet.Seek(-2, SeekOrigin.End);
                    packet.Write(hue);
                }
            }
            else if (targetObject is Mobile)
            {
                Item mount = ((Mobile)targetObject).GetItemOnLayer(Layer.Mount);
                if (mount != null)
                    packet = new EquipmentItem(mount, hue, targetObject.Serial);
                else
                {
                    packet = new MobileIncoming((Mobile)targetObject);
                    packet.Seek(15, SeekOrigin.Begin);
                    packet.Write(hue);
                }
            }
            else
                return;
            WorldEx.SendToClient(packet);
        }
    }
}