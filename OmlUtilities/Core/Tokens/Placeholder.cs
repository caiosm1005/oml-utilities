using OmlUtilities.Core.Tokens.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OmlUtilities.Core.Tokens
{
    public class Placeholder : Widget
    {
        protected List<Widget> _childWidgets = new List<Widget>();

        public string TemplateName => ElementHelper.GetAttribute<string>(_xml, "TemplateName");

        public ReadOnlyCollection<Widget> ChildWidgets => _childWidgets.AsReadOnly();

        public Placeholder(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            foreach (XElement childWidgetXml in xml.XPathSelectElements("./ChildWidgets/*"))
            {
                _childWidgets.Add(CreateInstance(childWidgetXml, eSpace));
            }
        }
    }
}
