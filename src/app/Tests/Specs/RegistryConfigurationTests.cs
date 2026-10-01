using Cmf.Cli.Plugin.Sos.Utilities;
using Cmf.Cli.Plugin.Sos.Commands;
using System.CommandLine;
using System.CommandLine.Parsing;
using Xunit;

namespace Console.Tests.Configuration;

public class RegistryConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingOverridesKeepInternalDefaults(string? value)
    {
        WithEnvironment("cmf_sos_debug_image", value, () =>
            Assert.Equal("dev.criticalmanufacturing.io/platformengineering/sos:latest", RegistryConfiguration.ResolveDebugImage(null)));
        WithEnvironment("cmf_sos_ubi_debug_image", value, () =>
            Assert.Equal("dev.criticalmanufacturing.io/platformengineering/sos-ubi:latest", RegistryConfiguration.ResolveDebugImage(null, true)));
        WithEnvironment("cmf_sos_registry", value, () =>
            Assert.Equal("https://dev.criticalmanufacturing.io/repository/npm-public", RegistryConfiguration.NpmRegistry));
        WithEnvironment("cmf_sos_symbol_server", value, () =>
            Assert.Equal("https://symbolserver.apps.rhos.cm-mes.dev", RegistryConfiguration.SymbolServer));
    }

    [Fact]
    public void OverridesApplyToInteractiveDefaultsAndExplicitImagesWin()
    {
        WithEnvironment("cmf_sos_debug_image", " mirror.example/sos:test ", () =>
        WithEnvironment("cmf_sos_ubi_debug_image", "mirror.example/ubi:test", () =>
        {
            Assert.Equal("mirror.example/sos:test", RegistryConfiguration.ResolveDebugImage(null));
            Assert.Equal("mirror.example/ubi:test", RegistryConfiguration.ResolveDebugImage(" ", true));
            Assert.Equal("explicit/image:tag", RegistryConfiguration.ResolveDebugImage("explicit/image:tag"));
            Assert.Equal("explicit/image:tag", RegistryConfiguration.ResolveDebugImage("explicit/image:tag", true));
        }));
        WithEnvironment("cmf_sos_registry", " https://npm.example/ ", () =>
            Assert.Equal("https://npm.example/", RegistryConfiguration.NpmRegistry));
        WithEnvironment("cmf_sos_symbol_server", "https://symbols.example", () =>
            Assert.Equal("https://symbols.example", RegistryConfiguration.SymbolServer));
    }

    [Fact]
    public void CommandDefaultsUseOverridesAndRemoteDebugDefersRuntimeSelection()
    {
        WithEnvironment("cmf_sos_debug_image", "mirror.example/sos:test", () =>
        {
            foreach (var configure in new Action<Command>[] {
                new DumpCommand().Configure, new RuntimeMetricsCommand().Configure,
                new InteractiveShellCommand().Configure, new RemoteDebugCommand().Configure })
            {
                var command = new Command("test");
                configure(command);
                var image = (Option<string>)command.Options.Single(option => option.Name == "image");
                var expected = configure.Target is RemoteDebugCommand ? string.Empty : "mirror.example/sos:test";
                Assert.Equal(expected, command.Parse("pod -n default").GetValueForOption(image));
                Assert.Equal("explicit/image:tag", command.Parse("pod -n default --image explicit/image:tag").GetValueForOption(image));
            }
        });
    }

    private static void WithEnvironment(string name, string? value, Action assertion)
    {
        var previous = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            assertion();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, previous);
        }
    }
}
