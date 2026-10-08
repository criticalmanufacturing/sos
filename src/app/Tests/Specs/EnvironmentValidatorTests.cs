using System.ComponentModel;
using System.Text.Json;
using Cmf.CLI.Utilities;
using Cmf.Cli.Plugin.Sos.Utilities;
using Xunit;

namespace Console.Tests;

public class EnvironmentValidatorTests
{
    [Theory]
    [InlineData("v1.35.3")]
    [InlineData("v1.35.4")]
    [InlineData("v1.36.0")]
    [InlineData("v1.40.0")]
    [InlineData("v1.34.0")]
    [InlineData("v1.35.3-eks-example")]
    [InlineData("v1.36.0-alpha.1")]
    public void AcceptsClientVersionsWithoutAnExactVersionRestriction(string version)
    {
        var actual = EnvironmentValidator.GetKubectlClientVersion(args =>
        {
            Assert.Equal(new[] { "version", "--client", "--output=json" }, args);
            return new CommandResult(0, JsonSerializer.Serialize(new
            {
                clientVersion = new { gitVersion = version },
                serverVersion = new { gitVersion = "v1.35.3" }
            }), "");
        });

        Assert.Equal(version, actual);
    }

    [Fact]
    public void NonzeroExitFailsEvenIfOutputContainsOriginalVersion()
    {
        var error = Assert.Throws<CliException>(() => EnvironmentValidator.GetKubectlClientVersion(_ =>
            new CommandResult(1, "{\"clientVersion\":{\"gitVersion\":\"v1.35.3\"}}", "version query failed")));

        Assert.Contains("version query failed", error.Message);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"serverVersion\":{\"gitVersion\":\"v1.35.3\"}}")]
    [InlineData("{\"clientVersion\":{}}")]
    [InlineData("{\"clientVersion\":{\"gitVersion\":\"\"}}")]
    [InlineData("{\"clientVersion\":{\"gitVersion\":null}}")]
    [InlineData("{\"clientVersion\":{\"gitVersion\":123}}")]
    public void RejectsMissingOrInvalidClientVersion(string output)
    {
        Assert.Throws<CliException>(() => EnvironmentValidator.GetKubectlClientVersion(_ =>
            new CommandResult(0, output, "")));
    }

    [Fact]
    public void MissingExecutableHasActionableError()
    {
        var error = Assert.Throws<CliException>(() => EnvironmentValidator.GetKubectlClientVersion(_ =>
            throw new Win32Exception("No such file or directory")));

        Assert.Contains("installed and executable in PATH", error.Message);
    }
}
