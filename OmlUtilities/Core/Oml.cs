using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace OmlUtilities.Core
{
    public partial class Oml
    {
        /// <summary>
        /// Assembly instance.
        /// </summary>
        protected object _instance;

        /// <summary>
        /// Platform version used for loading the OML contents.
        /// </summary>
        public PlatformVersion PlatformVersion { get; }

        /// <summary>
        /// Dictionary of OML headers.
        /// </summary>
        public Dictionary<string, OmlHeader> Headers { get; }

        /// <summary>
        /// Returns a list of available fragment names.
        /// </summary>
        /// <returns>List of availabel fragment names.</returns>
        public List<string> GetFragmentNames()
        {
            return AssemblyUtility.ExecuteInstanceMethod<IEnumerable<string>>(_instance, "DumpFragmentsNames").ToList();
        }

        /// <summary>
        /// Returns the XML contents of a given fragment.
        /// </summary>
        /// <param name="fragmentName">Name of the desired fragment.</param>
        /// <returns>XML contents of the fragment.</returns>
        public XElement GetFragmentXml(string fragmentName)
        {
            OmlFragmentReader reader = new OmlFragmentReader(this, fragmentName);
            XElement fragmentXml = reader.GetXElement();
            reader.Close();
            fragmentXml.SetAttributeValue("FragmentName", fragmentName);
            return fragmentXml;
        }

        /// <summary>
        /// Sets the XML contents of a fragment.
        /// </summary>
        /// <param name="fragmentName">Name of the fragment to be set.</param>
        /// <param name="fragmentXml">New XML contents of the fragment.</param>
        public void SetFragmentXml(XElement fragmentXml)
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

            // Write fragment
            OmlFragmentWriter writer = new OmlFragmentWriter(this, fragmentName);
            writer.Write(fragmentXml);
            writer.Close();
        }

        /// <summary>
        /// Returns a single XML document containing all fragments.
        /// </summary>
        /// <returns>Full XML document.</returns>
        public XDocument GetXml()
        {
            XElement root = new XElement("OML");
            
            foreach(string fragmentName in GetFragmentNames())
            {
                XElement fragment = GetFragmentXml(fragmentName);
                fragment.SetAttributeValue("FragmentName", fragmentName);
                root.Add(fragment);
            }

            return new XDocument(root);
        }

        /// <summary>
        /// Exports the OML contents to an OML stream.
        /// </summary>
        /// <param name="outputStream">Destination stream to write the OML file contents to.</param>
        public void Save(Stream outputStream)
        {
            AssemblyUtility.ExecuteInstanceMethod<object>(_instance, "WriteTo", new object[] { outputStream });
        }

        /// <summary>
        /// Represents an OML file with manipulable OML headers and XML fragments.
        /// </summary>
        /// <param name="omlStream">Input stream containing the OML file contents.</param>
        public Oml(Stream omlStream)
        {
            if (AssemblyUtility.PlatformVersion == null)
            {
                throw new OmlException("Platform version must be defined before loading the OML content.");
            }

            Type assemblyType;
            PlatformVersion = AssemblyUtility.PlatformVersion;

            if (PlatformVersion == PlatformVersion.O_11_0)
            {
                assemblyType = AssemblyUtility.GetAssemblyType("OutSystems.Model.Implementation", "OutSystems.Model.Implementation.Oml.Oml");
            }
            else
            {
                assemblyType = AssemblyUtility.GetAssemblyType("OutSystems.Common", "OutSystems.Oml.Oml");
            }

            try
            {
                if (PlatformVersion == PlatformVersion.O_11_0)
                {
                    _instance = Activator.CreateInstance(assemblyType, new object[] { omlStream, false, null, null, null });
                }
                else
                {
                    _instance = Activator.CreateInstance(assemblyType, new object[] { omlStream, false, null, null });
                }

                // Create OML headers
                var headerList = new List<OmlHeader>
                {
                    new OmlHeader(this, "ActivationCode",       typeof(string), true),
                    new OmlHeader(this, "Name",                 typeof(string)),
                    new OmlHeader(this, "Description",          typeof(string)),
                    new OmlHeader(this, "ESpaceType",           typeof(string)),
                    new OmlHeader(this, "LastModifiedUTC",      typeof(DateTime)),
                    new OmlHeader(this, "NeedsRecover",         typeof(bool)),
                    new OmlHeader(this, "Signature",            typeof(string), true),
                    new OmlHeader(this, "Version",              typeof(Version)),
                    new OmlHeader(this, "LastUpgradeVersion",   typeof(Version))
                };
                Headers = headerList.ToDictionary(keySelector: e => e.HeaderName);

                // Set OML version to the current version
                Headers["Version"].SetValue(PlatformVersion.Version);
            }
            catch (Exception e)
            {
                if (e.GetBaseException() is AssemblyUtilityException)
                {
                    throw e.GetBaseException();
                }
                else if (e is OmlException)
                {
                    throw e;
                }
                else if (e.InnerException != null && e.InnerException.GetType().Name.Equals("UnsupportedNewerVersion"))
                {
                    throw e.InnerException;
                }
                else
                {
                    throw new OmlException("Unable to load OML. Make sure the given OML content is valid.", e);
                }
            }
        }
    }
}
