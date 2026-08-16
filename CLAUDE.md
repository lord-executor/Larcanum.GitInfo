# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

`Larcanum.GitInfo` is a Roslyn incremental source generator, packaged as a NuGet package, that generates a `GitInfo` class exposing git commit/tag/version info (`IsDirty`, `Branch`, `CommitShortHash`, `CommitHash`, `CommitDate`, `Tag`, `DotNetVersion`) into consuming projects. It also drives assembly versioning via MSBuild properties derived from `git describe`.

Solution: `src/GitInfo.sln`, containing three projects:
- `src/GitInfo/` — the generator itself (`Larcanum.GitInfo`, netstandard2.0). Packs `build/Larcanum.GitInfo.props`/`.targets` and the compiled analyzer DLL into the NuGet package.
- `src/GitInfo.Out/` — a demo/smoke-test console app (net10.0) that consumes the generator via a real `PackageReference` (currently pinned to a specific published version — bump it after each release).
- `src/GitInfo.UnitTests/` — unit tests (net10.0, TUnit + AwesomeAssertions). References the generator via `ProjectReference` with `OutputItemType="Analyzer"`, and therefore has to **manually** `<Import>` `build/Larcanum.GitInfo.props` (top) and `.targets` (bottom) in its `.csproj` — package-based auto-import doesn't apply to project references.

## Build, test, and verify

Match the CI sequence in `.github/workflows/build.yaml` exactly — the final step is a real smoke test, not optional:

```
dotnet restore src/GitInfo.sln
dotnet build --no-restore src/GitInfo.sln
dotnet test --no-build --verbosity normal --solution src/GitInfo.sln
dotnet run --no-build --project src/GitInfo.Out/GitInfo.Out.csproj
```

- Test runner is `Microsoft.Testing.Platform` (set in `global.json`), not VSTest — use `--solution` with `dotnet test`, not a project path alone.
- One test asserts against `GitInfo.Tag`, which requires git tags to be present locally (CI checks out with `fetch-depth: 0`). If tags are missing from a shallow clone, that test will fail for reasons unrelated to your change.
- The `dotnet run` step on `GitInfo.Out` is the real end-to-end check: it proves the packed generator works for a genuine package consumer, not just a project reference.

## Source generator internals (read before touching generation logic)

- Generation is driven by a two-step MSBuild pipeline in `src/GitInfo/build/Larcanum.GitInfo.targets`: `GitInfoFingerprintFile` writes `obj/GitInfo.fingerprint.txt` from `git describe --tags --always --dirty` (only rewritten when changed), then `GitInfoCollect` (`BeforeTargets=BeforeBuild`) runs several git commands via a custom inline `RunGitCommand` task (`RoslynCodeTaskFactory`) and writes `obj/GitInfo.data.txt`. `GitInfoGenerator.cs` triggers purely off that file changing.
- **IDE staleness**: Rider/VS incremental builds may not always re-run the full target chain, so `GitInfo.data.txt` (and the generated code shown in-IDE) can lag behind actual git state. A real `dotnet build`/`pack`/`publish` from the CLI is authoritative — don't debug generation issues purely from IDE-displayed generated code.
- `RunGitCommand` supports `CommandBehavior` metadata of `ExitCode` or `ExitCodeInverted`. `IsDirty` uses `ExitCodeInverted` against `git diff --quiet HEAD` (exit 0 = clean = false) — this exact inversion was previously buggy (see commit `d36751d`), so be careful with sign/polarity when touching this.
- `GitInfoGenerator.cs` reports an `EmptyDataFile` diagnostic when `GitInfo.data.txt` comes back empty — preserve this when refactoring the generator's read path.
- Version parsing (`ParseVersion` in `GitInfoGenerator.cs`) expects `git describe` output like `v1.2.0-5-g627b708`, parsed via `GitInfoVersionRegex` (MAJOR/MINOR/PATCH/LABEL) plus `GitLabelRegex` for commit-count-since-tag as the .NET `Revision`. No matching tag anywhere in history defaults to `1.0.0.0`.

## Release process

`publish.ps1` at repo root: takes a `-version` param, runs `git tag $version`, `dotnet pack src\GitInfo\GitInfo.csproj -p:Version=$packageVersion`, then `dotnet nuget push` to nuget.org using `$env:NUGET_API_KEY`. This creates a real git tag and publishes to the public NuGet feed — do not run it without explicit user confirmation. After a release, `src/GitInfo.Out/GitInfo.Out.csproj`'s `PackageReference` version should be bumped to match.

## Code style (`.editorconfig`)

Differs from language defaults in these enforced (not just suggested) rules:
- `dotnet_style_readonly_field = true:warning` — fields must be `readonly` when possible.
- `csharp_prefer_static_local_function = true:warning` — local functions must be `static` when possible.
- `end_of_line = crlf` for `*.cs` (explicit, not the tool default on non-Windows).
