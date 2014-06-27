using Assistant;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RazorEx.Fixes
{
    public static class ItemProps
    {
        public static void OnInit()
        {
            Command.Register("props", args => Targeting.OneTimeTarget(OnCommand));
            Command.Register("props2", args => Targeting.OneTimeTarget(OnCommand2));
            ConfigAgent.AddItem<byte>(25, "MinDurability");
            PacketHandler.RegisterServerToClientViewer(0xD6, OnProps);
        }

        private static void OnProps(PacketReader p, PacketHandlerEventArgs args)
        {
            try
            {
                p.Seek(5, SeekOrigin.Begin);
                Item item = World.FindItem(p.ReadUInt32());
                if (item == null)
                    return;

                item.ReadPropertyList(p);
                if (item.RootContainer != World.Player || item.IsInBank)
                    return;
                ObjectPropertyList.OPLEntry opl = item.ObjPropList.m_Content.OfType<ObjectPropertyList.OPLEntry>().FirstOrDefault(o => o.Number == 1060639);
                if (opl == null)
                    return;

                string str = opl.Args.Trim();
                int start, end;
                while ((start = str.IndexOf('<')) != -1 && (end = str.IndexOf('>')) != -1)
                    str = str.Remove(start, end - start + 1).Trim();
                string[] durab = str.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                double current, max;
                if (double.TryParse(durab[0], out current) && double.TryParse(durab[1], out max) &&
                    (((current / max) * 100) < ConfigEx.GetElement(25, "MinDurability") && current < max))
                {
                    opl = (ObjectPropertyList.OPLEntry)item.ObjPropList.m_Content[0];
                    string[] name = opl.Args.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    WorldEx.SendMessage(string.Format("!{0} {1}/{2}!", FormatString(name.Last()), current, max));
                }
            }
            catch {}
        }

        private static void OnCommand(bool location, Serial serial, Point3D p, ushort gfxid)
        {
            UOEntity item = WorldEx.GetEntity(serial);
            if (item == null)
                return;
            StringBuilder sb = new StringBuilder();
            foreach (ObjectPropertyList.OPLEntry opl in item.ObjPropList.m_Content)
                sb.AppendLine(FormatString(string.IsNullOrEmpty(opl.Args)
                                               ? Language.GetCliloc(opl.Number)
                                               : Language.ClilocFormat(opl.Number, opl.Args)));
            new MessageDialog("Props", sb.ToString()) { TopMost = true }.Show(Engine.MainWindow);
        }

        private static void OnCommand2(bool location, Serial serial, Point3D p, ushort gfxid)
        {
            UOEntity item = WorldEx.GetEntity(serial);
            if (item == null)
                return;
            StringBuilder sb = new StringBuilder();
            foreach (ObjectPropertyList.OPLEntry opl in item.ObjPropList.m_Content)
                sb.AppendLine(string.Format("{0} - {1}", opl.Number, string.IsNullOrEmpty(opl.Args) ? string.Empty : opl.Args.Trim()));
            new MessageDialog("Props", sb.ToString()) { TopMost = true }.Show(Engine.MainWindow);
        }

        private static string FormatString(string str)
        {
            str = str.Trim();
            int start, end;
            while ((start = str.IndexOf('<')) != -1 && (end = str.IndexOf('>')) != -1)
                str = str.Remove(start, end - start + 1).Trim();
            if (str.StartsWith("#"))
            {
                int space = str.IndexOf(' ');
                if (space == -1)
                    str = Language.GetCliloc(int.Parse(str.Substring(1)));
                else
                    str = Language.GetCliloc(int.Parse(str.Substring(1, space - 1))) + str.Substring(space);
            }
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str.Trim());
        }
    }
}