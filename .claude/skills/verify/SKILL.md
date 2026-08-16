---
name: verify
description: Build, test, and smoke-test the GitInfo solution using the same sequence as CI. Use after making changes to the generator, MSBuild targets, or tests, before telling the user something works.
---

Run these commands in order from the repo root, matching `.github/workflows/build.yaml` exactly. Stop and report if any step fails — do not skip ahead.

```
dotnet restore src/GitInfo.slnx
dotnet build --no-restore src/GitInfo.slnx
dotnet test --no-build --verbosity normal --solution src/GitInfo.slnx
dotnet run --no-build --project src/GitInfo.Out/GitInfo.Out.csproj
```

Notes:
- `dotnet test` uses the `Microsoft.Testing.Platform` runner (set in `global.json`) and requires the `--solution` flag, not a bare project path.
- The `GitInfo.Tag` unit test requires git tags to be present in the local clone. If it fails on a shallow clone, that's an environment issue, not a code regression — note it rather than treating it as a real failure.
- The final `dotnet run` step against `GitInfo.Out` is a real smoke test: it exercises the packed generator as a genuine NuGet package consumer (not a project reference), so don't treat it as optional even if the build and unit tests pass.
