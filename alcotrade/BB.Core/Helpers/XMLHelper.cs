using System;
using System.IO;

namespace BB.Core.Helpers
{
    public static class XMLHelper
    {
        public static void Write<T>(this T obj)
        {
            Write(obj, typeof (T).Name);
        }

        public static void Write<T>(this T obj, string fileName)
        {
            System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (StreamWriter file = new StreamWriter(fileName))
            {
                writer.Serialize(file, obj);
            }
        }

        public static T Read<T>() where T : new()
        {
            return Read<T>(typeof (T).Name);
        }

        public static T Read<T>(string fileName) where T: new()
        {
            System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(T));
            T obj = default(T);

            if (!File.Exists(typeof (T).Name))
                return Activator.CreateInstance<T>();

            using (StreamReader file = new StreamReader(typeof(T).Name))
            {
                obj = (T)reader.Deserialize(file);
            }

            return obj;
        }
    }
}
