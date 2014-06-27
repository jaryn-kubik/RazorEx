using Assistant;
using Assistant.Macros;
using System.Collections.Generic;

namespace RazorEx.Addons
{
    public static class AutoGrab
    {
        private static readonly List<Serial> list = new List<Serial>();
        public static void OnInit()
        {
            ConfigAgent.AddItem(false, OnChange, "AutoGrab");
            Command.Register("grab", OnCommand);
        }

        private static void OnCommand(string[] args)
        {
            bool enabled = !ConfigEx.GetElement(false, "AutoGrab");
            ConfigEx.SetElement(enabled, "AutoGrab");
            WorldEx.SendMessage("Autograb " + (enabled ? "enabled." : "disabled."));
            OnChange(enabled);
        }

        private static void OnChange(bool enabled)
        {
            if (enabled)
            {
                Event.PlayerMoved += Event_PlayerMoved;
                PacketHandler.RegisterServerToClientViewer(0x1A, WorldItem);
            }
            else
            {
                Event.PlayerMoved -= Event_PlayerMoved;
                PacketHandler.RemoveServerToClientViewer(0x1A, WorldItem);
            }
        }

        private static void WorldItem(PacketReader p, PacketHandlerEventArgs args)
        {
            Item item = World.FindItem(p.ReadUInt32() & 0x7fffffff);
            if (item.ItemID == 0x2006 && item.DistanceTo(World.Player) < 3)
                new SpeechAction(MessageType.Regular, World.Player.SpeechHue, 3, null, null, ".grab").Perform();
        }

        private static void Event_PlayerMoved()
        {
            bool grab = false;
            foreach (Item item in WorldEx.FindItems(i => i.ItemID == 0x2006 && i.DistanceTo(World.Player) < 3 && !list.Contains(i.Serial)))
            {
                grab = true;
                list.Add(item.Serial);
            }

            if (grab)
            {
                list.RemoveAll(i => !World.Items.ContainsKey(i));
                new SpeechAction(MessageType.Regular, World.Player.SpeechHue, 3, null, null, ".grab").Perform();
            }
        }
    }
}
