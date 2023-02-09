using OmlUtilities.Core.Tokens.Helper;
using OmlUtilities.Core.Tokens.Nodes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OmlUtilities.Core.Tokens
{
    public class Action : NamedToken
    {
        protected List<Variable> _inputParameters = new List<Variable>();
        protected List<Variable> _outputParameters = new List<Variable>();
        protected List<Variable> _localVariables = new List<Variable>();
        protected List<Node> _nodes = new List<Node>();

        public enum ActionType
        {
            Unknown,
            ServerAction,
            ServiceAction,
            ClientAction
        }

        public ActionType Type => _xml.Name.LocalName switch
        {
            "NRFlows.DataScreenActionFlow" => ActionType.ServerAction,
            "ReferenceAction" => ActionType.ServerAction,
            "NRFlows.ClientActionFlow" => ActionType.ClientAction,
            "NRFlows.ClientScreenActionFlow" => ActionType.ClientAction,
            "ReferenceClientAction" => ActionType.ClientAction,
            _ => ActionType.Unknown
        };

        public ReadOnlyCollection<Variable> InputParameters => _inputParameters.AsReadOnly();

        public ReadOnlyCollection<Variable> OutputParameters => _outputParameters.AsReadOnly();

        public ReadOnlyCollection<Variable> LocalVariables => _localVariables.AsReadOnly();
        
        public ReadOnlyCollection<Node> Nodes => _nodes.AsReadOnly();

        public bool IsFunction => ElementHelper.TestAttribute(_xml, "IsFunction", "Yes");

        public bool IsReferenceAction => _xml.Name.LocalName.StartsWith("Reference");

        public string OriginalName => ElementHelper.GetAttribute<string>(_xml, "OriginalName") ?? Name;

        public Action(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            // Parse input parameters
            foreach (XElement inputParameterXml in xml.XPathSelectElements("./InputParameters/*[starts-with(local-name(), 'Variables.')]"))
            {
                _inputParameters.Add(new Variable(inputParameterXml, eSpace));
            }

            // Parse output parameters
            foreach (XElement outputParameterXml in xml.XPathSelectElements("./OutputParameters/*[starts-with(local-name(), 'Variables.')]"))
            {
                _outputParameters.Add(new Variable(outputParameterXml, eSpace));
            }

            // Parse local variables
            foreach (XElement localVariableXml in xml.XPathSelectElements("./LocalVariables/*[starts-with(local-name(), 'Variables.')]"))
            {
                _localVariables.Add(new Variable(localVariableXml, eSpace));
            }

            // Parse nodes
            XElement nodesFragmentXml = eSpace.GetFragmentXml($"NodesNotShownInESpaceTree#{Key}");
            if (nodesFragmentXml != null)
            {
                foreach (XElement nodeXml in nodesFragmentXml.Elements())
                {
                    _nodes.Add(Node.CreateInstance(nodeXml, eSpace));
                }
            }
        }
    }
}
