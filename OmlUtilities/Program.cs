using OmlUtilities.Core;
using OmlUtilities;

internal class Program
{
    private static int Main(string[] args)
    {
        AssemblyUtility.PlatformVersion = null;
        return ProgramWorker.Run(args);
    }
}