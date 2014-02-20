using System;
using Assistant;

namespace RazorEx
{
    public static partial class Event
    {
        public delegate bool? MessageHandler(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, string lang, string name, string msg);
        public delegate bool? LocMessageHandler(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, int num, string name, string args);

        private static MessageHandler asciiMessage;
        public static event MessageHandler ASCIIMessage
        {
            add { asciiMessage += value; }
            remove { asciiMessage -= value; }
        }

        private static MessageHandler unicodeMessage;
        public static event MessageHandler UnicodeMessage
        {
            add { unicodeMessage += value; }
            remove { unicodeMessage -= value; }
        }

        private static LocMessageHandler localizedMessage;
        public static event LocMessageHandler LocalizedMessage
        {
            add { localizedMessage += value; }
            remove { localizedMessage -= value; }
        }

        private static void OnASCIIMessage(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            ItemID graphic = p.ReadUInt16();
            byte type = p.ReadByte();
            ushort hue = p.ReadUInt16();
            ushort font = p.ReadUInt16();
            string name = p.ReadStringSafe(30);
            string msg = p.ReadStringSafe().Trim();
            Handle(asciiMessage, args, serial, graphic, type, hue, font, string.Empty, name, msg);
        }

        private static void OnUnicodeMessage(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            ItemID graphic = p.ReadUInt16();
            byte type = p.ReadByte();
            ushort hue = p.ReadUInt16();
            ushort font = p.ReadUInt16();
            string lang = p.ReadStringSafe(4);
            string name = p.ReadStringSafe(30);
            string msg = p.ReadUnicodeStringSafe().Trim();
            Handle(unicodeMessage, args, serial, graphic, type, hue, font, lang, name, msg);
        }

        private static void OnLocalizedMessage(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            ItemID graphic = p.ReadUInt16();
            byte type = p.ReadByte();
            ushort hue = p.ReadUInt16();
            ushort font = p.ReadUInt16();
            int num = p.ReadInt32();
            string name = p.ReadStringSafe(30);
            string arguments = p.ReadUnicodeStringBE(((p.Length - 1) - p.Position) / 2);
            Handle(localizedMessage, args, serial, graphic, type, hue, font, num, name, arguments);
        }

        private static void OnMessageLocalizedAffix(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            ItemID graphic = p.ReadUInt16();
            byte type = p.ReadByte();
            ushort hue = p.ReadUInt16();
            ushort font = p.ReadUInt16();
            int num = p.ReadInt32();
            string name = p.ReadStringSafe(30);
            Handle(localizedMessage, args, serial, graphic, type, hue, font, num, name, string.Empty);
        }

        private static void Handle(Delegate del, PacketHandlerEventArgs args, params object[] parameters)
        {
            if (del != null)
                foreach (Delegate d in del.GetInvocationList())
                {
                    bool? result = (bool?)d.Method.Invoke(d.Target, parameters);
                    if (result.HasValue)
                        args.Block = result.Value;
                }
        }
    }
}