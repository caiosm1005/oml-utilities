using OmlUtilities.Core.Tokens.Helper;
using System.Xml.Linq;

namespace OmlUtilities.Core.Tokens
{
    public abstract class KeyedToken : Token
    {
        public string Key => ElementHelper.GetAttribute<string>(_xml, "Key");

        public KeyedToken(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            if (eSpace != null)
            {
                eSpace.RegisterToken(this);
            }
        }

        public override string ToString() 
        {
            return $"{GetType().Name} {Key}";
        }
    }
}
