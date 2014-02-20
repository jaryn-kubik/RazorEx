using System.IO;
using Assistant;

namespace RazorEx
{
    public static partial class Event
    {
        public delegate bool? HuedEffectHandler(byte type, Serial src, Serial dest, ItemID itemID, byte speed, byte count, uint hue, uint mode);
        private static HuedEffectHandler huedEffect;
        public static event HuedEffectHandler HuedEffect
        {
            add { huedEffect += value; }
            remove { huedEffect -= value; }
        }

        private static void OnHuedEffect(PacketReader p, PacketHandlerEventArgs args)
        {
            byte type = p.ReadByte();
            Serial src = p.ReadUInt32();
            Serial dest = p.ReadUInt32();
            ItemID itemID = p.ReadUInt16();
            p.Seek(10, SeekOrigin.Current);
            byte speed = p.ReadByte();
            byte count = p.ReadByte();
            p.ReadUInt32();
            uint hue = p.ReadUInt32();
            uint mode = p.ReadUInt32();
            Handle(huedEffect, args, type, src, dest, itemID, speed, count, hue, mode);
        }
    }
}