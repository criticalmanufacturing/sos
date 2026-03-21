using Xunit;
using Cmf.Cli.Plugin.Sos.Runtime;

namespace Console.Tests.Runtime;

public class AppRuntimeTests
{
    [Theory]
    [InlineData("host", AppRuntime.Dotnet)]
    [InlineData("clickhouse-ui", AppRuntime.NodeJs)]
    [InlineData("unknown-app", AppRuntime.Unknown)]
    [InlineData(null, AppRuntime.Unknown)]
    [InlineData("", AppRuntime.Unknown)]
    [InlineData("   ", AppRuntime.Unknown)]
    public void GetRuntime_ReturnsCorrectRuntime(string? input, AppRuntime expected)
    {
        // Act
        var result = AppRuntimeRegistry.GetRuntime(input);

        // Assert
        Assert.Equal(expected, result);
    }
}