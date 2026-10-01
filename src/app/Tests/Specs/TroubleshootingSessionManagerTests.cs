using Cmf.Cli.Plugin.Sos.Utilities;
using Xunit;

namespace Console.Tests;

public class TroubleshootingSessionManagerTests
{
    [Theory]
    [InlineData("devopsrhosqa", 20)]
    [InlineData(null, 5)]
    public void DebugCommandPinsBaselineAndPreservesTargetAndLifetime(string? ns, int minutes)
    {
        var args = TroubleshootingSessionManager.BuildStartArguments("host-pod", "host", "example/sos:latest", ns, minutes);
        var separator = args.IndexOf("--");
        var options = args.Take(separator).ToArray();

        // Regression: the newer kubectl default adds SYS_PTRACE and fails SCC admission.
        Assert.Equal(new[] { "--profile=baseline" }, options.Where(arg => arg.StartsWith("--profile")));
        Assert.Contains("--target=host", options);
        Assert.Contains("--image=example/sos:latest", options);
        Assert.Contains("--share-processes", options);
        Assert.Contains("--attach=false", options);
        Assert.DoesNotContain(options, arg => arg.StartsWith("--custom") || arg.StartsWith("--copy-to"));
        Assert.Equal(new[] { "timeout", "--signal=TERM", (minutes * 60).ToString(), "sh", "-c",
            "while [ ! -f /tmp/debug-done ]; do sleep 1; done" }, args.Skip(separator + 1));
        Assert.Equal(ns == null ? new[] { "debug", "host-pod" } : new[] { "-n", ns, "debug", "host-pod" },
            args.Take(ns == null ? 2 : 4));
    }
}
