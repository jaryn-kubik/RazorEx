using Assistant;
using RazorEx.Addons;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RazorEx.UI
{
    public class SpellIcon : InGameWindow
    {
        private readonly Spell spell;
        private readonly ArtBox gump;
        protected override bool Borderless { get { return true; } }

        private SpellIcon(Spell spell)
        {
            this.spell = spell;
            table.Controls.Add(gump = new ArtBox((ushort)GetID(), 0, ConfigEx.GetElement(false, "SmallSpellIcon") ? 35 : -1, true));
            Location = new Point(ConfigEx.GetAttribute(Location.X, "locX", "Spells", "Spell_" + spell.GetID()),
                                 ConfigEx.GetAttribute(Location.Y, "locY", "Spells", "Spell_" + spell.GetID()));
            if (IsUsable)
            {
                WeaponMoves.Changed += SetGump;
                Event.LocalizedMessage += Event_LocalizedMessage;
                if (IsPrimaryAbility || IsSecondaryAbility)
                    WeaponAbilities.WeaponSwitched += SetGump;
            }
            if (spell.GetID() == 403)//evasion
                Event.LocalizedMessage += OnEvasion;
        }

        private bool IsPrimaryAbility { get { return spell.Circle == 100 && spell.Number == 0; } }
        private bool IsSecondaryAbility { get { return spell.Circle == 100 && spell.Number == 1; } }
        private bool IsUsable { get { return IsPrimaryAbility || IsSecondaryAbility || Enum.IsDefined(typeof(MoveType), spell.GetID()); } }
        private bool InUse
        {
            get
            {
                return (int)WeaponMoves.Current < 3
                           ? ((WeaponMoves.Current == MoveType.PrimaryAbility && IsPrimaryAbility) ||
                              (WeaponMoves.Current == MoveType.SecondaryAbility && IsSecondaryAbility))
                           : (int)WeaponMoves.Current == spell.GetID();
            }
        }

        private bool? OnEvasion(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, int num, string name, string args)
        {
            if (num == 1063120)
                gump.Set((ushort)GetID(), 0x0021);
            else if (num == 1063121)
                gump.Set((ushort)GetID());
            return null;
        }

        private bool? Event_LocalizedMessage(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, int num, string name, string args)
        {
            if (InUse && Array.IndexOf(msgs, num) != -1)
            {
                gump.Set((ushort)GetID(), 0x0017);
                Assistant.Timer.DelayedCallback(TimeSpan.FromMilliseconds(100), SetGump).Start();
            }
            return null;
        }

        private void SetGump()
        {
            if (InUse)
                gump.Set((ushort)GetID(), 0x0021);
            else
                gump.Set((ushort)GetID());
        }

        private int GetID()
        {
            switch (spell.Circle)
            {
                case 10:
                    return 0x4FFF + spell.Number;
                case 20:
                    return 0x50FF + spell.Number;
                case 40:
                    return 0x541F + spell.Number;
                case 50:
                    return 0x531F + spell.Number;
                case 60:
                    return 0x59D7 + spell.Number;
                case 100:
                    return 0x51FF + (spell.Number == 0 ? WeaponAbilities.Primary : WeaponAbilities.Secondary);
                default:
                    return 0x08BF + spell.GetID();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                Targeting.CancelTarget();
                OnUse();
                Targeting.LastTarget(true);
            }
            else
                base.OnMouseClick(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                OnUse();
            else
                base.OnMouseDoubleClick(e);
        }

        private void OnUse()
        {
            if (spell.Circle != 100)
            {
                KeyData key = HotKey.Get(spell.Name);
                if (key == null)
                    spell.OnCast(new CastSpellFromMacro((ushort)spell.GetID()));
                else
                    key.Callback();
                OpenEUO.SetAsync("LSpell", spell.GetID() - 1);
            }
            else
                OpenEUO.CallAsync("Macro", spell.Number == 0 ? 35 : 36, 0);
        }

        protected override void Dispose(bool disposing)
        {
            WeaponMoves.Changed -= SetGump;
            Event.LocalizedMessage -= Event_LocalizedMessage;
            WeaponAbilities.WeaponSwitched -= SetGump;
            base.Dispose(disposing);
        }

        protected override void Save()
        {
            ConfigEx.SetAttribute(Location.X, "locX", "Spells", "Spell_" + spell.GetID());
            ConfigEx.SetAttribute(Location.Y, "locY", "Spells", "Spell_" + spell.GetID());
        }

        protected override void OnMouseRightClick()
        {
            ConfigEx.GetXElement(false, "Spells", "Spell_" + spell.GetID()).Remove();
            base.OnMouseRightClick();
        }

        public new static void OnInit()
        {
            MainFormEx.Connected += MainFormEx_Connected;
            ConfigAgent.AddItem(false, "SmallSpellIcon");
            Command.Register("spell", OnCommand);
        }

        private static void MainFormEx_Connected()
        {
            foreach (XElement element in ConfigEx.GetXElement(true, "Spells").Elements())
            {
                int id;
                if (int.TryParse(element.Name.ToString().Substring(6), out id))
                {
                    if (id < 1000)
                        new SpellIcon(Spell.Get(id)).Show();
                    else
                        new SpellIcon(new Spell((char)Spell.SpellFlag.None, id - 1000, 100, null, null)).Show();
                }
            }
        }

        private static void OnCommand(string[] args)
        {
            try
            {
                new SpellIcon(int.Parse(args[0]) == 100
                                  ? new Spell((char)Spell.SpellFlag.None, int.Parse(args[1]), 100, null, null)
                                  : Spell.Get(int.Parse(args[0]), int.Parse(args[1]))).ShowUnlocked();
            }
            catch (Exception) { WorldEx.SendMessage("Invalid spell!"); }
        }

        private static readonly int[] msgs =
        {
            1060076, // Your attack penetrates their armor!
            1063350, // You pierce your opponent's armor!
            1060159, // Your target is bleeding!
            1063345, // You block an attack!
            1060165, // You have delivered a concussion!
            1060090, // You have delivered a crushing blow!
            1063353, // You perform a masterful defense!
            1060092, // You disarm their weapon!
            1060082, // The force of your attack has dislodged them from their mount!
            1063348, // You launch two shots at once!
            1060084, // You attack with lightning speed!
            1063362, // You dually wield for increased speed!
            1063360, // You baffle your target with a feint!
            1060080, // Your precise strike has increased the level of the poison by 1
            1008096, // You have poisoned your target
            1060086, // You deliver a mortal wound!
            1060216, // Your shot was successful
            1063356, // You cripple your target with a nerve strike!
            1060163, // You deliver a paralyzing blow!
            1060078, // You strike and hide in the shadows!
            1063358, // You deliver a talon strike!
            1060161, // The whirling attack strikes a target!

            1063168, // You attack with lightning precision!
            1063171, // You transfer the momentum of your weapon into another enemy!
            1063098, // You focus all of your abilities and strike with deadly force!
            1063094, // You inflict a Death Strike upon your opponent!
            1063100, // Your quick flight to your target causes extra damage as you strike!
            1063129, // You catch your opponent off guard with your Surprise Attack!
            1063090 // You quickly stab your opponent as you come out of hiding!
        };
    }
}