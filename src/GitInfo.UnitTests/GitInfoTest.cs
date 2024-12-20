using FluentAssertions;

namespace Larcanum.GitInfo.UnitTests;

public class GitInfoTest
{
    [Test]
    public void GeneratedClass_GitInfo_CommitExists()
    {
        global::GitInfo.Commit.Should().HaveLength(40);
    }
}
