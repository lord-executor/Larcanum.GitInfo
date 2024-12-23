using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis.Diagnostics;

namespace Larcanum.GitInfo;

/// <summary>
/// Contains all the configuration and context that is needed for the source generator. The configuration is mostly
/// taken from global analyzer configuration in the form of MSBuild properties while the rest is added later in
/// the process.
/// </summary>
public record GitInfoConfig
{
    public string ProjectDir { get; set; } = string.Empty;
    public string GitInfoNamespace { get; set; } = string.Empty;
    public bool GitInfoGlobalNamespace { get; set; }
    public string GitInfoGitBin { get; set; } = string.Empty;
    public Regex GitInfoVersionRegex { get; set; } = new Regex("");
    public bool GitInfoGenerateAssemblyVersion { get; set; }
    public bool GitInfoDebug { get; set; }

    public string? GitPath { get; set; }
    public string? GitVersion { get; set; }
    public string? GitRoot { get; set; }
    public string? GitFingerprint { get; set; }
    public string Timestamp => DateTime.Now.ToString("o");

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>
        {
            [nameof(ProjectDir)] = ProjectDir,
            [nameof(GitInfoNamespace)] = GitInfoNamespace,
            [nameof(GitInfoGlobalNamespace)] = GitInfoGlobalNamespace.ToString(),
            [nameof(GitInfoGitBin)] = GitInfoGitBin,
            [nameof(GitInfoVersionRegex)] = GitInfoVersionRegex.ToString(),
            [nameof(GitInfoGenerateAssemblyVersion)] = GitInfoGenerateAssemblyVersion.ToString(),
            [nameof(GitInfoDebug)] = GitInfoDebug.ToString(),
            [nameof(GitPath)] = GitPath ?? string.Empty,
            [nameof(GitVersion)] = GitVersion ?? string.Empty,
            [nameof(GitRoot)] = GitRoot ?? string.Empty,
            [nameof(GitFingerprint)] = GitFingerprint ?? string.Empty,
            [nameof(Timestamp)] = Timestamp,
        };
    }

    public static GitInfoConfig FromOptions(AnalyzerConfigOptions options)
    {
        return new GitInfoConfig()
        {
            ProjectDir = GetBuildProp(options, nameof(ProjectDir)),
            GitInfoNamespace = GetBuildProp(options, nameof(GitInfoNamespace)),
            GitInfoGlobalNamespace = GetBuildPropBool(options, nameof(GitInfoGlobalNamespace)),
            GitInfoGitBin = GetBuildProp(options, nameof(GitInfoGitBin)),
            GitInfoVersionRegex = new Regex(GetBuildProp(options, nameof(GitInfoVersionRegex)), RegexOptions.Compiled),
            GitInfoGenerateAssemblyVersion = GetBuildPropBool(options, nameof(GitInfoGenerateAssemblyVersion)),
            GitInfoDebug = GetBuildPropBool(options, nameof(GitInfoDebug)),
        };
    }

    private static string GetBuildProp(AnalyzerConfigOptions options, string propertyName)
    {
        return options.TryGetValue($"build_property.{propertyName}", out var value)
            ? value
            : string.Empty;
    }

    private static bool GetBuildPropBool(AnalyzerConfigOptions options, string propertyName)
    {
        return bool.TryParse(GetBuildProp(options, propertyName), out var value) && value;
    }
}
