using Assistant;
using System.Xml.Linq;

namespace RazorEx.Addons
{
    public static class BackpackOrganizer
    {
        public static void OnInit() { Command.Register("backpack", OnCommand); }
        private static void OnCommand(string[] args)
        {
            if (args.Length > 0 && args[0] == "set")
            {
                XElement parent = ConfigEx.GetXElement(true, "BackpackOrganizer");
                parent.RemoveAll();
                foreach (Item item in World.Player.Backpack.Contains)
                    parent.Add(new XElement("Item", new XAttribute("position", item.Position), item.Serial));
            }
            else
            {
                XElement parent = ConfigEx.GetXElement(true, "BackpackOrganizer");
                foreach (XElement element in parent.Elements())
                {
                    Serial serial = Serial.Parse(element.Value);
                    Point3D position = Point3D.Parse(element.Attribute("position").Value);
                    Item item = World.FindItem(serial);
                    if (item != null && item.Position != position)
                        DragDrop.Move(item, World.Player.Backpack, position);
                }
            }
        }
    }
}