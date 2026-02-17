using System.Diagnostics;
using System.Text;

namespace Cmf.Cli.Plugin.Sos.Utilities;

public sealed class KubeCliRunner
{
    private readonly string exe;

    public KubeCliRunner()
    {
        exe = "kubectl";
    }

    public CommandResult Run(IReadOnlyList<string> args)
    {
        var res = RunInternal(args);
        if (res.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{exe} failed (exit {res.ExitCode}).\n\nARGS:\n{string.Join(" ", args)}\n\nSTDOUT:\n{res.StdOut}\n\nSTDERR:\n{res.StdErr}");
        }
        return res;
    }

    public CommandResult RunAllowFailure(IReadOnlyList<string> args) => RunInternal(args);

    private CommandResult RunInternal(IReadOnlyList<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        p.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();

        return new CommandResult(p.ExitCode, stdout.ToString(), stderr.ToString());
    }
}

public readonly record struct CommandResult(int ExitCode, string StdOut, string StdErr);

