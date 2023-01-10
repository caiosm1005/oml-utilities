using System.Xml.Linq;

namespace OmlUtilities.Core
{
    partial class Oml
    {
        public class OmlFragmentReader
        {
            /// <summary>
            /// Assembly instance.
            /// </summary>
            protected object _instance;

            /// <summary>
            /// Returns XML element of the fragment.
            /// </summary>
            /// <returns>XML element of the fragment.</returns>
            public XElement GetXElement()
            {
                XElement? xelement = AssemblyUtility.ExecuteInstanceMethod<XElement>(_instance, "ToXElement");
                if (xelement == null)
                {
                    throw new Exception("Unable to get XElement from fragment. Null returned.");
                }
                return xelement;
            }

            /// <summary>
            /// Closes the reader.
            /// </summary>
            public void Close()
            {
                AssemblyUtility.ExecuteInstanceMethod<object>(_instance, "Close");
            }

            /// <summary>
            /// Creates an fragment XML reader instance.
            /// </summary>
            /// <param name="oml">OML instance which owns the fragment.</param>
            /// <param name="fragmentName">Name of the fragment to be parsed.</param>
            public OmlFragmentReader(Oml oml, string fragmentName)
            {
                object? localInstance = AssemblyUtility.ExecuteInstanceMethod<object>(oml._instance, "GetFragmentXmlReader", new object[] { fragmentName });
                if (localInstance == null)
                {
                    throw new Exception("Unable to get fragment XML reader instance. Null returned.");
                }
                _instance = localInstance;
            }
        }
    }
}
