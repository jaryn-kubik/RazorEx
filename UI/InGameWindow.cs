using Assistant;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RazorEx.UI
{
    public abstract class InGameWindow : Form
    {
        protected readonly TableLayoutPanel table = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        private Point offset;

        protected InGameWindow()
        {
            ControlBox = false;
            Text = string.Empty;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(MousePosition.X - 15, MousePosition.Y - 15);
            BackColor = Color.Black;
            TopMost = true;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Controls.Add(table);
            FormBorderStyle = FormBorderStyle.None;
            table.MouseClick += (s, e) => OnMouseClick(e);
            table.MouseDoubleClick += (s, e) => OnMouseDoubleClick(e);
            table.MouseDown += (s, e) => OnMouseDown(e);
            table.MouseMove += (s, e) => OnMouseMove(e);
            if (!Borderless)
                table.Paint += table_Paint;
            Cursor = curDefault;

            PacketHandler.RegisterClientToServerViewer(0x6C, OnTarget);
            PacketHandler.RegisterServerToClientViewer(0x6C, OnTarget);
            PacketHandler.RegisterServerToClientViewer(0x72, OnTarget);
            PacketHandler.RegisterServerToClientFilter(0x77, MobileMoving);
            MainFormEx.Disconnected += Close;
        }

        protected bool Hidden { get; set; }
        protected virtual bool Borderless { get { return false; } }
        private void table_Paint(object sender, PaintEventArgs e)
        { ControlPaint.DrawBorder(e.Graphics, table.ClientRectangle, Color.Gray, ButtonBorderStyle.Solid); }

        protected override void Dispose(bool disposing)
        {
            PacketHandler.RemoveClientToServerViewer(0x6C, OnTarget);
            PacketHandler.RemoveServerToClientViewer(0x6C, OnTarget);
            PacketHandler.RemoveServerToClientViewer(0x72, OnTarget);
            PacketHandler.RemoveServerToClientFilter(0x77, MobileMoving);
            MainFormEx.Disconnected -= Close;
            base.Dispose(disposing);
        }

        public static void OnInit()
        {
            Command.Register("lock", args => Locked = !locked);
            MainFormEx.FocusChanged += MainFormEx_FocusChanged;
        }

        private static void MainFormEx_FocusChanged()
        {
            bool topMost = MainFormEx.UOWindow == GetForegroundWindow() || Engine.MainWindow.ContainsFocus;
            Engine.MainWindow.BeginInvoke((Action)(() => ChangeFocus(topMost, IsMinimized())));
        }

        protected static void ChangeFocus(bool topMost, bool isMinimized)
        {
            foreach (InGameWindow window in Application.OpenForms.OfType<InGameWindow>().Where(w => !w.Hidden))
            {
                window.TopMost = topMost;
                if (window.TopMost)
                    window.Show();
                else if (isMinimized)
                    window.Hide();
            }
        }

        protected abstract void Save();
        protected virtual bool Lockable { get { return true; } }
        protected static bool locked = true;
        private static bool Locked
        {
            set
            {
                if (locked == value)
                    return;

                locked = value;
                WorldEx.SendMessage(locked ? "UI locked." : "UI unlocked.");
                if (locked)
                    foreach (InGameWindow window in Application.OpenForms.OfType<InGameWindow>())
                        window.Save();
            }
        }

        protected void ShowUnlocked()
        {
            Locked = false;
            Show();
        }

        #region Cursor
        private static readonly Cursor curDefault = new Cursor(LoadCursorFromFile("curDefault.cur"));
        private static readonly Cursor curTarget = new Cursor(LoadCursorFromFile("curTarget.cur"));
        private static readonly Cursor curWar = new Cursor(LoadCursorFromFile("curWar.cur"));

        protected virtual bool Targetable { get { return false; } }
        protected virtual void OnTarget() { PostMessage(MainFormEx.UOWindow, 0x100, new IntPtr(27), new IntPtr(0x00010001)); }

        private void MobileMoving(Packet p, PacketHandlerEventArgs args) { SetCursor(); }
        private void OnTarget(PacketReader p, PacketHandlerEventArgs args) { SetCursor(); }
        private void SetCursor()
        {
            if (Targeting.ClientTarget && Targetable)
                Cursor = curTarget;
            else if (World.Player != null && World.Player.Warmode)
                Cursor = curWar;
            else
                Cursor = curDefault;
        }
        #endregion

        #region Mouse
        protected override void OnMouseDown(MouseEventArgs e)
        {
            SetCursor();
            if ((!locked || !Lockable) && e.Button == MouseButtons.Left)
                offset = new Point(-e.X, -e.Y);
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SetCursor();
            if ((!locked || !Lockable) && e.Button == MouseButtons.Left)
            {
                Point p = MousePosition;
                p.Offset(offset);
                Location = p;
            }
            base.OnMouseMove(e);
        }

        protected virtual void OnMouseRightClick() { Close(); }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            SetCursor();
            if ((!locked || !Lockable) && e.Button == MouseButtons.Right)
                OnMouseRightClick();
            else if (Targetable && Targeting.ClientTarget && e.Button == MouseButtons.Left)
                OnTarget();
            base.OnMouseClick(e);
        }
        #endregion

        #region Native
        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursorFromFile(string fileName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        private struct WINDOWPLACEMENT
        {
            public int length, flags, showCmd;
            public Point ptMinPosition, ptMaxPosition;
            public Rectangle rcNormalPosition;
        }

        private static bool IsMinimized()
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(MainFormEx.UOWindow, ref placement);
            return placement.showCmd != 3 && placement.showCmd != 1;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x100 || m.Msg == 0x101 || m.Msg == 0x104 || m.Msg == 0x105)// WM_KEYDOWN || WM_KEYUP || WM_SYSKEYDOWN || WM_SYSKEYUP
            {
                PostMessage(MainFormEx.UOWindow, m.Msg, m.WParam, m.LParam);
                m.Result = IntPtr.Zero;
            }
            else
                base.WndProc(ref m);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams p = base.CreateParams;
                p.ExStyle |= 0x8000000;//WS_EX_NOACTIVATE
                return p;
            }
        }
        #endregion
    }
}