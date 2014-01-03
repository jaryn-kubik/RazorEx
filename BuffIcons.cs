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
            PacketHandler.RegisterServerToClientViewer(0xC0, OnEffect);
            PacketHandler.RegisterServerToClientViewer(0x1C, OnASCIIMessage);
            PacketHandler.RegisterServerToClientViewer(0xAE, OnUnicodeMessage);
            MainFormEx.Disconnected += buffs.Clear;
        }

        private static void OnASCIIMessage(PacketReader p, PacketHandlerEventArgs args)
        {
            p.Seek(6, SeekOrigin.Current);
            if (p.ReadByte() == 0 && p.ReadUInt16() == 0x00C1 && p.ReadUInt16() == 3)
            {
                p.Seek(30, SeekOrigin.Current);
                if (p.ReadStringSafe() == "Silence on " + World.Player.Name + "!")
                {
                    RemoveBuff(BuffIcon.Silence);
                    AddBuff(BuffIcon.Silence, -1, -1, string.Empty, 30);
                }
            }
        }

        private static void OnUnicodeMessage(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            ItemID itemID = p.ReadUInt16();
            byte mode = p.ReadByte();
            ushort color = p.ReadUInt16();
            ushort font = p.ReadUInt16();
            string lang = p.ReadStringSafe(4);
            string name = p.ReadStringSafe(30);
            string text = p.ReadUnicodeStringSafe();

            if (serial == 0xFFFFFFFF && itemID == 0xFFFF && mode == 0 && color == 0x03B2 && font == 3 &&
                lang == "ENU" && name == "System" && text == "Silence faded")
                RemoveBuff(BuffIcon.Silence);
            else if (serial == World.Player.Serial && mode == 2 && color == 0x0225 && font == 3 &&
                     lang == "ENU" && text.StartsWith("Flames of revenge"))
            {
                flamesTimer.Stop();
                RemoveBuff(BuffIcon.FlamesOfRevenge);
                AddBuff(BuffIcon.FlamesOfRevenge, -2, -2, text.Substring(21, text.Length - 22), 90);
                flamesTimer.Start();
            }
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

        private static void OnEffect(PacketReader p, PacketHandlerEventArgs args)
        {
            if (p.ReadByte() != 3 || p.ReadUInt32() != World.Player.Serial || p.ReadUInt32() != 0 || p.ReadUInt16() != 0x3779)
                return;

            p.Seek(10, SeekOrigin.Current);
            if (p.ReadByte() != 1 || p.ReadByte() != 30)
                return;

            ushort duration = 11;
            Item item = World.Player.GetItemOnLayer(Layer.Arms);
            if (item != null)
            {
                ArrayList content = item.ObjPropList.m_Content;
                if (content.Count > 0)
                {
                    ObjectPropertyList.OPLEntry entry = ((ObjectPropertyList.OPLEntry)content[content.Count - 1]);
                    if (entry.Number == 1042971 && entry.Args != null && entry.Args.Contains("Consecrate Weapon"))
                        duration += 20;
                }
            }

            consTimer.Stop();
            RemoveBuff(BuffIcon.ConsecrateWeapon);
            AddBuff(BuffIcon.ConsecrateWeapon, 1060587, 1060587, string.Empty, duration);
            consTimer.Delay = TimeSpan.FromSeconds(duration);
            consTimer.Start();
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
            if (primaryCliloc == -1)
                Title = "Silence";
            else if (primaryCliloc == -2)
                Title = "Flames of Revenge";
            else
                Title = Language.GetCliloc(primaryCliloc);

            if (secondaryCliloc == -1)
                Info = "Silence";
            else if (secondaryCliloc == -2)
                Info = args;
            else
                Info = Language.ClilocFormat(secondaryCliloc, args).Replace("<br>", "\n").Trim();

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
        FlamesOfRevenge
    }
}