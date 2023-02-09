using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OmlUtilities.Core.Tokens
{
    public class Reference : NamedToken
    {
        protected List<Entity> _referenceEntities = new List<Entity>();
        protected List<Structure> _referenceStructures = new List<Structure>();
        protected List<Action> _referenceServerActions = new List<Action>();
        protected List<Action> _referenceClientActions = new List<Action>();

        public ReadOnlyCollection<Entity> ReferenceEntities => _referenceEntities.AsReadOnly();
        public ReadOnlyCollection<Structure> ReferenceStructures => _referenceStructures.AsReadOnly();
        public ReadOnlyCollection<Action> ReferenceServerActions => _referenceServerActions.AsReadOnly();
        public ReadOnlyCollection<Action> ReferenceClientActions => _referenceClientActions.AsReadOnly();

        public Reference(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            // Parse entity references
            foreach (XElement referenceEntityXml in xml.XPathSelectElements("./ReferenceEntities/ReferenceEntity"))
            {
                _referenceEntities.Add(new Entity(referenceEntityXml, eSpace, this));
            }

            // Parse structure references
            foreach (XElement referenceStructureXml in xml.XPathSelectElements("./ReferenceStructures/ReferenceStructure"))
            {
                _referenceStructures.Add(new Structure(referenceStructureXml, eSpace));
            }

            // Parse server action references
            foreach (XElement actionXml in xml.XPathSelectElements("./ReferenceActions/ReferenceAction"))
            {
                _referenceServerActions.Add(new Action(actionXml, eSpace));
            }

            // Parse client action references
            foreach (XElement actionXml in xml.XPathSelectElements("./ReferenceClientActions/ReferenceClientAction"))
            {
                _referenceClientActions.Add(new Action(actionXml, eSpace));
            }
        }
    }
}
