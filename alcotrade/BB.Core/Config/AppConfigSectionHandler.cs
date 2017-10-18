using System.Configuration;
using System.Xml;

namespace BB.Core.Config
{
    public class AppConfigSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Создает обработчика раздела конфигурации.
        /// </summary>
        /// <returns>
        /// Созданный объект обработчика раздела.
        /// </returns>
        /// <param name="parent">Родительский объект.</param><param name="configContext">Объект контекста конфигурации.</param><param name="section">узел раздела XML.</param>
        public object Create(object parent, object configContext, XmlNode section)
        {
            return new ConfigData(section);
        }
    }
}