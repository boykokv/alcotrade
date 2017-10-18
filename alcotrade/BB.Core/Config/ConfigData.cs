using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace BB.Core.Config
{
    internal class ConfigData
    {
        private readonly Dictionary<string, XmlNode> _loadedXmlSections = new Dictionary<string, XmlNode>();
        private readonly Dictionary<string, object> _parsedConfigurationsCache = new Dictionary<string, object>();
        private readonly object _syncRoot = new object();

        public ConfigData(XmlNode mainConfigSection)
        {
            if (mainConfigSection == null)
                throw new ArgumentNullException("mainConfigSection");
            if (!mainConfigSection.HasChildNodes)
                throw new InvalidOperationException("!mainConfigSection.HasChildNodes");

            foreach (XmlNode child in mainConfigSection.ChildNodes)
                _loadedXmlSections[child.Name] = child;
        }

        public T Get<T>()
        {
            var result = default(T);

            Failover
                .Execute(() =>
                {
                    var sectionName = typeof(T).Name;

                    object config;

                    lock (_syncRoot)
                    {
                        if (!_parsedConfigurationsCache.TryGetValue(sectionName, out config))
                            config = ParseConfigData<T>(sectionName);
                    }

                    result = (T)config;
                });

            return result;
        }
        public string GetJson(string sectionName)
        {
            string result = null;
            Failover
                .Execute(() =>
                {
                    XmlNode config;

                    lock (_syncRoot)
                    {
                        config = GetXmlNode(sectionName);
                    }

                    if (config != null)
                    {
                        result = JsonConvert.SerializeXmlNode(config, Formatting.None, true);
                    }
                });
            return result;
        }

        private object ParseConfigData<T>(string sectionName)
        {
            object ret = null;
            if (_loadedXmlSections.ContainsKey(sectionName) && _loadedXmlSections[sectionName] != null)
                ret = _parsedConfigurationsCache[sectionName] =
                    new XmlSerializer(typeof(T)).Deserialize(new XmlNodeReader(_loadedXmlSections[sectionName]));
            return ret;
        }

        private XmlNode GetXmlNode(string sectionName)
        {
            if (_loadedXmlSections.ContainsKey(sectionName) && _loadedXmlSections[sectionName] != null)
                return _loadedXmlSections[sectionName];
            return null;
        }

        public string GetXml(string sectionName)
        {
            string result = null;
            Failover
                .Execute(() =>
                {
                    XmlNode config;

                    lock (_syncRoot)
                    {
                        config = GetXmlNode(sectionName);
                    }

                    if (config != null)
                    {
                        result =config.OuterXml;
                    }
                });
            return result;
        }
    }
}