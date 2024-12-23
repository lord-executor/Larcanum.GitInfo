using System.Diagnostics;

namespace Larcanum.GitInfo;

/// <summary>
/// Wrapper for running git commands using <see cref="Process"/>.
/// </summary>
public class GitCommands
{
    private readonly string _gitBinPath;
    private readonly string _contextDirectory;

    public GitCommands(string gitBinPath, string contextDirectory)
    {
        _gitBinPath = gitBinPath;
        _contextDirectory = contextDirectory;
    }

    /// <summary>
    /// Tries to determine the git version along with the actual path of the executable. If the git command cannot be
    /// found, both parts of the result will be null.
    /// </summary>
    public (string? GitPath, string? Version) Version()
    {
        if (Path.IsPathRooted(_gitBinPath))
        {
            return (_gitBinPath, GetOutput("--version"));
        }

        var procInfo = new ProcessStartInfo
        {
            FileName = IsWindowsPlatform() ? "where" : "which",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = "git"
        };
        var process = Process.Start(procInfo)!;
        process.WaitForExit();
        // The windows "where" command can actually return multiple results, separated by newlines. In that case, we
        // just take the first.
        var binPath = (process.StandardOutput.ReadLine() ?? string.Empty).Trim();

        return process.ExitCode == 0
            ? (binPath, GetOutput("--version"))
            : (null, null);
    }

    public string RepositoryRoot()
    {
        return GetOutput("rev-parse --show-toplevel");
    }

    public string CommitShortHash()
    {
        return GetOutput("rev-parse --short HEAD");
    }

    public string CommitHash()
    {
        return GetOutput("rev-parse HEAD");
    }

    public string BranchName()
    {
        return GetOutput("rev-parse --abbrev-ref HEAD");
    }

    public string Tag()
    {
        return GetOutput("describe --tags --always");
    }

    public string CommitDate()
    {
        return GetOutput("show -s --format=\"%cI\"");
    }

    public bool IsDirty()
    {
        return RunGitCommand("diff --quiet HEAD").ExitCode != 0;
    }

    private string GetOutput(string args, bool withContext = true)
    {
        var process = RunGitCommand(args, withContext);
        return process.ExitCode == 0
            ? process.StandardOutput.ReadToEnd().Trim()
            : process.StandardError.ReadToEnd().Trim();
    }

    private Process RunGitCommand(string args, bool withContext = true)
    {
        var procInfo = new ProcessStartInfo
        {
            FileName = _gitBinPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = withContext
                // of course, escaping can get a bit tricky...
                // as explained here https://stackoverflow.com/questions/5510343/escape-command-line-arguments-in-c-sharp
                // when an arugment ends with a '\' and we surround it with '"' then that ending '"' will get
                // lost in translation...
                ? $"-C \"{_contextDirectory.TrimEnd('\\')}\" {args}"
                : $"{args}"
        };

        var process = Process.Start(procInfo)!;
        process.WaitForExit();
        return process;
    }

    private bool IsWindowsPlatform()
    {
#pragma warning disable RS1035
        return Environment.OSVersion.Platform == PlatformID.Win32S || Environment.OSVersion.Platform == PlatformID.Win32NT
            || Environment.OSVersion.Platform == PlatformID.Win32Windows || Environment.OSVersion.Platform == PlatformID.WinCE;
#pragma warning restore RS1035
    }
}
