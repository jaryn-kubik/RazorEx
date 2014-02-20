using Assistant;
using RazorEx.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RazorEx
{
    public static class BuffIcons
    {
        private static readonly Dictionary<BuffIcon, ushort> buffIcons = new Dictionary<BuffIcon, ushort>();
        private static Dictionary<ushort, BuffIcon> gumpIDs;
        private static readonly Dictionary<BuffIcon, BuffInfo> buffs = new Dictionary<BuffIcon, BuffInfo>();
        private static readonly Timer consTimer = Timer.DelayedCallback(TimeSpan.FromSeconds(11), () => RemoveBuff(BuffIcon.ConsecrateWeapon));
        private static readonly Timer flamesTimer = Timer.DelayedCallback(TimeSpan.FromSeconds(90), () => RemoveBuff(BuffIcon.FlamesOfRevenge));
        private static readonly Timer hammerTimer = Timer.DelayedCallback(TimeSpan.FromSeconds(35), () => RemoveBuff(BuffIcon.BlessedHammer));

        public static void OnInit()
        {
            using (Stream stream = typeof(WeaponAbilities).Assembly.GetManifestResourceStream("RazorEx.BuffIcons.xml"))
                foreach (XElement element in XDocument.Load(stream).Root.Elements())
                {
                    ushort gump = Convert.ToUInt16(element.Value.Substring(2), 0x10);
                    BuffIcon buff = (BuffIcon)Enum.Parse(typeof(BuffIcon), element.Name.ToString());
                    buffIcons.Add(buff, gump);
                }

            gumpIDs = buffIcons.ToDictionary(x => x.Value, x => x.Key);
            PacketHandler.RegisterServerToClientFilter(0xDF, OnBuff);
            Event.HuedEffect += Event_HuedEffect;
            Event.MobileUpdated += Event_MobileUpdated;
            Event.ASCIIMessage += Event_ASCIIMessage;
            Event.UnicodeMessage += Event_UnicodeMessage;
            MainFormEx.Disconnected += buffs.Clear;
        }

        private static void Event_MobileUpdated(Serial obj)
        {
            if (obj != World.Player.Serial || World.Player.Hue != 0x07A8)
                return;
            if (!buffs.ContainsKey(BuffIcon.WanderingPlague))
                AddBuff(BuffIcon.WanderingPlague, "Wandering Plague", 125);
        }

        private static bool? Event_ASCIIMessage(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, string lang, string name, string msg)
        {
            if (type != 0 || font != 3)
                return null;
            if (hue == 0x00C1 && msg == "Silence on " + World.Player.Name + "!")
            {
                RemoveBuff(BuffIcon.Silence);
                AddBuff(BuffIcon.Silence, "Silence", 30);
            }
            else if (serial == World.Player.Serial && hue == 0x03B2 && msg == "Blessed Hammer")
            {
                hammerTimer.Stop();
                RemoveBuff(BuffIcon.BlessedHammer);
                AddBuff(BuffIcon.BlessedHammer, "Blessed Hammer", 35);
                hammerTimer.Start();
            }
            return null;
        }

        private static bool? Event_UnicodeMessage(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, string lang, string name, string msg)
        {
            if (serial == 0xFFFFFFFF && graphic == 0xFFFF && type == 0 && hue == 0x03B2 && font == 3 &&
                lang == "ENU" && name == "System")
            {
                if (msg == "Silence faded")
                    RemoveBuff(BuffIcon.Silence);
                else if (msg == "Wandering plague faded")
                    RemoveBuff(BuffIcon.WanderingPlague);
            }
            else if (serial == World.Player.Serial && type == 2 && hue == 0x0225 && font == 3 &&
                     msg.StartsWith("Flames of revenge", StringComparison.InvariantCultureIgnoreCase))
            {
                flamesTimer.Stop();
                RemoveBuff(BuffIcon.FlamesOfRevenge);
                AddBuff(BuffIcon.FlamesOfRevenge, "Flames of Revenge", 90);
                flamesTimer.Start();
            }
            return null;
        }

        public static ushort GetGumpID(BuffIcon buffID) { return buffIcons[buffID]; }
        public static BuffInfo GetBuffByGump(ushort gumpID) { return buffs[gumpIDs[gumpID]]; }

        private static void RemoveBuff(BuffIcon buffID)
        {
            if (buffs.ContainsKey(buffID))
            {
                buffs.Remove(buffID);
                if (removed != null)
                    removed(buffID);
            }
        }

        private static void AddBuff(BuffIcon buffID, int primaryCliloc, int secondaryCliloc, string args, ushort duration)
        {
            BuffInfo buff = new BuffInfo(primaryCliloc, secondaryCliloc, args, duration);
            buffs.Add(buffID, buff);
            if (added != null)
                added(buffID, buff);
        }

        private static void AddBuff(BuffIcon buffID, string title, ushort duration)
        {
            BuffInfo buff = new BuffInfo(title, duration);
            buffs.Add(buffID, buff);
            if (added != null)
                added(buffID, buff);
        }

        private static bool? Event_HuedEffect(byte type, Serial src, Serial dest, ItemID itemID, byte speed, byte count, uint hue, uint mode)
        {
            if (type != 3 || src != World.Player.Serial || dest != 0 ||
                itemID != 0x3779 || speed != 1 || count != 30)
                return null;

            ushort duration = 11;
            Item item = World.Player.GetItemOnLayer(Layer.Arms);
            if (item != null)
            {
                ArrayList content = item.ObjPropList.m_Content;
                if (content.Count > 0)
                {
                    ObjectPropertyList.OPLEntry entry =
                        ((ObjectPropertyList.OPLEntry)content[content.Count - 1]);
                    if (entry.Number == 1042971 && entry.Args != null &&
                        entry.Args.Contains("Consecrate Weapon"))
                        duration += 20;
                }
            }

            consTimer.Stop();
            RemoveBuff(BuffIcon.ConsecrateWeapon);
            AddBuff(BuffIcon.ConsecrateWeapon, 1060587, 1060587, string.Empty, duration);
            consTimer.Delay = TimeSpan.FromSeconds(duration);
            consTimer.Start();
            return null;
        }

        private static void OnBuff(Packet p, PacketHandlerEventArgs args)
        {
            p.Seek(7, SeekOrigin.Begin);
            BuffIcon id = (BuffIcon)p.ReadUInt16();
            ushort count = p.ReadUInt16();
            if (count != 0)
            {
                p.Seek(12, SeekOrigin.Current);
                ushort duration = p.ReadUInt16();
                p.Seek(3, SeekOrigin.Current);
                int primaryCliloc = p.ReadInt32();
                int secondaryCliloc = p.ReadInt32();
                p.Seek(4, SeekOrigin.Current);
                string argstr = string.Empty;
                if (p.ReadUInt16() == 1)
                {
                    p.Seek(4, SeekOrigin.Current);
                    argstr = p.ReadUnicodeStringLE();
                }

                AddBuff(id, primaryCliloc, secondaryCliloc, argstr, duration);
            }
            else
                RemoveBuff(id);
        }

        private static Action<BuffIcon, BuffInfo> added;
        public static event Action<BuffIcon, BuffInfo> Added
        {
            add { added += value; }
            remove { added -= value; }
        }

        private static Action<BuffIcon> removed;
        public static event Action<BuffIcon> Removed
        {
            add { removed += value; }
            remove { removed -= value; }
        }
    }

    public class BuffInfo
    {
        public string Title { get; private set; }
        public int Duration { get { return end == default(DateTime) ? 0 : (int)end.Subtract(DateTime.Now).TotalSeconds; } }
        public string Info { get; private set; }
        private readonly DateTime end;

        public BuffInfo(int primaryCliloc, int secondaryCliloc, string args, ushort duration)
        {
            Title = Language.GetCliloc(primaryCliloc);
            Info = Language.ClilocFormat(secondaryCliloc, args).Replace("<br>", "\n").Trim();
            if (duration != 0)
                end = DateTime.Now.AddSeconds(duration);
        }

        public BuffInfo(string title, ushort duration)
        {
            Title = Info = title;
            if (duration != 0)
                end = DateTime.Now.AddSeconds(duration);
        }
    }

    public enum BuffIcon : ushort
    {
        DismountPrevention = 0x3E9,
        NoRearm = 0x3EA,
        NightSight = 0x3ED,
        DeathStrike,
        EvilOmen,
        UnknownStandingSwirl,
        UnknownKneelingSword,
        DivineFury,
        EnemyOfOne,
        HidingAndOrStealth,
        ActiveMeditation,
        BloodOathCaster,
        BloodOathCurse,
        CorpseSkin,
        Mindrot,
        PainSpike,
        Strangle,
        GiftOfRenewal,
        AttuneWeapon,
        Thunderstorm,
        EssenceOfWind,
        EtherealVoyage,
        GiftOfLife,
        ArcaneEmpowerment,
        MortalStrike,
        ReactiveArmor,
        Protection,
        ArchProtection,
        MagicReflection,
        Incognito,
        Disguised,
        AnimalForm,
        Polymorph,
        Invisibility,
        Paralyze,
        Poison,
        Bleed,
        Clumsy,
        FeebleMind,
        Weaken,
        Curse,
        MassCurse,
        Agility,
        Cunning,
        Strength,
        Bless,
        ConsecrateWeapon,
        Silence,
        FlamesOfRevenge,
        BlessedHammer,
        WanderingPlague
    }
}