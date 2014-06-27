using Assistant;
using System;

namespace RazorEx
{
    public class EncodedMessage : Packet
    {
        public EncodedMessage(string text, ushort keyword)
            : base(0xAD)
        {
            EnsureCapacity(50 + (text.Length * 2) + 3);
            Write((byte)0xC0);
            if (World.Player == null || World.Player.SpeechHue == 0)
                Write((ushort)50);
            else
                Write(World.Player.SpeechHue);
            Write((ushort)3);
            WriteAsciiFixed("CSY", 4);
            Write((byte)0);
            Write((ushort)(keyword | 0x1000));
            WriteUTF8Null(text);
        }
    }

    public class QuestArrow : Packet
    {
        public QuestArrow(bool active, ushort x, ushort y)
            : base(0xBA, 6)
        {
            Write(Convert.ToByte(active));
            Write(x);
            Write(y);
        }
    }

    public class SetAbility : Packet
    {
        public SetAbility(int id)
            : base(0xD7)
        {
            EnsureCapacity(0xF);
            Write(World.Player.Serial);
            Write((ushort)0x19);
            Write((byte)0);
            Write(id);
            Write((byte)0x0A);
        }
    }

    public class InvokeVirtue : Packet
    {
        public InvokeVirtue(byte id)
            : base(0x12)
        {
            EnsureCapacity(0x06);
            Write((byte)0xF4);
            Write(id);
            Write((byte)0);
        }
    }

    public class Damage : Packet
    {
        public Damage(uint serial, ushort damage)
            : base(0x0B, 7)
        {
            Write(serial);
            Write(damage);
        }
    }

    public class ToggleMove : Packet
    {
        public ToggleMove(ushort spellId, bool use)
            : base(0xBF)
        {
            EnsureCapacity(0x08);
            Write((ushort)0x25);
            Write(spellId);
            Write(use);
        }
    }

    public class LaunchBrowser : Packet
    {
        public LaunchBrowser(string url)
            : base(0xA5)
        {
            EnsureCapacity(4 + url.Length);
            WriteAsciiNull(url);
        }
    }
}