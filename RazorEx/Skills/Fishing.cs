using Assistant;
using Assistant.Macros;
using RazorEx.Addons;
using RazorEx.Fixes;
using System;

namespace RazorEx.Skills
{
    public static class Fishing
    {
        public static void OnInit()
        {
            MacroEx macro = new MacroEx();
            WaitForMsg wait = new WaitForMsg(1042635, 1008124, 1008125, 1043297, 1055086, 1055087, 503172, 503171);
            macro.Insert(-1, new UseFishingPole(wait));
            macro.Insert(-1, new WaitForTargetAction(new[] { "", "2" }));
            macro.Insert(-1, new TargetRelLocAction(0, 0));
            macro.Insert(-1, new AttackClosestAction());
            macro.Insert(-1, new Clean());
            Track track = new Track();
            macro.Insert(-1, track);
            macro.Insert(-1, new WaitForTrack(track));
            macro.Insert(-1, new TrackResponse(track));
            macro.Insert(-1, wait);
            macro.Insert(-1, new SailAction(wait));
            Command.Register("fish", args => MacroManager.HotKeyPlay(macro));
            Core.AddHotkey("Fishing", () => MacroManager.HotKeyPlay(macro));

            MacroEx sos = new MacroEx();
            WaitForMsg waitSos = new WaitForMsg(1042635, 1008124, 1008125, 1043297, 1055086, 1055087, 503172, 503171, 501747);
            sos.Insert(-1, new UseFishingPole(waitSos));
            sos.Insert(-1, new WaitForTargetAction(new[] { "", "2" }));
            sos.Insert(-1, new TargetRelLocAction(0, 0));
            sos.Insert(-1, waitSos);
            sos.Insert(-1, new FoundSOSAction(waitSos));
            Command.Register("sos", args => MacroManager.HotKeyPlay(sos));
            Core.AddHotkey("SOS", () => MacroManager.HotKeyPlay(sos));
            Command.Register("sit", OnCommand);

        }

        private static void OnCommand(string[] args)
        {
            foreach (Item item in WorldEx.FindItemsG(0x2006, i => i.Amount == 0x0096 || i.Amount == 0x004D || (i.Hue == 0 && i.Amount == 0x0010)))
                WorldEx.SendToClient(new RemoveObject(item));
        }

        private class Track : MacroAction
        {
            public bool Used { get; private set; }
            private readonly UseSkillAction skill = new UseSkillAction(38);
            public override bool Perform()
            {
                if (ConfigEx.GetElement(false, "FishingTracking") && (Used = !Used))
                    skill.Perform();
                return true;
            }
        }

        private class WaitForTrack : WaitForGumpAction
        {
            private readonly Track track;
            public WaitForTrack(Track track)
            {
                m_Timeout = TimeSpan.FromSeconds(5);
                this.track = track;
            }

            public override bool Perform() { return !ConfigEx.GetElement(false, "FishingTracking") || !track.Used || !PerformWait(); }
        }

        private class TrackResponse : MacroAction
        {
            private readonly Track track;
            public TrackResponse(Track track) { this.track = track; }
            public override bool Perform()
            {
                if (!ConfigEx.GetElement(false, "FishingTracking") || !track.Used)
                    return true;
                ClientCommunication.SendToClient(new CloseGump(World.Player.CurrentGumpI));
                ClientCommunication.SendToServer(new GumpResponse(World.Player.CurrentGumpS, World.Player.CurrentGumpI, 1, new int[0], new GumpTextEntry[0]));
                World.Player.HasGump = false;
                return true;
            }
        }

        private class FoundSOSAction : MacroAction
        {
            private readonly WaitForMsg wait;
            public FoundSOSAction(WaitForMsg wait) { this.wait = wait; }
            public override bool Perform()
            {
                if (wait.Last == 501747)
                    Parent.Stop();
                return true;
            }
        }

        private class AttackClosestAction : MacroAction
        {
            public override bool Perform()
            {
                Mobile closest = TargetingEx.GetClosest(3, 4);
                if (closest != null)
                    WorldEx.SendToServer(new AttackReq(closest.Serial));
                return true;
            }
        }

        private class SailAction : MacroWaitAction
        {
            private static readonly Packet forward = new EncodedMessage("forward", 0x45);
            private static readonly Packet stop = new EncodedMessage("stop", 0x69);
            private readonly WaitForMsg wait;
            private Point3D position;

            public SailAction(WaitForMsg wait)
            {
                this.wait = wait;
                m_Timeout = TimeSpan.FromMinutes(1);
            }

            public override bool Perform()
            {
                if (wait.Last != 503172)
                    return true;
                position = World.Player.Position;
                WorldEx.SendToServer(forward);
                return !PerformWait();
            }

            public override bool PerformWait()
            {
                if (Utility.Distance(position, World.Player.Position) < 5)
                    return true;
                WorldEx.SendToServer(stop);
                return false;
            }
        }

        private class UseFishingPole : MacroAction
        {
            private readonly WaitForMsg wait;
            public UseFishingPole(WaitForMsg wait) { this.wait = wait; }
            public override bool Perform()
            {
                Targeting.CancelTarget();
                Item item = World.Player.GetItemOnLayer(Layer.RightHand);
                if (item == null || item.ItemID != 0x0DC0)
                    item = World.Player.Backpack.FindItem(0x0DC0);
                if (item != null)
                    WorldEx.SendToServer(new DoubleClick(item.Serial));
                wait.Recieved = false;
                return true;
            }
        }
    }
}