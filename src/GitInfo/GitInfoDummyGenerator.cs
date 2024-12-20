using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Larcanum.GitInfo
{
    [Generator]
    public class GitInfoDummyGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static postInitializationContext => {
                postInitializationContext.AddSource("GitInfo.g.cs", SourceText.From("""
                    using System;

                    public partial class GitInfo
                    {
                        public const string Commit = "0000000000000000000000000000000000000000";
                    }
                    """, Encoding.UTF8));
            });
        }
    }
}
