using Cmf.CLI.Core;
using Cmf.CLI.Utilities;
using System.Text.Json;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public static class EnvironmentValidator
{
    public static void Validate()
    {
        var kube = new KubeCliRunner();
        Log.Information("Current Kubectl Version: " + GetKubectlClientVersion(kube.RunAllowFailure));
        EnsureAuthenticated();
    }

    internal static string GetKubectlClientVersion(Func<IReadOnlyList<string>, CommandResult> run)
    {
        try
        {
            // Check the installed client independently of cluster connectivity and version.
            var result = run(new[] { "version", "--client", "--output=json" });

            if (result.ExitCode != 0)
            {
                throw new CliException("Failed to query the kubectl client version. " + result.StdErr.Trim());
            }

            using var document = JsonDocument.Parse(result.StdOut);
            var version = document.RootElement.GetProperty("clientVersion").GetProperty("gitVersion").GetString();
            if (string.IsNullOrWhiteSpace(version))
                throw new CliException("kubectl returned an empty client version.");

            // No exact-version or upper-version restriction, including vendor builds.
            return version;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new CliException("Could not start kubectl. Ensure it is installed and executable in PATH.");
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new CliException("Could not read the client version from 'kubectl version --client --output=json'. " + ex.Message);
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