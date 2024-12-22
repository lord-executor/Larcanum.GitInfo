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
                };

                var versionAttributesWriter = new StringWriter();
                if (configValue.GitInfoGenerateAssemblyVersion)
                {
                    versionAttributesWriter.WriteLine("[assembly: System.Reflection.AssemblyVersion(\"1.2.4.8\")]");
                    versionAttributesWriter.WriteLine("[assembly: System.Reflection.AssemblyFileVersion(\"1.2.4.8\")]");
                    versionAttributesWriter.WriteLine($"[assembly: System.Reflection.AssemblyInformationalVersion(\"{values["GitTag"]}\")]");
                }
                values["VersionAttributes"] = versionAttributesWriter.ToString();

                var source = GetGitInfoTemplate();
                var regex = new Regex(@"\$\((.*?)\)", RegexOptions.Compiled);
                source = regex.Replace(source, match =>
                {
                    return values.TryGetValue(match.Groups[1].Value, out var value)
                        ? value
                        : "<unknown>";
                });

                ctx.AddSource("GitInfo.g.cs", SourceText.From(source, Encoding.UTF8));
            });

            // context.AdditionalTextsProvider
            //     .Combine(context.AnalyzerConfigOptionsProvider)
            //     .Select((pair, ctx) => pair.Right.GetOptions(pair.Left).TryGetValue("build_property.RootNamespace"))

            // context.RegisterPostInitializationOutput(postInitializationContext => {
            //     postInitializationContext.AddSource("GitInfo.g.cs", SourceText.From(GetGitInfoTemplate("hmm"), Encoding.UTF8));
            // });
        }

        private static string GetGitInfoTemplate()
        {
            using var sourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Larcanum.GitInfo.GitInfo.cs.tpl");
            if (sourceStream != null)
            {
                using var reader = new StreamReader(sourceStream);
                return reader.ReadToEnd();
            }

            return "Unable to load GitInfo.cs.tpl";
        }
    }
}
