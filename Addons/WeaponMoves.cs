using System;
using System.Linq;
using Assistant;
using Assistant.Macros;

namespace RazorEx.Addons
{
    public static class WeaponMoves
    {
        private static Action changed;
        public static event Action Changed
        {
            add { changed += value; }
            remove { changed += value; }
        }

        private static MoveType current;
        public static MoveType Current
        {
            get { return current; }
            private set
            {
                if (current != value)
                {
                    current = value;
                    if (changed != null)
                        changed();
                }
            }
        }

        private static int manaReq = -1;

        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "WeaponMoves"); }
        private static void OnChange(bool enabled)
        {
            if (enabled)
            {
                WeaponAbilities.WeaponSwitched += WeaponAbilities_WeaponSwitched;
                Event.LocalizedMessage += Event_LocalizedMessage;
                PacketHandler.RegisterServerToClientViewer(0xBF, OnServer);
                PacketHandler.RegisterClientToServerViewer(0xD7, OnClientAbility);
                PacketHandler.RegisterClientToServerViewer(0xBF, OnClientMove);
                Event.MobileUpdated += Event_MobileUpdated;
                foreach (int spell in Enum.GetValues(typeof(MoveType)).Cast<int>().Where(spell => spell > 2))
                    HotKey.Get(Spell.Get(spell).Name).m_Callback = () => OnMove(spell);
            }
            else
            {
                WeaponAbilities.WeaponSwitched -= WeaponAbilities_WeaponSwitched;
                Event.LocalizedMessage -= Event_LocalizedMessage;
                PacketHandler.RemoveServerToClientViewer(0xBF, OnServer);
                PacketHandler.RemoveClientToServerViewer(0xD7, OnClientAbility);
                PacketHandler.RemoveClientToServerViewer(0xBF, OnClientMove);
                Event.MobileUpdated -= Event_MobileUpdated;
                foreach (int spell in Enum.GetValues(typeof(MoveType)).Cast<int>().Where(spell => spell > 2))
                    HotKey.Get(Spell.Get(spell).Name).m_Callback = () => new ExtCastSpellAction(spell, Serial.Zero).Perform();
            }
        }

        private static void OnMove(int id)
        {
            Current = MoveType.None;
            new ExtCastSpellAction(id, Serial.Zero).Perform();
        }

        private static void OnClientAbility(PacketReader p, PacketHandlerEventArgs args)
        {
            p.ReadInt32();
            if (p.ReadInt16() == 0x19)
            {
                p.ReadByte();
                int ability = p.ReadInt32();
                manaReq = -1;
                if (ability == WeaponAbilities.Primary)
                    Current = MoveType.PrimaryAbility;
                else if (ability == WeaponAbilities.Secondary)
                    Current = MoveType.SecondaryAbility;
                else
                    Current = MoveType.None;
            }
        }

        private static void OnClientMove(PacketReader p, PacketHandlerEventArgs args)
        {
            if (p.ReadInt16() == 0x1C && p.ReadInt16() == 0x02 && Enum.IsDefined(typeof(MoveType), (int)p.ReadInt16()))
                Current = MoveType.None;
        }

        private static void OnServer(PacketReader p, PacketHandlerEventArgs args)
        {
            short id = p.ReadInt16();
            if (id == 0x21)
            {
                args.Block = true;
                if (manaReq != -1)
                    return;
                if (Current == MoveType.PrimaryAbility)
                    WorldEx.SendToServer(new SetAbility(WeaponAbilities.Primary));
                else if (Current == MoveType.SecondaryAbility)
                    WorldEx.SendToServer(new SetAbility(WeaponAbilities.Secondary));
                else
                    args.Block = false;
            }
            else if (id == 0x25)
            {
                int move = p.ReadUInt16();
                if (!Enum.IsDefined(typeof(MoveType), move))
                    return;
                byte use = p.ReadByte();
                if (use == 1)
                    Current = (MoveType)move;
                else if (use == 0 && (int)Current == move)
                    args.Block = manaReq != -1 || new ExtCastSpellAction(move, Serial.Zero).Perform();
            }
        }

        private static void Event_LocalizedMessage(Serial serial, int msg, string args)
        {
            if (msg == 1060181) // You need ~1~ mana to perform that attack
                manaReq = int.Parse(args.Trim());
            else if (Array.IndexOf(msgs, msg) != -1)
                Current = MoveType.None;
        }

        private static void Event_MobileUpdated(Serial serial)
        {
            if (manaReq != -1 && World.Player.Mana >= manaReq && serial == World.Player.Serial)
            {
                manaReq = -1;
                if (Current == MoveType.PrimaryAbility)
                    WorldEx.SendToServer(new SetAbility(WeaponAbilities.Primary));
                else if (Current == MoveType.SecondaryAbility)
                    WorldEx.SendToServer(new SetAbility(WeaponAbilities.Secondary));
                else if (Current != MoveType.None)
                    new ExtCastSpellAction((int)Current, Serial.Zero).Perform();
            }
        }

        private static void WeaponAbilities_WeaponSwitched()
        {
            WorldEx.SendToClient(new ClearAbility());
            if ((int)Current > 2)
                new ExtCastSpellAction((int)Current, Serial.Zero).Perform();
            Current = MoveType.None;
        }

        private static readonly int[] msgs =
        {
            1079308, // You need ~1~ weapon and tactics skill to perform that attack
            1060182, // You need ~1~ weapon skill to perform that attack
            1063347, // You need ~1~ Bushido or Ninjitsu skill to perform that attack!
            1063352, // You need ~1~ Ninjitsu skill to perform that attack!
            1070768, // You need ~1~ Bushido skill to perform that attack!
            1060183, // You lack the required stealth to perform that attack
            1063024, // You cannot perform this special move right now.
            1063013, // You need at least ~1~ ~2~ skill to use that ability.
            1061812, // Disarm - You lack the required skill in armslore to perform that attack!
            1061283, // Dismount - You cannot perform that attack while mounted!
            1070770, // DoubleShot - You can only execute this attack while mounted!
            1061811, // ParalyzingBlow - You lack the required anatomy skill to perform that attack!
            1063096, // FocusAttack - You cannot use this ability while holding a shield.
            1063097, // FocusAttack - You must be wielding a melee weapon without a shield to use this ability.
            1063127, // KiAttack - You cannot use this ability while in stealth mode.
            1075858, // KiAttack - You can only use this with melee attacks.
            1063087 // SurpriseAttack/Backstab - You must be in stealth mode to use this ability.
        };
    }

    public enum MoveType
    {
        None,
        PrimaryAbility,
        SecondaryAbility,
        HonorableExecution = 0x0191, //Bushido
        LightningStrike = 0x0195,
        MomentumStrike = 0x0196,
        FocusAttack = 0x01F5, //Ninjitsu
        DeathStrike = 0x01F6,
        KiAttack = 0x01F8,
        SurpriseAttack = 0x01F9,
        Backstab = 0x01FA
    }
}