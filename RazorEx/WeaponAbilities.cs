using Assistant;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace RazorEx
{
    public static class WeaponAbilities
    {
        private static readonly Dictionary<ushort, Tuple<int, int>> moves = new Dictionary<ushort, Tuple<int, int>>();
        public static void OnInit()
        {
            using (Stream stream = typeof(WeaponAbilities).Assembly.GetManifestResourceStream("RazorEx.WeaponAbilities.xml"))
                foreach (XElement element in XDocument.Load(stream).Root.Elements().Elements())
                {
                    ushort graphic = Convert.ToUInt16(element.Value.Substring(2), 0x10);
                    int primary = (int)Enum.Parse(typeof(WeaponAbility), element.Attribute("primary").Value);
                    int secondary = (int)Enum.Parse(typeof(WeaponAbility), element.Attribute("secondary").Value);
                    moves.Add(graphic, new Tuple<int, int>(primary, secondary));
                }

            PacketHandler.RegisterServerToClientFilter(0x2E, OnEquip);
            Event.RemoveObject += Event_RemoveObject;
        }

        private static void Event_RemoveObject(Serial serial)
        {
            if (serial == current)
            {
                current = Serial.Zero;
                if (weaponSwitched != null)
                    weaponSwitched();
            }
        }

        private static void OnEquip(Packet p, PacketHandlerEventArgs args)
        {
            p.ReadInt32();
            p.ReadUInt16();
            ushort layer = p.ReadUInt16();
            uint serial = p.ReadUInt32();
            if (serial == World.Player.Serial && (layer == 1 || layer == 2))
            {
                Serial weapon = CurrentWeapon == null ? Serial.Zero : CurrentWeapon.Serial;
                if (current != weapon)
                {
                    current = weapon;
                    if (weaponSwitched != null)
                        weaponSwitched();
                }
            }
        }

        private static Action weaponSwitched;
        public static event Action WeaponSwitched
        {
            add { weaponSwitched += value; }
            remove { weaponSwitched -= value; }
        }

        private static Serial current = Serial.Zero;
        public static int Primary { get { return moves[CurrentID].Item1; } }
        public static int Secondary { get { return moves[CurrentID].Item2; } }
        private static ItemID CurrentID { get { return CurrentWeapon == null ? (ItemID)0x0000 : CurrentWeapon.ItemID; } }
        private static Item CurrentWeapon
        {
            get
            {
                if (World.Player != null)
                {
                    Item right = World.Player.GetItemOnLayer(Layer.RightHand);
                    Item left = World.Player.GetItemOnLayer(Layer.LeftHand);
                    if (right != null)
                        return right;
                    if (left != null && !left.IsShield())
                        return left;
                }
                return null;
            }
        }
    }

    public enum WeaponAbility
    {
        ArmorIgnore = 1,
        BleedAttack,
        ConcussionBlow,
        CrushingBlow,
        Disarm,
        Dismount,
        DoubleStrike,
        InfectiousStrike,
        MortalStrike,
        MovingShot,
        ParalyzingBlow,
        ShadowStrike,
        WhirlwindAttack,
        RidingSwipe,
        FrenziedWhirlwind,
        Block,
        DefenseMastery,
        NerveStrike,
        TalonStrike,
        Feint,
        DualWield,
        DoubleShot,
        ArmorPierce,
        Bladeweave,
        ForceArrow,
        LightningArrow,
        PsychicAttack,
        SerpentArrow,
        ForceOfNature
    }
}