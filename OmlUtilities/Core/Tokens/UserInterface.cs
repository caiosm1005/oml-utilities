using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Xml.XPath;
using static OmlUtilities.Core.Tokens.Widget;

namespace OmlUtilities.Core.Tokens
{
    public abstract class UserInterface : NamedToken
    {
        private static List<Widget> GetInternalWidgetsHierarchy(UserInterface screen)
        {
            List<Widget> widgets = new List<Widget>();
            foreach (Widget widget in screen.Widgets)
            {
                widgets.AddRange(Widget.GetAllWidgetsHierarchy(widget));
            }
            return widgets;
        }

        public static ReadOnlyCollection<Widget> GetAllWidgetsHierarchy(UserInterface screen)
        {
            return GetInternalWidgetsHierarchy(screen).AsReadOnly();
        }

        protected List<Variable> _inputParameters = new List<Variable>();
        protected List<Variable> _localVariables = new List<Variable>();
        protected List<Aggregate> _aggregates = new List<Aggregate>();
        protected List<Action> _dataActions = new List<Action>();
        protected List<Action> _clientActions = new List<Action>();
        protected List<Widget> _widgets = new List<Widget>();

        public ReadOnlyCollection<Variable> InputParameters => _inputParameters.AsReadOnly();
        public ReadOnlyCollection<Variable> LocalVariables => _localVariables.AsReadOnly();
        public ReadOnlyCollection<Aggregate> Aggregates => _aggregates.AsReadOnly();
        public ReadOnlyCollection<Action> DataActions => _dataActions.AsReadOnly();
        public ReadOnlyCollection<Action> ClientActions => _clientActions.AsReadOnly();
        public ReadOnlyCollection<Widget> Widgets => _widgets.AsReadOnly();

        public ReadOnlyCollection<Widget> GetWidgetsByType(WidgetType type) => GetInternalWidgetsHierarchy(this).FindAll(x => x.Type.Equals(type)).AsReadOnly();

        public UserInterface(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            // Parse input parameters
            foreach (XElement inputParameterXml in xml.XPathSelectElements("./InputParameters/*[starts-with(local-name(), 'Variables.')]"))
            {
                _inputParameters.Add(new Variable(inputParameterXml, eSpace));
            }

            // Parse local variables
            foreach (XElement localVariableXml in xml.XPathSelectElements("./LocalVariables/*[starts-with(local-name(), 'Variables.')]"))
            {
                _localVariables.Add(new Variable(localVariableXml, eSpace));
            }

            // Parse aggregates
            foreach (XElement screenDataSetXml in xml.XPathSelectElements("./ScreenDataSets/NRNodes.WebScreenDataSet"))
            {
                _aggregates.Add(new Aggregate(screenDataSetXml, eSpace));
            }

            // Parse data actions
            foreach (XElement dataActionXml in xml.XPathSelectElements("./DataActions/NRFlows.DataScreenActionFlow"))
            {
                _dataActions.Add(new Action(dataActionXml, eSpace));
            }

            // Parse client actions
            foreach (XElement clientActionXml in xml.XPathSelectElements("./ClientActions/NRFlows.ClientScreenActionFlow"))
            {
                _clientActions.Add(new Action(clientActionXml, eSpace));
            }

            // Parse widgets
            XElement widgetsFragmentXml = eSpace.GetFragmentXml($"Widgets#{Key}");
            if (widgetsFragmentXml != null)
            {
                foreach (XElement widgetXml in widgetsFragmentXml.Elements())
                {
                    _widgets.Add(CreateInstance(widgetXml, eSpace));
                }
            }
        }
    }
}
