using System.Xml.Linq;

namespace OmlUtilities.Core.Tokens
{
    public abstract class Token
    {
        protected XElement _xml = null;
        protected ESpace _eSpace = null;

        public ESpace ESpace => _eSpace;

        public XElement Xml => new XElement(_xml);

        public Token(XElement xml, ESpace eSpace)
        {
            _xml = xml;
            _eSpace = eSpace;
        }
    }
}
