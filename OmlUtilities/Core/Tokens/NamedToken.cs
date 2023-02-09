using OmlUtilities.Core.Tokens.Helper;
using System;
using System.Xml.Linq;

namespace OmlUtilities.Core.Tokens
{
    public abstract class NamedToken : KeyedToken
    {
        public string Name => ElementHelper.GetAttribute<string>(_xml, "Name");
        public string Description => ElementHelper.GetAttribute<string>(_xml, "Description");
        public string CreatedBy => ElementHelper.GetAttribute<string>(_xml, "CreatedBy");
        public string LastModifiedBy => ElementHelper.GetAttribute<string>(_xml, "LastModifiedBy");
        public DateTime LastModifiedDate => ElementHelper.GetAttribute<DateTime>(_xml, "LastModifiedDate");

        public NamedToken(XElement xml, ESpace eSpace) : base(xml, eSpace)
        {
            // Default constructor
        }

        public override string ToString()
        {
            return $"{GetType().Name} {Name}".Trim();
        }
    }
}
