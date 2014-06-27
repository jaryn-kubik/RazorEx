using Assistant;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RazorEx.UI
{
    public class MainFormEx : MainForm
    {
        public MainFormEx()
        {
            m_Ver = typeof(MainFormEx).Assembly.GetName().Version;
            Engine.m_Version = m_Ver.ToString(3);
            showWelcome.CheckedChanged += (s, e) => ConfigEx.SetElement(showWelcome.Checked, "ShowWelcome");
        }

        protected override void WndProc(ref Message msg)
        {
            if (msg.Msg == 0x401)
            {
                ClientCommunication.UONetMessage message = (ClientCommunication.UONetMessage)msg.WParam.ToInt32();
                if ((message == ClientCommunication.UONetMessage.Activate || message == ClientCommunication.UONetMessage.Focus) && focusChanged != null)
                    focusChanged();
                else if (message == ClientCommunication.UONetMessage.Connect && connected != null)
                {
                    UOWindow = FindUOWindow();
                    connected();
                }
                else if (message == ClientCommunication.UONetMessage.Disconnect && disconnected != null)
                    disconnected();
            }
            base.WndProc(ref msg);
        }

        [DllImport("Crypt.dll")]
        private static extern IntPtr FindUOWindow();
        public static IntPtr UOWindow { get; private set; }

        private static Action focusChanged;
        public static event Action FocusChanged
        {
            add { focusChanged += value; }
            remove { focusChanged -= value; }
        }

        private static Action connected;
        public static event Action Connected
        {
            add { connected += value; }
            remove { connected -= value; }
        }

        private static Action disconnected;
        public static event Action Disconnected
        {
            add { disconnected += value; }
            remove { disconnected -= value; }
        }
    }
}