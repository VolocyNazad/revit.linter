# Development policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

The stack described in [Repository guide](../repository.md) is used by default and takes priority over
whatever the agent might choose on its own. Prefer whatever is already used
in the affected part of the repository over an alternative. If an alternative
seems better — don't substitute it silently: ask the user,
explain the reason, and wait for confirmation.

If a change affects the folder structure or the technology stack (a new/removed project, a new dependency, a version bump worth recording, a new convention) — update the relevant policy and [repository guide](../repository.md) as part of the same change, not as a separate follow-up later.

Before changing existing tests or writing new ones, ask the user first — clarify what and how it should be covered (or that the change is trivial enough that no clarification is needed), rather than deciding this on your own.

Commit messages follow Conventional Commits (`<type>(<scope>): <description>`, e.g. `feat(manifest): ...`, `fix(...): ...`, `docs(agents): ...`, `test(...): ...`, `chore(...): ...`, `refactor(...): ...`) — the scope is optional but preferred when it clarifies what exactly changed.

Do not `git push` — commit locally and leave the push to the user, unless they explicitly ask you to push.

Tests that talk to live Revit API objects must use `Nice3point.TUnit.Revit` (see the [repository guide](../repository.md) for stack/testing for when this is needed versus when plain xunit is sufficient) — it launches a real Revit process, so it cannot be used to test code that uses `RevitAPIUI`.

Keep `CHANGELOG.md` up to date, in [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format — add an entry under `## [Unreleased]` (in the appropriate category: Added/Changed/Fixed/Removed/...) as part of the same change, not as a separate follow-up later.

## .NET SDK selection

Use the repository-root `global.json` for local builds and CI: SDK `10.0.103` or a later stable SDK in the `10.0` major/minor line (`rollForward: latestFeature`, `allowPrerelease: false`). Do not roll forward to another major/minor line without updating this policy and `global.json` together. GitHub Actions setup steps must read `global-json-file: global.json` after checkout. Additional SDKs may be installed to supply runtimes for older test targets; they do not replace the SDK selected by `global.json`. This policy does not change project target frameworks.

## Solution items

Keep the root solution in sync with existing repository-level documentation, license, Git/editor settings, SDK/NuGet/versioning and shared MSBuild configuration, and root maintenance scripts. Expose root files under `solutionItems`, `.github/` and `scripts/` under matching subfolders, and documentation under `docs/` with its directory hierarchy. Add, rename or remove solution links together with the corresponding files. Include only existing files, once each; exclude local settings, secrets and generated output. Solution templates must follow the same convention.
## Repository validation

Run `./scripts/Validate-Repository.ps1` after changing repository-level files,
documentation or the solution. CI runs the same validator. It checks the required
files and navigation links and verifies that managed repository files are present
exactly once in Solution Items. Update the solution together with added, renamed or
removed managed files. Solution templates follow the same contract.

## Formatting baseline

Keep the root `.editorconfig` in the solution and apply its repository-wide encoding, line-ending and trailing-whitespace rules. Repository-specific sections may add stricter language rules. Update the file deliberately when formatting conventions change; do not replace specialized rules with the shared minimum.
## Test execution

Projects using xUnit v3 4.0 run through Microsoft.Testing.Platform, selected in global.json. CI invokes each headless test project explicitly and treats a run with no discovered tests as a failure. Tests that require a running Revit process use the .RevitTests suffix and are excluded from hosted headless test runs.

## Roslyn compatibility

Distributed analyzers and source generators target Microsoft.CodeAnalysis 4.14, matching Visual Studio 2022 version 17.14. Keep Roslyn references private implementation dependencies and verify analyzer or generator tests before changing this compatibility baseline.
