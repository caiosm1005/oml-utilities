using OmlUtilities.Core.Tokens.Helper;
using System.Xml.Linq;

namespace OmlUtilities.Core.Tokens.Nodes
{
    public abstract class LabeledNode : Node
    {
        public string Label => ElementHelper.GetAttribute<string>(_xml, "Label");

        public LabeledNode(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            // Default constructor
        }

        public override string ToString()
        {
            return $"{GetType().Name} {Label}".Trim();
        }
    }
}
