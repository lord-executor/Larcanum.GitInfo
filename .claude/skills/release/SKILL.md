---
name: release
description: Cut a new Larcanum.GitInfo release by tagging and pushing, which triggers the automated NuGet publish workflow. Use when the user asks to release, publish, cut a version, or ship a new package version.
---

Releasing is a tag push, nothing more — `.github/workflows/publish.yaml` does the rest (build, test, pack, and push to nuget.org via Trusted Publishing).

## Steps

1. Make sure `main` is clean and holds everything you want to release (`git status`, `git log`).
2. Pick the version number (SemVer, no `v` prefix yet), e.g. `1.4.0`. Check the last tag with `git describe --tags --abbrev=0` if unsure what's next.
3. Confirm the version with the user before tagging — this is irreversible-in-spirit (a tag push immediately publishes a public NuGet package) and needs explicit confirmation per `CLAUDE.md`.
4. Create and push an annotated tag:
   ```
   git tag -a v1.4.0 -m "v1.4.0"
   git push origin v1.4.0
   ```
5. Watch the `Publish` workflow run (`gh run watch` or `gh run list --workflow=publish.yaml`). It runs the same build/format/test/smoke-test sequence as CI, then packs and pushes `Larcanum.GitInfo` + its `.snupkg` to nuget.org.
6. Once it succeeds, bump the `PackageReference` version for `Larcanum.GitInfo` in `src/GitInfo.Out/GitInfo.Out.csproj` to the new version, commit, and push to `main` — this keeps the smoke-test project (and CI) on the latest published package.

## How publishing works

- The workflow triggers on `push: tags: ["v*.*.*"]` (`.github/workflows/publish.yaml`).
- The package version is derived from the tag name by stripping the leading `v` (`v1.4.0` → `1.4.0`), not from any `.csproj` `<Version>`.
- No NuGet API key is stored anywhere. The job requests `id-token: write`, exchanges a GitHub OIDC token for a short-lived nuget.org API key via the `NuGet/login` action (nuget.org **Trusted Publishing**, see https://aka.ms/nuget/trusted-publishing), and uses that key for a single `dotnet nuget push`.
- The `NUGET_USER` repository secret holds the nuget.org account/profile name (not the email) that the trusted publishing policy is registered under.
- The nuget.org trusted publishing policy for this repo must have: Repository Owner = the GitHub org/user, Repository = `Larcanum.GitInfo`, Workflow File = `publish.yaml` (no path), no Environment restriction (the workflow doesn't use a GitHub Environment gate).
- Symbols: the pack step sets `IncludeSymbols=true` and `SymbolPackageFormat=snupkg`; pushing the `.nupkg` by exact path lets `dotnet nuget push` auto-discover and push the matching `.snupkg` from the same output directory.
- If a tag is pushed but the push step fails after a partial publish (e.g. nupkg succeeded, snupkg didn't), `--skip-duplicate` makes the push step safe to re-run by re-triggering the workflow (e.g. via `gh workflow run` after fixing the issue) — nuget.org will reject/skip the already-published nupkg rather than erroring.
- There is no local packing/publishing path anymore (the old `publish.ps1` was removed) — everything happens in CI off the tag push.

## If something goes wrong

- A failed run does **not** auto-retry. Fix the issue on `main`, then delete and re-push the tag (`git tag -d v1.4.0 && git push origin :refs/tags/v1.4.0`, then redo step 4) — confirm with the user before deleting/re-pushing a tag, since it rewrites what the tag points to.
- A tag pushed to the wrong commit should be deleted and re-created the same way, with user confirmation.
