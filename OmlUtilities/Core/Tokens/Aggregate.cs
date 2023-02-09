using OmlUtilities.Core.Tokens.Helper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OmlUtilities.Core.Tokens
{
    public class Aggregate : NamedToken
    {
        public abstract class Source : KeyedToken
        {
            public readonly CombineSourcesOperation CombineSourcesOperation = null;
            public readonly AddSourceOperation AddSourceOperation = null;

            public string Name
            {
                get
                {
                    string customName = ElementHelper.GetAttribute<string>(_xml, "CustomName");

                    if (string.IsNullOrEmpty(customName))
                    {
                        return OriginalName;
                    }

                    if (ElementHelper.TestAttribute(_xml, "CustomNameIsSuffix", "Yes"))
                    {
                        return OriginalName + customName;
                    }
                    else
                    {
                        return customName;
                    }
                }
            }

            public abstract string OriginalName { get; }

            public Source(XElement xml, CombineSourcesOperation combineSourcesOperation, AddSourceOperation addSourceOperation, ESpace eSpace) : base(xml, eSpace)
            {
                CombineSourcesOperation = combineSourcesOperation;
                AddSourceOperation = addSourceOperation;
            }

            public override string ToString()
            {
                string name = Name;
                string originalName = OriginalName;
                string str = $"{GetType().Name} {name}";
                if (!name.Equals(originalName))
                {
                    str += $" ({originalName})";
                }
                return str;
            }
        }

        public class EntitySource : Source
        {
            public Entity Entity => ESpace.GetTokenByKey<Entity>(AddSourceOperation.SourceKey);

            public override string OriginalName
            {
                get
                {
                    Entity entity = Entity;
                    return entity != null ? entity.Name : "";
                }
            }

            public EntitySource(XElement xml, CombineSourcesOperation combineSourcesOperation, AddSourceOperation addSourceOperation, ESpace eSpace) : base(xml, combineSourcesOperation, addSourceOperation, eSpace)
            {
                // Default constructor
            }
        }

        public class StructureSource : Source
        {
            public Structure Structure => ESpace.GetTokenByKey<Structure>(AddSourceOperation.SourceKey);

            public override string OriginalName
            {
                get
                {
                    Structure structure = Structure;
                    return structure != null ? structure.Name : "";
                }
            }

            public StructureSource(XElement xml, CombineSourcesOperation combineSourcesOperation, AddSourceOperation addSourceOperation, ESpace eSpace) : base(xml, combineSourcesOperation, addSourceOperation, eSpace)
            {
                // Default constructor
            }
        }

        public abstract class Operation : KeyedToken
        {
            public readonly Aggregate Aggregate = null;

            public string ResultingType => ElementHelper.GetAttribute<string>(_xml, "ResultingType");

            public Operation(XElement xml, Aggregate aggregate, ESpace eSpace) : base(xml, eSpace)
            {
                Aggregate = aggregate;
            }
        }

        public class AddSourceOperation : Operation
        {
            public ReadOnlyCollection<Entity.Attribute> AttributesInPreview
            {
                get
                {
                    var attributes = new List<Entity.Attribute>();
                    foreach (XElement attributeReferenceXml in _xml.XPathSelectElements("./AttributesInPreview/AttributeReference[@Attribute]"))
                    {
                        string attributeKey = Regex.Replace(attributeReferenceXml.Attribute("Attribute").Value, @".*\.", "");
                        Entity.Attribute attribute = ESpace.GetTokenByKey<Entity.Attribute>(attributeKey);
                        if (attribute != null)
                        {
                            attributes.Add(attribute);
                        }
                    }
                    return attributes.AsReadOnly();
                }
            }

            public string Source => ElementHelper.GetAttribute<string>(_xml, "Source");

            public string SourceKey => Regex.Replace(Source, @".*\.", "");

            public string SourcePrefix => Regex.Replace(Source, @":.*", "");

            public AddSourceOperation(XElement xml, Aggregate aggregate, ESpace eSpace) : base(xml, aggregate, eSpace)
            {
                // Default constructor
            }
        }

        public class CombineSourcesOperation : Operation
        {
            protected List<Source> _sources = new List<Source>();
            protected List<JoinCondition> _joinConditions = new List<JoinCondition>();
            protected List<Filter> _filters = new List<Filter>();

            public ReadOnlyCollection<Source> Sources => _sources.AsReadOnly();

            public ReadOnlyCollection<JoinCondition> JoinConditions => _joinConditions.AsReadOnly();

            public ReadOnlyCollection<Filter> Filters => _filters.AsReadOnly();

            public CombineSourcesOperation(XElement xml, Aggregate aggregate, ESpace eSpace) : base(xml, aggregate, eSpace)
            {
                // Parse sources
                foreach (XElement sourceXml in _xml.XPathSelectElements("./Sources/DataSource"))
                {
                    string operationKey = Regex.Replace(sourceXml.Attribute("SourceOperation").Value, @".*\.", "");
                    AddSourceOperation addSourceOperation = Aggregate.AddSourceOperations.FirstOrDefault(x => x.Key.Equals(operationKey));
                    if (addSourceOperation != null)
                    {
                        switch (addSourceOperation.SourcePrefix)
                        {
                            case "Entity":
                            case "ReferenceEntity":
                                _sources.Add(new EntitySource(sourceXml, this, addSourceOperation, eSpace));
                                break;
                            case "Structure":
                            case "ReferenceStructure":
                                _sources.Add(new StructureSource(sourceXml, this, addSourceOperation, eSpace));
                                break;
                        }
                    }
                }

                // Parse joins
                foreach (XElement joinConditionXml in _xml.XPathSelectElements("./Joins/JoinCondition"))
                {
                    _joinConditions.Add(new JoinCondition(joinConditionXml, this, eSpace));
                }

                // Parse filters
                foreach (XElement filterXml in _xml.XPathSelectElements("./Filters/Filter"))
                {
                    _filters.Add(new Filter(filterXml, this, eSpace));
                }

                // Parse sorting
                // TODO
            }
        }

        public class JoinCondition : KeyedToken
        {
            public readonly CombineSourcesOperation CombineSourcesOperation = null;

            public readonly ParsedExpression Condition = null;

            public enum JoinType
            {
                Inner,
                Left,
                Full
            }

            public JoinType Type
            {
                get
                {
                    return ElementHelper.GetAttribute<string>(_xml, "JoinType") switch
                    {
                        "Left" => JoinType.Left,
                        "Full" => JoinType.Full,
                        _ => JoinType.Inner
                    };
                }
            }

            public string LeftSourceKey => Regex.Replace(ElementHelper.GetAttribute<string>(_xml, "LeftSource"), @".*\.", "");

            public string RightSourceKey => Regex.Replace(ElementHelper.GetAttribute<string>(_xml, "RightSource"), @".*\.", "");

            public EntitySource LeftSource => (EntitySource)CombineSourcesOperation.Sources.FirstOrDefault(x => x.Key.Equals(LeftSourceKey));

            public EntitySource RightSource => (EntitySource)CombineSourcesOperation.Sources.FirstOrDefault(x => x.Key.Equals(RightSourceKey));

            public JoinCondition(XElement xml, CombineSourcesOperation combineSourcesOperation, ESpace eSpace) : base(xml, eSpace)
            {
                CombineSourcesOperation = combineSourcesOperation;

                XElement parsedExpressionXml = _xml.XPathSelectElement("./Condition/ParsedExpression");
                if (parsedExpressionXml != null)
                {
                    Condition = new ParsedExpression(parsedExpressionXml, eSpace);
                }
            }

            public override string ToString()
            {
                string leftSourceStr = LeftSource.ToString();
                string rightSourceStr = RightSource.ToString();
                string str = GetType().Name;
                if (!string.IsNullOrEmpty(leftSourceStr))
                {
                    str += $" {leftSourceStr}";
                }
                str += $" {Type}";
                if (!string.IsNullOrEmpty(rightSourceStr))
                {
                    str += $" {rightSourceStr}";
                }
                return str;
            }
        }

        public class Filter : KeyedToken
        {
            public readonly CombineSourcesOperation CombineSourcesOperation = null;

            public readonly ParsedExpression Condition = null;

            public override string ToString()
            {
                return $"{GetType().Name} {Condition}";
            }

            public Filter(XElement xml, CombineSourcesOperation combineSourcesOperation, ESpace eSpace) : base(xml, eSpace)
            {
                CombineSourcesOperation = combineSourcesOperation;

                XElement parsedExpressionXml = _xml.XPathSelectElement("./Condition/ParsedExpression");
                if (parsedExpressionXml != null)
                {
                    Condition = new ParsedExpression(parsedExpressionXml, eSpace);
                }
            }
        }

        protected List<Operation> _operations = new List<Operation>();

        public readonly ParsedExpression MaxRecords = null;

        public ReadOnlyCollection<Operation> Operations => _operations.AsReadOnly();

        public ReadOnlyCollection<AddSourceOperation> AddSourceOperations =>
            (from x in _operations
             where x is AddSourceOperation
             select (AddSourceOperation)x).ToList().AsReadOnly();

        public ReadOnlyCollection<Source> Sources
        {
            get
            {
                CombineSourcesOperation combineSourcesOperation = (CombineSourcesOperation)_operations.FirstOrDefault(x => x is CombineSourcesOperation);
                return combineSourcesOperation != null ? combineSourcesOperation.Sources : new List<Source>().AsReadOnly();
            }
        }

        public ReadOnlyCollection<EntitySource> EntitySources => 
            (from x in Sources
             where x is EntitySource
             select (EntitySource)x).ToList().AsReadOnly();

        public ReadOnlyCollection<StructureSource> StructureSources =>
            (from x in Sources
             where x is StructureSource
             select (StructureSource)x).ToList().AsReadOnly();

        public ReadOnlyCollection<JoinCondition> JoinConditions
        {
            get
            {
                CombineSourcesOperation combineSourcesOperation = (CombineSourcesOperation)_operations.FirstOrDefault(x => x is CombineSourcesOperation);
                return combineSourcesOperation != null ? combineSourcesOperation.JoinConditions : new List<JoinCondition>().AsReadOnly();
            }
        }

        public ReadOnlyCollection<Filter> Filters
        {
            get
            {
                CombineSourcesOperation combineSourcesOperation = (CombineSourcesOperation)_operations.FirstOrDefault(x => x is CombineSourcesOperation);
                return combineSourcesOperation != null ? combineSourcesOperation.Filters : new List<Filter>().AsReadOnly();
            }
        }

        public Aggregate(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            XElement maxRecordsXml = xml.XPathSelectElement("./MaxRecords/ParsedExpression");
            if (maxRecordsXml != null)
            {
                MaxRecords = new ParsedExpression(maxRecordsXml, eSpace);
            }

            foreach (XElement operationXml in xml.XPathSelectElements("./Table/DataTable/TableOperations/*"))
            {
                switch (operationXml.Name.LocalName)
                {
                    case "DataSetOperations.AddSource":
                        _operations.Add(new AddSourceOperation(operationXml, this, eSpace));
                        break;
                }
            }

            // Create CombineSources operation last, because it needs the AddSource operations in order to work properly
            XElement combineSourcesOperationXml = xml.XPathSelectElement("./Table/DataTable/TableOperations/DataSetOperations.CombineSources");
            if (combineSourcesOperationXml != null)
            {
                _operations.Add(new CombineSourcesOperation(combineSourcesOperationXml, this, eSpace));
            }
        }
    }
}
