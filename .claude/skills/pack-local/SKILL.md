---
name: pack-local
description: Pack the GitInfo generator as a NuGet package for local consumption in another project, verifying/configuring the local package source first. Use when the user asks to test the package locally, build a local NuGet package, or pack a dev/pre-release version for another project to consume.
---

Implements the "Testing NuGet Package Locally" workflow from `README.md` (Development section), end to end: make sure a local package source exists, then `dotnet pack` `src/GitInfo/GitInfo.csproj` with the right version into it.

## Arguments

`args` may contain, in either order, separated by whitespace:
- A version string, e.g. `v1.3.2-rc.1` or `1.3.2-rc.1`. Strip any leading `v` before using it as `-p:Version` — MSBuild's `$(Version)` property cannot have a `v` prefix (see README "Build Property $(Version)").
- A target directory path, e.g. `D:\some\dir`.

Either or both may be omitted:
- No version given → compute the next version automatically (see below).
- No target directory given → default to `%userprofile%\.nuget.local`, matching the README's documented convention.

## Step 1 — Determine the version

If the user passed an explicit version, use it (minus any `v` prefix) and skip to Step 2.

Otherwise compute the next incremental version from the latest git tag:
```
git describe --tags --abbrev=0
```
- If this returns a tag like `v1.3.1`, strip the `v` and bump PATCH by one → `1.3.2`. This is a plain patch bump; if the user wants a minor/major bump or a pre-release label instead, ask them for an explicit version rather than guessing.
- If it fails (no tags reachable from `HEAD` at all), default to `1.0.0` — this matches the generator's own fallback behavior documented in `CLAUDE.md` ("No matching tag anywhere in history defaults to 1.0.0.0").

Confirm the resulting version with the user before packing only if you had to guess at anything non-obvious (e.g. tags exist but look unusual); a straightforward patch bump doesn't need confirmation.

## Step 2 — Verify or configure the local package source

Check what's registered:
```
dotnet nuget list source
```

- If a source already points at the target directory (by path, not just by name), nothing to do — move on.
- If not, and the target directory doesn't exist yet, create it first.
- Then register it:
  ```
  dotnet nuget add source "<target-dir>" --name "Local Test Packages"
  ```
  Use the name `Local Test Packages` when using the default directory (matches the README example); if the user gave a custom directory, a descriptive name is fine.

Use `dotnet nuget add/list/remove source` rather than hand-editing `NuGet.Config` — it's the cross-platform-correct way and avoids XML formatting mistakes. (Don't rely on `dotnet nuget add source --help`; its output is misleadingly incomplete on some SDK versions even though the command works.)

## Step 3 — Pack

```
dotnet pack src\GitInfo\GitInfo.csproj -c Release -p:Version="<version>" -o "<target-dir>"
```

## Step 4 — Warn about the local NuGet cache

Check whether the resolved version already exists in the user's global packages cache:
```
Test-Path "$env:userprofile\.nuget\packages\larcanum.gitinfo\<version>"
```
If it does, tell the user they must delete that cached copy before a consuming project will pick up the newly packed one (per the README warning) — don't delete it yourself without asking, since it's a global, shared cache that other projects may also be using.

## Step 5 — Report

Tell the user the package path (`<target-dir>\Larcanum.GitInfo.<version>.nupkg`) and remind them that a consuming project needs `-p:Version="<version>"` or an explicit `<PackageReference Version="<version>">` pointed at the local source to pick it up.
