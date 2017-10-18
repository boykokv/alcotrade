using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BB.Common.Migrations.Utils
{
    internal class DatabaseName
    {
        private static readonly Regex PartExtractor = new Regex(string.Format(CultureInfo.InvariantCulture, "^{0}(?:\\.{1})?$", new object[2]
        {
            string.Format(CultureInfo.InvariantCulture, "(?:(?:\\[(?<part{0}>(?:(?:\\]\\])|[^\\]])+)\\])|(?<part{0}>[^\\.\\[\\]]+))", new object[1]
            {
                1
            }),
            string.Format(CultureInfo.InvariantCulture, "(?:(?:\\[(?<part{0}>(?:(?:\\]\\])|[^\\]])+)\\])|(?<part{0}>[^\\.\\[\\]]+))", new object[1]
            {
                2
            })
        }), RegexOptions.Compiled);

        private readonly string _name;
        private readonly string _schema;

        public string Name
        {
            get
            {
                return this._name;
            }
        }

        public string Schema
        {
            get
            {
                return this._schema;
            }
        }

        static DatabaseName()
        {
        }

        public DatabaseName(string name)
            : this(name, (string)null)
        {
        }

        public DatabaseName(string name, string schema)
        {
            this._name = name;
            this._schema = schema;
        }

        public static DatabaseName Parse(string name)
        {
            var match = DatabaseName.PartExtractor.Match(name.Trim());
            if (!match.Success)
                throw new Exception("Invalid database name " + name);
            var str = match.Groups["part1"].Value.Replace("]]", "]");
            var name1 = match.Groups["part2"].Value.Replace("]]", "]");
            return string.IsNullOrWhiteSpace(name1) ? new DatabaseName(str) : new DatabaseName(name1, str);
        }

        public override string ToString()
        {
            var str = Escape(this._name);
            if (this._schema != null)
                str = Escape(this._schema) + "." + str;
            return str;
        }

        private static string Escape(string name)
        {
            if (name.IndexOfAny(new char[3]
            {
                ']',
                '[',
                '.'
            }) == -1)
                return name;
            return "[" + name.Replace("]", "]]") + "]";
        }

        public bool Equals(DatabaseName other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (string.Equals(other._name, _name, StringComparison.Ordinal))
                return string.Equals(other._schema, this._schema, StringComparison.Ordinal);
            return false;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() == typeof(DatabaseName))
                return Equals((DatabaseName)obj);
            return false;
        }

        public override int GetHashCode()
        {
            return this._name.GetHashCode() * 397 ^ (this._schema != null ? this._schema.GetHashCode() : 0);
        }
    }
}