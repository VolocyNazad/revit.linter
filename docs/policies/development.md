# Development policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

The stack described in [Repository guide](../repository.md) is used by default and takes priority over
whatever the agent might choose on its own. Prefer whatever is already used
in the affected part of the repository over an alternative. If an alternative
seems better — don't substitute it silently: ask the user,
explain the reason, and wait for confirmation.

If a change affects the folder structure or the technology stack (a new/removed project, a new dependency, a version bump worth recording, a new convention) — update the relevant policy and [repository guide](../repository.md) as part of the same change, not as a separate follow-up later.

Before changing existing tests or writing new ones, ask the user first — clarify what and how it should be covered (or that the change is trivial enough that no clarification is needed), rather than deciding this on your own.

Commit messages follow the [commit message policy](commits.md).

Do not `git push` — commit locally and leave the push to the user, unless they explicitly ask you to push.

Branching follows the [branching policy](branching.md): `main` holds the released state,
`dev` integrates the next release, and every change reaches them through a short-lived
branch and a pull request.

Tests that talk to live Revit API objects must use `Nice3point.TUnit.Revit` (see the [repository guide](../repository.md) for stack/testing for when this is needed versus when plain xunit is sufficient) — it launches a real Revit process, so it cannot be used to test code that uses `RevitAPIUI`.
When a service needs both `RevitAPI` and `RevitAPIUI`, keep the `RevitAPIUI` interaction in a thin boundary and move meaningful document logic behind it so it can be covered with `Nice3point.TUnit.Revit`. Do this where it creates a real testable boundary; do not add interfaces around trivial private UI calls solely to satisfy the rule.

Do not update `CHANGELOG.md` with every change — leave `## [Unreleased]` empty during regular development. The release section is composed once when a release is prepared, from the changes since the previous stable tag (see the [release changelog](#release-changelog)). `CHANGELOG.md` keeps the [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format.

Follow the stable module boundaries and design rules recorded in [Architecture decisions](../architecture.md). When a change introduces or revises a fundamental architectural decision, update that document as part of the same change.

Cross-project services and replaceable integrations live behind abstractions owned by the module that defines the contract. Consumers depend on interfaces; implementations are resolved through dependency injection. Construct concrete implementations directly only in composition roots, factories that are themselves part of the composition boundary, and tests. Do not introduce an interface for a private, stateless implementation that does not cross a module boundary or require substitution.

Abstractions live in a top-level `Abstractions/` folder inside their owning project and mirror the implementation layout (`Abstractions/Services/IFooService.cs` for `Infrastructure/Services/FooService.cs`; namespaces follow folders). Preserve this convention in new code. Move existing files into it only as part of an explicitly scoped architectural refactoring, not incidentally.

Avoid application state and business logic in WPF code-behind. Prefer bindings, commands, converters, behaviors and `DynamicResource`. Code-behind is reserved for view mechanics with no practical declarative equivalent. Reusable mechanics belong in `Microsoft.Xaml.Behaviors.Wpf` behaviors; mechanics intimate to one view may remain local. Do not repeat a XAML-declared base type in code-behind.

Every WPF view and window must render representative content in the XAML designer instead of empty fields. Design-time data lives in a `DesignData/` folder of the owning presenter project: a `<ViewName>DesignData` class whose instance properties mirror every binding path the view reads through its data context, including item templates, converters and behaviors, with sample values and no-op commands. The view root declares `d:DataContext="{d:DesignInstance Type={x:Type designData:<ViewName>DesignData}, IsDesignTimeCreatable=True}"`, and item templates receive sample rows through the collection the design data exposes. Localized text is read through `LocalizationResourceReader` with the same resource base name the view model uses, not copied as literals into the design class. A view composed through `ViewLocatorExtension` shows its sub-view with that sub-view's own design data. Change the design data together with any binding change; an empty field, an unresolved binding or a designer exception is a defect, not an acceptable preview state. For a step-driven or mode-driven view it is enough to preview one representative state. A view, window or dialog whose runtime constructor needs application services also exposes a public parameterless constructor for the designer that only loads its XAML; the runtime composition keeps the service constructor.

XML documentation is mandatory for public APIs in production projects; test projects are exempt. Documentation must explain the observable contract rather than restating the identifier. Use `<remarks>` for behavior that is not evident from the signature, including implicit fallback, inferred defaults, side effects, ordering, caching, thread or Revit-context requirements, transaction ownership, and intentionally ignored failures. Keep the note next to the affected API or configuration; do not rely on a distant architecture document as the only explanation. If a type is public only accidentally and has no external or cross-project consumer, reduce its visibility instead of documenting it as an API. Run a strict audit with `dotnet build Revit.Linter.slnx --configuration <configuration> -p:EnforcePublicApiDocumentation=true` and, for the updater, `dotnet build updater/Revit.Linter.Updater.slnx --configuration Release -p:EnforcePublicApiDocumentation=true`; it promotes `CS1591` to an error. Keep the switch opt-in until the existing public API baseline is documented, then make strict enforcement the default.

Do not invent abbreviations in identifiers, file names or documentation. Use complete words such as `ViewModel` and `Configuration`; established terms such as `API`, `WPF`, `XML`, `Guid`, `Id` and `Uri` remain valid.

Use `global using` only when most files in that project need the namespace. Keep specialized namespaces local, and do not repeat namespaces already supplied by `ImplicitUsings`. Apply this rule to new and touched code; clean up existing files only when doing so stays within the task scope.

Keep [CHANGELOG.md](../../CHANGELOG.md) up to date as the [changelog policy](changelog.md) requires.

## Release channels

The product has two release channels that share one repository, changelog and build pipeline.

A **stable release** is a `vMAJOR.MINOR.PATCH` tag. It is the only channel the updater offers. Publish
its GitHub Release without the **Set as a pre-release** flag, so `/releases/latest` returns it and
existing users can discover it; the Build pipeline creates the release without that flag. Push the tag
only after the build is green for every supported Revit configuration, the working tree is clean and the
release changelog section is committed, as described above. A push of the tag starts the publishing
workflow; do not rebuild or move a pushed release tag.

A **pre-release** is a `vMAJOR.MINOR.PATCH-<channel>.<number>` tag, for example `-beta.1` or `-rc.1`.
Use it when a build should reach testers before the stable release: a risky migration, a new installer or
a large diagnostic change that benefits from real-model feedback. Publish its GitHub Release with the
**Set as a pre-release** flag. Pre-releases are downloaded from the Releases page by people who opt in;
the updater never announces them, and GitHub does not treat them as `latest`.

Current automation publishes stable releases only: the release workflow requires
`GitVersion` to produce a stable version and `gh release create` is invoked without `--prerelease`.
Publish a pre-release manually with `gh release create vX.Y.Z-beta.N --prerelease ...` (or extend the
pipeline first) and do not use a stable number for it.

The rules that prevent broken release metadata:

- A stable version must never be marked as a pre-release. That hides it from `/releases/latest` and from
  the updater, leaving users on the previous stable release even though the tag and installer exist.
- A pre-release must never use a stable version number. Use `vX.Y.Z-beta.N`; `vX.Y.Z` is reserved for the
  stable release that follows it.
- A pushed tag is immutable. Do not delete it, move it to another commit or force-push it; publish the
  next version instead. If a mistake is found before the tag is pushed and no release was published, fix
  the commit, move the local tag and push it once.
- If a stable release was published with the pre-release flag by mistake, correct the GitHub release
  metadata (uncheck the flag) instead of moving or deleting the tag. Keep its assets: users may have
  downloaded them, and the release page and changelog link to them.
- Only the stable release has a `CHANGELOG.md` version section. Pre-release notes may summarise the
  changes for testers, but they do not create a permanent changelog section.

## Migration tracking

Every user-visible breaking migration is recorded in `docs/migrations/` when it is designed, not when it ships. The
registry defines the lifecycle, the version window, the entry template and the removal gate in
[docs/migrations/README.md](../migrations/README.md); follow it.

An entry states the product versions on both sides, why the change is breaking, the exact user action, what the
product does automatically, the data-safety guarantees, the removal gate and the exact cleanup checklist. A change
that introduces, revises or ends a migration updates its entry in the same change. The release that ships the
migration sets the entry to `Active` with the actual tag and describes the migration and its removal plan in the
release changelog section. Transitional behaviour is removed only through the entry's removal gate, and that
change also updates the entry, the architecture decision, the compatibility contract, the manual checklist and the
`TODO.md` task. A Wiki migration guide is never deleted: after retirement it stays as historical documentation for
users who skip several versions.

## .NET SDK selection

Use the repository-root `global.json` for local builds and CI: SDK `10.0.103` or a later stable SDK in the `10.0` major/minor line (`rollForward: latestFeature`, `allowPrerelease: false`). Do not roll forward to another major/minor line without updating this policy and `global.json` together. GitHub Actions setup steps must read `global-json-file: global.json` after checkout. Additional SDKs may be installed to supply runtimes for older test targets; they do not replace the SDK selected by `global.json`. This policy does not change project target frameworks.

## Solution items

Keep the root solution in sync with existing repository-level documentation, license, Git/editor settings, SDK/NuGet/versioning and shared MSBuild configuration, and root maintenance scripts. Expose root files under `solutionItems`, `.github/` and `scripts/` under matching subfolders, and documentation under `docs/` with its directory hierarchy. Add, rename or remove solution links together with the corresponding files. Include only existing files, once each; exclude local settings, secrets and generated output. Solution templates must follow the same convention.
## Repository validation

Run `./scripts/Validate-Repository.ps1` after changing repository-level files,
documentation or the solution. CI runs the same validator. It checks the required
files and navigation links and verifies that managed repository files are present
exactly once in Solution Items. It also fails when build output (`bin/` or `obj/`)
is tracked by Git. Update the solution together with added, renamed or removed
managed files. Solution templates follow the same contract.

## Formatting baseline

Keep the root `.editorconfig` in the solution and apply its repository-wide encoding, line-ending and trailing-whitespace rules. Repository-specific sections may add stricter language rules. Update the file deliberately when formatting conventions change; do not replace specialized rules with the shared minimum.

## Warnings

Treat compiler and analyzer warnings as errors-in-waiting: do not leave new warnings in code you touch. Fix them as part of the same change. When a warning is a deliberate false positive, suppress it as narrowly as possible and add a nearby comment explaining why. The release publish enforces this: the build pipeline compiles the add-in with `TreatWarningsAsErrors=true`, so any warning in a release configuration fails the publish. The build-and-test workflow builds the root and updater solutions; the build pipeline and installer are compiled by the release publish, so a warning there is caught during release preparation instead of on a pull request. Keep every release configuration warning-free; verify with a plain `dotnet build` before tagging.

## Updater build unit

Everything under `updater/` builds independently of the root solution. Add updater package versions to `updater/Directory.Packages.props` and shared updater MSBuild settings to `updater/Directory.Build.props`; the root files do not apply there. Keep updater projects on `Microsoft.NET.Sdk` with `Debug`/`Release` configurations and out of `Revit.Linter.slnx`.

## Test execution

Projects using xUnit v3 4.0 run through Microsoft.Testing.Platform, selected in global.json. CI invokes each headless test project explicitly and treats a run with no discovered tests as a failure. Tests that require a running Revit process use the .RevitTests suffix and are excluded from hosted headless test runs.

Testing rules scoped to one area live with that area: see [adding a built-in diagnostic](../diagnostics/adding.md).

The post-publish localization artifact project is an intentional exception: the build pipeline invokes its xUnit v3 executable through `dotnet run` after producing each ILRepack output. Microsoft.Testing.Platform does not discover this executable's test through `dotnet test`, while the in-process xUnit runner does. Do not replace this command with `dotnet test` unless discovery is verified for every supported target framework and configuration.

## Roslyn compatibility

Distributed analyzers and source generators target the Microsoft.CodeAnalysis.CSharp version pinned
centrally in `Directory.Packages.props` (currently 5.9.0), which is the analyzer API of the Visual
Studio version the repository builds against. Keep Roslyn references private implementation
dependencies and verify analyzer or generator tests before changing this compatibility baseline.

## Logging

Log significant lifecycle steps and caught exceptions exactly once:

- Log application startup (including version) and shutdown at `Information`. Log user-meaningful operations such as diagnostic runs, configuration loads, report exports and applied fixes at `Information`; keep routine per-element details at `Debug`.
- Log expected refusals and validation failures at `Warning`. Log data, configuration, file-system and unexpected Revit API failures at `Error` with the exception instance.
- Log an exception where the failure is first understood. Callers that only surface the message remain silent; do not log the same exception again at every layer.
- Keep documented benign fallbacks, `Try*` probes and cancellation used as control flow silent, with a code comment where the reason is not obvious.
- Use structured message templates with named properties; do not interpolate values into message templates.
