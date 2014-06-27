using Assistant;
using Assistant.Macros;
using System.Windows.Forms;

namespace RazorEx.Addons
{
    public static class Spells
    {
        private static TreeNode node;

        public static void OnInit()
        {
            node = Core.AddHotkeyNode("Spells");
            AddMacro("Fireball", 18);
            AddMacro("Lightning", 30);
            AddMacro("Energy Bolt", 42);
            AddMacro("Flamestrike", 51);
            AddMacro("Chain Lightning", 49);
            AddMacro("Meteor Swarm", 55);

            MacroEx macro = new MacroEx();
            macro.Insert(-1, new MacroHealPet());
            macro.Insert(-1, new MacroTargetPet());

            Core.AddHotkey(node, "HealPet", () => Cast(macro));
            Core.AddHotkey(node, "SetPet", () => Targeting.OneTimeTarget(SetPet));
            Command.Register("setpet", args => Targeting.OneTimeTarget(SetPet));

            HotKey.Get((int)LocString.HealOrCureSelf).m_Callback = HealOrCureSelf;
            HotKey.Get(Spell.Get(22).Name).m_Callback = () => Cast(22); // Teleport
            HotKey.Get(Spell.Get(23).Name).m_Callback = () => Cast(23); // Unlock
            HotKey.Get(Spell.Get(24).Name).m_Callback = () => Cast(24); // Wall of Stone
            HotKey.Get(Spell.Get(33).Name).m_Callback = () => Cast(33); // Blade Spirits
            HotKey.Get(Spell.Get(44).Name).m_Callback = () => Cast(44); // Invisibility
            HotKey.Get(Spell.Get(58).Name).m_Callback = () => Cast(58); // Energy Vortex
            HotKey.Get(Spell.Get(59).Name).m_Callback = () => Cast(59); // Resurrection

            MacroEx invis = new MacroEx { Loop = false };
            invis.Insert(-1, new MacroCastSpellAction(44));
            invis.Insert(-1, new TargetSelf());
            invis.Insert(-1, new ResetWarmode());
            Core.AddHotkey(node, "Invisibility", () => Cast(invis));
        }

        private static void Cast(ushort num)
        {
            Targeting.CancelTarget();
            Spell.Get(num).OnCast(new CastSpellFromMacro(num));
        }

        private static void HealOrCureSelf()
        {
            Targeting.CancelTarget();
            Targeting.TargetSelf(true);
            new MacroCastSpellAction(World.Player.Poisoned ? 25 : 29).Perform();
        }

        private static void Cast(Macro macro)
        {
            Targeting.CancelTarget();
            MacroManager.Play(macro);
            Engine.MainWindow.PlayMacro(macro);
        }

        private static void AddMacro(string name, int spellID)
        {
            MacroEx macro = new MacroEx();
            macro.Insert(-1, new MacroCastSpellAction(spellID));
            macro.Insert(-1, new SpellTarget());
            Core.AddHotkey(node, name, () => Cast(macro));
        }

        private static void SetPet(bool location, Serial serial, Point3D p, ushort gfxid) { ConfigEx.SetElement((int)serial, "Pet"); }
        private class MacroTargetPet : SpellTarget { protected override void Target() { Targeting.Target(World.FindMobile((uint)ConfigEx.GetElement(0, "Pet"))); } }
        private class MacroHealPet : MacroAction
        {
            public override bool Perform()
            {
                Mobile pet = World.FindMobile((uint)ConfigEx.GetElement(0, "Pet"));
                return pet != null && (pet.Hits < pet.HitsMax || pet.Poisoned) && new MacroCastSpellAction(pet.Poisoned ? 25 : 29).Perform();
            }
        }
    }
}