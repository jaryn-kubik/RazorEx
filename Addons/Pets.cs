using System.Windows.Forms;
using Assistant;

namespace RazorEx.Addons
{
    public static class Pets
    {
        public static void OnInit()
        {
            TreeNode nodePets = Core.AddHotkeyNode("Pets");
            Core.AddHotkey(nodePets, "all come", () => PetCommand("AllCome", 0x164));
            Core.AddHotkey(nodePets, "all guard", () => PetCommand("AllGuard", 0x166));
            Core.AddHotkey(nodePets, "all stay", () => PetCommand("AllStay", 0x170));
            Core.AddHotkey(nodePets, "all kill", () => AllKill("AllKill", 0x168, false));
            Core.AddHotkey(nodePets, "all attack", () => AllKill("AllAttack", 0x169, false));
            Core.AddHotkey(nodePets, "all kill (last target)", () => AllKill("AllKill", 0x168, true));
            Core.AddHotkey(nodePets, "all attack (last target)", () => AllKill("AllAttack", 0x169, true));
            Core.AddHotkey(nodePets, "release", Release);

            ConfigAgent.AddItem("all come", "PetCommands", "AllCome");
            ConfigAgent.AddItem("all guard", "PetCommands", "AllGuard");
            ConfigAgent.AddItem("all stay", "PetCommands", "AllStay");
            ConfigAgent.AddItem("all kill", "PetCommands", "AllKill");
            ConfigAgent.AddItem("all attack", "PetCommands", "AllAttack");
            ConfigAgent.AddItem("release", "PetCommands", "Release");
        }
        
        private static void Release() { WorldEx.SendToServer(new EncodedMessage(World.Player.Name + " " + ConfigEx.GetElement("release", "PetCommands", "Release"), 0x16D)); }
        private static void PetCommand(string cmd, ushort keyword) { WorldEx.SendToServer(new EncodedMessage(ConfigEx.GetElement(cmd.Insert(3, " ").ToLower(), "PetCommands", cmd), keyword)); }
        private static void AllKill(string cmd, ushort keyword, bool lastTarget)
        {
            Targeting.ClearQueue();
            PetCommand(cmd, keyword);
            if (lastTarget)
                Targeting.LastTarget(true);
        }
    }
}