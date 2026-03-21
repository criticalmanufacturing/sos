namespace Cmf.Cli.Plugin.Sos.Runtime;

/// <summary>
/// Runtime type inferred from pod label app.kubernetes.io/name.
/// Used by the dump factory to choose DotNet vs NodeJs (and future runtimes).
/// </summary>
public enum AppRuntime
{
    Unknown,
    Dotnet,
    NodeJs
}
