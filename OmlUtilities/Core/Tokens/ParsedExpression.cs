using OmlUtilities.Core.Tokens.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OmlUtilities.Core.Tokens
{
    public class ParsedExpression : Token
    {
        private static List<Operand> GetInternalOperandsHierarchy(Operand operand)
        {
            var operands = new List<Operand>
            {
                operand
            };

            if (operand is BinaryOperationOperand binaryOperationOperand)
            {
                Operand leftOperand = binaryOperationOperand.LeftOperand;
                Operand rightOperand = binaryOperationOperand.RightOperand;
                if (leftOperand != null)
                {
                    operands.AddRange(GetInternalOperandsHierarchy(leftOperand));
                }
                if (rightOperand != null)
                {
                    operands.AddRange(GetInternalOperandsHierarchy(rightOperand));
                }
            }
            else if (operand is UnaryOperationOperand unaryOperationOperand)
            {
                Operand childOperand = unaryOperationOperand.ChildOperand;
                if (childOperand != null)
                {
                    operands.AddRange(GetInternalOperandsHierarchy(childOperand));
                }
            }
            else if (operand is CompoundIdentifierOperand compoundIdentifierOperand)
            {
                Operand childIdentifier = compoundIdentifierOperand.ChildIdentifier;
                if (childIdentifier != null)
                {
                    operands.AddRange(GetInternalOperandsHierarchy(childIdentifier));
                }
            }
            else if (operand is CallOperand callOperand)
            {
                foreach (CallOperandArgument argument in callOperand.Arguments)
                {
                    Operand childOperand = argument.ChildOperand;
                    if (childOperand != null)
                    {
                        operands.AddRange(GetInternalOperandsHierarchy(childOperand));
                    }
                }
            }

            return operands;
        }

        public static string XmlToString(XElement xml, ESpace eSpace)
        {
            if (xml == null || !xml.Name.LocalName.Equals("ParsedExpression"))
            {
                return "";
            }
            return new ParsedExpression(xml, eSpace).ToString();
        }

        public static ReadOnlyCollection<Operand> GetAllOperandsHierarchy(ParsedExpression parsedExpression)
        {
            if (parsedExpression != null)
            {
                Operand rootOperand = parsedExpression.ChildOperand;
                if (rootOperand != null)
                {
                    return GetInternalOperandsHierarchy(rootOperand).AsReadOnly();
                }
            }
            return new List<Operand>().AsReadOnly();
        }

        public static OperatorType GetOperatorType(string operatorValue)
        {
            return operatorValue switch
            {
                "Minus" => OperatorType.Minus,
                "Plus" => OperatorType.Plus,
                "Mul" => OperatorType.Multiplication,
                "Div" => OperatorType.Division,
                "Or" => OperatorType.Or,
                "And" => OperatorType.And,
                "LT" => OperatorType.LessThan,
                "GT" => OperatorType.GreaterThan,
                "LE" => OperatorType.LessOrEqual,
                "GE" => OperatorType.GreaterOrEqual,
                "Eq" => OperatorType.Equal,
                "NEq" => OperatorType.NotEqual,
                "Not" => OperatorType.Not,
                _ => OperatorType.Unknown
            };
        }

        public abstract class Operand : Token
        {
            public static Operand CreateInstance(XElement xml, ESpace eSpace)
            {
                if (xml == null)
                {
                    return null;
                }
                return xml.Name.LocalName switch
                {
                    "Text" => new TextOperand(xml, eSpace),
                    "Integer" => new IntegerOperand(xml, eSpace),
                    "Decimal" => new DecimalOperand(xml, eSpace),
                    "Boolean" => new BooleanOperand(xml, eSpace),
                    "BinaryOperation" => new BinaryOperationOperand(xml, eSpace),
                    "UnaryOperation" => new UnaryOperationOperand(xml, eSpace),
                    "Identifier" => new IdentifierOperand(xml, eSpace),
                    "CompoundIdentifier" => new CompoundIdentifierOperand(xml, eSpace),
                    "Call" => new CallOperand(xml, eSpace),
                    _ => null,
                };
            }

            public string FormattingBefore => ElementHelper.GetAttribute<string>(_xml, "FormattingBefore");

            public string FormattingAfter => ElementHelper.GetAttribute<string>(_xml, "FormattingAfter");

            protected Operand(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                // Default constructor
            }
        }

        public class TextOperand : Operand
        {
            public string Value => ElementHelper.GetAttribute<string>(_xml, "Value");

            public TextOperand(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                // Default constructor
            }

            public override string ToString()
            {
                return Value;
            }
        }

        public class IntegerOperand : Operand
        {
            public long Value => ElementHelper.GetAttribute<long>(_xml, "Value");

            public IntegerOperand(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                // Default constructor
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        public class DecimalOperand : Operand
        {
            public decimal Value => ElementHelper.GetAttribute<decimal>(_xml, "Value");

            public DecimalOperand(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                // Default constructor
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        public class BooleanOperand : Operand
        {
            public bool Value => ElementHelper.GetAttribute<bool>(_xml, "Value");

            public BooleanOperand(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                // Default constructor
            }

            public override string ToString()
            {
                return Value ? "True" : "False";
            }
        }

        public class BinaryOperationOperand : Operand
        {
            public readonly Operand LeftOperand = null;
            public readonly Operand RightOperand = null;

            public OperatorType Operator => GetOperatorType(ElementHelper.GetAttribute<string>(_xml, "Operator"));

            public BinaryOperationOperand(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                LeftOperand = CreateInstance(xml.XPathSelectElement("./*[1]"), eSpace);
                RightOperand = CreateInstance(xml.XPathSelectElement("./*[2]"), eSpace);
            }

            public override string ToString()
            {
                if (Operator.Equals(OperatorType.Plus))
                {
                    return $"{LeftOperand}{RightOperand}";
                }
                return "";
            }
        }

        public class UnaryOperationOperand : Operand
        {
            public readonly Operand ChildOperand = null;

            public OperatorType Operator => GetOperatorType(ElementHelper.GetAttribute<string>(_xml, "Operator"));

            public UnaryOperationOperand(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                ChildOperand = CreateInstance(xml.XPathSelectElement("./*"), eSpace);
            }

            public override string ToString()
            {
                return "";
            }
        }

        public abstract class BaseIdentifierOperand : Operand
        {
            public string Ref => ElementHelper.GetAttribute<string>(Xml, "Ref");

            public string RefKey => Regex.Replace(Ref, @".*\.", "");

            public KeyedToken RefObject => ESpace.GetTokenByKey<KeyedToken>(RefKey);

            public string NameRef => ElementHelper.GetAttribute<string>(Xml, "NameRef");

            public bool IsInsideIteration => ElementHelper.TestAttribute(_xml, "IsInsideIteration", "Yes");

            protected BaseIdentifierOperand(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                // Default constructor
            }

            public override string ToString()
            {
                // TODO: Display name of static entity record?
                return "";
            }
        }

        public class IdentifierOperand : BaseIdentifierOperand
        {
            public IdentifierOperand(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                // Default constructor
            }
        }

        public class CompoundIdentifierOperand : IdentifierOperand
        {
            public readonly BaseIdentifierOperand ChildIdentifier = null;

            public CompoundIdentifierOperand(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                ChildIdentifier = (BaseIdentifierOperand)CreateInstance(xml.XPathSelectElement("./*"), eSpace);
            }
        }

        public class CallOperandArgument : Token
        {
            public readonly Operand ChildOperand = null;

            public string Ref => ElementHelper.GetAttribute<string>(Xml, "Ref");

            public string RefKey => Regex.Replace(Ref, @".*\.", "");

            public Variable RefObject => ESpace.GetTokenByKey<Variable>(RefKey);

            public string Name
            {
                get
                {
                    Variable variable = RefObject;
                    if (variable != null)
                    {
                        return variable.Name;
                    }
                    return null;
                }
            }

            public CallOperandArgument(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                ChildOperand = Operand.CreateInstance(xml.XPathSelectElement("./*"), eSpace);
            }

            public override string ToString()
            {
                if (ChildOperand != null)
                {
                    return ChildOperand.ToString();
                }
                return "";
            }
        }

        public class CallOperand : Operand
        {
            protected List<CallOperandArgument> _arguments = new List<CallOperandArgument>();

            public ReadOnlyCollection<CallOperandArgument> Arguments => _arguments.AsReadOnly();

            public string Ref => ElementHelper.GetAttribute<string>(Xml, "Ref");

            public string RefKey => Regex.Replace(Ref, @".*\.", "");

            public Action RefObject => ESpace.GetTokenByKey<Action>(RefKey);

            public string NameRef => ElementHelper.GetAttribute<string>(Xml, "NameRef");

            public string Name
            {
                get
                {
                    Action action = RefObject;
                    if (action != null)
                    {
                        return action.Name;
                    }
                    return null;
                }
            }

            public CallOperand(XElement xml, ESpace eSpace) : base(xml, eSpace)
            {
                foreach (XElement argumentXml in xml.Elements("Argument"))
                {
                    _arguments.Add(new CallOperandArgument(argumentXml, eSpace));
                }
            }

            public override string ToString()
            {
                return "";
            }
        }

        public readonly Operand ChildOperand = null;

        public enum OperatorType
        {
            Unknown,
            Minus,
            Plus,
            Multiplication,
            Division,
            Or,
            And,
            LessThan,
            GreaterThan,
            LessOrEqual,
            GreaterOrEqual,
            Equal,
            NotEqual,
            Not
        }

        public ParsedExpression(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            ChildOperand = Operand.CreateInstance(xml.XPathSelectElement("./*"), eSpace);
        }

        public override string ToString()
        {
            return ChildOperand.ToString();
        }
    }
}
