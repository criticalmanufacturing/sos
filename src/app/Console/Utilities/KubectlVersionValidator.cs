using Cmf.CLI.Utilities;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public static class KubectlVersionValidator
{
    private static readonly string RequiredVersion = "1.35.3";
    public static void EnsureCorrectVersion()
    {
        try
        {
            var kube = new KubeCliRunner();
            var result = kube.RunAllowFailure(new List<string> { "version" });

            if (result.StdOut.Contains(RequiredVersion))
            {
                Console.WriteLine("Current Kubectl Version: " + RequiredVersion);
            }
            else
            {
                Console.WriteLine("Kubectl Version:" + RequiredVersion + " is not installed or not in PATH. Please install the correct version to proceed.");
                throw new CliException("Wrong kubectl version.");
            }
        }
        catch (Exception ex) when (ex is not CliException)
        {
            Console.WriteLine("Kubectl Version:" + RequiredVersion + " is not installed or not in PATH. Please install the correct version to proceed.");
            throw new CliException("Wrong kubectl version.");
        }
    }
}