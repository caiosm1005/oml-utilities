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
        /// List of available fragment names.
        /// </summary>
        private List<string> _fragmentNames = null;

        /// <summary>
        /// Dictionary of available fragments.
        /// </summary>
        private Dictionary<string, XElement> _fragments = new Dictionary<string, XElement>();

        /// <summary>
        /// OML assembly instance.
        /// </summary>
        protected object _omlInstance;

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
        /// <returns>List of available fragment names.</returns>
        public List<string> DumpFragmentsNames()
        {
            if (_fragmentNames == null)
            {
                _fragmentNames = AssemblyUtility.ExecuteInstanceMethod<IEnumerable<string>>(_omlInstance, "DumpFragmentsNames").ToList();
            }
            return _fragmentNames;
        }

        /// <summary>
        /// Returns the XML contents of a given fragment.
        /// </summary>
        /// <param name="fragmentName">Name of the desired fragment.</param>
        /// <returns>XML contents of the fragment.</returns>
        public XElement GetFragmentXml(string fragmentName)
        {
            XElement fragmentXml;
            if (_fragments.ContainsKey(fragmentName))
            {
                fragmentXml = _fragments[fragmentName];
            }
            else {
                object reader = AssemblyUtility.ExecuteInstanceMethod<object>(_omlInstance, "GetFragmentXmlReader", new object[] { fragmentName });
                fragmentXml = AssemblyUtility.ExecuteInstanceMethod<XElement>(reader, "ToXElement");
                AssemblyUtility.ExecuteInstanceMethod<object>(reader, "Close");
                _fragments[fragmentName] = fragmentXml;
            }
            return fragmentXml;
        }

        /// <summary>
        /// Replaces a fragment XML.
        /// </summary>
        /// <param name="fragmentName">Name of the fragment to be replaced.</param>
        /// <param name="fragmentXml">New XML contents of the fragment.</param>
        public void ReplaceFragmentXml(string fragmentName, XElement fragmentXml)
        {
            if (fragmentXml != null)
            {
                object writer = AssemblyUtility.ExecuteInstanceMethod<object>(_omlInstance, "GetFragmentXmlWriter", new object[] { fragmentName });
                AssemblyUtility.ExecuteInstanceMethod<object>(writer, "Replace", new object[] { fragmentXml });
                AssemblyUtility.ExecuteInstanceMethod<object>(writer, "Close");
                _fragments[fragmentName] = fragmentXml;
                if (_fragmentNames != null && _fragmentNames.IndexOf(fragmentName) == -1)
                {
                    _fragmentNames.Add(fragmentName);
                }
            }
            else // Delete fragment when the provided XML is null
            {
                AssemblyUtility.ExecuteInstanceMethod<object>(_omlInstance, "DeleteFragment", new object[] { fragmentName });
                _fragments.Remove(fragmentName);
                if (_fragmentNames != null)
                {
                    _fragmentNames.Remove(fragmentName);
                }
            }
        }

        /// <summary>
        /// Exports the OML contents to an OML stream.
        /// </summary>
        /// <param name="outputStream">Destination stream to write the OML file contents to.</param>
        public void WriteTo(Stream outputStream)
        {
            AssemblyUtility.ExecuteInstanceMethod<object>(_omlInstance, "WriteTo", new object[] { outputStream });
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
                    _omlInstance = Activator.CreateInstance(assemblyType, new object[] { omlStream, false, null, null, null });
                }
                else
                {
                    _omlInstance = Activator.CreateInstance(assemblyType, new object[] { omlStream, false, null, null });
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
