using System.Drawing;
using System.Windows.Forms;

namespace RazorEx.UI
{
    public class RichTextBoxEx : RichTextBox
    {
        private bool autoScroll = true;
        public bool AutoScroll
        {
            get { return autoScroll; }
            set
            {
                if (value)
                {
                    SelectionStart = TextLength;
                    SelectionLength = 0;
                    ScrollToCaret();
                }
                autoScroll = value;
            }
        }

        public RichTextBoxEx()
        {
            Dock = DockStyle.Fill;
            BorderStyle = BorderStyle.None;
            ReadOnly = true;
            BackColor = Color.Black;
            ForeColor = Color.White;
            ContextMenu = new ContextMenu(new[] { new MenuItem("Copy", (s, e) => Copy()) });
        }

        protected override void WndProc(ref Message m)
        {
            if (autoScroll || m.Msg != 0x0007)//WM_SETFOCUS
                base.WndProc(ref m);
        }

        public void AddLine(string text, Color color)
        {
            Focus();
            SelectionStart = TextLength;
            SelectionLength = 0;
            SelectionColor = color;
            if (string.IsNullOrEmpty(Text))
                AppendText(text);
            else
                AppendText("\n" + text);
        }
    }
}