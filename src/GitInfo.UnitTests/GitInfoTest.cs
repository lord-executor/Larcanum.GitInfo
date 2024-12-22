using System.Text.RegularExpressions;

using FluentAssertions;

namespace Larcanum.GitInfo.UnitTests;

public partial class GitInfoTest
{
    [GeneratedRegex(@"^v\d+\.\d+.\d+(?:-[\dA-Za-z\-\.]+)?$")]
    private partial Regex VersionExpression { get; }

    [Test]
    public void GeneratedClass_GitInfo_CommitHashExists()
    {
        GitInfo.CommitHash.Should().HaveLength(40);
    }

    [Test]
    public void GeneratedClass_GitInfo_CommitShortHashIsPrefixOfCommitHash()
    {
        GitInfo.CommitHash.StartsWith(GitInfo.CommitShortHash).Should().BeTrue();
    }

    [Test]
    public void GeneratedClass_GitInfo_CommitDateIsInThePast()
    {
        DateTime.TryParse(GitInfo.CommitDate, out var commitDate).Should().BeTrue();
        commitDate.Should().BeBefore(DateTime.Now);
    }

    [Test]
    public void GeneratedClass_GitInfo_TagMatchesProjectVersioning()
    {
        GitInfo.Tag.Should().MatchRegex(VersionExpression);
    }

    [Test]
    public void GeneratedClass_GitInfo_DebugEnabled()
    {
        GitInfo.Debug.Should().NotBeNull();
    }
}
