using Assistant;
using System;
using System.Drawing;
using System.Windows.Forms;
using Ultima;

namespace RazorEx.UI
{
    public class Chat : InGameWindow
    {
        private static Chat instance;
        private readonly RichTextBoxEx textBox = new RichTextBoxEx();

        private Chat()
        {
            LabelEx label = new LabelEx("Chat", 12, style: FontStyle.Bold | FontStyle.Underline);
            CheckBox check = new CheckBox { AutoSize = true, Checked = textBox.AutoScroll, Dock = DockStyle.Fill };
            check.CheckedChanged += (s, e) => textBox.AutoScroll = check.Checked;
            table.Controls.Add(check, 0, 0);
            table.Controls.Add(label, 1, 0);
            table.SetColumnSpan(textBox, 2);
            table.Controls.Add(textBox, 0, 1);

            ConfigEx.SetAttribute(true, "enabled", "Chat");
            AutoSize = false;
            AutoSizeMode = AutoSizeMode.GrowOnly;
            Location = new Point(ConfigEx.GetAttribute(Location.X, "locX", "Chat"),
                                 ConfigEx.GetAttribute(Location.Y, "locY", "Chat"));
            Size = new Size(ConfigEx.GetAttribute(Size.Width, "sizeW", "Chat"),
                            ConfigEx.GetAttribute(Size.Height, "sizeH", "Chat"));

            PacketHandler.RegisterServerToClientFilter(0xAE, OnSpeech);
            PacketHandler.RegisterServerToClientFilter(0x1C, OnAsciiSpeech);
            PacketHandler.RegisterServerToClientViewer(0xBF, OnPartySpeech);
            instance = this;
            MainFormEx.Disconnected -= Close;
            MainFormEx.Disconnected += () =>
                                           {
                                               Hidden = true;
                                               Hide();
                                           };
            textBox.LinkClicked += textBox_LinkClicked;
        }

        private void textBox_LinkClicked(object s, LinkClickedEventArgs e)
        {
            WorldEx.SendToClient(new LaunchBrowser(e.LinkText));
            Engine.MainWindow.BeginInvoke((Action)(() => ChangeFocus(false, false)));
        }

        private void OnPartySpeech(PacketReader p, PacketHandlerEventArgs args)
        {
            if (p.ReadInt16() == 6 && p.ReadByte() == 4)
            {
                Mobile mobile = World.FindMobile(p.ReadUInt32());
                string text = p.ReadUnicodeStringSafe();
                textBox.AddLine(string.Format("[P] {0}: {1}", mobile == null || string.IsNullOrEmpty(mobile.Name) ? "Unknown" : mobile.GetName(), text), partyColor);
            }
        }

        private void OnSpeech(Packet p, PacketHandlerEventArgs args)
        {
            p.Seek(9, System.IO.SeekOrigin.Begin);
            byte type = p.ReadByte();
            ushort color = p.ReadUInt16();
            p.ReadUInt16();
            string lang = p.ReadStringSafe(4);
            string name = p.ReadStringSafe(30);
            string text = p.ReadUnicodeStringSafe();

            if (type == 0x0D)
                textBox.AddLine(string.Format("[G] {0}: {1}", name, text), guildColor);
            else if (type != 0x0A && color != 0x03B2)
            {
                if (name == "System")
                    textBox.AddLine(string.Format("[¤] {0}", text), Hues.GetHue(color).GetColor(30));
                else if (lang != "ENU")
                    textBox.AddLine(string.Format("{0}: {1}", name, text), Hues.GetHue(color).GetColor(30));
            }
        }

        private void OnAsciiSpeech(Packet p, PacketHandlerEventArgs args)
        {
            p.Seek(10, System.IO.SeekOrigin.Begin);
            ushort color = p.ReadUInt16();
            p.ReadUInt16();
            string name = p.ReadStringSafe(30);
            string text = p.ReadStringSafe();

            if (name == "System" && color != 0x018B)
                textBox.AddLine(string.Format("[×] {0}", text), Hues.GetHue(color).GetColor(30));
        }

        protected override void Dispose(bool disposing)
        {
            PacketHandler.RemoveServerToClientFilter(0xAE, OnSpeech);
            PacketHandler.RemoveServerToClientViewer(0xBF, OnPartySpeech);
            instance = null;
            base.Dispose(disposing);
        }

        protected override void Save()
        {
            ConfigEx.SetAttribute(Location.X, "locX", "Chat");
            ConfigEx.SetAttribute(Location.Y, "locY", "Chat");
            ConfigEx.SetAttribute(Size.Width, "sizeW", "Chat");
            ConfigEx.SetAttribute(Size.Height, "sizeH", "Chat");
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                FormBorderStyle = FormBorderStyle == FormBorderStyle.None ? FormBorderStyle.SizableToolWindow : FormBorderStyle.None;
            else
                base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseRightClick()
        {
            ConfigEx.SetAttribute(false, "enabled", "Chat");
            base.OnMouseRightClick();
        }

        private static Color guildColor, partyColor;
        public new static void OnInit()
        {
            ConfigAgent.AddItem(ushort.MaxValue, c => guildColor = c == ushort.MaxValue ? Color.Red : Hues.GetHue(c).GetColor(30), "ChatGuildColor");
            ConfigAgent.AddItem(ushort.MaxValue, c => partyColor = c == ushort.MaxValue ? Color.Purple : Hues.GetHue(c).GetColor(30), "ChatPartyColor");
            Command.Register("chat", args => new Chat().ShowUnlocked());
            MainFormEx.Connected += MainFormEx_Connected;
        }

        private static void MainFormEx_Connected()
        {
            if (ConfigEx.GetAttribute(false, "enabled", "Chat"))
                if (instance != null)
                {
                    instance.Hidden = false;
                    instance.Show();
                }
                else
                    new Chat().Show();
        }
    }
}