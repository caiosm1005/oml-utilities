using CommandDotNet;
using OmlUtilities.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using static OmlUtilities.Core.Oml;
using static OmlUtilities.Core.Oml.OmlHeader;

namespace OmlUtilities
{
    public class OmlUtilities
    {
        protected Stream GetStream(string path, bool isInput)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new OmlException($"The {(isInput ? "input" : "output")} argument is mandatory.");
            }

            if (path.StartsWith("pipe:", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!Console.IsInputRedirected)
                {
                    throw new OmlException($"Unable to pipe the {(isInput ? "input" : "output")} argument because the console input is not redirected.");
                }

                string pipeIndex = path[5..];

                if (string.IsNullOrEmpty(pipeIndex))
                {
                    pipeIndex = isInput ? "0" : "1";
                }

                return pipeIndex switch
                {
                    "0" => Console.OpenStandardInput(),
                    "1" => Console.OpenStandardOutput(),
                    "2" => Console.OpenStandardError(),
                    _ => throw new OmlException($"Unknown pipe index {pipeIndex} used in the {(isInput ? "input" : "output")} argument."),
                };
            }
            else
            {
                if (isInput)
                {
                    return File.OpenRead(path);
                }
                else
                {
                    return File.OpenWrite(path);
                }
            }
        }

        protected Oml GetOmlInstance(string input, string version)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new OmlException("The input argument is mandatory.");
            }
            if (string.IsNullOrEmpty(version))
            {
                throw new OmlException("The platform version argument is mandatory.");
            }

            if (version.Equals("OL", StringComparison.InvariantCultureIgnoreCase))
            {
                AssemblyUtility.PlatformVersion = PlatformVersion.LatestSupportedVersion;
            }
            else
            {
                PlatformVersion platformVersion = PlatformVersion.Versions.FirstOrDefault(p => p.Label.Equals(version, StringComparison.InvariantCultureIgnoreCase));
                AssemblyUtility.PlatformVersion = platformVersion ?? throw new OmlException($"Platform version \"{version}\" not recognized. Please run ShowPlatformVersions in order to list supported versions.");
            }

            Stream stream = GetStream(input, true);

            if (stream.CanSeek)
            {
                return new Oml(stream);
            }
            else
            {
                MemoryStream memoryStream = new MemoryStream();
                byte[] buffer = new byte[32 * 1024]; // 32K buffer for example
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }
                memoryStream.Position = 0;

                return new Oml(memoryStream);
            }
        }

        [Command(Description = "Displays a list of compatible platform versions.",
        ExtendedHelpText = "Displays a list of Service Studio versions that this utility is compatible with for loading and saving OML files.")]
        public void ShowPlatformVersions(
            [Option(ShortName = "l", LongName = "latest", Description = "Whether only the latest compatible version should be shown.")]
            bool onlyLatest = false,
            [Option(ShortName = "v", LongName = "fullversion", Description = "Whether to show the full formatted version (e.g. '9.1.603.0' instead of 'O9.1').")]
            bool showFullVersion = false)
        {
            if (onlyLatest)
            {
                Console.WriteLine(showFullVersion ? PlatformVersion.LatestSupportedVersion.Version.ToString() : PlatformVersion.LatestSupportedVersion.ToString());
            }
            else
            {
                foreach (PlatformVersion version in PlatformVersion.Versions)
                {
                    Console.WriteLine(showFullVersion ? version.Version.ToString() : version.ToString());
                }
            }
        }

        [Command(Description = "Prints header values.",
        ExtendedHelpText = "Displays a list containing key:value pairs of headers of the given OML file. These header values can be changed through the Manipulate command.")]
        public void ShowHeaders(
            [Operand(Description = "Path to the OML file to be loaded. It is possible to read from stdin instead of a file by using UNIX pipe access syntax (e.g.: 'pipe:').")]
            string input,
            [Operand(Description = "Target platform version to use for loading the OML file. Defaults to the latest compatible version ('OL').")]
            string version = "OL",
            [Operand(Description = "If set, returns only the value of the specified header.")]
            string headerName = null)
        {
            Oml oml = GetOmlInstance(input, version);
            bool found = false;

            foreach (PropertyInfo property in typeof(OmlHeader).GetProperties())
            {
                OmlHeaderAttribute attribute = (OmlHeaderAttribute)Attribute.GetCustomAttribute(property, typeof(OmlHeaderAttribute));

                if (attribute == null || !string.IsNullOrEmpty(headerName) && !headerName.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(headerName))
                {
                    Console.WriteLine(property.GetValue(oml.Header, null));
                }
                else
                {
                    Console.WriteLine("{0}:{1}", property.Name, property.GetValue(oml.Header, null));
                }

                found = true;
            }

            if (!string.IsNullOrEmpty(headerName) && !found)
            {
                throw new OmlException($"Header name \"{headerName}\" was not found.");
            }
        }

        [Command(Description = "Prints fragment data.",
        ExtendedHelpText = "Lists fragment names or prints its XML content if the fragment name is specified.")]
        public void ShowFragments(
            [Operand(Description = "Path to the OML file to be loaded. It is possible to read from stdin instead of a file by using UNIX pipe access syntax (e.g.: 'pipe:').")]
            string input,
            [Operand(Description = "Target platform version to use for loading the OML file. Defaults to the latest compatible version ('OL').")]
            string version = "OL",
            [Operand(Description = "If set, prints the XML content of the desired fragment.")]
            string fragmentName = null)
        {
            Oml oml = GetOmlInstance(input, version);

            if (string.IsNullOrEmpty(fragmentName))
            {
                foreach(string innerFragmentName in oml.GetFragmentNames())
                {
                    Console.WriteLine(innerFragmentName);
                }
            }
            else
            {
                XElement fragment = oml.GetFragmentXml(fragmentName);

                if (fragment == null)
                {
                    throw new OmlException($"Unable to get XML content of fragment \"{fragmentName}\".");
                }

                Console.WriteLine(fragment.ToString(SaveOptions.DisableFormatting));
            }
        }

        [Command(Description = "Manipulates an OML file.",
        ExtendedHelpText = "Opens an OML file in order to manipulate and save it.")]
        public void Manipulate(
            [Operand(Description = "Path to the OML file to be loaded. It is possible to read from stdin instead of a file by using UNIX pipe access syntax (e.g.: 'pipe:').")]
            string input,
            [Operand(Description = "Destination path to save the manipulated OML file. It is possible to send the data stream to stdout instead by using UNIX pipe access syntax (e.g.: 'pipe:').")]
            string output,
            [Operand(Description = "Target platform version to use for loading the OML file. Defaults to the latest compatible version ('OL').")]
            string version = "OL",
            [Option(ShortName = "f", LongName = "format", Description = "Destination file format. Possible formats are 'oml' and 'xml'. If not set, will be guessed according to the output file extension.")]
            string format = null,
            [Option(ShortName = "H", LongName = "header", Description = "Sets a header value. Name and value must be separated by colon (':').")]
            List<string> headers = null,
            [Option(ShortName = "F", LongName = "fragment", Description = "Sets the content of a fragment. Name and value must be separated by colon (':').")]
            List<string> fragments = null)
        {
            Oml oml = GetOmlInstance(input, version);

            // Set headers
            if (headers != null)
            {
                foreach (string headerLine in headers)
                {
                    int colonIndex = headerLine.IndexOf(':');

                    if (colonIndex == -1)
                    {
                        throw new OmlException($"Unable to parse header value \"{headerLine}\". Name and value must be separated by colon (':').");
                    }

                    string headerName = headerLine[..colonIndex];

                    if (string.IsNullOrEmpty(headerName))
                    {
                        throw new OmlException("The header name in the header parameter is mandatory.");
                    }

                    bool found = false;

                    foreach (PropertyInfo property in typeof(OmlHeader).GetProperties())
                    {
                        OmlHeaderAttribute attribute = (OmlHeaderAttribute)Attribute.GetCustomAttribute(property, typeof(OmlHeaderAttribute));

                        if (attribute == null || !headerName.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            continue;
                        }

                        if (attribute.IsReadOnly)
                        {
                            throw new OmlException($"Cannot change header \"{property.Name}\" because it is read-only.");
                        }

                        string headerValue = headerLine[(colonIndex + 1)..];
                        property.SetValue(oml.Header, headerValue);
                        found = true;
                    }

                    if (!found)
                    {
                        throw new OmlException($"Header name \"{headerName}\" was not found.");
                    }
                }
            }

            // Set fragments
            if (fragments != null)
            {
                foreach (string fragmentLine in fragments)
                {
                    int colonIndex = fragmentLine.IndexOf(':');

                    if (colonIndex == -1)
                    {
                        throw new OmlException($"Unable to parse fragment value \"{fragmentLine}\". Name and value must be separated by colon (':').");
                    }

                    string fragmentName = fragmentLine[..colonIndex];

                    if (string.IsNullOrEmpty(fragmentName))
                    {
                        throw new OmlException("The fragment name in the fragment parameter is mandatory.");
                    }

                    XElement fragment = XElement.Parse(fragmentLine[(colonIndex + 1)..]);
                    oml.SetFragmentXml(fragmentName, fragment);
                }
            }

            // Save manipulated OML
            Stream outputStream = GetStream(output, false);
            if (format != null && format.Equals("xml", StringComparison.InvariantCultureIgnoreCase) || format == null && output.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
            {
                StreamWriter sw = new StreamWriter(outputStream);
                sw.Write(oml.GetXml().ToString(SaveOptions.DisableFormatting)); // Export XML
                sw.Flush();
                sw.Close();
            }
            else
            {
                oml.Save(outputStream); // Export OML
            }

            outputStream.Close();
        }


        [Command(Description = "Search for a text inside an OML file.",
        ExtendedHelpText = "Perform a textual search for any expression inside an OML file.")]
        public void TextSearch(
            [Operand(Description = "Path to the directory with OML files to be examined.")]
            string omlPathDir,
            [Operand(Description = "Text to be searched inside an OML file.")]
            string keywordSearch,
            [Operand(Description = "Target platform version to use for loading the OML file. Defaults to the latest compatible version ('OL').")]
            string version = "OL")
        {
            if (string.IsNullOrEmpty(keywordSearch)) {
                Console.WriteLine("Please inform a expression for search and try again.");
            }
            else
            {
                Console.WriteLine("Search for keyword '{0}'", keywordSearch);
            }

            if (Directory.Exists(omlPathDir))
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                Console.WriteLine("Search OMLs inside of {0} ...", omlPathDir);

                DirectoryInfo omlDir = new DirectoryInfo(omlPathDir);
                FileInfo[] Files = omlDir.GetFiles("*.oml");

                Console.WriteLine("{0} files found.", Files.Count());

                int CountFile = 0;
                foreach (FileInfo file in Files)
                {
                    try
                    {
                        Oml oml = GetOmlInstance(omlPathDir + Path.DirectorySeparatorChar + file, version);

                        string txtXml = oml.GetXml().ToString();

                        int i = 0;
                        int count = 0;
                        while ((i = txtXml.IndexOf(keywordSearch, i)) != -1)
                        {
                            i += keywordSearch.Length;
                            count++;

                        }

                        Console.WriteLine("[{0}/{1}] - {2} ocurrences found in {3}.", ++CountFile, Files.Count(), count, file);
                    }
                    catch(Exception err)
                    {
                        Console.WriteLine("[{0}/{1}] - Error ocurred parsing file: {2}", ++CountFile, Files.Count(), err.Message.Replace("\n", ""));
                    }
                }

                watch.Stop();
                TimeSpan ElapsedMS = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
                string formatElapsedTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s",
                        ElapsedMS.Hours,
                        ElapsedMS.Minutes,
                        ElapsedMS.Seconds);

                Console.WriteLine("Elapsed time of {0}", formatElapsedTime);
            }
            else
            {
                Console.WriteLine("Directory not found.");
            }
        }
    }
}
