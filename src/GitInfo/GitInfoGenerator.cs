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
        /// <summary>
        /// The "git describe" format is roughly "[tag]-[additiona-commits]-g[short-hash]" as explained in
        /// https://git-scm.com/docs/git-describe - we extract the number of commits and use that as the _revision_
        /// for .NET versions.
        /// e.g. "v1.0.4-14-g2414721" is 14 commits ahead of v1.0.4 with a hash of 2414721
        /// </summary>
        private static readonly Regex GitLabelRegex = new Regex(@"^(?<REVISION>\d+)-g[\da-fA-F]", RegexOptions.Compiled);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var config = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
                GitInfoConfig.FromOptions(options.GlobalOptions));

            var pipeline = context.AdditionalTextsProvider
                .Where(static (text) => text.Path.Contains("GitInfo.fingerprint.txt"))
                .Select(static (text, _) => text.GetText());

            context.RegisterSourceOutput(pipeline.Combine(config),
                static (ctx, input) =>
                {
                    var (fingerprintSource, configValue) = input;

                    var git = new GitCommands(configValue.GitInfoGitBin, configValue.ProjectDir);
                    (configValue.GitPath, configValue.GitVersion) = git.Version();
                    configValue.GitRoot = git.RepositoryRoot();
                    configValue.GitFingerprint = fingerprintSource?.ToString().Trim();
                    var generatorContext = configValue.ToDictionary();

                    var tag = git.Tag();
                    var version = ParseVersion(tag, configValue);

                    var values = new Dictionary<string, string>
                    {
                        ["Context"] = ContextToComment(generatorContext),
                        ["Namespace"] = configValue.GitInfoGlobalNamespace ? string.Empty : $"namespace {configValue.RootNamespace};",
                        ["GitIsDirty"] = git.IsDirty().ToString().ToLowerInvariant(),
                        ["GitBranch"] = git.BranchName(),
                        ["GitCommitHash"] = git.CommitHash(),
                        ["GitCommitShortHash"] = git.CommitShortHash(),
                        ["GitCommitDate"] = git.CommitDate(),
                        ["GitTag"] = tag,
                        ["DotNetVersion"] = version.ToString(),
                    };

                    var versionAttributesWriter = new StringWriter();
                    if (configValue.GitInfoGenerateAssemblyVersion)
                    {
                        versionAttributesWriter.WriteLine($"[assembly: System.Reflection.AssemblyVersion(\"{version}\")]");
                        versionAttributesWriter.WriteLine($"[assembly: System.Reflection.AssemblyFileVersion(\"{version}\")]");
                        versionAttributesWriter.WriteLine($"[assembly: System.Reflection.AssemblyInformationalVersion(\"{values["GitTag"]}\")]");
                    }

                    values["VersionAttributes"] = versionAttributesWriter.ToString();

                    ctx.AddSource("GitInfo.g.cs", BuildSourceText("GitInfo.cs.tpl", values));

                    if (configValue.GitInfoDebug)
                    {
                        ctx.AddSource("GitInfo.Debug.g.cs",
                            BuildSourceText("GitInfo.Debug.cs.tpl",
                                new Dictionary<string, string>
                                {
                                    ["Namespace"] = configValue.GitInfoGlobalNamespace ? string.Empty : $"namespace {configValue.RootNamespace};",
                                    ["DebugProps"] = ContextToAnonProps(generatorContext)
                                }));
                    }
                });
        }

        private static Version ParseVersion(string tag, GitInfoConfig config)
        {
            // from a tag description like "v1.0.4-14-g2414721"
            //                                THIS ^^ is what we are extracting as the revision
            var revision = 0;
            var versionMatch = config.GitInfoVersionRegex.Match(tag);
            if (versionMatch.Groups["LABEL"].Success)
            {
                var revisionGroup = GitLabelRegex.Match(versionMatch.Groups["LABEL"].Value).Groups["REVISION"];
                revision = revisionGroup.Success ? int.Parse(revisionGroup.Value) : 0;
            }

            return versionMatch.Success
                ? new Version(int.Parse(versionMatch.Groups["MAJOR"].Value),
                    int.Parse(versionMatch.Groups["MINOR"].Value),
                    int.Parse(versionMatch.Groups["PATCH"].Value),
                    revision)
                : new Version(1, 0, 0, 0);
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
