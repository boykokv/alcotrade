using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace BB.Common.Migrations.Utils
{
    public static class EdmXNames
    {
        private static readonly XNamespace _csdlNamespaceV2 = XNamespace.Get("http://schemas.microsoft.com/ado/2008/09/edm");
        private static readonly XNamespace _mslNamespaceV2 = XNamespace.Get("http://schemas.microsoft.com/ado/2008/09/mapping/cs");
        private static readonly XNamespace _ssdlNamespaceV2 = XNamespace.Get("http://schemas.microsoft.com/ado/2009/02/edm/ssdl");
        private static readonly XNamespace _csdlNamespaceV3 = XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/edm");
        private static readonly XNamespace _mslNamespaceV3 = XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/mapping/cs");
        private static readonly XNamespace _ssdlNamespaceV3 = XNamespace.Get("http://schemas.microsoft.com/ado/2009/11/edm/ssdl");

        static EdmXNames()
        {
        }

        public static string ActionAttribute(this XElement element)
        {
            return (string)element.Attribute("Action");
        }

        public static string ColumnNameAttribute(this XElement element)
        {
            return (string)element.Attribute("ColumnName");
        }

        public static string EntitySetAttribute(this XElement element)
        {
            return (string)element.Attribute("EntitySet");
        }

        public static string NameAttribute(this XElement element)
        {
            return (string)element.Attribute("Name");
        }

        public static string NamespaceAttribute(this XElement element)
        {
            return (string)element.Attribute("Namespace");
        }

        public static string EntityTypeAttribute(this XElement element)
        {
            return (string)element.Attribute("EntityType");
        }

        public static string FromRoleAttribute(this XElement element)
        {
            return (string)element.Attribute("FromRole");
        }

        public static string ToRoleAttribute(this XElement element)
        {
            return (string)element.Attribute("ToRole");
        }

        public static string NullableAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Nullable");
        }

        public static string MaxLengthAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"MaxLength");
        }

        public static string MultiplicityAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Multiplicity");
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string FixedLengthAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"FixedLength");
        }

        public static string PrecisionAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Precision");
        }

        public static string ProviderAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Provider");
        }

        public static string ProviderManifestTokenAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"ProviderManifestToken");
        }

        public static string RelationshipAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Relationship");
        }

        public static string ScaleAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Scale");
        }

        public static string StoreGeneratedPatternAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"StoreGeneratedPattern");
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string UnicodeAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Unicode");
        }

        public static string RoleAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Role");
        }

        public static string SchemaAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Schema");
        }

        public static string StoreEntitySetAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"StoreEntitySet");
        }

        public static string TableAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Table");
        }

        public static string TypeAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Type");
        }

        public static string TypeNameAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"TypeName");
        }

        public static string ValueAttribute(this XElement element)
        {
            return (string)element.Attribute((XName)"Value");
        }

        public static class Csdl
        {
            public static readonly IEnumerable<XName> AssociationNames = EdmXNames.Csdl.Names("Association");
            public static readonly IEnumerable<XName> ComplexTypeNames = EdmXNames.Csdl.Names("ComplexType");
            public static readonly IEnumerable<XName> EndNames = EdmXNames.Csdl.Names("End");
            public static readonly IEnumerable<XName> EntityContainerNames = EdmXNames.Csdl.Names("EntityContainer");
            public static readonly IEnumerable<XName> EntitySetNames = EdmXNames.Csdl.Names("EntitySet");
            public static readonly IEnumerable<XName> EntityTypeNames = EdmXNames.Csdl.Names("EntityType");
            public static readonly IEnumerable<XName> NavigationPropertyNames = EdmXNames.Csdl.Names("NavigationProperty");
            public static readonly IEnumerable<XName> PropertyNames = EdmXNames.Csdl.Names("Property");
            public static readonly IEnumerable<XName> SchemaNames = EdmXNames.Csdl.Names("Schema");

            static Csdl()
            {
            }

            private static IEnumerable<XName> Names(string elementName)
            {
                return (IEnumerable<XName>)new List<XName>()
                {
                    EdmXNames._csdlNamespaceV3 + elementName,
                    EdmXNames._csdlNamespaceV2 + elementName
                };
            }
        }

        public static class Msl
        {
            public static readonly IEnumerable<XName> AssociationSetMappingNames = EdmXNames.Msl.Names("AssociationSetMapping");
            public static readonly IEnumerable<XName> ComplexPropertyNames = EdmXNames.Msl.Names("ComplexProperty");
            public static readonly IEnumerable<XName> ConditionNames = EdmXNames.Msl.Names("Condition");
            public static readonly IEnumerable<XName> EntityContainerMappingNames = EdmXNames.Msl.Names("EntityContainerMapping");
            public static readonly IEnumerable<XName> EntitySetMappingNames = EdmXNames.Msl.Names("EntitySetMapping");
            public static readonly IEnumerable<XName> EntityTypeMappingNames = EdmXNames.Msl.Names("EntityTypeMapping");
            public static readonly IEnumerable<XName> MappingNames = EdmXNames.Msl.Names("Mapping");
            public static readonly IEnumerable<XName> MappingFragmentNames = EdmXNames.Msl.Names("MappingFragment");
            public static readonly IEnumerable<XName> ScalarPropertyNames = EdmXNames.Msl.Names("ScalarProperty");

            static Msl()
            {
            }

            private static IEnumerable<XName> Names(string elementName)
            {
                return (IEnumerable<XName>)new List<XName>()
                {
                    EdmXNames._mslNamespaceV3 + elementName,
                    EdmXNames._mslNamespaceV2 + elementName
                };
            }
        }

        public static class Ssdl
        {
            public static readonly IEnumerable<XName> AssociationNames = EdmXNames.Ssdl.Names("Association");
            public static readonly IEnumerable<XName> DependentNames = EdmXNames.Ssdl.Names("Dependent");
            public static readonly IEnumerable<XName> EndNames = EdmXNames.Ssdl.Names("End");
            public static readonly IEnumerable<XName> EntityContainerNames = EdmXNames.Ssdl.Names("EntityContainer");
            public static readonly IEnumerable<XName> EntitySetNames = EdmXNames.Ssdl.Names("EntitySet");
            public static readonly IEnumerable<XName> EntityTypeNames = EdmXNames.Ssdl.Names("EntityType");
            public static readonly IEnumerable<XName> KeyNames = EdmXNames.Ssdl.Names("Key");
            public static readonly IEnumerable<XName> OnDeleteNames = EdmXNames.Ssdl.Names("OnDelete");
            public static readonly IEnumerable<XName> PrincipalNames = EdmXNames.Ssdl.Names("Principal");
            public static readonly IEnumerable<XName> PropertyNames = EdmXNames.Ssdl.Names("Property");
            public static readonly IEnumerable<XName> PropertyRefNames = EdmXNames.Ssdl.Names("PropertyRef");
            public static readonly IEnumerable<XName> SchemaNames = EdmXNames.Ssdl.Names("Schema");

            static Ssdl()
            {
            }

            private static IEnumerable<XName> Names(string elementName)
            {
                return (IEnumerable<XName>)new List<XName>()
                {
                    EdmXNames._ssdlNamespaceV3 + elementName,
                    EdmXNames._ssdlNamespaceV2 + elementName
                };
            }
        }
    }
}