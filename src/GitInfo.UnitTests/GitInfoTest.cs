using System.Text.RegularExpressions;

using AwesomeAssertions;

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
        // Expecting all tags to be present in CI is a bit problematic, so we don't
        // do that. The tag _either_ matches the version expression or it matches
        // the short hash if tags are not available.
        if (VersionExpression.IsMatch(GitInfo.Tag))
        {
            true.Should().BeTrue();
        }
        else
        {
            GitInfo.Tag.Should().Be(GitInfo.CommitShortHash);
        }
    }

    [Test]
    public void GeneratedClass_GitInfo_DebugEnabled()
    {
        GitInfo.Debug.Should().NotBeNull();
    }
}
