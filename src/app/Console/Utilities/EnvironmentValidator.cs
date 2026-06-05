using Cmf.CLI.Core;
using Cmf.CLI.Utilities;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public static class EnvironmentValidator
{
    private static readonly string RequiredVersion = "1.35.3";

    public static void Validate()
    {
        EnsureCorrectKubectlVersion();
        EnsureAuthenticated();
    }

    private static void EnsureCorrectKubectlVersion()
    {
        try
        {
            var kube = new KubeCliRunner();
            var result = kube.RunAllowFailure(new List<string> { "version" });

            if (result.StdOut.Contains(RequiredVersion))
            {
                Log.Information("Current Kubectl Version: " + RequiredVersion);
            }
            else
            {
                Log.Information("Kubectl Version: " + RequiredVersion + " is not installed or not in PATH. Please install the correct version to proceed.");
                throw new CliException("Wrong kubectl version.");
            }
        }
        catch (Exception ex) when (ex is not CliException)
        {
            Log.Information("Kubectl Version: " + RequiredVersion + " is not installed or not in PATH. Please install the correct version to proceed.");
            throw new CliException("Wrong kubectl version.");
        }
    }

    private static void EnsureAuthenticated()
    {
        try
        {
            var kube = new KubeCliRunner();
            var result = kube.RunAllowFailure(new List<string> { "auth", "whoami" });

            if (result.ExitCode != 0)
            {
                throw new CliException("User not authenticated. Please login into your cluster.");
            }
            
            Log.Information("Authenticated as: " + result.StdOut.Trim());
        }
        catch (Exception ex) when (ex is not CliException)
        {
            Log.Information("Failed to verify authentication with 'kubectl'. Ensure it is installed and you are logged in.");
            throw new CliException("User not authenticated or 'kubectl' not found.");
        }
    }
}