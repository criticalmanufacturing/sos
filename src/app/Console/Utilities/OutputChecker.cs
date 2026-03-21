using Cmf.CLI.Core;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public static class OutputChecker
{
    /// <summary>
    /// This function will understand if the output file provided by the user is valid according to the operation.
    /// This means that a dotnet dump will need a .dmp for example.
    /// If it's not valid it will assign a valid output with a default name using "/tmp/dump_pod_timezone.extension"
    /// </summary>
    public static string ResolveOutputPath(
        string? output,
        string pod,
        string expectedExtension)
    {
        if (!expectedExtension.StartsWith(".")) 
        {
            expectedExtension = "." + expectedExtension;
        }

        if(!string.IsNullOrWhiteSpace(output) && !Directory.Exists(output) && output.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase))
        {
            return output!;
        } 

        var fileName = $"/tmp/dump_{pod}_{DateTime.Now:yyyyMMdd_HHmmss}{expectedExtension}";

        Log.Warning("Using the following output: " + fileName);

        return Path.Combine(Directory.GetCurrentDirectory(), fileName);
    }
}