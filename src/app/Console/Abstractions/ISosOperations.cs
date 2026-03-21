namespace Cmf.Cli.Plugin.Sos.Abstractions;

/// <summary>
/// Abstraction for runtime-specific SoS operations (dump, counters, and future actions).
/// The factory returns the appropriate implementation (e.g. .NET vs Node.js) per pod.
/// </summary>
public interface ISosOperations
{
    /// <summary>
    /// Capture a runtime-specific dump for the pod (e.g. .NET dump, Node.js heap snapshot).
    /// </summary>
    void Dump(string pod, string output, string pid, string? container, string? ns, string image);

    /// <summary>
    /// Collect .NET counters when supported by the runtime.
    /// Runtimes that do not support this (e.g. Node.js) should log a warning and no-op.
    /// </summary>
    void DotnetCounters(string pod, string output, string pid, string format, int duration, string counters, string? container, string? ns, string image);

    /// <summary>
    /// Start a remote debug session when supported by the runtime.
    /// </summary>
    void RemoteDebug(string pod, string pid, string? container, string? ns, string image);
}
