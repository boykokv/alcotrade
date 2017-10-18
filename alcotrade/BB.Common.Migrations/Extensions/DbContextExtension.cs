using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace BB.Common.Migrations.Extensions
{
    public static class DbContextExtensions
    {
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static XDocument GetModel(this DbContext context)
        {
            return GetModel(w => EdmxWriter.WriteEdmx(context, w));
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static XDocument GetModel(Action<XmlWriter> writeXml)
        {
            using (var memoryStream1 = new MemoryStream())
            {
                var memoryStream2 = memoryStream1;
                var settings = new XmlWriterSettings()
                {
                    Indent = true
                };
                using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream2, settings))
                    writeXml(xmlWriter);
                memoryStream1.Position = 0L;
                return XDocument.Load(memoryStream1);
            }
        }
    }
}
