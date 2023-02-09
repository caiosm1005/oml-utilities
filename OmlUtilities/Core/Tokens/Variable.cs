using OmlUtilities.Core.Tokens.Helper;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OmlUtilities.Core.Tokens
{
    public class Variable : NamedToken
    {
        public enum VariableKind
        {
            Unknown,
            Local,
            Input,
            Output
        }

        public VariableKind Kind => _xml.Name.LocalName switch
        {
            "Variables.LocalVariable" => VariableKind.Local,
            "Variables.GenericInputParameter" => VariableKind.Input,
            "Variables.SerializableInputParameter" => VariableKind.Input,
            "Variables.ReferenceGenericInputParameter" => VariableKind.Input,
            "Variables.ReferenceSerializableInputParameter" => VariableKind.Input,
            "Variables.GenericOutputParameter" => VariableKind.Output,
            "Variables.ReferenceGenericOutputParameter" => VariableKind.Output,
            _ => VariableKind.Unknown
        };

        public string Type => ElementHelper.GetAttribute<string>(_xml, "Type");

        public bool IsMandatory => ElementHelper.TestAttribute(_xml, "IsMandatory", "Yes");

        public bool IsReferenceVariable => _xml.Name.LocalName.StartsWith("Variables.Reference");

        public string DefaultValue => IsReferenceVariable ?
                ElementHelper.GetAttribute<string>(_xml.XPathSelectElement("./DefaultValue/ReferenceExpression[@Text]"), "Text").Trim('"') :
                ParsedExpression.XmlToString(_xml.XPathSelectElement("./DefaultValue/ParsedExpression"), ESpace);

        public Variable(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            // Default constructor
        }
    }
}
