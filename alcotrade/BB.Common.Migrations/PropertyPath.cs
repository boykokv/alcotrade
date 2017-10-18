using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BB.Common.Migrations.Extensions;

namespace BB.Common.Migrations
{
    public class PropertyPath : IEnumerable<PropertyInfo>, IEnumerable
    {
        private static readonly PropertyPath _empty = new PropertyPath();
        private readonly List<PropertyInfo> _components = new List<PropertyInfo>();

        public int Count
        {
            get
            {
                return this._components.Count;
            }
        }

        public static PropertyPath Empty
        {
            get
            {
                return _empty;
            }
        }

        public PropertyInfo this[int index]
        {
            get
            {
                return this._components[index];
            }
        }

        static PropertyPath()
        {
        }

        public PropertyPath(IEnumerable<PropertyInfo> components)
        {
            this._components.AddRange(components);
        }

        public PropertyPath(PropertyInfo component)
        {
            this._components.Add(component);
        }

        private PropertyPath()
        {
        }

        public static bool operator ==(PropertyPath left, PropertyPath right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PropertyPath left, PropertyPath right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            var propertyPathName = new StringBuilder();
            this._components.Each(pi =>
            {
                propertyPathName.Append((string) pi.Name);
                propertyPathName.Append('.');
            });
            return propertyPathName.ToString(0, propertyPathName.Length - 1);
        }

        public bool Equals(PropertyPath other)
        {
            if (ReferenceEquals(null, other))
                return false;
            return ReferenceEquals(this, other) || true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof(PropertyPath))
                return false;
            return this.Equals((PropertyPath)obj);
        }

        public override int GetHashCode()
        {
            return this._components.Aggregate(0, (t, n) => t ^ n.DeclaringType.GetHashCode() * n.Name.GetHashCode() * 397);
        }

        IEnumerator<PropertyInfo> IEnumerable<PropertyInfo>.GetEnumerator()
        {
            return this._components.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._components.GetEnumerator();
        }
    }
}