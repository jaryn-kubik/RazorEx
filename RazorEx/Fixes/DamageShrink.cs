using System;
using Assistant;

namespace RazorEx.Fixes
{
    public static class DamageShrink
    {
        private static Serial currentSerial;
        private static ushort currentDmg;
        private static Timer timer;

        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "ShrinkDMG"); }
        private static void OnChange(bool shringDmg)
        {
            if (shringDmg)
            {
                PacketHandler.RegisterServerToClientViewer(0x0B, OnDamage);
                PacketHandler.RegisterServerToClientViewer(0x2F, OnSwing);
                PacketHandler.RegisterServerToClientViewer(0xAF, OnDeath);
            }
            else
            {
                PacketHandler.RemoveServerToClientViewer(0x0B, OnDamage);
                PacketHandler.RemoveServerToClientViewer(0x2F, OnSwing);
                PacketHandler.RemoveServerToClientViewer(0xAF, OnDeath);
            }
        }

        private static void OnDamage(PacketReader p, PacketHandlerEventArgs args)
        {
            if (p.ReadUInt32() == currentSerial && timer != null && timer.Running)
            {
                timer.Stop();
                currentDmg += p.ReadUInt16();
                args.Block = true;
                timer.Delay = TimeSpan.FromMilliseconds(100);
                timer.Start();
            }
        }

        private static void OnSwing(PacketReader p, PacketHandlerEventArgs args)
        {
            p.ReadByte();
            if (p.ReadUInt32() == World.Player.Serial && timer == null)
            {
                currentSerial = p.ReadUInt32();
                currentDmg = 0;
                timer = Timer.DelayedCallback(TimeSpan.FromMilliseconds(250), OnTimer);
                timer.Start();
            }
        }

        private static void OnDeath(PacketReader p, PacketHandlerEventArgs args)
        {
            if (p.ReadUInt32() == currentSerial && timer != null && timer.Running)
            {
                timer.Stop();
                OnTimer();
                args.Block = true;
                ClientCommunication.SendToClient(p);
            }
        }

        private static void OnTimer()
        {
            timer = null;
            if (currentSerial.IsValid && currentDmg > 0)
                WorldEx.SendToClient(new Damage(currentSerial, currentDmg));
            currentSerial = currentDmg = 0;
        }
    }
}
