using OmlUtilities.Core.Tokens;
using OmlUtilities.Core.Tokens.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using static OmlUtilities.Core.Tokens.ParsedExpression;
using Action = OmlUtilities.Core.Tokens.Action;

namespace OmlUtilities.Core
{
    partial class Oml
    {
        public sealed class OmlInspector
        {
            private Oml _oml;
            private ESpace _eSpace;

            public OmlInspector(Oml oml)
            {
                _oml = oml;
                
                XElement eSpaceXml = null;
                Dictionary<string, XElement> fragmentsXml = new Dictionary<string, XElement>();
                foreach (string fragmentName in _oml.DumpFragmentsNames())
                {
                    XElement fragmentXml = _oml.GetFragmentXml(fragmentName);
                    if (fragmentName.Equals("eSpace"))
                    {
                        eSpaceXml = fragmentXml;
                    }
                    fragmentsXml[fragmentName] = fragmentXml;
                }

                if (eSpaceXml == null)
                {
                    throw new OmlException("Could not find eSpace fragment.");
                }

                _eSpace = new ESpace(eSpaceXml, fragmentsXml);
            }

            public struct Finding
            {
                public FindingType FindingType;
                public string FragmentName;
                public string ElementName;
                
                public Finding(FindingType findingType, string fragmentName, string elementName)
                {
                    FindingType = findingType;
                    FragmentName = fragmentName;
                    ElementName = elementName;
                }
            }

            public enum FindingType
            {
                EmptyAction,
                MissingEndNode,
                InfiniteLoop,
                SequentialBinaryOperation,
                RedundantTypeCasting,
                RedundantIdentifierCasting,
                AggregateWithSelfComparison,
                DynamicSortMissingBrackets,
                DynamicSortWithInvalidEntityOrAttribute,
                UnwrappedIntegrationBuilderEntityAttributeShownOnUI
            }

            public void ScanEmptyActions()
            {
                // TODO
                // Start -> End
            }

            public void ScanMissingEndNodes()
            {
                // TODO
            }

            public void ScanInfiniteLoops()
            {
                // TODO
            }

            public void ScanSequentialBinaryOperations()
            {
                // TODO
                // a = True = True
                // not a = True
            }

            /// <summary>
            /// Scans for redundant identifier castings (i.e. casting an identifier as an identifier).
            /// 
            /// An example of a redundant identifier casting is `IntegerToIdentifier(GetUserId())`.
            /// </summary>
            /// <returns>List of findings.</returns>
            public List<Finding> ScanRedundantIdentifierCastings()
            {
                var findings = new List<Finding>();

                string[] castingFunctions = {
                        "IntegerToIdentifier",
                        "LongIntegerToIdentifier",
                        "TextToIdentifier"
                    };
                string xpath = $"//Call[@NameRef='{string.Join("' or @NameRef='", castingFunctions)}']";

                foreach (string fragmentName in _oml.DumpFragmentsNames())
                {
                    XElement fragmentXml = _oml.GetFragmentXml(fragmentName);

                    foreach (XElement call in fragmentXml.XPathSelectElements(xpath))
                    {
                        if (call.XPathSelectElements("./Argument/Identifier").Count() == 1)
                        {
                            string castingFunction = call.Attribute("NameRef").Value;
                            findings.Add(new Finding(FindingType.RedundantIdentifierCasting, fragmentName, castingFunction));
                        }
                    }
                }

                return findings;
            }

            /// <summary>
            /// Scans for aggregates that contains filters and/or join conditions with self-comparisons.
            /// 
            /// An example of a self comparison is `1 = 1` or `MyEntity.Id = MyEntity.Id`.
            /// 
            /// These types of conditions are confusing, and usually made by a mistake and might yield unexpected results.
            /// </summary>
            /// <returns>List of findings.</returns>
            public List<Finding> ScanAggregatesWithSelfComparison()
            {
                var findings = new List<Finding>();

                foreach(string fragmentName in _oml.DumpFragmentsNames())
                {
                    if (!(fragmentName.StartsWith("NodesNotShownInESpaceTree") || fragmentName.StartsWith("NodesShownInESpaceTree")))
                    {
                        continue;
                    }

                    XElement fragmentXml = _oml.GetFragmentXml(fragmentName);

                    foreach(XElement dataSet in fragmentXml.XPathSelectElements("//Nodes.DataSet | //NRNodes.WebScreenDataSet"))
                    {
                        string dataSetName = dataSet.Attribute("Name").Value;

                        foreach(XElement binaryOperation in dataSet.XPathSelectElements(".//BinaryOperation[@Operator='Eq' or @Operator='NEq']"))
                        {
                            XElement[] operands = binaryOperation.Elements().ToArray();

                            if (operands.Length != 2 || !operands[0].Name.Equals(operands[1].Name))
                            {
                                continue;
                            }
                            
                            bool hasFinding = false;

                            switch (operands[0].Name.LocalName)
                            {
                                case "CompoundIdentifier":
                                    XElement identifier1 = operands[0].Element("Identifier");
                                    XElement identifier2 = operands[1].Element("Identifier");
                                    hasFinding =
                                        identifier1 != null &&
                                        identifier2 != null &&
                                        identifier1.Attribute("Ref") != null &&
                                        identifier2.Attribute("Ref") != null &&
                                        identifier1.Attribute("Ref").Value.Equals(identifier2.Attribute("Ref").Value);
                                    break;
                                case "Identifier":
                                    hasFinding =
                                        operands[0].Attribute("Ref") != null &&
                                        operands[1].Attribute("Ref") != null &&
                                        operands[0].Attribute("Ref").Value.Equals(operands[1].Attribute("Ref").Value);
                                    break;
                                case "Text":
                                case "Integer":
                                case "Decimal":
                                case "Boolean":
                                case "Date":
                                case "DateTime":
                                case "Time":
                                    hasFinding =
                                        operands[0].Attribute("Value") != null &&
                                        operands[1].Attribute("Value") != null &&
                                        operands[0].Attribute("Value").Value.Equals(operands[1].Attribute("Value").Value);
                                    break;
                            }

                            if (hasFinding)
                            {
                                findings.Add(new Finding(FindingType.AggregateWithSelfComparison, fragmentName, dataSetName));
                                break;
                            }
                        }
                    }
                }

                return findings;
            }

            /// <summary>
            /// Scans for dynamic sort variables used in aggregates with a default value missing brackets (i.e. `Entity.Attribute` instead of `{Entity}.[Attribute]`).
            /// </summary>
            /// <returns>List of findings.</returns>
            public List<Finding> ScanDynamicSortsMissingBrackets()
            {
                var findings = new List<Finding>();

                foreach (string fragmentName in _oml.DumpFragmentsNames())
                {
                    if (!fragmentName.StartsWith("NodesShownInESpaceTree"))
                    {
                        continue;
                    }

                    XElement fragmentXml = _oml.GetFragmentXml(fragmentName);

                    foreach (XElement sortIdentifier in fragmentXml.XPathSelectElements("//NRNodes.WebScreenDataSet//Sorts/AttributeSort[@IsDynamic='Yes']//Identifier[starts-with(@Ref, 'Variables.LocalVariable')]"))
                    {
                        string sortVariableKey = Regex.Replace(sortIdentifier.Attribute("Ref").Value, @".*\.", string.Empty);
                        XElement sortVariable = fragmentXml.XPathSelectElement($"//Variables.LocalVariable[@Key='{sortVariableKey}']");

                        if (sortVariable == null)
                        {
                            continue;
                        }

                        XElement sortVariableDefaultText = sortVariable.XPathSelectElement("./DefaultValue/ParsedExpression/Text[@Value]");

                        if (sortVariableDefaultText == null)
                        {
                            continue;
                        }
                        
                        string defaultText = sortVariableDefaultText.Attribute("Value").Value.Trim();

                        if (!string.IsNullOrEmpty(defaultText) && !Regex.IsMatch(defaultText, @"^(\{[^}]+\}\.)?\[[^\]]+\]$"))
                        {
                            findings.Add(new Finding(FindingType.DynamicSortMissingBrackets, fragmentName, sortVariable.Attribute("Name").Value));
                        }
                    }
                }

                return findings;
            }

            /// <summary>
            /// Scans for dynamic sort variables used in aggregates with a default value that does not correspond to a valid `{Entity}.[Attribute]`.
            /// </summary>
            /// <returns>List of findings.</returns>
            public List<Finding> ScanDynamicSortsWithInvalidEntityOrAttribute()
            {
                var findings = new List<Finding>();

                foreach (string fragmentName in _oml.DumpFragmentsNames())
                {
                    if (!fragmentName.StartsWith("NodesShownInESpaceTree"))
                    {
                        continue;
                    }

                    XElement fragmentXml = _oml.GetFragmentXml(fragmentName);

                    foreach (XElement dataSet in fragmentXml.Descendants("NRNodes.WebScreenDataSet"))
                    {
                        foreach (XElement sortIdentifier in dataSet.XPathSelectElements(".//Sorts/AttributeSort[@IsDynamic='Yes']//Identifier[starts-with(@Ref, 'Variables.LocalVariable:')]"))
                        {
                            string sortVariableKey = Regex.Replace(sortIdentifier.Attribute("Ref").Value, @".*\.", string.Empty);
                            XElement sortVariable = fragmentXml.XPathSelectElement($"//Variables.LocalVariable[@Key='{sortVariableKey}']");

                            if (sortVariable == null)
                            {
                                continue;
                            }

                            XElement sortVariableDefaultText = sortVariable.XPathSelectElement("./DefaultValue/ParsedExpression/Text[@Value]");

                            if (sortVariableDefaultText == null)
                            {
                                continue;
                            }

                            Match match = Regex.Match(sortVariableDefaultText.Attribute("Value").Value.Trim(), @"^(\{([^}]+)\}\.)?\[([^\]]+)\]$");

                            if (!match.Success)
                            {
                                continue;
                            }

                            bool hasFinding = true;

                            string entityName = match.Groups[2].Value;
                            string attributeName = match.Groups[3].Value;

                            if (!string.IsNullOrEmpty(entityName))
                            {
                                foreach (XElement addSourceOperation in dataSet.XPathSelectElements(".//DataSetOperations.AddSource[starts-with(@Source, 'Entity:') or starts-with(@Source, 'ReferenceEntity:')]"))
                                {
                                    string entityKey = Regex.Replace(addSourceOperation.Attribute("Source").Value, @".*\.", string.Empty);
                                    Entity entity = _eSpace.GetTokenByKey<Entity>(entityKey);

                                    if (entity != null && !entity.Name.Equals(entityName, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        continue;
                                    }

                                    // Entity found, now let's see if we can find the attribute and remove the finding status
                                    if (entity.Attributes.Any(x => x.Name.Equals(attributeName, StringComparison.InvariantCultureIgnoreCase)))
                                    {
                                        hasFinding = false;
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                foreach (XElement calculatedAttribute in dataSet.XPathSelectElements("//CalculatedAttribute | //GroupByAttribute"))
                                {
                                    XAttribute nameAttr = calculatedAttribute.Attribute("Name") ?? calculatedAttribute.Attribute("CustomName");

                                    if (nameAttr != null && nameAttr.Value.Equals(attributeName, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        hasFinding = false;
                                        break;
                                    }
                                }
                            }

                            if (hasFinding)
                            {
                                findings.Add(new Finding(FindingType.DynamicSortWithInvalidEntityOrAttribute, fragmentName, sortVariable.Attribute("Name").Value));
                            }
                        }
                    }
                }

                return findings;
            }

            /// <summary>
            /// Searches for entity attributes with Integration Builder default values that are not wrapped in some sanitizing function and that can be surfaced on the UI, which could provide a less than optimal UX.
            /// 
            /// The sanitizing wrapper functions should clear Integration Builder default values, so that the UI does not show odd-looking values like `<ib>NULL</ib>` or `-1999999991`.
            /// </summary>
            /// <param name="wrapperFunctions">List of sanitizing wrapper function names.</param>
            /// <returns>List of findings.</returns>
            public List<Finding> ScanUnwrappedIntegrationBuilderEntityAttributesShownOnUI(string[] wrapperFunctions)
            {
                var findings = new List<Finding>();

                var uiItems = new List<UserInterface>();
                uiItems.AddRange(_eSpace.Screens);
                uiItems.AddRange(_eSpace.Blocks);

                foreach (UserInterface ui in uiItems)
                {
                    var ibAttributesUsedInUi = new List<Entity.Attribute>();

                    foreach (CustomWidget widget in UserInterface.GetAllWidgetsHierarchy(ui).Where(x => x is CustomWidget).Cast<CustomWidget>())
                    {
                        ParsedExpression expression = null;

                        if (widget.ValueProperty != null)
                        {
                            expression = widget.ValueProperty.ValueExpression;
                        }
                        else if (widget.VariableProperty != null)
                        {
                            expression = widget.VariableProperty.ValueExpression;
                        }

                        var operands = GetAllOperandsHierarchy(expression);

                        // If any of the wrapper functions is found, we'll assume that this IB attribute is wrapped and will move on
                        var wrapperCallOperands =
                            from operand in operands
                            where operand is CallOperand callOperand &&
                                  callOperand.RefObject != null &&
                                  wrapperFunctions.Contains(callOperand.RefObject.OriginalName)
                            select operand;

                        // When no wrapper function could be found, let's add any found IB attributes to the list
                        if (wrapperCallOperands.Count() == 0)
                        {
                            var attributes =
                                from operand in operands
                                where operand is IdentifierOperand identifierOperand &&
                                      identifierOperand.RefObject is Entity.Attribute attribute &&
                                      attribute.IsIntegrationBuilderDefaultValue
                                select (Entity.Attribute)((IdentifierOperand)operand).RefObject;

                            foreach (Entity.Attribute attribute in attributes)
                            {
                                if (!ibAttributesUsedInUi.Contains(attribute))
                                {
                                    ibAttributesUsedInUi.Add(attribute);
                                }
                            }
                        }
                    }

                    // For each IB attribute used in this UI, check if there exists an Assign node somewhere wrapping the same attribute in a wrapper function
                    foreach (Entity.Attribute ibAttribute in ibAttributesUsedInUi)
                    {
                        bool attributeIsWrapped = false;

                        var actions = new List<Action>();
                        actions.AddRange(ui.DataActions);
                        actions.AddRange(ui.ClientActions);

                        var assignments =
                            from action in actions
                            from assign in action.Nodes.Where(x => x is Assign).Cast<Assign>()
                            from assignment in assign.Assignments
                            select assignment;

                        foreach (var assignment in assignments)
                        {
                            var variableOperands = GetAllOperandsHierarchy(assignment.Variable);
                            var valueOperands = GetAllOperandsHierarchy(assignment.Value);

                            if (variableOperands.Any(x => x is IdentifierOperand identifierOperand && ibAttribute.Equals(identifierOperand.RefObject)) &&
                                valueOperands.Any(x => x is CallOperand callOperand && callOperand.RefObject != null && wrapperFunctions.Contains(callOperand.RefObject.OriginalName)) &&
                                valueOperands.Any(x => x is IdentifierOperand identifierOperand && ibAttribute.Equals(identifierOperand.RefObject)))
                            {
                                attributeIsWrapped = true;
                                break;
                            }
                        }

                        if (!attributeIsWrapped)
                        {
                            findings.Add(new Finding(FindingType.UnwrappedIntegrationBuilderEntityAttributeShownOnUI, "", $"{ui.Name}->{ibAttribute.Entity.Name}.{ibAttribute.Name}"));
                        }
                    }
                }

                return findings;
            }
        }
    }
}
