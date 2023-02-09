using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OmlUtilities.Core.Tokens.Nodes
{
    public class Assign : LabeledNode
    {
        public class Assignment : KeyedToken
        {
            public readonly ParsedExpression Variable = null;
            public readonly ParsedExpression Value = null;

            public Assignment(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                XElement variableXml = xml.XPathSelectElement("./Variable/ParsedExpression");
                if (variableXml != null)
                {
                    Variable = new ParsedExpression(variableXml, eSpace);
                }

                XElement valueXml = xml.XPathSelectElement("./Value/ParsedExpression");
                if (valueXml != null)
                {
                    Value = new ParsedExpression(valueXml, eSpace);
                }
            }
        }

        protected List<Assignment> _assignments = new List<Assignment>();

        public ReadOnlyCollection<Assignment> Assignments => _assignments.AsReadOnly();

        public Assign(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            foreach (XElement assignmentXml in xml.XPathSelectElements("./Assignments/Assignment"))
            {
                _assignments.Add(new Assignment(assignmentXml, eSpace));
            }
        }
    }
}
