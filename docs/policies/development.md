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
When a service needs both `RevitAPI` and `RevitAPIUI`, keep the `RevitAPIUI` interaction in a thin boundary and move meaningful document logic behind it so it can be covered with `Nice3point.TUnit.Revit`. Do this where it creates a real testable boundary; do not add interfaces around trivial private UI calls solely to satisfy the rule.

Keep `CHANGELOG.md` up to date, in [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format — add an entry under `## [Unreleased]` (in the appropriate category: Added/Changed/Fixed/Removed/...) as part of the same change, not as a separate follow-up later.

Follow the stable module boundaries and design rules recorded in [Architecture decisions](../architecture.md). When a change introduces or revises a fundamental architectural decision, update that document as part of the same change.

Cross-project services and replaceable integrations live behind abstractions owned by the module that defines the contract. Consumers depend on interfaces; implementations are resolved through dependency injection. Construct concrete implementations directly only in composition roots, factories that are themselves part of the composition boundary, and tests. Do not introduce an interface for a private, stateless implementation that does not cross a module boundary or require substitution.

Abstractions live in a top-level `Abstractions/` folder inside their owning project and mirror the implementation layout (`Abstractions/Services/IFooService.cs` for `Infrastructure/Services/FooService.cs`; namespaces follow folders). Preserve this convention in new code. Move existing files into it only as part of an explicitly scoped architectural refactoring, not incidentally.

Avoid application state and business logic in WPF code-behind. Prefer bindings, commands, converters, behaviors and `DynamicResource`. Code-behind is reserved for view mechanics with no practical declarative equivalent. Reusable mechanics belong in `Microsoft.Xaml.Behaviors.Wpf` behaviors; mechanics intimate to one view may remain local. Do not repeat a XAML-declared base type in code-behind.

XML documentation is mandatory for public APIs in production projects; test projects are exempt. Documentation must explain the observable contract rather than restating the identifier. Use `<remarks>` for behavior that is not evident from the signature, including implicit fallback, inferred defaults, side effects, ordering, caching, thread or Revit-context requirements, transaction ownership, and intentionally ignored failures. Keep the note next to the affected API or configuration; do not rely on a distant architecture document as the only explanation. If a type is public only accidentally and has no external or cross-project consumer, reduce its visibility instead of documenting it as an API. Run a strict audit with `dotnet build Revit.Linter.slnx --configuration <configuration> -p:EnforcePublicApiDocumentation=true` and, for the updater, `dotnet build updater/Revit.Linter.Updater.slnx --configuration Release -p:EnforcePublicApiDocumentation=true`; it promotes `CS1591` to an error. Keep the switch opt-in until the existing public API baseline is documented, then make strict enforcement the default.

Do not invent abbreviations in identifiers, file names or documentation. Use complete words such as `ViewModel` and `Configuration`; established terms such as `API`, `WPF`, `XML`, `Guid`, `Id` and `Uri` remain valid.

Use `global using` only when most files in that project need the namespace. Keep specialized namespaces local, and do not repeat namespaces already supplied by `ImplicitUsings`. Apply this rule to new and touched code; clean up existing files only when doing so stays within the task scope.

## Release changelog

Before creating a release tag `vMAJOR.MINOR.PATCH`, move the content of `## [Unreleased]` in `CHANGELOG.md` into a new `## [MAJOR.MINOR.PATCH] - YYYY-MM-DD` section directly below it and leave `## [Unreleased]` empty. Commit that change and create the tag on that commit, so the changelog at the tagged commit names the released version and carries no unreleased entries. While moving the entries, review them against the previous release: keep changes a user of that release can observe, merge related entries, and drop fixes for behavior that was never released. This is a required step of the release procedure, not an automated gate: the release pipeline does not verify it.

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

## Warnings

Treat compiler and analyzer warnings as errors-in-waiting: do not leave new warnings in code you touch. Fix them as part of the same change. When a warning is a deliberate false positive, suppress it as narrowly as possible and add a nearby comment explaining why.

## Updater build unit

Everything under `updater/` builds independently of the root solution. Add updater package versions to `updater/Directory.Packages.props` and shared updater MSBuild settings to `updater/Directory.Build.props`; the root files do not apply there. Keep updater projects on `Microsoft.NET.Sdk` with `Debug`/`Release` configurations and out of `Revit.Linter.slnx`.

## Test execution

Projects using xUnit v3 4.0 run through Microsoft.Testing.Platform, selected in global.json. CI invokes each headless test project explicitly and treats a run with no discovered tests as a failure. Tests that require a running Revit process use the .RevitTests suffix and are excluded from hosted headless test runs.

The post-publish localization artifact project is an intentional exception: the build pipeline invokes its xUnit v3 executable through `dotnet run` after producing each ILRepack output. Microsoft.Testing.Platform does not discover this executable's test through `dotnet test`, while the in-process xUnit runner does. Do not replace this command with `dotnet test` unless discovery is verified for every supported target framework and configuration.

## Roslyn compatibility

Distributed analyzers and source generators target Microsoft.CodeAnalysis 4.14, matching Visual Studio 2022 version 17.14. Keep Roslyn references private implementation dependencies and verify analyzer or generator tests before changing this compatibility baseline.

## Logging

Log significant lifecycle steps and caught exceptions exactly once:

- Log application startup (including version) and shutdown at `Information`. Log user-meaningful operations such as diagnostic runs, configuration loads, report exports and applied fixes at `Information`; keep routine per-element details at `Debug`.
- Log expected refusals and validation failures at `Warning`. Log data, configuration, file-system and unexpected Revit API failures at `Error` with the exception instance.
- Log an exception where the failure is first understood. Callers that only surface the message remain silent; do not log the same exception again at every layer.
- Keep documented benign fallbacks, `Try*` probes and cancellation used as control flow silent, with a code comment where the reason is not obvious.
- Use structured message templates with named properties; do not interpolate values into message templates.
