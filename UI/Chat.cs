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

            Event.ASCIIMessage += Event_ASCIIMessage;
            Event.UnicodeMessage += Event_UnicodeMessage;
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

        private bool? Event_UnicodeMessage(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, string lang, string name, string msg)
        {
            if ((type == 0 && hue == 0x34 && msg == "zZz") ||
                (type == 2 && hue == 0x225 && msg.StartsWith("*is AFK (") && msg.EndsWith(")*")))
                return null;

            if (type == 0x0D)
                textBox.AddLine(string.Format("[G] {0}: {1}", name, msg), guildColor);
            else if (type != 0x0A && hue != 0x03B2)
            {
                if (name == "System")
                {
                    if ((msg.StartsWith("Z herbare bylo vyjmuto") ||
                        (msg.StartsWith("Kolik") && msg.EndsWith("chces vybrat?")) ||
                        msg.EndsWith("bylo vlozeno do herbare.")) &&
                        hue == 0x55C)
                        return null;
                    textBox.AddLine(string.Format("[¤] {0}", msg), Hues.GetHue(hue).GetColor(30));
                }
                else if (lang != "ENU")
                    textBox.AddLine(string.Format("{0}: {1}", name, msg), Hues.GetHue(hue).GetColor(30));
            }
            return null;
        }

        private bool? Event_ASCIIMessage(Serial serial, ItemID graphic, byte type, ushort hue, ushort font, string lang, string name, string msg)
        {
            if (name == "System" && hue != 0x018B)
                textBox.AddLine(string.Format("[×] {0}", msg), Hues.GetHue(hue).GetColor(30));
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            Event.UnicodeMessage -= Event_UnicodeMessage;
            Event.ASCIIMessage -= Event_ASCIIMessage;
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