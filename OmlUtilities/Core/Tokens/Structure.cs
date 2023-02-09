using OmlUtilities.Core.Tokens.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OmlUtilities.Core.Tokens
{
    public class Structure : NamedToken
    {
        public class Attribute : NamedToken
        {
            public readonly Structure Structure = null;

            public bool IsReferenceStructureAttribute => _xml.Name.LocalName.Equals("ReferenceStructureAttribute");

            public int? Length
            {
                get
                {
                    if (IsReferenceStructureAttribute)
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
                    if (IsReferenceStructureAttribute)
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

            public string DefaultValue => IsReferenceStructureAttribute ?
                ElementHelper.GetAttribute<string>(_xml.XPathSelectElement("./DefaultValue/ReferenceExpression[@Text]"), "Text").Trim('"') :
                ParsedExpression.XmlToString(_xml.XPathSelectElement("./DefaultValue/ParsedExpression"), ESpace);

            public Attribute(XElement xml, Structure structure, ESpace eSpace) : base(xml, eSpace)
            {
                Structure = structure;
            }
        }

        protected List<Attribute> _attributes = new List<Attribute>();

        public bool IsReferenceStructure => _xml.Name.LocalName.Equals("ReferenceStructure");

        public string OriginalName => ElementHelper.GetAttribute<string>(_xml, "OriginalName") ?? Name;

        public ReadOnlyCollection<Attribute> Attributes => _attributes.AsReadOnly();

        public Structure(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            foreach (XElement attributeXml in xml.XPathSelectElements($"./Attributes/{(IsReferenceStructure ? "ReferenceStructureAttribute" : "StructureAttribute")}"))
            {
                _attributes.Add(new Attribute(attributeXml, this, eSpace));
            }
        }
    }
}
