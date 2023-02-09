using OmlUtilities.Core.Tokens.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OmlUtilities.Core.Tokens
{
    public class CustomWidget : Widget
    {
        public class CustomProperty : KeyedToken
        {
            public readonly ParsedExpression ValueExpression = null;

            public string PropertyName => ElementHelper.GetAttribute<string>(_xml, "PropertyName");

            public string Value => ElementHelper.GetAttribute<string>(_xml, "Value");

            public CustomProperty(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                XElement parsedExpressionXml = _xml.XPathSelectElement("./ValueExpression/ParsedExpression");
                if (parsedExpressionXml != null)
                {
                    ValueExpression = new ParsedExpression(parsedExpressionXml, eSpace);
                }
            }
        }

        protected Dictionary<string, CustomProperty> _customProperties = new Dictionary<string, CustomProperty>();
        protected ReadOnlyDictionary<string, CustomProperty> _customPropertiesReadOnly = null;
        protected List<Placeholder> _childPlaceholders = new List<Placeholder>();

        public ReadOnlyCollection<Placeholder> ChildPlaceholders => _childPlaceholders.AsReadOnly();

        /// <summary>
        /// Returns a list of all children of all placeholders of this block.
        /// 
        /// Especially useful shortcut for custom widgets that contain only one placeholder.
        /// </summary>
        public ReadOnlyCollection<Widget> ChildWidgets =>
            (from placeholder in _childPlaceholders
            from widget in placeholder.ChildWidgets
            select widget).ToList().AsReadOnly();

        public ReadOnlyDictionary<string, CustomProperty> CustomProperties => _customPropertiesReadOnly;

        public CustomProperty ValueProperty => _customProperties.GetValueOrDefault("Value");

        public CustomProperty VariableProperty => _customProperties.GetValueOrDefault("Variable");

        public CustomProperty ExampleProperty => _customProperties.GetValueOrDefault("Example");

        public CustomProperty EnabledProperty => _customProperties.GetValueOrDefault("Enabled");

        public CustomProperty IsDefaultProperty => _customProperties.GetValueOrDefault("IsDefault");

        public CustomProperty VisibleProperty => _customProperties.GetValueOrDefault("Visible");

        public CustomProperty WidthProperty => _customProperties.GetValueOrDefault("Width");

        public CustomProperty MarginTopProperty => _customProperties.GetValueOrDefault("MarginTop");

        public CustomProperty MarginLeftProperty => _customProperties.GetValueOrDefault("MarginLeft");

        public CustomProperty StyleProperty => _customProperties.GetValueOrDefault("Style");

        public CustomWidget(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            // Parse custom properties
            foreach (XElement customPropertyXml in xml.XPathSelectElements("./CustomProperties/CustomProperty[@PropertyName]"))
            {
                CustomProperty customProperty = new CustomProperty(customPropertyXml, eSpace);
                _customProperties.Add(customProperty.PropertyName, customProperty);
            }
            _customPropertiesReadOnly = new ReadOnlyDictionary<string, CustomProperty>(_customProperties);

            // Parse placeholders
            foreach (XElement placeholderXml in xml.XPathSelectElements("./ChildPlaceholders/NRWebWidgets.CustomPlaceholderWidget"))
            {
                _childPlaceholders.Add(new Placeholder(placeholderXml, eSpace));
            }
        }
    }
}
