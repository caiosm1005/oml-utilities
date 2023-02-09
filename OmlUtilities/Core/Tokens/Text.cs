using OmlUtilities.Core.Tokens.Helper;
using System.Xml.Linq;

namespace OmlUtilities.Core.Tokens
{
    public class Text : Widget
    {
        public string Value => ElementHelper.GetAttribute<string>(_xml, "Value");

        public Text(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            // Default constructor
        }
    }
}
