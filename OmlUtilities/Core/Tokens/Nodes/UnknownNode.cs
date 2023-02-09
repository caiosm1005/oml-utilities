using System.Xml.Linq;

namespace OmlUtilities.Core.Tokens.Nodes
{
    public class UnknownNode : Node
    {
        public UnknownNode(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            // Default constructor
        }
    }
}
