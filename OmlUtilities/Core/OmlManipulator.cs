using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Linq;

namespace OmlUtilities.Core
{
    partial class Oml
    {
        public class OmlManipulator
        {
            protected Oml _oml;

            public OmlManipulator(Oml oml)
            {
                _oml = oml;
            }

            public void SetHeader(string headerName, string headerValue)
            {
                if (!_oml.Headers.ContainsKey(headerName))
                {
                    throw new OmlException($"Header name \"{headerName}\" was not found.");
                }
                _oml.Headers[headerName].SetValue(headerValue);
            }

            /// <summary>
            /// Returns a single XML document containing all fragments.
            /// </summary>
            /// <returns>Full XML document.</returns>
            public XDocument GetXmlDocument()
            {
                XElement root = new XElement("OML");
                foreach (string fragmentName in _oml.DumpFragmentsNames())
                {
                    XElement fragment = GetFragment(fragmentName);
                    root.Add(fragment);
                }
                return new XDocument(root);
            }

            public XElement GetFragment(string fragmentName)
            {
                XElement fragmentXml = new XElement(_oml.GetFragmentXml(fragmentName));
                fragmentXml.SetAttributeValue("FragmentName", fragmentName);
                return fragmentXml;
            }

            public void SetFragment(XElement fragmentXml)
            {
                // Make a copy of the provided XElement
                fragmentXml = new XElement(fragmentXml);

                // Get fragment name attribute and remove it from XElement
                var fragmentNameAttribute = fragmentXml.Attribute("FragmentName");
                if (fragmentNameAttribute == null)
                {
                    throw new OmlException("The provided fragment XML does not have a 'FragmentName' attribute.");
                }
                string fragmentName = fragmentNameAttribute.Value;
                fragmentNameAttribute.Remove();

                _oml.ReplaceFragmentXml(fragmentName, fragmentXml);
            }

            public void DeleteFragment(string fragmentName)
            {
                _oml.ReplaceFragmentXml(fragmentName, null);
            }
        }
    }
}
