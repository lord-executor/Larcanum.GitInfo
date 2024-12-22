using FluentAssertions;

namespace Larcanum.GitInfo.UnitTests;

public class GitInfoTest
{
    [Test]
    public void GeneratedClass_GitInfo_CommitExists()
    {
        GitInfo.CommitHash.Should().HaveLength(40);
    }
}
