using Xunit;
using Cmf.Cli.Plugin.Sos.Utilities;

namespace Console.Tests;

public class OutputCheckerTests
{
    [Fact]
    public void ResolveOutputPath_ValidOutput_ReturnsProvidedOutput()
    {
        // Arrange
        string expectedOutput = "/custom/path/my_dump.dmp";
        string pod = "my-pod";
        string expectedExtension = ".dmp";

        // Act
        var result = OutputChecker.ResolveOutputPath(expectedOutput, pod, expectedExtension);

        // Assert
        Assert.Equal(expectedOutput, result);
    }

    [Fact]
    public void ResolveOutputPath_MissingDotInExtension_AddsDotAndValidates()
    {
        // Arrange
        string expectedOutput = "/custom/path/my_dump.dmp";
        string pod = "my-pod";
        string expectedExtension = "dmp"; // Missing dot

        // Act
        var result = OutputChecker.ResolveOutputPath(expectedOutput, pod, expectedExtension);

        // Assert
        Assert.Equal(expectedOutput, result);
    }

    [Fact]
    public void ResolveOutputPath_InvalidExtension_ReturnsDefaultPath()
    {
        // Arrange
        string inputOutput = "/custom/path/my_dump.txt";
        string pod = "my-pod";
        string expectedExtension = ".dmp";

        // Act
        var result = OutputChecker.ResolveOutputPath(inputOutput, pod, expectedExtension);

        // Assert
        Assert.Contains($"/tmp/dump_{pod}_", result);
        Assert.EndsWith(expectedExtension, result);
    }
}