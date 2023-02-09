using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OmlUtilities.Core.Tokens
{
    public class ESpace : KeyedToken
    {
        protected Dictionary<string, XElement> _fragmentsXml = new Dictionary<string, XElement>();
        protected List<KeyedToken> _registeredTokens = new List<KeyedToken>();
        protected List<Reference> _references = new List<Reference>();
        protected List<Entity> _entities = new List<Entity>();
        protected List<Action> _clientActions = new List<Action>();
        protected List<Structure> _structures = new List<Structure>();
        protected List<Block> _blocks = new List<Block>();
        protected List<Screen> _screens = new List<Screen>();

        public XElement GetFragmentXml(string fragmentName)
        {
            if (_fragmentsXml.ContainsKey(fragmentName))
            {
                return _fragmentsXml[fragmentName];
            }
            return null;
        }

        public void RegisterToken(KeyedToken token)
        {
            _registeredTokens.Add(token);
        }

        public T GetTokenByKey<T>(string key) where T : KeyedToken
        {
            KeyedToken token = _registeredTokens.FirstOrDefault(x => x.Key.Equals(key) && typeof(T).IsAssignableFrom(x.GetType()));
            return (T)token;
        }

        public ReadOnlyCollection<Reference> References => _references.AsReadOnly();
        public ReadOnlyCollection<Entity> Entities => _entities.AsReadOnly();
        public ReadOnlyCollection<Structure> Structures => _structures.AsReadOnly();
        public ReadOnlyCollection<Action> ClientActions => _clientActions.AsReadOnly();
        public ReadOnlyCollection<Screen> Screens => _screens.AsReadOnly();
        public ReadOnlyCollection<Block> Blocks => _blocks.AsReadOnly();

        public ESpace(XElement xml, Dictionary<string, XElement> fragmentsXml) : base(xml, null)
        {
            RegisterToken(this);
            _fragmentsXml = fragmentsXml;

            foreach (KeyValuePair<string, XElement> kv in fragmentsXml)
            {
                string fragmentName = kv.Key;
                XElement fragmentXml = kv.Value;

                switch (fragmentName)
                {
                    case "References":
                        foreach (XElement referenceXml in fragmentXml.Elements("Reference"))
                        {
                            _references.Add(new Reference(referenceXml, this));
                        }
                        break;
                    case "Entities":
                        foreach (XElement entityXml in fragmentXml.Elements("Entity"))
                        {
                            _entities.Add(new Entity(entityXml, this));
                        }
                        break;
                    case "Structures":
                        foreach (XElement structureXml in fragmentXml.Elements("Structure"))
                        {
                            _structures.Add(new Structure(structureXml, this));
                        }
                        break;
                    case "ClientActionFlows":
                        foreach (XElement clientActionXml in fragmentXml.Elements("NRFlows.ClientActionFlow"))
                        {
                            _clientActions.Add(new Action(clientActionXml, this));
                        }
                        break;
                    default:
                        if (fragmentName.StartsWith("NodesShownInESpaceTree"))
                        {
                            foreach (XElement blockXml in fragmentXml.Elements("NRNodes.WebBlock"))
                            {
                                _blocks.Add(new Block(blockXml, this));
                            }
                            foreach (XElement screenXml in fragmentXml.Elements("NRNodes.WebScreen"))
                            {
                                _screens.Add(new Screen(screenXml, this));
                            }
                        }
                        break;
                }
            }
        }
    }
}
