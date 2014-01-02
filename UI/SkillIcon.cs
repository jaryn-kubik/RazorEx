using Assistant;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RazorEx.UI
{
    public class SkillIcon : InGameWindow
    {
        private readonly SkillName skill;

        private SkillIcon(SkillName skill)
        {
            this.skill = skill;
            table.Controls.Add(new LabelEx(skill.ToString()));
            Location = new Point(ConfigEx.GetAttribute(Location.X, "locX", "Skills", skill.ToString()),
                                 ConfigEx.GetAttribute(Location.Y, "locY", "Skills", skill.ToString()));
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                Targeting.CancelTarget();
                Targeting.LastTarget(true);
                OnUse();
            }
            else
                base.OnMouseClick(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                OnUse();
            else
                base.OnMouseDoubleClick(e);
        }

        private void OnUse()
        {
            new Assistant.Macros.UseSkillAction((int)skill).Perform();
            World.Player.LastSkill = (int)skill;
            OpenEUO.SetAsync("LSkill", (int)skill);
        }

        protected override void Save()
        {
            ConfigEx.SetAttribute(Location.X, "locX", "Skills", skill.ToString());
            ConfigEx.SetAttribute(Location.Y, "locY", "Skills", skill.ToString());
        }

        protected override void OnMouseRightClick()
        {
            ConfigEx.GetXElement(false, "Skills", skill.ToString()).Remove();
            base.OnMouseRightClick();
        }

        public new static void OnInit()
        {
            MainFormEx.Connected += MainFormEx_Connected;
            Command.Register("skill", OnCommand);
        }

        private static void MainFormEx_Connected()
        {
            foreach (XElement element in ConfigEx.GetXElement(true, "Skills").Elements())
            {
                SkillName skill;
                if (Enum.TryParse(element.Name.ToString(), out skill))
                    new SkillIcon(skill).Show();
            }
        }

        private static void OnCommand(string[] args)
        {
            if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
                return;
            string skillName = Enum.GetNames(typeof(SkillName)).FirstOrDefault(skill => skill.StartsWith(args[0], true, null));
            if (skillName == null)
                WorldEx.SendMessage("Invalid skill!");
            else
                new SkillIcon((SkillName)Enum.Parse(typeof(SkillName), skillName)).ShowUnlocked();
        }
    }
}
