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
                (configValue.GitPath, configValue.GitVersion) = git.Version();
                var generatorContext = configValue.ToDictionary();

                var values = new Dictionary<string, string>
                {
                    ["Context"] = ContextToComment(generatorContext),
                    ["Namespace"] = configValue.GitInfoGlobalNamespace ? string.Empty : $"namespace {configValue.RootNamespace};",
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

                if (configValue.GitInfoDebug)
                {
                    ctx.AddSource("GitInfo.Debug.g.cs", BuildSourceText("GitInfo.Debug.cs.tpl", new Dictionary<string, string>
                    {
                        ["Namespace"] = configValue.GitInfoGlobalNamespace ? string.Empty : $"namespace {configValue.RootNamespace};",
                        ["DebugProps"] = ContextToAnonProps(generatorContext)
                    }));
                }
            });
        }

        private static string ContextToComment(Dictionary<string, string> context)
        {
            return string.Join("\n", context
                .Select(pair => $"// {pair.Key}: {pair.Value}")
                .Prepend("// Generator Context"));
        }

        private static string ContextToAnonProps(Dictionary<string, string> context)
        {
            return string.Join("\n", context
                .Select(pair => $"        {pair.Key} = @\"{pair.Value}\","));
        }

        private static SourceText BuildSourceText(string templateName, Dictionary<string, string> values)
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
