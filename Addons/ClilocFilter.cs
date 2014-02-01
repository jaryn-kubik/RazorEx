using System;
using System.Collections;
using System.Collections.Generic;
using Assistant;

namespace RazorEx.Addons
{
    public static class ClilocFilter
    {
        private static bool enabled;
        private static int[] filter;

        public static void OnInit()
        {
            ArrayList list = (ArrayList)PacketHandler.m_ServerFilters[0xC1];
            list.Clear();
            list.Add(new PacketFilterCallback(OnLocalizedMessage));
            ConfigAgent.AddItem(false, value => enabled = value, "Filter");
            ConfigAgent.AddItem(false, OnChanged, "FilterHerbar");
            Command.Register("filter", OnCommand);

            List<int> msgs = new List<int>();
            foreach (string[] data in ConfigEx.LoadCfg("filter.cfg", 0))
            {
                int msg;
                if (int.TryParse(data[0], out msg))
                    msgs.Add(msg);
            }
            filter = msgs.ToArray();
        }

        private static void OnCommand(string[] args)
        {
            ConfigEx.SetElement(enabled = !enabled, "Filter");
            WorldEx.SendMessage("Filter " + (enabled ? "enabled." : "disabled."));
        }

        private static void OnChanged(bool value)
        {
            if (value)
                PacketHandler.RegisterServerToClientFilter(0xAE, OnSpeech);
            else
                PacketHandler.RemoveServerToClientFilter(0xAE, OnSpeech);
        }

        private static void OnSpeech(Packet p, PacketHandlerEventArgs args)
        {
            p.MoveToData();
            Serial serial = p.ReadUInt32();
            ItemID itemID = p.ReadUInt16();
            p.ReadByte();
            ushort color = p.ReadUInt16();
            p.ReadUInt16();
            p.ReadStringSafe(4);
            p.ReadStringSafe(30);
            string text = p.ReadUnicodeStringSafe().Trim();

            if (serial == Serial.MinusOne && itemID == 0xFFFF && color == 0x55C &&
                text.EndsWith("bylo vlozeno do herbare."))
                args.Block = true;
        }

        private static void OnLocalizedMessage(Packet p, PacketHandlerEventArgs args)
        {
            Serial ser = p.ReadUInt32();
            ushort body = p.ReadUInt16();
            MessageType spell = (MessageType)p.ReadByte();
            ushort hue = p.ReadUInt16();
            ushort font = p.ReadUInt16();
            int num = p.ReadInt32();
            string name = p.ReadStringSafe(30);
            string argstr = p.ReadUnicodeStringLESafe();
            if (((((num >= 0x2dce9b) && (num < 0x2dcedb)) || ((num >= 0x102e9d) && (num < 0x102ead))) || (((num >= 0x102ee9) && (num < 0x102ef3)) || ((num >= 0x102e8d) && (num < 0x102e97)))) || (((num >= 0x102ef3) && (num < 0x102ef9)) || ((num >= 0x102f02) && (num < 0x102f0a))))
                spell = MessageType.Spell;

            BandageTimer.OnLocalizedMessage(num);
            try
            {
                if (enabled && Array.IndexOf(filter, num) != -1)
                    args.Block = true;
                else
                    PacketHandlers.HandleSpeech(p, args, ser, body, spell, hue, font, Language.CliLocName.ToUpper(), name, Language.ClilocFormat(num, argstr));
            }
            catch (Exception exception)
            { Engine.LogCrash(new Exception(string.Format("Exception in Ultima.dll cliloc: {0}, {1}", num, argstr), exception)); }
        }
    }
}