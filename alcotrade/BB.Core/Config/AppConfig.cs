using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace BB.Core.Config
{
    public static class AppConfig
    {
        public const string APP_CONFIG_SECTION_NAME = "ApplicationConfiguration";
        private static readonly ConfigData _configData;

        private static Func<string, string> _userSettingsGetFunc;
        private static Action<string, string> _userSettingsSetAction;

        static AppConfig()
        {
            _configData = (ConfigData)ConfigurationManager.GetSection(APP_CONFIG_SECTION_NAME);

            ConfigurationManager.GetSection(APP_CONFIG_SECTION_NAME);
            if (_configData == null)
                throw new InvalidOperationException("Секция ApplicationConfiguration отсутствует или неправильно определена.");
        }

        public static T Get<T>()
        {
            return _configData.Get<T>();
        }

        private static string GetJson(string sectionName)
        {
            return _configData.GetJson(sectionName);
        }
        public static string GetXml(string sectionName)
        {
            return _configData.GetXml(sectionName);
        }

        public static string GetUserOptions(string fullPath)
        {
            var option = GetUserOption(fullPath);
            return option == null ? null : option.ToString();
        }

        public static T GetUserOptions<T>(string fullPath)
        {
            var option = GetUserOption(fullPath);
            if (option == null)
                return default(T);
            else if (option is T)
                return (T)option;
            else if (Type.GetTypeCode(typeof(T)) == TypeCode.Object)
                return (T)new XmlSerializer(typeof(T)).Deserialize(new StringReader((string)option));
            else
                return (T)Convert.ChangeType(option, typeof(T));
        }

        public static void SaveUserOptions(string optionPath, string value)
        {
            if (String.IsNullOrWhiteSpace(optionPath))
                throw new ArgumentNullException("optionPath");

            if (_userSettingsSetAction == null)
                throw new NullReferenceException("User settings storage setAction was not defined");

            _userSettingsSetAction(optionPath, value);
        }

        public static void SaveUserOptions<T>(string optionPath, T value)
        {
            var valueToSave = Type.GetTypeCode(typeof(T)) == TypeCode.Object ? SerializeToString(value) : value.ToString();
            SaveUserOptions(optionPath, valueToSave);
        }

        public static void SetUserOptionsStorage(Func<string, string> getFunc, Action<string, string> setAction)
        {
            if (getFunc == null)
                throw new ArgumentNullException("getFunc");

            if (setAction == null)
                throw new ArgumentNullException("setAction");

            _userSettingsGetFunc = getFunc;
            _userSettingsSetAction = setAction;
        }

        private static object GetUserOption(string fullPath)
        {
            if (String.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentNullException("optionPath");

            if (_userSettingsGetFunc == null)
                throw new NullReferenceException("User settings storage getFunc was not defined");

            string pat = @"\{(.*?)\}";
            Regex pattern = new Regex(pat);
            var allMatches = pattern.Matches(fullPath);

            string typeName = allMatches[0].Groups[1].Value;
            string propName = allMatches[1].Groups[1].Value;

            var val = _userSettingsGetFunc(propName);

            if (val == null)
            {
                Type t = Type.GetType(typeName);
                MethodInfo method = typeof(AppConfig).GetMethod("Get");
                MethodInfo generic = method.MakeGenericMethod(t);
                var se = generic.Invoke(null, null);


                object defaultVal = null;
                PropertyInfo pInfo = t.GetProperty(propName);
                if (se != null && pInfo != null)
                    defaultVal = pInfo.GetValue(se, null);

                if (defaultVal == null)
                    return null;
                else
                    return defaultVal;
            }
            else
                return val;
        }

        private static string SerializeToString<T>(T value)
        {
            var emptyNamepsaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var serializer = new XmlSerializer(value.GetType());
            var settings = new XmlWriterSettings { OmitXmlDeclaration = true };

            using (var stream = new StringWriter())
            using (var writer = XmlWriter.Create(stream, settings))
            {
                serializer.Serialize(writer, value, emptyNamepsaces);
                return stream.ToString();
            }
        }
    }
}