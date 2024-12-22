using System.Reflection;
using System.Text.Json;

namespace GitInfo.Out;

internal static class Program
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions() { WriteIndented = true};

    public static void Main(string[] args)
    {
        var gitInfo = typeof(GitInfo)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(f => f.Name, f => f.GetValue(null));
        Console.WriteLine("GitInfo");
        Console.WriteLine(JsonSerializer.Serialize(gitInfo, Options));

        var assembly = Assembly.GetExecutingAssembly();
        var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
        var version = new Dictionary<string, string>
        {
            ["Version"] = assembly.GetName()?.Version?.ToString() ?? string.Empty,
            ["FileVersion"] = fvi.FileVersion ?? string.Empty,
            ["ProductVersion"] = fvi.ProductVersion ?? string.Empty,
        };
        Console.WriteLine("AssemblyInfo");
        Console.WriteLine(JsonSerializer.Serialize(version, Options));
    }
}
