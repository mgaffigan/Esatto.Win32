using System.Xml;
using System.Xml.Linq;

namespace Esatto.Utilities
{
    public static class XNodeExtensions
    {
        public static XElement ToXElement(this XmlElement xmlDocument)
        {
            using (var nodeReader = new XmlNodeReader(xmlDocument))
            {
                nodeReader.MoveToContent();
                return XDocument.Load(nodeReader).Root!;
            }
        }

        public static XmlElement ToXmlElement(this XNode el)
        {
            var doc = new XmlDocument();
            doc.Load(el.CreateReader());
            return doc.DocumentElement!;
        }
    }
}
