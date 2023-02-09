using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace OmlUtilities.Core.Tokens
{
    public abstract class Widget : KeyedToken
    {
        private static List<Widget> GetInternalWidgetsHierarchy(Widget widget)
        {
            var widgets = new List<Widget>
            {
                widget
            };

            if (widget is Placeholder placeholder)
            {
                foreach (Widget childWidget in placeholder.ChildWidgets)
                {
                    widgets.AddRange(GetInternalWidgetsHierarchy(childWidget));
                }
            }
            else if (widget is CustomWidget customWidget)
            {
                foreach (Placeholder childPlaceholder in customWidget.ChildPlaceholders)
                {
                    widgets.AddRange(GetInternalWidgetsHierarchy(childPlaceholder));
                }
            }

            return widgets;
        }

        public static WidgetType GetTypeByNodeName(string nodeName)
        {
            return nodeName switch
            {
                "NRWebWidgets.Text" => WidgetType.Placeholder,
                "NRWebWidgets.CustomPlaceholderWidget" => WidgetType.Placeholder,
                "NRWebWidgets.CustomWidget-OutSystems.Plugin.NRWidgets.Container" => WidgetType.Container,
                "NRWebWidgets.CustomWidget-OutSystems.Plugin.NRWidgets.Expression" => WidgetType.Expression,
                "NRWebWidgets.CustomWidget-OutSystems.Plugin.NRWidgets.Icon" => WidgetType.Icon,
                "NRWebWidgets.CustomWidget-OutSystems.Plugin.NRWidgets.Label" => WidgetType.Label,
                "NRWebWidgets.CustomWidget-OutSystems.Plugin.NRWidgets.Button" => WidgetType.Button,
                "NRWebWidgets.CustomWidget-OutSystems.Plugin.NRWidgets.Input" => WidgetType.Input,
                "NRWebWidgets.CustomWidget-OutSystems.Plugin.NRWidgets.Checkbox" => WidgetType.Checkbox,
                _ => WidgetType.Unknown
            };
        }

        public static Widget CreateInstance(XElement xml, ESpace eSpace)
        {
            return GetTypeByNodeName(xml.Name.LocalName) switch
            {
                WidgetType.Placeholder => new Placeholder(xml, eSpace),
                WidgetType.Text => new Text(xml, eSpace),
                _ => new CustomWidget(xml, eSpace)
            };
        }

        public static ReadOnlyCollection<Widget> GetAllWidgetsHierarchy(Widget widget)
        {
            if (widget != null)
            {
                return GetInternalWidgetsHierarchy(widget).AsReadOnly();
            }
            return new List<Widget>().AsReadOnly();
        }

        public enum WidgetType
        {
            Unknown,
            Text,
            Placeholder,
            Container,
            Expression,
            Icon,
            Label,
            Button,
            Input,
            Checkbox
        }

        public WidgetType Type => GetTypeByNodeName(_xml.Name.LocalName);

        public Widget(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            // Default constructor
        }

        public override string ToString()
        {
            return $"{GetType().Name} {Type}";
        }
    }
}
