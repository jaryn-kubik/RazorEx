using Assistant;

namespace RazorEx.Fixes
{
    public static class AutoLogin
    {
        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "AutoLogin"); }
        private static void OnChange(bool login)
        {
            if (login)
            {
                PacketHandler.RegisterServerToClientViewer(0xA8, OnServerOrCharList);
                PacketHandler.RegisterServerToClientViewer(0xA9, OnServerOrCharList);
            }
            else
            {
                PacketHandler.RemoveServerToClientViewer(0xA8, OnServerOrCharList);
                PacketHandler.RemoveServerToClientViewer(0xA9, OnServerOrCharList);
            }
        }

        private static void OnServerOrCharList(PacketReader p, PacketHandlerEventArgs args) { OpenEUO.CallAsync("Key", "ENTER"); }
    }
}