using Assistant;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RazorEx.Skills
{
    public static class FishingSOS
    {
        public static Dictionary<Serial, SOSInfo> List { get { return list; } }
        private static readonly Dictionary<Serial, SOSInfo> list = new Dictionary<Serial, SOSInfo>();
        private static Serial lastSerial;
        private static Serial lastContainer;
        private static int lastTime;

        public static void OnInit()
        {
            Command.Register("sosadd", args => Targeting.OneTimeTarget(OnAdd));
            Command.Register("sosload", args => MoveNext());
            Command.Register("sosclear", OnClear);
            Command.Register("sosclean", OnClean);
            Command.Register("sosstats", OnStats);
            Command.Register("sosexport", OnExport);
            Command.Register("sosget", OnGet);
            Command.Register("sosnear", OnNear);

            PacketHandler.RegisterServerToClientViewer(0x25, ContainerContentUpdate);
            Fixes.SOS.OnGump += SOS_OnGump;

            foreach (string[] line in ConfigEx.LoadCfg("sos", 3))
            {
                SOSInfo info = new SOSInfo { Location = Point3D.Parse(line[2]) };
                if (!string.IsNullOrEmpty(line[1]))
                    info.Felucca = bool.Parse(line[1]);
                list.Add(Serial.Parse(line[0]), info);
            }
        }

        private static void OnNear(string[] args)
        {
            Serial closest = Serial.Zero;
            foreach (Item sos in World.Player.Backpack.FindItems(0x14ED))
            {
                SOSInfo info;
                if (list.TryGetValue(sos.Serial, out info) && (!list.ContainsKey(closest) || info.Distance(World.Player.Position) < list[closest].Distance(World.Player.Position)))
                    closest = sos.Serial;
            }

            if (World.Items.ContainsKey(closest))
                WorldEx.SendToServer(new DoubleClick(closest));
        }

        private static void OnGet(string[] args)
        {
            bool felucca;
            int count;
            try
            {
                felucca = Convert.ToBoolean(byte.Parse(args[0]));
                count = byte.Parse(args[1]);
            }
            catch { return; }

            Serial closest = Serial.Zero;
            foreach (KeyValuePair<Serial, SOSInfo> sos in list)
                if (sos.Value.Felucca == felucca && (!list.ContainsKey(closest) || sos.Value.Distance() < list[closest].Distance()))
                    closest = sos.Key;
            if (!list.ContainsKey(closest))
                return;

            List<Serial> chosen = new List<Serial> { closest };
            List<Serial> toAdd = GetInRange(list[closest].Location, felucca).Where(s => !chosen.Contains(s)).ToList();
            while (toAdd.Count > 0)
            {
                chosen.AddRange(toAdd);
                toAdd.Clear();
                foreach (Serial serial in chosen)
                    toAdd.AddRange(GetInRange(list[serial].Location, felucca).Where(s => !chosen.Contains(s) && !toAdd.Contains(s)));
            }

            while (chosen.Count > count)
            {
                Serial farest = Serial.Zero;
                foreach (Serial serial in chosen)
                    if (!list.ContainsKey(farest) || list[closest].Distance(list[farest].Location) < list[serial].Distance(list[farest].Location))
                        farest = serial;
                chosen.Remove(farest);
            }

            chosen.ForEach(s => DragDrop.Move(World.FindItem(s), Fixes.LootBag.Bag));
            WorldEx.SendMessage(chosen.Count + " SOS messages found.");
        }

        private static IEnumerable<Serial> GetInRange(Point3D location, bool felucca)
        { return list.Where(sos => sos.Value.Felucca == felucca && sos.Value.Distance(location) < 750).Select(sos => sos.Key); }

        private static void OnClear(string[] args)
        {
            list.Clear();
            Save();
        }

        private static void OnClean(string[] args)
        {
            foreach (Serial serial in list.Keys.Where(s => !World.Items.ContainsKey(s)).ToArray())
                list.Remove(serial);
            Save();
        }

        private static void OnStats(string[] args)
        {
            WorldEx.SendMessage(list.Count(s => s.Value.Felucca == true) + " sos messages on Felucca.");
            WorldEx.SendMessage(list.Count(s => s.Value.Felucca == false) + " sos messages on Trammel.");
            WorldEx.SendMessage(list.Count(s => s.Value.Felucca == null) + " sos messages Unknown.");
        }

        private static void SOS_OnGump(int x, int y)
        {
            if (Environment.TickCount - lastTime < 5000)
            {
                list[lastSerial].Location = new Point3D(x, y, 0);
                Item item = World.FindItem(lastSerial);
                if (item != null && item.ObjPropList.m_Content.Count == 3)
                {
                    ObjectPropertyList.OPLEntry entry = (ObjectPropertyList.OPLEntry)item.ObjPropList.m_Content[2];
                    if (entry.Number == 1042971)
                    {
                        if (entry.Args == "Trammel")
                            list[lastSerial].Felucca = false;
                        else if (entry.Args == "Felucca")
                            list[lastSerial].Felucca = true;
                    }
                }
                DragDrop.Move(World.FindItem(lastSerial), World.FindItem(lastContainer));
                Save();
                MoveNext();
            }
        }

        private static void ContainerContentUpdate(PacketReader p, PacketHandlerEventArgs args)
        {
            Serial serial = p.ReadUInt32();
            p.ReadUInt16();
            p.Seek(7, SeekOrigin.Current);
            Serial container = p.ReadUInt32();
            if (container == World.Player.Backpack.Serial && serial == lastSerial && (Environment.TickCount - lastTime) < 5000)
                Timer.DelayedCallback(TimeSpan.FromSeconds(0.5), () => WorldEx.SendToServer(new DoubleClick(serial))).Start();
        }

        private static void MoveNext()
        {
            foreach (KeyValuePair<Serial, SOSInfo> sos in list)
            {
                Item item = World.FindItem(sos.Key);
                if (item != null && (sos.Value.Location == Point3D.Zero || !sos.Value.Felucca.HasValue))
                {
                    lastSerial = item.Serial;
                    lastTime = Environment.TickCount;
                    Item cont = item.Container as Item;
                    if (cont == null)
                        continue;
                    lastContainer = cont.Serial;
                    DragDrop.Move(item, World.Player.Backpack);
                    return;
                }
            }
        }

        private static void OnAdd(bool ground, Serial serial, Point3D p, ushort gfxid)
        {
            Item container = World.FindItem(serial);
            if (container == null || !container.IsContainer)
                return;
            foreach (Item sos in container.FindItems(0x14ED).Where(sos => !list.ContainsKey(sos.Serial)))
                list.Add(sos.Serial, new SOSInfo());
            Save();
        }

        private static void OnExport(string[] args)
        {
            using (StreamWriter stream = new StreamWriter(ConfigEx.GetPath("sos.map"), false))
            {
                stream.WriteLine(3);
                foreach (SOSInfo sos in list.Values.Where(s => s.Felucca.HasValue))
                    stream.WriteLine("+sos: {0} {1} {2} sos", sos.Location.X, sos.Location.Y, sos.Felucca == true ? 1 : 2);
            }
        }

        private static void Save()
        {
            using (StreamWriter stream = new StreamWriter(ConfigEx.GetPath("sos"), false))
                foreach (KeyValuePair<Serial, SOSInfo> sos in list)
                    stream.WriteLine("{0};{1};{2}", sos.Key, sos.Value.Felucca.HasValue ? sos.Value.Felucca.ToString() : "", sos.Value.Location);
        }

        public class SOSInfo
        {
            public Point3D Location { get; set; }
            public bool? Felucca { get; set; }
            public int Distance(Point3D location = default(Point3D)) { return Utility.Distance(Location, location); }
        }
    }
}