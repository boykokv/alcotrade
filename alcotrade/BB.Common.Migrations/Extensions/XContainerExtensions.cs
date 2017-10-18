using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace BB.Common.Migrations.Extensions
{
    internal static class XContainerExtensions
    {
        public static XElement GetOrAddElement(this XContainer container, XName name)
        {
            var xelement = container.Element(name);
            if (xelement == null)
            {
                xelement = new XElement(name);
                container.Add((object)xelement);
            }
            return xelement;
        }

        public static IEnumerable<XElement> Descendants(this XContainer container, IEnumerable<XName> name)
        {
            return name.SelectMany(container.Descendants);
        }

        public static IEnumerable<XElement> Elements(this XContainer container, IEnumerable<XName> name)
        {
            return name.SelectMany(container.Elements);
        }

        public static IEnumerable<XElement> Descendants<T>(this IEnumerable<T> source, IEnumerable<XName> name) where T : XContainer
        {
            return name.SelectMany(n => source.SelectMany(c => c.Descendants(n)));
        }
    }
}