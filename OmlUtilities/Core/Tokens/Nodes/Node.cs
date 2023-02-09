using OmlUtilities.Core.Tokens.Helper;
using System.Xml.Linq;

namespace OmlUtilities.Core.Tokens.Nodes
{
    public abstract class Node : KeyedToken
    {
        public static Node CreateInstance(XElement xml, ESpace eSpace)
        {
            return xml.Name.LocalName switch
            {
                "Nodes.Assign" => new Assign(xml, eSpace),
                _ => new UnknownNode(xml, eSpace)
            };
        }

        public int X => ElementHelper.GetAttribute<int?>(_xml, "X") ?? 0;

        public int Y => ElementHelper.GetAttribute<int?>(_xml, "Y") ?? 0;

        public Node(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            // Default constructor
        }
    }
}
