using Assistant;
using Assistant.Macros;
using System;

namespace RazorEx.Addons
{
    public static class Healing
    {
        private static readonly SpeechAction ebs = new SpeechAction(MessageType.Regular, 0, 3, null, null, ".ebs");
        private static bool enabled;
        private static Timer targetTimer;
        private static DateTime healingTimeout = DateTime.Now;
        private static Targeting.QueueTarget prevQueue;
        private static Serial healSerial = Serial.Zero;
        private static bool isHealing;
        private static bool IsHealing
        {
            get { return (isHealing && DateTime.Now > healingTimeout) ? isHealing = false : isHealing; }
            set
            {
                isHealing = value;
                if (isHealing)
                {
                    double timeout = Math.Max(11000 - (50 * World.Player.Dex), 2000) + 1000;
                    if (PositionCheck.InWisp)
                        timeout += 4000;
                    healingTimeout = DateTime.Now.AddMilliseconds(timeout);
                }
            }
        }

        public static void OnInit()
        {
            ConfigAgent.AddItem(true, e => enabled = e, "Healing");
            Event.MobileUpdated += WorldEx_MobileUpdated;
            Event.LocalizedMessage += Event_LocalizedMessage;
            PacketHandler.RegisterClientToServerViewer(0x6C, OnClientTarget);
            PacketHandler.RegisterServerToClientViewer(0x6C, OnServerTarget);
            Command.Register("heal", args => HealTarget());
            Command.Register("healing", OnCommand);
            Core.AddHotkey("Bandage Target", HealTarget);
            PositionCheck.EnterKhaldun += () => { if (enabled) OnCommand(null); };
            PositionCheck.LeaveKhaldun += () => { if (!enabled) OnCommand(null); };
        }

        private static void OnCommand(string[] args)
        {
            enabled = !ConfigEx.GetElement(true, "Healing");
            ConfigEx.SetElement(enabled, "Healing");
            WorldEx.SendMessage("Healing " + (enabled ? "enabled." : "disabled."));
        }

        private static void HealTarget()
        { Targeting.OneTimeTarget(OnTarget, () => healSerial = Serial.Zero); }

        private static void OnTarget(bool l, Serial s, Point3D p, ushort g)
        { healSerial = (s == World.Player.Serial) ? Serial.Zero : s; }

        private static void Event_LocalizedMessage(Serial serial, int msg, string args)
        {
            switch (msg)
            {
                case 500956: // You begin applying the bandages.
                    IsHealing = true;
                    break;
                case 500962: // You were unable to finish your work before you died.
                case 500963: // You did not stay close enough to heal your target.
                case 501042: // Target can not be resurrected at that location.
                case 500965: // You are able to resurrect your patient.
                case 503255: // You are able to resurrect the creature.
                case 1049670: // The pet's owner must be nearby to attempt resurrection.
                case 503256: // You fail to resurrect the creature.
                case 500966: // You are unable to resurrect your patient.
                case 500969: // You finish applying the bandages.
                case 1010058: // You have cured the target of all poisons.
                case 1010060: // You have failed to cure your target!
                case 1060088: // You bind the wound and stop the bleeding
                case 1005000: // mortal
                case 1010398: // mortal
                case 500967: // You heal what little damage your patient had.
                case 500968: // You apply the bandages, but they barely help.
                case 1010395: // The veil of death in this area is too strong and resists thy efforts to restore life.
                    IsHealing = false;
                    break;
            }
        }

        private static void WorldEx_MobileUpdated(Serial serial)
        {
            if (serial != World.Player.Serial && serial != healSerial)
                return;
            if (IsHealing || World.Player.IsGhost || !World.Player.Visible)
                return;

            Mobile target;
            if (enabled && (World.Player.Hits < World.Player.HitsMax * 0.90 || World.Player.Poisoned))
                IsHealing = !Bandages.Valid || Bandages.Enchanced ? ebs.Perform() : BandageSelf();
            else if (healSerial.IsValid && (target = World.FindMobile(healSerial)) != null && target.Hits < target.HitsMax * 0.90 && Utility.Distance(target.Position, World.Player.Position) < 3 && Bandages.Valid)
            {
                prevQueue = Targeting.QueuedTarget;
                Targeting.CancelTarget();
                Targeting.QueuedTarget = () =>
                {
                    Targeting.Target(healSerial);
                    return true;
                };
                Bandages.Use();
                IsHealing = true;
            }
        }

        private static bool BandageSelf()
        {
            if (targetTimer != null && targetTimer.Running)
                return false;

            if (Targeting.HasTarget)
            {
                targetTimer = Timer.DelayedCallback(TimeSpan.FromSeconds(5), OnTargetTimer);
                targetTimer.Start();
                return false;
            }

            if (!Bandages.Valid)
                return false;
            prevQueue = Targeting.QueuedTarget;
            Targeting.CancelTarget();
            Targeting.QueuedTarget = Targeting.DoTargetSelf;
            Bandages.Use();
            return true;
        }

        private static void OnClientTarget(PacketReader p, PacketHandlerEventArgs args)
        {
            if (targetTimer != null && targetTimer.Running)
                OnTargetTimer();
        }

        private static void OnServerTarget(PacketReader p, PacketHandlerEventArgs args)
        {
            if (prevQueue != null)
            {
                Targeting.QueuedTarget = prevQueue;
                prevQueue = null;
            }
        }

        private static void OnTargetTimer()
        {
            targetTimer.Stop();
            targetTimer = null;
            Targeting.CancelTarget();
            IsHealing = BandageSelf();
        }
    }
}