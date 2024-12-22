using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Larcanum.GitInfo
{
    [Generator]
    public class GitInfoGenerator : IIncrementalGenerator
    {
        private static readonly Regex PlaceholderRegex = new Regex(@"\$\((.*?)\)", RegexOptions.Compiled);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var config = context.AnalyzerConfigOptionsProvider.Select(static (options, cancellationToken) =>
                GitInfoConfig.FromOptions(options.GlobalOptions));

            context.RegisterSourceOutput(config, (ctx, configValue) =>
            {
                var git = new GitCommands(configValue.GitInfoGitBin, configValue.ProjectDir);
                var gitVersion = git.Version();

                var sw = new StringWriter();
                sw.WriteLine("// Generator Context");
                sw.WriteLine($"// ProjectDir: {configValue.ProjectDir}");
                sw.WriteLine($"// RootNamespace: {configValue.RootNamespace}");
                sw.WriteLine($"// GitBin: {gitVersion.GitPath}");
                sw.WriteLine($"// GitVersion: {gitVersion.Version}");
                sw.WriteLine($"// GitInfoGenerateAssemblyVersion: {configValue.GitInfoGenerateAssemblyVersion}");
                sw.WriteLine($"// Test: {configValue}");
                sw.WriteLine($"// Timestamp: {DateTime.Now:o}");

                var values = new Dictionary<string, string>
                {
                    ["Context"] = sw.ToString(),
                    ["GitRoot"] = git.RepositoryRoot(),
                    ["GitIsDirty"] = git.IsDirty().ToString().ToLowerInvariant(),
                    ["GitBranch"] = git.BranchName(),
                    ["GitCommitHash"] = git.CommitHash(),
                    ["GitCommitShortHash"] = git.CommitShortHash(),
                    ["GitCommitDate"] = git.CommitDate(),
                    ["GitTag"] = git.Tag(),
                    ["DebugConstants"] = string.Empty,
                };

                var versionAttributesWriter = new StringWriter();
                if (configValue.GitInfoGenerateAssemblyVersion)
                {
                    versionAttributesWriter.WriteLine("[assembly: System.Reflection.AssemblyVersion(\"1.2.4.8\")]");
                    versionAttributesWriter.WriteLine("[assembly: System.Reflection.AssemblyFileVersion(\"1.2.4.8\")]");
                    versionAttributesWriter.WriteLine($"[assembly: System.Reflection.AssemblyInformationalVersion(\"{values["GitTag"]}\")]");
                }
                values["VersionAttributes"] = versionAttributesWriter.ToString();

                ctx.AddSource("GitInfo.g.cs", BuildSourceText("GitInfo.cs.tpl", values));

                var debugValues = typeof(GitInfoConfig)
                    .GetProperties()
                    .ToDictionary(p => p.Name, p => p.GetValue(configValue));
                debugValues["GitPath"] = gitVersion.GitPath ?? string.Empty;
                debugValues["GitVersion"] = gitVersion.Version ?? string.Empty;
                debugValues["Timestamp"] = DateTime.Now.ToString("o");
                debugValues["GitPath"] = gitVersion.GitPath ?? string.Empty;

                var propWriter = new StringWriter();
                foreach (var pair in debugValues)
                {
                    propWriter.WriteLine($"{pair.Key} = @\"{pair.Value}\",");
                }

                ctx.AddSource("GitInfo.Debug.g.cs", BuildSourceText("GitInfo.Debug.cs.tpl", new Dictionary<string, string> { ["DebugProps"] = propWriter.ToString() }));
            });
        }

        private SourceText BuildSourceText(string templateName, Dictionary<string, string> values)
        {
            var source = GetGitInfoTemplate(templateName);
            source = PlaceholderRegex.Replace(source, match =>
                values.TryGetValue(match.Groups[1].Value, out var value)
                    ? value
                    : "<unknown>");
            return SourceText.From(source, Encoding.UTF8);
        }

        private static string GetGitInfoTemplate(string templateName)
        {
            using var sourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Larcanum.GitInfo.{templateName}");
            if (sourceStream != null)
            {
                using var reader = new StreamReader(sourceStream);
                return reader.ReadToEnd();
            }

            return $"Unable to load {templateName}";
        }
    }
}
