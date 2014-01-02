using Assistant;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace RazorEx
{
    public static class StartInfo
    {
        public static string ClientPath { get; private set; }
        public static IPAddress Server { get; private set; }
        public static int Port { get; private set; }

        static StartInfo()
        {
            if (ConfigEx.GetElement(true, "ShowWelcome"))
            {
                WelcomeFormEx form = new WelcomeFormEx();
                if (form.ShowDialog() != DialogResult.OK)
                    Environment.Exit(0);

                ConfigEx.SetElement(form.Client, "Client");
                ConfigEx.SetElement(form.Server, "Server");
                ConfigEx.SetElement(form.Port, "Port");
            }

            ClientPath = ConfigEx.GetElement(string.Empty, "Client");
            if (!File.Exists(ClientPath) || Path.GetExtension(ClientPath) != ".exe")
                throw new Exception("Selected client path not found!");

            Port = ConfigEx.GetElement(-1, "Port");
            if (Port < 0 || Port > 0xFFFF)
                throw new Exception("Invalid port!");

            Server = Engine.Resolve(ConfigEx.GetElement(string.Empty, "Server"));
            if (Equals(Server, IPAddress.None))
                throw new Exception("Invalid server address!");
        }

        private class WelcomeFormEx : Form
        {
            private readonly Button buttonOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
            private readonly Button buttonClient = new Button { Text = "..." };
            private readonly TextBox boxClient = new TextBox { Size = new Size(150, 15), ReadOnly = true }, boxServer = new TextBox { Size = new Size(150, 15) }, boxPort = new TextBox { Size = new Size(50, 15) };
            private readonly OpenFileDialog file = new OpenFileDialog { CheckFileExists = true, Filter = "client.exe|*.exe", Title = "Select client.exe in your UO directory" };
            public string Client { get { return boxClient.Text; } }
            public string Server { get { return boxServer.Text; } }
            public string Port { get { return boxPort.Text; } }

            public WelcomeFormEx()
            {
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                StartPosition = FormStartPosition.CenterScreen;

                TableLayoutPanel table = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
                table.Controls.Add(new Label { Text = "Client: ", TextAlign = ContentAlignment.MiddleCenter }, 0, 0);
                table.Controls.Add(new Label { Text = "Server: ", TextAlign = ContentAlignment.MiddleCenter }, 0, 1);
                table.Controls.Add(new Label { Text = "Port: ", TextAlign = ContentAlignment.MiddleCenter }, 0, 2);
                table.Controls.Add(boxClient, 1, 0);
                table.Controls.Add(buttonClient, 2, 0);
                table.Controls.Add(boxServer, 1, 1);
                table.SetColumnSpan(boxServer, 2);
                table.Controls.Add(boxPort, 1, 2);
                table.SetColumnSpan(boxPort, 2);
                table.Controls.Add(buttonOK, 0, 3);
                table.SetColumnSpan(buttonOK, 3);
                Controls.Add(table);

                boxClient.Text = ConfigEx.GetElement(Ultima.Client.GetFilePath("client.exe") ?? string.Empty, "Client");
                boxServer.Text = ConfigEx.GetElement("217.117.220.138", "Server");
                boxPort.Text = ConfigEx.GetElement("2593", "Port");
                buttonClient.Click += buttonClient_Click;
            }

            private void buttonClient_Click(object sender, EventArgs e)
            {
                if (file.ShowDialog() == DialogResult.OK)
                    boxClient.Text = file.FileName;
            }
        }
    }
}