using System.Diagnostics;

namespace Larcanum.GitInfo;

public class GitCommands
{
    private readonly string _contextDirectory;

    public GitCommands(string contextDirectory)
    {
        _contextDirectory = contextDirectory;
    }

    public string Version()
    {
        return GetOutput("--version", withContext: false);
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
            : process.StartInfo.Arguments;// process.StandardError.ReadToEnd().Trim();
    }

    private Process RunGitCommand(string args, bool withContext = true)
    {
        var procInfo = new ProcessStartInfo
        {
            FileName = "git",
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
}
