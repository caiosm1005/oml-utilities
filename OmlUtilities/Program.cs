using OmlUtilities.Core;
using OmlUtilities;
using System.IO;
using System;

internal class Program
{
    private static int Main(string[] args)
    {
        AssemblyUtility.PlatformVersion = null;
        try
        {
            return ProgramWorker.Run(args);
        }
        catch(AssemblyUtilityException error)
        {
            Console.WriteLine(error.Message);
            return 32; // Assembly utility exception exit code
        }
        catch (OmlException error)
        {
            Console.WriteLine(error.Message);
            return 31; // OML exception exit code
        }
        catch (Exception error)
        {
            Console.WriteLine(error.Message);
            return 30; // Generic error exit code code
        }
    }
}