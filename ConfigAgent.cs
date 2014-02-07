using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Assistant;

namespace RazorEx
{
    public class ConfigAgent : Agent
    {
        protected readonly List<ConfigItem> items = new List<ConfigItem>();
        private ListBox listBox;
        public override string Name { get { return "Config"; } }

        private static readonly ConfigAgent instance = new ConfigAgent();
        public static void OnInit() { Add(instance); }
        public static void AddItem<T>(T defaultValue, params XName[] nodes) where T : IConvertible { instance.items.Add(new ConfigItem(defaultValue, null, nodes)); }
        public static void AddItem<T>(T defaultValue, Action<T> onChange, params XName[] nodes) where T : IConvertible { instance.items.Add(new ConfigItem(defaultValue, value => onChange((T)value), nodes)); }

        public override void Save(XmlTextWriter xml) { }
        public override void Load(XmlElement node) { }
        public override void Clear() { }
        public override void OnSelected(ListBox subList, params Button[] buttons)
        {
            buttons[0].Text = "Change";
            buttons[0].Visible = true;
            listBox = subList;
            subList.BeginUpdate();
            subList.Sorted = true;
            subList.Items.Clear();
            foreach (ConfigItem item in items)
                subList.Items.Add(item);
            subList.EndUpdate();
        }

        public override void OnButtonPress(int num)
        {
            ConfigItem item = listBox.SelectedItem as ConfigItem;
            if (item == null)
                return;
            if (item.Value is string)
            {
                if (InputBox.Show("Input string") && !string.IsNullOrEmpty(InputBox.GetString()))
                    item.Value = InputBox.GetString();
            }
            else if (item.Value is bool)
                item.Value = !(bool)item.Value;
            else if (item.Value is uint)
                Targeting.OneTimeTarget((l, s, p, g) =>
                                        {
                                            item.Value = s == World.Player.Serial ? 0 : s.Value;
                                            listBox.Items[listBox.SelectedIndex] = item;
                                        });
            else if (item.Value is byte)
            {
                if (InputBox.Show("Input value") && (InputBox.GetInt(-1) >= 0 || InputBox.GetInt(-1) <= 100))
                    item.Value = (byte)InputBox.GetInt(-1);
            }
            else if (item.Value is ushort)
            {
                HueEntry hueEntry = new HueEntry((ushort)item.Value);
                if (hueEntry.ShowDialog(Engine.MainWindow) == DialogResult.OK)
                    item.Value = (ushort)hueEntry.Hue;
            }
            else if (item.Value.GetType().IsEnum)
            {
                Type type = item.Value.GetType();
                int value = (int)item.Value;
                while (!Enum.IsDefined(type, ++value))
                    if (value >= Enum.GetValues(type).Length)
                        value = -1;
                item.Value = Enum.ToObject(type, value);
            }
            else
                throw new NotImplementedException();
            listBox.Items[listBox.SelectedIndex] = item;
        }

        protected class ConfigItem
        {
            private readonly XName[] nodes;
            private readonly object defaultValue;
            private readonly Action<object> onChange;

            public ConfigItem(object defaultValue, Action<object> onChange, params XName[] nodes)
            {
                this.defaultValue = defaultValue;
                this.nodes = nodes;
                this.onChange = onChange;
                if (onChange != null) onChange(Value);
            }

            public object Value
            {
                get
                {
                    XElement element = ConfigEx.GetXElement(true, nodes);
                    try { return defaultValue.GetType().IsEnum ? Enum.Parse(defaultValue.GetType(), element.Value) : Convert.ChangeType(element.Value, defaultValue.GetType()); }
                    catch { element.SetValue(defaultValue); }
                    return defaultValue;
                }
                set
                {
                    ConfigEx.SetElement(value ?? defaultValue, nodes);
                    if (onChange != null) onChange(Value);
                }
            }

            public override string ToString()
            {
                if (defaultValue is uint)
                    return string.Format("{0}: 0x{1:X}", nodes[nodes.Length - 1], Value);
                return string.Format("{0}: {1}", nodes[nodes.Length - 1], Value);
            }
        }
    }
}