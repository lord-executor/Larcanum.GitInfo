using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Larcanum.GitInfo
{
    [Generator]
    public class GitInfoDummyGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static postInitializationContext => {
                postInitializationContext.AddSource("GitInfo.g.cs", SourceText.From(GetGitInfoTemplate(), Encoding.UTF8));
            });
        }

        private static string GetGitInfoTemplate()
        {
            using var sourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Larcanum.GitInfo.GitInfo.cs.tpl");
            if (sourceStream != null)
            {
                using var reader = new StreamReader(sourceStream);
                var source = reader.ReadToEnd();
                source = source.Replace("$[GitIsDirty]", "false").Replace("$[GitCommit]", "0000000000000000000000000000000000000000");
                return source;
            }

            return "Unable to load GitInfo.cs.tpl";
        }
    }
}
