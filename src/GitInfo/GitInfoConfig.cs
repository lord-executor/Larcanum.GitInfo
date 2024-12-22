using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis.Diagnostics;

namespace Larcanum.GitInfo;

public record GitInfoConfig
{
    public string RootNamespace { get; set; } = string.Empty;
    public string ProjectDir { get; set; } = string.Empty;
    public bool GitInfoGlobalNamespace { get; set; }
    public string GitInfoGitBin { get; set; } = string.Empty;
    public Regex GitInfoVersionRegex { get; set; } = new Regex("");
    public bool GitInfoGenerateAssemblyVersion { get; set; }

    public static GitInfoConfig FromOptions(AnalyzerConfigOptions options)
    {
        return new GitInfoConfig()
        {
            RootNamespace = GetBuildProp(options, nameof(RootNamespace)),
            ProjectDir = GetBuildProp(options, nameof(ProjectDir)),
            GitInfoGlobalNamespace = GetBuildPropBool(options, nameof(GitInfoGlobalNamespace)),
            GitInfoGitBin = GetBuildProp(options, nameof(GitInfoGitBin)),
            GitInfoVersionRegex = new Regex(GetBuildProp(options, nameof(GitInfoVersionRegex)), RegexOptions.Compiled),
            GitInfoGenerateAssemblyVersion = GetBuildPropBool(options, nameof(GitInfoGenerateAssemblyVersion)),
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
