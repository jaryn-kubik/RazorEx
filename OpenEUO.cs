using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RazorEx
{
    public static class OpenEUO
    {
        private static int handle;
        private static readonly object syncRoot = new object();

        public static void OnInit()
        {
            Core.AddHotkey("Primary Ability", () => CallAsync("Macro", 35, 0));
            Core.AddHotkey("Secondary Ability", () => CallAsync("Macro", 36, 0));
        }

        public static object[] Call(string name, params object[] args) { return Execute("Call", name, true, args); }
        public static void CallAsync(string name, params object[] args) { Task.Factory.StartNew(() => Execute("Call", name, false, args)); }
        public static void Set(string name, params object[] args) { Execute("Set", name, false, args); }
        public static void SetAsync(string name, params object[] args) { Task.Factory.StartNew(() => Set(name, args)); }
        public static object[] Get(string name) { return Execute("Get", name, true); }
        private static object[] Execute(string function, string name, bool ret, params object[] args)
        {
            lock (syncRoot)
            {
                if (handle == 0)
                {
                    handle = Open();
                    Set("CliNr", 1);
                }
                SetTop(handle, 0);
                PushStrVal(handle, function);
                PushStrVal(handle, name);
                foreach (object obj in args)
                {
                    if (obj == null)
                        PushNil(handle);
                    else if (obj is string)
                        PushStrVal(handle, (string)obj);
                    else if (obj is int)
                        PushInteger(handle, (int)obj);
                    else if (obj is double)
                        PushDouble(handle, (double)obj);
                    else if (obj is bool)
                        PushBoolean(handle, (byte)((bool)obj ? 1 : 0));
                    else
                        PushStrVal(handle, obj.ToString());
                }

                if (Execute(handle) < 0 || !ret)
                    return null;
                int count = GetTop(handle);
                object[] result = new object[count];
                for (int i = 0; i < count; i++)
                    switch (GetType(handle, i + 1))
                    {
                        case 1:
                            result[i] = GetBoolean(handle, i + 1) != 0;
                            break;
                        case 3:
                            result[i] = GetInteger(handle, i + 1);
                            break;
                        case 4:
                            result[i] = GetString(handle, i + 1);
                            break;
                        default:
                            result[i] = null;
                            break;
                    }
                return result;
            }
        }

        [DllImport("uo.dll")]
        private static extern int Open();

        [DllImport("uo.dll")]
        private static extern void SetTop(int handle, int value);

        [DllImport("uo.dll")]
        private static extern int GetTop(int handle);

        [DllImport("uo.dll")]
        private static extern int GetType(int handle, int index);

        [DllImport("uo.dll")]
        private static extern int GetBoolean(int handle, int index);

        [DllImport("uo.dll")]
        private static extern int GetInteger(int handle, int index);

        [DllImport("uo.dll")]
        private static extern string GetString(int handle, int index);

        [DllImport("uo.dll")]
        private static extern void PushNil(int handle);

        [DllImport("uo.dll")]
        private static extern void PushBoolean(int handle, byte value);

        [DllImport("uo.dll")]
        private static extern void PushInteger(int handle, int value);

        [DllImport("uo.dll")]
        private static extern void PushDouble(int handle, double value);

        [DllImport("uo.dll")]
        private static extern void PushStrVal(int handle, string value);

        [DllImport("uo.dll")]
        private static extern int Execute(int handle);
    }
}