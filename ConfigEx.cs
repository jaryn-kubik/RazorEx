using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace RazorEx
{
    public static class ConfigEx
    {
        private static readonly XDocument document;

        static ConfigEx()
        {
            try
            {
                document = XDocument.Load(GetPath("RazorEx.xml"));
                Save(GetPath("RazorEx.xml.backup"));
            }
            catch (Exception ex)
            {
                if (!(ex is FileNotFoundException))
                    Core.OnCrash(ex);
                document = new XDocument(new XElement("RazorEx"));
            }
            document.Changed += (s, e) => Save(GetPath("RazorEx.xml"));
        }

        public static string GetPath(string filename) { return Path.Combine(Path.GetDirectoryName(typeof(ConfigEx).Assembly.Location) ?? string.Empty, filename); }
        private static void Save(string filename)
        {
            using (XmlWriter writer = XmlWriter.Create(filename, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
                document.WriteTo(writer);
        }

        public static IEnumerable<string[]> LoadCfg(string path, byte paramCount)
        {
            path = GetPath(path);
            if (File.Exists(path))
                foreach (string[] data in File.ReadLines(path).Select(l => l.Trim().Split(';')))
                    if (paramCount == 0 || data.Length == paramCount)
                        yield return data;
        }

        public static void SetElement(object value, params XName[] nodes) { GetXElement(true, nodes).SetValue(value); }
        public static void SetAttribute(object value, XName attribute, params XName[] nodes) { GetXElement(true, nodes).SetAttributeValue(attribute, value); }
        public static XElement GetXElement(bool autoCreate, params XName[] nodes)
        {
            XElement element = document.Root;
            foreach (XName node in nodes)
            {
                if (element == null)
                    return null;

                XElement child = element.Element(node);
                if (child == null && autoCreate)
                {
                    child = new XElement(node);
                    element.Add(child);
                }
                element = child;
            }
            return element;
        }

        public static T GetElement<T>(T defaultValue, params XName[] nodes) where T : IConvertible
        {
            XElement element = GetXElement(true, nodes);
            if (string.IsNullOrEmpty(element.Value))
                element.SetValue(defaultValue);
            try { return ConvertType<T>(element.Value); }
            catch { element.SetValue(defaultValue); }
            return defaultValue;
        }

        private static T ConvertType<T>(object value) { return typeof(T).IsEnum ? (T)Enum.Parse(typeof(T), value.ToString()) : (T)Convert.ChangeType(value, typeof(T)); }

        public static T GetAttribute<T>(T defaultValue, XName attribute, params XName[] nodes) where T : IConvertible
        {
            try
            {
                XAttribute xAttribute = GetXElement(true, nodes).Attribute(attribute);
                return string.IsNullOrEmpty(xAttribute.Value) ? defaultValue : ConvertType<T>(xAttribute.Value);
            }
            catch { return defaultValue; }
        }
    }
}