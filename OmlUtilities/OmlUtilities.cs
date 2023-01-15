using CommandDotNet;
using OmlUtilities.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using static OmlUtilities.Core.Oml;

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
                AssemblyUtility.PlatformVersion = PlatformVersion.LatestSupportedVersion;
            }
            else
            {
                PlatformVersion platformVersion = PlatformVersion.Versions.FirstOrDefault(p => p.Label.Equals(version, StringComparison.InvariantCultureIgnoreCase));
                AssemblyUtility.PlatformVersion = platformVersion ?? throw new OmlException($"Platform version \"{platformVersion}\" not recognized. Please run 'show-platform-versions' in order to list supported versions.");
            }

            Stream stream = GetStream(input, true);

            if (stream.CanSeek)
            {
                return new Oml(stream);
            }
            else
            {
                MemoryStream memoryStream = new MemoryStream();
                byte[] buffer = new byte[32 * 1024];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }
                memoryStream.Position = 0;

                return new Oml(memoryStream);
            }
        }

        /// <summary>
        /// Displays a list of Service Studio versions that this utility is compatible with for loading and saving OML files.
        /// </summary>
        /// <param name="onlyLatest">Whether only the latest compatible version should be shown.</param>
        /// <param name="showFullVersion">Whether to show the full formatted version (e.g.: '9.1.603.0' instead of 'O9.1').</param>
        [Command(
            Name = "show-platform-versions",
            Description = "Displays a list of compatible platform versions.",
            ExtendedHelpText = "Displays a list of Service Studio versions that this utility is compatible with for loading and saving OML files.")]
        public void ShowPlatformVersions(
            [Option(
                ShortName = "l",
                LongName = "latest",
                Description = "Whether only the latest compatible version should be shown.")]
            bool onlyLatest = false,
            [Option(
                ShortName = "f",
                LongName = "full-version",
                Description = "Whether to show the full formatted version (e.g.: '9.1.603.0' instead of 'O9.1').")]
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

        /// <summary>
        /// Displays a list of headers of the given OML file. Header values can be changed using the 'manipulate' command.
        /// 
        /// Headers are shown in the following format: [Writable|ReadOnly],[DataType],[HeaderName]:[HeaderValue].
        /// </summary>
        /// <param name="input">Path to the OML file to be loaded. It is possible to read from stdin instead of a file by using UNIX pipe access syntax (e.g.: 'pipe:').</param>
        /// <param name="headerName">If set, returns only the specified header.</param>
        /// <param name="version">Platform version to be used for decoding the OML file.</param>
        /// <exception cref="OmlException">Throws an exception if an invalid header name is provided.</exception>
        [Command(
            Name = "show-headers",
            Description = "Prints header values.",
            ExtendedHelpText = "Displays a list of headers of the given OML file. Header values can be changed using the 'manipulate' command.\n\n" +
                "Headers are shown in the following format: [Writable|ReadOnly],[DataType],[HeaderName]:[HeaderValue].")]
        public void ShowHeaders(
            [Operand(
                Name = "input",
                Description = "Path to the OML file to be loaded. It is possible to read from stdin instead of a file by using UNIX pipe access syntax (e.g.: 'pipe:').")]
            string input,
            [Operand(
                Name = "header-name",
                Description = "If set, returns only the specified header.")]
            string headerName = null,
            [Option(
                ShortName = "v",
                LongName = "version",
                Description = "Platform version (e.g.: 'O11') to be used for decoding the OML file. If not set, will use the latest version available.")]
            string version = null)
        {
            // Get OML instance
            Oml oml = GetOmlInstance(input, version);
            bool found = false;

            foreach (KeyValuePair<string, OmlHeader> headerPair in oml.Headers)
            {
                if (!string.IsNullOrEmpty(headerName) && !headerName.Equals(headerPair.Key))
                {
                    continue;
                }
                Console.WriteLine("{0},{1},{2}:{3}", headerPair.Value.IsReadOnly ? "ReadOnly" : "Writable", headerPair.Value.HeaderType.Name, headerPair.Key, headerPair.Value.GetValue<string>());
                found = true;
            }

            if (!string.IsNullOrEmpty(headerName) && !found)
            {
                throw new OmlException($"Header name \"{headerName}\" was not found.");
            }
        }

        /// <summary>
        /// Lists fragment names or prints its XML content if the fragment name is specified.
        /// </summary>
        /// <param name="input">Path to the OML file to be loaded. It is possible to read from stdin instead of a file by using UNIX pipe access syntax (e.g.: 'pipe:').</param>
        /// <param name="fragmentName">If set, prints the XML content of the desired fragment.</param>
        /// <param name="version">Platform version to be used for decoding the OML file.</param>
        [Command(
            Name = "show-fragments",
            Description = "Prints fragment data.",
            ExtendedHelpText = "Lists fragment names or prints its XML content if the fragment name is specified.")]
        public void ShowFragments(
            [Operand(
                Name = "input",
                Description = "Path to the OML file to be loaded. It is possible to read from stdin instead of a file by using UNIX pipe access syntax (e.g.: 'pipe:').")]
            string input,
            [Operand(
                Name = "fragment-name",
                Description = "If set, prints the XML content of the desired fragment.")]
            string fragmentName = null,
            [Option(
                ShortName = "v",
                LongName = "version",
                Description = "Platform version (e.g.: 'O11') to be used for decoding the OML file. If not set, will use the latest version available.")]
            string version = null)
        {
            // Get OML instance
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
                Console.WriteLine(fragment.ToString(SaveOptions.DisableFormatting));
            }
        }

        /// <summary>
        /// Opens an OML file in order to manipulate and save it.
        /// </summary>
        /// <param name="input">Path to the OML file to be loaded.</param>
        /// <param name="output">Destination path to save the manipulated OML file.</param>
        /// <param name="format">Destination file format. Possible formats are 'oml' and 'xml'. If not set, will be guessed according to the output file extension.</param>
        /// <param name="headers">Sets a header value. Name and value must be separated by colon (':').</param>
        /// <param name="fragments">Sets the content of a fragment. Name and value must be separated by colon (':').</param>
        /// <param name="version">Platform version to be used for decoding the OML file.</param>
        [Command(
            Name = "manipulate",
            Description = "Manipulates an OML file.",
            ExtendedHelpText = "Opens an OML file in order to manipulate and save it.")]
        public void Manipulate(
            [Operand(
                Name = "input",
                Description = "Path to the OML file to be loaded. It is possible to read from stdin instead of a file by using UNIX pipe access syntax (e.g.: 'pipe:').")]
            string input,
            [Operand(
                Name = "output",
                Description = "Destination path to save the manipulated OML file. It is possible to send the data stream to stdout instead by using UNIX pipe access syntax (e.g.: 'pipe:').")]
            string output,
            [Option(
                ShortName = "f",
                LongName = "format",
                Description = "Destination file format. Possible formats are 'oml' and 'xml'. If not set, will be guessed according to the output file extension.")]
            string format = null,
            [Option(
                ShortName = "H",
                LongName = "header",
                Description = "Sets a header value. Name and value must be separated by colon (e.g.: 'Description:Hello, World!').")]
            List<string> headers = null,
            [Option(
                ShortName = "F",
                LongName = "fragment",
                Description = "Saves a fragment XML into the OML file.")]
            List<string> fragments = null,
            [Option(
                ShortName = "v",
                LongName = "version",
                Description = "Platform version (e.g.: 'O11') to be used for decoding the OML file. If not set, will use the latest version available.")]
            string version = null)
        {
            // Get OML instance
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

                    if (!oml.Headers.ContainsKey(headerName))
                    {
                        throw new OmlException($"Header name \"{headerName}\" was not found.");
                    }

                    OmlHeader header = oml.Headers[headerName];
                    string headerValue = headerLine[(colonIndex + 1)..];
                    header.SetValue(headerValue);
                }
            }

            // Set fragments
            if (fragments != null)
            {
                foreach (string fragmentLine in fragments)
                {
                    XElement fragmentXml = XElement.Parse(fragmentLine);
                    oml.SetFragmentXml(fragmentXml);
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

        /// <summary>
        /// Performs a textual search for any expression inside an OML file.
        /// </summary>
        /// <param name="omlPathDir">Path to the directory with OML files to be examined.</param>
        /// <param name="keywordSearch">Text to be searched inside an OML file.</param>
        /// <param name="version">Platform version to be used for decoding the OML file.</param>
        [Command(
            Name = "text-search",
            Description = "Search for a text inside an OML file.",
            ExtendedHelpText = "Performs a textual search for any expression inside an OML file.")]
        public void TextSearch(
            [Operand(
                Name = "oml-path-dir",
                Description = "Path to the directory with OML files to be examined.")]
            string omlPathDir,
            [Operand(
                Name = "keyword-search",
                Description = "Text to be searched inside an OML file.")]
            string keywordSearch,
            [Option(
                ShortName = "v",
                LongName = "version",
                Description = "Platform version (e.g.: 'O11') to be used for decoding the OML file. If not set, will use the latest version available.")]
            string version = null)
        {
            if (string.IsNullOrEmpty(keywordSearch))
            {
                Console.WriteLine("Please inform a keyword-search value and try again.");
            }
            else
            {
                Console.WriteLine($"Searching for keyword '{keywordSearch}'");
            }

            if (Directory.Exists(omlPathDir))
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                Console.WriteLine("Searching OMLs inside of {0} ...", omlPathDir);

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

                        Console.WriteLine("[{0}/{1}] - {2} occurrences found in {3}.", ++CountFile, Files.Count(), count, file);
                    }
                    catch(Exception err)
                    {
                        Console.WriteLine("[{0}/{1}] - Error occurred parsing file: {2}", ++CountFile, Files.Count(), err.Message.Replace("\n", ""));
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
