namespace Larcanum.GitInfo;

public record GitInfoConfig
{
    public string RootNamespace { get; set; } = string.Empty;
    public string ProjectDir { get; set; } = string.Empty;
}
