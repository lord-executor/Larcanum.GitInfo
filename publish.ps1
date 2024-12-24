param(
    [Parameter(Mandatory=$true)]
    [string] $version = $null
)

$packageVersion = $version -replace 'v(.*)','$1'

git tag $version
dotnet pack .\src\GitInfo\GitInfo.csproj

dotnet nuget push .\src\GitInfo\bin\Release\Larcanum.GitInfo.$packageVersion.nupkg -k $env:NUGET_API_KEY --source "https://api.nuget.org/v3/index.json"
# Publishing to a local NuGet directory instead with
# dotnet nuget push .\src\GitInfo\bin\Release\Larcanum.GitInfo.$packageVersion.nupkg --source "C:\Users\..."
