namespace Cmf.Cli.Plugin.Sos.Utilities;

/// <summary>External resource defaults shared by CLI and interactive operations.</summary>
public static class RegistryConfiguration
{
    public static string NpmRegistry => Read("cmf_sos_registry", "https://registry.npmjs.org/");
    public static string DebugImage => Read("cmf_sos_debug_image", "criticalmanufacturing/sos:latest");
    public static string RemoteDotnetDebugImage => Read("cmf_sos_remote_debug_image", "criticalmanufacturing/sos-ubi:latest");
    public static string SymbolServer => Read("cmf_sos_symbol_server", "https://symbolserver.apps.rhos.cm-mes.dev");

    private static string Read(string name, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
