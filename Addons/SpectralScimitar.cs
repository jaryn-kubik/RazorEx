using Assistant;

namespace RazorEx.Addons
{
    public static class SpectralScimitar
    {
        private const uint gumpID = 0x28D55B66;
        private static uint responseID;
        private static bool enabled;

        public static void OnInit()
        {
            ConfigAgent.AddItem(false, e => enabled = e, "SpectralScimitar");
            PacketHandler.RegisterServerToClientViewer(0xDD, OnCompressedGump);
            PacketHandler.RegisterServerToClientViewer(0xBF, OnCloseGump);
            Command.Register("ss", args => OnCommand());
            Core.AddHotkey("Spectral Scimitar", OnCommand);
        }

        private static void OnCommand()
        {
            if (responseID != 0)
            {
                ClientCommunication.SendToClient(new CloseGump(gumpID));
                WorldEx.SendToServer(new GumpResponse(responseID, gumpID, 2, new int[0], new GumpTextEntry[0]));
            }
            else
                WorldEx.SendMessage("You are not wielding the Spectral Scimitar!");
        }

        private static void OnCompressedGump(PacketReader p, PacketHandlerEventArgs e)
        {
            p.MoveToData();
            uint sender = p.ReadUInt32();
            uint id = p.ReadUInt32();
            if (id == gumpID)
            {
                responseID = sender;
                e.Block = enabled;
            }
        }

        private static void OnCloseGump(PacketReader p, PacketHandlerEventArgs e)
        {
            p.MoveToData();
            if (p.ReadUInt16() == 4 && p.ReadUInt32() == gumpID)
                responseID = 0;
        }
    }
}