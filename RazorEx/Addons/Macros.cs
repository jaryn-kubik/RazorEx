using Assistant;
using Assistant.Macros;
using System;

namespace RazorEx.Addons
{
    public class MacroEx : Macro
    {
        public MacroEx() : base(null) { m_Loaded = Loop = true; }
        public override string ToString() { return "RazorEx"; }
    }

    public class SpellTarget : MacroWaitAction
    {
        private static bool disturbed;

        public SpellTarget() { m_Timeout = TimeSpan.FromSeconds(2); }
        public static void OnInit() { Event.LocalizedMessage += Event_LocalizedMessage; }
        private static bool? Event_LocalizedMessage(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, int num, string name, string args)
        {
            if (num == 500641)
                disturbed = true;
            return null;
        }

        public override bool CheckMatch(MacroAction a)
        {
            if (!(a is WaitForTargetAction))
                return false;
            Target();
            return true;
        }

        protected virtual void Target()
        {
            if (Targeting.DoLastTarget())
                return;
            Targeting.CancelTarget();
            Parent.Stop();
        }

        public override bool PerformWait() { return !Targeting.HasTarget && !disturbed; }
        public override bool Perform()
        {
            disturbed = false;
            return !PerformWait();
        }
    }

    public class TargetSelf : SpellTarget { protected override void Target() { Targeting.TargetSelf(); } }
    public class ResetWarmode : MacroAction
    {
        public override bool Perform()
        {
            WorldEx.SendToServer(new SetWarMode(true));
            WorldEx.SendToServer(new SetWarMode(false));
            return true;
        }
    }

    public class WaitForMsg : MacroWaitAction
    {
        public int Last { get; private set; }
        public bool Recieved { get; set; }
        private readonly int[] msgs;

        public WaitForMsg(params int[] msgs)
        {
            m_Timeout = TimeSpan.FromSeconds(10);
            this.msgs = msgs;
            Event.LocalizedMessage += Event_LocalizedMessage;
        }

        public override bool PerformWait() { return !Recieved; }
        public override bool Perform() { return !PerformWait(); }
        private bool? Event_LocalizedMessage(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, int num, string name, string args)
        {
            if (Array.IndexOf(msgs, num) != -1)
            {
                Last = num;
                Recieved = true;
            }
            return null;
        }
    }

    public class Clean : MacroAction
    {
        public override bool Perform()
        {
            Cleaner.Clean();
            return true;
        }
    }
}