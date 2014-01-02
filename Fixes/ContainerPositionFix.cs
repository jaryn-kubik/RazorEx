using Assistant;
using System.Drawing;
using System.Threading.Tasks;

namespace RazorEx.Fixes
{
    public static class ContainerPositionFix
    {
        public static void OnInit() { ConfigAgent.AddItem(false, OnChange, "ContainerPositionFix"); }
        private static void OnChange(bool fix)
        {
            if (fix)
                PacketHandler.RegisterServerToClientViewer(0x24, OnGump);
            else
                PacketHandler.RemoveServerToClientViewer(0x24, OnGump);
        }

        private static void OnGump(PacketReader p, PacketHandlerEventArgs args)
        {
            if (args.Block)
                return;
            p.ReadUInt32();
            Task.Factory.StartNew(CheckPosition, p.ReadUInt16());
        }

        private static void CheckPosition(object gump)
        {
            Size size = Core.GetGumpSize((ushort)gump);

            int nextX = (int)OpenEUO.Get("NextCPosX")[0];
            int nextY = (int)OpenEUO.Get("NextCPosY")[0];
            int cliLeft = (int)OpenEUO.Get("CliLeft")[0];
            int cliTop = (int)OpenEUO.Get("CliTop")[0];
            int cliXRes = (int)OpenEUO.Get("CliXRes")[0] - size.Width;
            int cliYRes = (int)OpenEUO.Get("CliYRes")[0] - size.Height;

            if (nextX < cliLeft || nextX > cliLeft + cliXRes)
                OpenEUO.Set("NextCPosX", cliLeft);
            if (nextY < cliTop || nextY > cliTop + cliYRes)
                OpenEUO.Set("NextCPosY", cliTop);
        }
    }
}
