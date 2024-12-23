# Overview

`Larcanum.GitInfo` is a package that you can add to your project to get access to some basic `git` information directly
in your source code through a _generated_ `GitInfo` class and some build integration to configure MSBuild to use the
version information extracted from `git`. The goal of the package is to simplify versioning of library packages and
binaries based on `git` conventions.

Being able to statically access basic `git` version information is also very useful in the context of applications
and web services since this allows that information to be exposed to the end user. This also works during development
which turns out to be rather useful when working with various different versions of web service projects at the same
time.

This package is designed to be as simple as possible while still getting the job done and to that end, it makes some
relatively strong assumptions.

- The `git` binary is present in the `PATH`
  - This can be configured manually, but by default it assumes that `git` is in the PATH and there is no attempt at discovering other locations
- You are using [Semantic Versioning](https://semver.org/)
- You are using `git` tags as the primary source of version information

If you need more features than what `Larcanum.GitInfo` provides, then you have three options

- You can create an issue [here](https://github.com/lord-executor/Larcanum.GitInfo/issues) and if it falls within the scope of the project, then it will likely be added
  - More customization options in the form of configurable variants are good candidates for this 
- You can fork this project and customize from there
  - As stated above, the simplicity of the implementation was a primary design goal which makes it easy for most developers to understand and thus easy to customize
- You can use [devlooped/GitInfo](https://www.devlooped.com/GitInfo/) instead which has very similar functionality but has way more features
  - Also note that said project comes with [SponsorLink](https://github.com/devlooped#sponsorlink) which is not everybody's cup of tea

# What it Looks Like

The generated code looks something like the example below which is actually taken from _this_ repository. The information includes:
- The _dirty_ state of the work tree which is considered dirty if there are any uncommitted changes.
- The branch and commit information of the current `HEAD`.
- The tag description which is assumed to contain a semantic version string.
- A version string that is compatible with the .NET `Version` class and is derived from the tag description.

**NOTE:** If you are looking at the generated source file in your IDE, it _may_ not be 100% up to date since IDEs like
VisualStudio and Rider try to do some "shortcuts" to optimize the build process which means that the `GitInfoFingerprintFile`
target is not always triggered. This should NOT matter, however since any proper `dotnet build` or packing or publishing
will include that target and thus the `GitInfo` class that is actually part of the final assembly will be up to date.

```cs
[assembly: System.Reflection.AssemblyVersion("0.5.0")]
[assembly: System.Reflection.AssemblyFileVersion("0.5.0")]
[assembly: System.Reflection.AssemblyInformationalVersion("v0.5.0")]

namespace Larcanum.GitInfo.UnitTests;

public partial class GitInfo
{
    public const bool IsDirty = true;

    public const string Branch = @"main";

    public const string CommitShortHash = @"a777e94";

    public const string CommitHash = @"a777e94677cbe32c3d2d1c1582ecdbf75ac159f8";

    public const string CommitDate = @"2024-12-23T11:54:14+01:00";

    public const string Tag = @"v0.5.0";

    public const string DotNetVersion = @"0.5.0";
}
```

- If the current `HEAD` is tagged, then the tag _is_ the description and if it contains a semantic version, this will be extracted as the .NET version
  - e.g. `v0.5.0` /  `0.5.0`
- If the current `HEAD` is not tagged, but there is a previous tag in the commit history, then the tag description will include the tag as well as the number of commits since that tag and the short commit hash
  - e.g. `v0.5.0-1-g7af059a` / `0.5.0`
- If there is _no_ tag between the current `HEAD` and the root, then the tag description will just consist of the short commit hash and the .NET version will default to 1.0.0
  - e.g. `e83e39d` / `1.0.0` 

# How it Works

## Incremental Source Generator
TODO

## Assembly Version Attributes
TODO

## 
TODO

# Configuration

All the configuration happens through MSBuild properties that can be added to the project file

- `<GitInfoGlobalNamespace>true</GitInfoGlobalNamespace>`
  Defaults to `false`. When set to `false`, the generated `GitInfo` class will be added to the project's root namespace, but when set to `true` it will be added to the _global_ namespace instead.
- `<GitInfoGitBin>/usr/bin/git</GitInfoGitBin>`
  Defaults to `git`. This is the path to the `git` binary that will be used to gather the `GitInfo` details.
- `<GitInfoUpdateVersionProp>false</GitInfoUpdateVersionProp>`
  Defaults to `true`. This value determines if the GitInfo targets are going to try to set the `$(Version)` property based on the git tag description. When set to `false`, the Version property will be left as is.
- `<GitInfoGenerateAssemblyVersion>false</GitInfoGenerateAssemblyVersion>`
  Defaults to `true`. When enabled, this will include the 3 versioning attributes `AssemblyVersion`, `AssemblyFileVersion` and `AssemblyInformationalVersion` in the generated "GitInfo.g.cs" file and disable the default "GenerateAssembly...Attribute" flags.
- `<GitInfoDebug>true</GitInfoDebug>`
  Defaults to `false`. Enables the generation of a dedicated `GitInfo.Debug` property when set to `true` which contains all the context that the generator class had when generating the `GitInfo` class. Useful for debugging and should of course not be enabled for release builds.
