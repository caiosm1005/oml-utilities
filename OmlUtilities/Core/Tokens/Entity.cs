using OmlUtilities.Core.Tokens.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OmlUtilities.Core.Tokens
{
    public class Entity : NamedToken
    {
        public class Attribute : NamedToken
        {
            private static string[] s_integrationBuilderDefaultValues =
            {
                "-1999999991",
                "-8999999999999998",
                "99999993.14159356",
                "<ib>NULL</ib>"
            };
            
            public readonly Entity Entity = null;

            public bool IsReferenceEntityAttribute => _xml.Name.LocalName.Equals("ReferenceEntityAttribute");

            public int? Length
            {
                get
                {
                    if (IsReferenceEntityAttribute)
                    {
                        return ElementHelper.GetAttribute<int?>(_xml.XPathSelectElement("./Length/ReferenceExpression[@Text]"), "Text");
                    }
                    else if (int.TryParse(ParsedExpression.XmlToString(_xml.XPathSelectElement("./Length/ParsedExpression"), ESpace), out int result))
                    {
                        return result;
                    }
                    return null;
                }
            }

            public int? Decimals
            {
                get
                {
                    if (IsReferenceEntityAttribute)
                    {
                        return ElementHelper.GetAttribute<int?>(_xml.XPathSelectElement("./Decimals/ReferenceExpression[@Text]"), "Text");
                    }
                    else if (int.TryParse(ParsedExpression.XmlToString(_xml.XPathSelectElement("./Decimals/ParsedExpression"), ESpace), out int result))
                    {
                        return result;
                    }
                    return null;
                }
            }

            public string DefaultValue => IsReferenceEntityAttribute ?
                ElementHelper.GetAttribute<string>(_xml.XPathSelectElement("./DefaultValue/ReferenceExpression[@Text]"), "Text").Trim('"') :
                ParsedExpression.XmlToString(_xml.XPathSelectElement("./DefaultValue/ParsedExpression"), ESpace);

            public bool IsIntegrationBuilderDefaultValue => Entity.IsFromIntegrationBuilder && s_integrationBuilderDefaultValues.Contains(DefaultValue);

            public Attribute(XElement xml, Entity entity, ESpace eSpace) : base(xml, eSpace)
            {
                Entity = entity;
            }
        }

        protected Reference _reference = null;
        protected List<Attribute> _attributes = new List<Attribute>();

        public enum EntityType
        {
            Entity,
            StaticEntity,
            ClientEntity
        }

        public EntityType Type
        {
            get
            {
                if (ElementHelper.TestAttribute(_xml, "IsStaticEntity", "Yes"))
                {
                    return EntityType.StaticEntity;
                }
                else if (ElementHelper.TestAttribute(_xml, "IsClientEntity", "Yes"))
                {
                    return EntityType.ClientEntity;
                }
                else
                {
                    return EntityType.Entity;
                }
            }
        }

        public string CreatedByTool => _reference != null ? ElementHelper.GetAttribute<string>(_reference.Xml, "CreatedByTool") : "";
        
        public bool IsReferenceEntity => _xml.Name.LocalName.Equals("ReferenceEntity");

        public string OriginalName => ElementHelper.GetAttribute<string>(_xml, "OriginalName") ?? Name;

        public bool ExposeReadOnly => ElementHelper.TestAttribute(_xml, "ExposeReadOnly", "Yes");

        public bool IsFromIntegrationBuilder => CreatedByTool.Equals("IntegrationBuilder");

        public ReadOnlyCollection<Attribute> Attributes => _attributes.AsReadOnly();

        public Entity(XElement xml, ESpace eSpace, Reference reference = null) : base(xml, eSpace)
        {
            _reference = reference;

            // Parse attributes
            foreach (XElement attributeXml in xml.XPathSelectElements($"./Attributes/{(IsReferenceEntity ? "ReferenceEntityAttribute" : "EntityAttribute")}"))
            {
                _attributes.Add(new Attribute(attributeXml, this, eSpace));
            }
        }
    }
}
