using Assistant;
using Assistant.Macros;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ultima;

[assembly: AssemblyTitle("RazorEx")]
[assembly: AssemblyVersion("3.0.3.*")]

namespace RazorEx
{
    public static class Core
    {
        private static readonly Lazy<TreeNode> node = new Lazy<TreeNode>(() => HotKey.MakeNode("RazorEx", null));
        private static readonly Dictionary<int, Bitmap> gumpCache = new Dictionary<int, Bitmap>();
        private static readonly object syncRoot = new object();

        [STAThread]
        public static void Main()
        {
            try
            {
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                Engine.m_Running = true;
                Engine.m_BaseDir = Application.StartupPath;
                Engine.m_MainWnd = new UI.MainFormEx();
                ClientCommunication.ClientEncrypted = true;
                ClientCommunication.ServerEncrypted = false;
                ClientCommunication.InitializeLibrary("1.0.12");

                Client.Directories.Clear();
                Client.Directories.Add(Path.GetDirectoryName(StartInfo.ClientPath));

                if (!Language.Load("ENU"))
                    throw new Exception("Unable to load Razor_lang.enu");

                Engine.Init(typeof(Engine).Assembly);
                Config.LoadCharList();

                foreach (Type type in typeof(Core).Assembly.GetExportedTypes())
                {
                    MethodInfo mi = type.GetMethod("OnInit", BindingFlags.Public | BindingFlags.Static);
                    if (mi != null)
                        mi.Invoke(null, null);
                }

                foreach (Type type in typeof(Core).Assembly.GetExportedTypes())
                {
                    MethodInfo mi = type.GetMethod("AfterInit", BindingFlags.Public | BindingFlags.Static);
                    if (mi != null)
                        mi.Invoke(null, null);
                }
                Config.CurrentProfile.Load();

                ClientCommunication.LaunchClient(StartInfo.ClientPath);
                ClientCommunication.SetConnectionInfo(StartInfo.Server, StartInfo.Port);
                Application.Run(Engine.MainWindow);

                Engine.m_Running = false;
                PacketPlayer.Stop();
                AVIRec.Stop();
                ClientCommunication.Close();
                Counter.Save();
                MacroManager.Save();
                Config.Save();
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is ReflectionTypeLoadException)
                    foreach (Exception e in ((ReflectionTypeLoadException)ex.InnerException).LoaderExceptions)
                        OnCrash(e);
                else
                    OnCrash(ex.InnerException);
            }
            catch (Exception ex) { OnCrash(ex); }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            OnCrash(e.Exception.GetBaseException());
            e.SetObserved();
        }

        public static void OnCrash(Exception ex)
        {
            Engine.LogCrash(ex);
            MessageBox.Show(ex.ToString());
        }

        public static TreeNode AddHotkeyNode(string name) { return HotKey.MakeNode(node.Value, name, null); }
        public static void AddHotkey(string name, HotKeyCallback callback) { AddHotkey(node.Value, name, callback); }
        public static void AddHotkey(TreeNode parent, string name, HotKeyCallback callback)
        {
            KeyData key = HotKey.Add(HKCategory.None, HKSubCat.None, name, callback);
            key.Remove();
            key.m_Node = HotKey.MakeNode(parent, key.StrName, key);
        }

        public static Bitmap GetGump(int gumpID)
        {
            lock (syncRoot)
            {
                Bitmap bitmap;
                if (!gumpCache.TryGetValue(gumpID, out bitmap))
                {
                    bitmap = Gumps.GetGump(gumpID);
                    gumpCache.Add(gumpID, bitmap);
                }
                return bitmap;
            }
        }

        public static Size GetGumpSize(int gumpID)
        {
            lock (syncRoot)
            {
                Bitmap bitmap;
                if (!gumpCache.TryGetValue(gumpID, out bitmap))
                {
                    bitmap = Gumps.GetGump(gumpID);
                    gumpCache.Add(gumpID, bitmap);
                }
                return bitmap == null ? Size.Empty : bitmap.Size;
            }
        }
    }
}