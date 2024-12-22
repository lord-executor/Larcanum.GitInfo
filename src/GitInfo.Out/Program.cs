// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Text.Json;

var options = new JsonSerializerOptions() { WriteIndented = true};

var gitInfo = typeof(GitInfo)
    .GetFields(BindingFlags.Public | BindingFlags.Static)
    .ToDictionary(f => f.Name, f => f.GetValue(null));
Console.WriteLine(JsonSerializer.Serialize(gitInfo, options));

var assembly = Assembly.GetExecutingAssembly();
var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
var version = new Dictionary<string, string>
{
    ["Version"] = assembly.GetName()?.Version?.ToString() ?? string.Empty,
    ["FileVersion"] = fvi.FileVersion ?? string.Empty,
    ["ProductVersion"] = fvi.ProductVersion ?? string.Empty,
};
Console.WriteLine(JsonSerializer.Serialize(version, options));
