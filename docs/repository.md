# Repository guide

Paths in this document are relative to the repository root.
Read and follow the [development policy](policies/development.md) alongside this guide.

## About the project

Revit.Linter is an extension for Autodesk Revit that lets users keep projects and families clean.

## Solution and structure

- `Revit.Linter.slnx` — solution
- `src/` - projects
- `tests/` - tests
- `docs/` - contributor documentation and repository policies
- `installer/` -msi installer
- `updater/` - the standalone updater (`src/`, `tests/`) with its own
  `Directory.Build.props`, `Directory.Packages.props` and `Revit.Linter.Updater.slnx`; it is not part of the root solution
- `build/` - solution building, compilation, package
- `output/` - artifacts after building
- `benchmark/` - benchmarking
- `sandbox/` - a separate solution (`Revit.Linter.Sandbox.slnx`) for
  local experiments, not part of the main build/tests
- `wiki/` - user documentation published to GitHub Wiki; open this folder as an Obsidian vault (`Home.md` is the entry point)

`src/` contains dozens of small, single-responsibility-per-project
projects (diagnostics, presenters, state managers, etc.) — most are
 named `Revit.Linter.<Area>`. `Toolkit.Revit.Extensions` is Revit API
 extensions, separate from `Revit.Linter.*`.
`Revit.Linter.DocumentQueries` owns the document queries shared by diagnostics (all elements, elements of a
class, element types, element geometry) and the typed `DocumentQueryKey` under which their results are kept
in the transaction cache. Diagnostic modules, the diagnostic service and the dependency definers reach
`IRevitTransactionMemoryCache` only through its `IDocumentQueryService`.
`Revit.Linter.ReportMessaging` holds the shared report message
template parser (`Template` + `Args` to cached plain text and text parts with element links)
and the typed WPF message view used by the diagnostic and fix report
presenters. The project has no compile-time dependency on Revit API;
presenters adapt `ElementId` values through the generic link factory.
`Revit.Linter.Presentation` holds shared WPF composition infrastructure. Its
`ViewLocator` resolves an embedded view from DI by the corresponding view-model type;
use it only at module composition boundaries, not as a general service locator.
`DocumentationPage` in `Revit.Linter.Core` names a Wiki page by its English and Russian page names and builds its
address for the current UI culture. Diagnostic registrations carry the page of their module, ribbon buttons use
it for F1 help, and the report row opens it; a renamed Wiki page must be renamed in the code that refers to it.
`Revit.Linter.WelcomePresenter` holds the first-run welcome wizard: its steps, the per-user state
(`welcomeSettings.yml` in the settings value store) and the example configuration installer. The
installed examples are the files under `wiki/examples/configuration/` (English) and its `ru/`
subfolder, embedded as resources, so the Wiki and the product share one source. The host supplies the
ribbon tab name, the log folder and the pane and link actions through `IWelcomeHost`. Its headless rules
(example composition, installation without overwriting, first-run state) are covered by
`Revit.Linter.WelcomePresenter.Tests`; the window itself is not. Debug builds show the whole wizard on every
Revit start; release builds show only the steps the user has not seen.
`Revit.Linter.ElementAccentor` contains atomic, reversible element interaction and graphics
operations such as show, select, isolate, crop, per-element overrides, and temporary-filter
overrides. `Revit.Linter.ElementVisualization` composes those operations into diagnostic
visualization pipelines and restores their sessions in reverse order.
`Revit.Linter.ElementFixing` composes configuration-driven destructive fix steps into fixes
consumed by the diagnostic report presenter; the initial supported step deletes the target element.
The updater lives in `updater/src/` and its tests in `updater/tests/`. `updater/Directory.Packages.props`
replaces the root central package versions for everything under `updater/` (MSBuild uses the nearest file and it
does not import the root one), so updater dependencies can be versioned independently of the Revit add-in.
`updater/Directory.Build.props` likewise replaces the root one: the updater projects use `Microsoft.NET.Sdk` instead of
`VolocyNazad.Revit.Sdk`, target `net8.0-windows` explicitly and have plain `Debug`/`Release` configurations because
the updater runs outside Revit. Nothing in the root solution references the updater projects; the add-in only starts
`Revit.Linter.Updater.exe` by path, and the release build publishes the updater project directly.
`Revit.Linter.Updater.Core` contains Revit-independent release discovery, stable-version comparison,
check scheduling, per-user state persistence, and single-instance coordination.
`Revit.Linter.Updater` is the short-lived executable entry point; it must remain free of Revit API
dependencies so the installer and sign-in startup can run it outside Revit.
Its product icon is embedded in the executable and reused by the MSI registration shown in Windows
Installed Apps.

Manual update checks run without a console window and always attempt to show a Windows notification
with the result. The updater derives comparison data from the numeric assembly version while retaining
the full GitVersion semantic version in logs and assembly metadata.

> **Note:** Windows App SDK activation registration can fail in a self-contained unpackaged process
> when its runtime resource DLL is unavailable. Notification display remains supported in that case,
> but action buttons are omitted because their callbacks cannot be delivered safely.

Updater policy is read from `Software\Policies\Volocy\Revit.Linter\Updater` in `HKLM` and then `HKCU`.
Machine values take precedence over current-user policy, user JSON settings, and built-in defaults. The
registry adapter is read-only; neither the updater nor the per-user installer provisions policy values.

The release pipeline publishes the updater and Windows App SDK notification runtime for `win-x64` as a
self-contained unpackaged application before MSI creation. The publish directory is an explicit
build-module result; installer generation must consume that result instead of relying on
configuration-specific paths under `bin`.

The updater accepts a release installer only when its GitHub asset name and HTTPS URL exactly match the
stable release version. An explicit notification action downloads that asset through a uniquely named
partial file, verifies its declared size and GitHub-provided SHA-256 digest, and moves it to
`%LOCALAPPDATA%\Volocy\Revit.Linter\updater\downloads\` only after both checks pass. A mismatch deletes
the partial file. The updater opens Explorer with the verified MSI selected but does not start Windows
Installer; Authenticode publisher verification is required before automatic launch can be added.
The download transport disables automatic redirects and follows at most five validated HTTPS redirects
through GitHub-owned hosts. Release metadata requests pin the GitHub API version and reject bodies larger
than one megabyte. Connection timeouts are independent from the overall download-operation timeout.

Release packaging produces one per-user MSI containing every supported Revit build and the shared
updater. It installs under `%LOCALAPPDATA%\Programs\VolocyNazad\Revit.Linter\`, creates version-specific
manifests under `%APPDATA%\Autodesk\Revit\Addins\<year>\`, and registers the updater under the current
user's `Run` key. The MSI has one stable `UpgradeCode`; its product code changes deterministically with
the stable product version.
The feature-tree installer UI keeps the per-user installation directory fixed. Manifest assembly paths still
use the effective MSI `INSTALLDIR`, keeping silent and administrative invocations internally consistent.
The first unified installation also detects and removes older per-Revit-version products by their
legacy upgrade codes, preventing duplicate Programs and Features entries during migration.

The MSI exposes one required `Core` feature and one optional feature per supported Revit year. A
`ProductName` lookup under `HKLM\SOFTWARE\Autodesk\Revit\Autodesk Revit <year>\Components` selects
installed versions by default; the feature-tree UI lets the user override that selection. Manifest
synchronization follows the requested feature states
on install, repair and modify. Before any file change, setup stops the short-lived updater and refuses to
continue while a Revit process is running.

## Technology stack

- `VolocyNazad.Revit.Sdk` (a custom MSBuild SDK, source in a separate
  repository `toolkit.revit.sdk`) + `Revit_All_Main_Versions_API_x64`
- WPF, CommunityToolkit.Mvvm, MaterialDesignThemes, Microsoft.Xaml.Behaviors.Wpf
- `VolocyNazad.Revit.Async`, `VolocyNazad.Revit.Context`,
  `VolocyNazad.Revit.Events`, `VolocyNazad.Revit.TransactionMemoryCache`,
  `VolocyNazad.MVVM.DependencyInjection`, `VolocyNazad.AssemblyResolver` —
  packages from the `toolkit.revit.*` family of repositories
- `Nice3point.TUnit.Revit` — testing inside the Revit environment
- Microsoft.Extensions.* (DependencyInjection, Logging, Localization,
  Options, Hosting) and System.Text.Json
- English is the explicitly declared neutral resource language; Russian UI resources use
  parent-culture `ru` satellite assemblies. Global, view-model, and feature-owned user-facing
  strings are resolved by `LocalizationResourceReader` from the localization assembly excluded
  from ILRepack
- Microsoft.CodeAnalysis.CSharp (Roslyn — presumably for code analysis/parsing)
- YamlDotNet, StringToExpression, Humanizer.Core(.ru)
- Serilog + Serilog.Sinks.* (Console and Debug in development, File in all builds); add-in logs
  are stored under `%LOCALAPPDATA%\Volocy\Revit.Linter\logs\`, roll daily and at 20 MB,
  retain the newest 14 files, record startup and shutdown lifecycle events, capture fatal failures,
  and are flushed when the add-in host shuts down
- ILRepack (assembly merging during publishing)
- Tests: **xunit.v3** + xunit.runner.visualstudio + Microsoft.NET.Test.Sdk
- Localization tests validate resource key/placeholder parity, require every source `.resx` to be
  declared by the localization project, and load every declared resource from the excluded
  localization assembly plus its Russian satellite in each final ILRepack output before MSI creation
- `VolocyNazad.ResxAnalyzer` runs during regular builds of `Revit.Linter.Localization` and reports
  missing, extra or duplicate keys, mismatched composite-format placeholders, empty translations,
  missing culture files, and satellites without a neutral resource (`RESX001`-`RESX007`)
- Central package management via `Directory.Packages.props` (`installer/`, `build/` and `updater/` have their own);
  AutoConstructor, PolySharp, SonarAnalyzer.CSharp are wired in globally
  via `GlobalPackageReference` for all projects

## Documentation layout

- `AGENTS.md` links to the required repository guidance.
- `TODO.md` records agreed future work that is not yet implemented.
- `docs/policies/development.md` contains the development policy.
- `docs/repository.md` describes the project, repository structure, and technology stack.
- `docs/architecture.md` records stable module boundaries and design decisions.
- `docs/FEATURES.md` summarizes the current product scope and compatibility contract.
- `wiki/Home.md` is the entry point for user documentation. Wiki pages use Obsidian-compatible links and are published to GitHub Wiki by `.github/workflows/publish-wiki.yml`.
- `wiki/getting-started/` contains installation, quick-start, and configuration setup.
- `wiki/diagnostics/` contains diagnostic modules and their configuration.
- `wiki/reference/` contains diagnostic and formula reference material.
- `wiki/interface/` contains the UI, panes, and ribbon-button documentation.
- `wiki/examples/` contains configuration files users can copy and edit; the welcome wizard installs the same files. Keep the discipline `section` comment markers in pairs, and keep the English files and their `ru/` counterparts in step.
- `wiki/assets/` contains static images shared by Obsidian and GitHub Wiki.
- English pages live in the task-oriented folders directly under `wiki/`; Russian pages mirror that structure under `wiki/ru/`.
- `wiki/Home.md` is the language selector. Every localized page links to its counterpart and declares `lang: en` or `lang: ru` in YAML front matter.
- Page names must remain unique across both languages because GitHub Wiki addresses pages by name rather than by language folder.
- Wiki sources use Obsidian's `[[Target|Label]]` alias order. Inside Markdown tables, escape the alias separator as `\|`. The publication workflow runs `scripts/Prepare-Wiki.ps1` to reject malformed brackets, missing pages and incorrect explicit paths, then convert aliased links to GitHub Wiki's `[[Label|Target]]` order in a staging directory without changing the Obsidian sources.

Local Obsidian settings under `wiki/.obsidian/` are ignored. Keep shared content and navigation in Markdown so the same files work in Obsidian and GitHub Wiki.

The root solution exposes contributor documentation under `docs` and user documentation under `wiki`, preserving their subfolder structure. When adding documentation files, also add them as solution items; solution folders do not automatically include new files.

Production projects generate XML documentation; test projects are exempt from missing-comment diagnostics. Set `EnforcePublicApiDocumentation=true` to promote missing documentation for public APIs (`CS1591`) to an error during the baseline migration. The shared settings live in `Directory.Build.props` and `Directory.Build.targets`; `updater/Directory.Build.props` repeats the documentation settings for the updater build unit.

> **Note:** Document behavior that cannot be inferred from an API signature with a nearby XML `<remarks>` section. This includes fallback values, inferred defaults, state changes, execution-order guarantees, caching, required Revit context, transaction ownership, and swallowed or deliberately ignored failures.

## Localization validation

`Revit.Linter.Localization` is the assembly and build-time validation boundary for localized resources. It explicitly embeds the neutral and `ru` resources owned by presenter and feature projects, so the RESX analyzer is referenced only by this aggregating project. Adding it globally would analyze incomplete per-project subsets and could report missing culture files that are intentionally owned and embedded elsewhere.

The analyzer complements rather than replaces the localization tests. `RESX001`-`RESX007` provide immediate feedback during a normal build, while the tests additionally verify that every source resource is registered in the aggregation project and that the final ILRepack artifacts can load neutral and Russian resources.

> **Note:** The post-publish artifact test is run through its xUnit v3 executable with `dotnet run`. `dotnet test` through Microsoft.Testing.Platform currently reports zero discovered tests for this executable even though the in-process runner discovers and executes it.

> **Note:** `RESX008` is intentionally disabled. Existing resource keys use both `PascalCase` and `snake_case`; enable a naming convention only as a separately planned migration that updates existing keys and their consumers together.

The root `global.json` selects stable .NET SDK 10.0 (minimum `10.0.103`, `rollForward: latestFeature`). CI and publishing install the SDK from this file. Additional SDK installations may provide older test runtimes. See the [SDK selection policy](policies/development.md#net-sdk-selection).

## Solution items

The root solution exposes repository-level documents and configuration under `solutionItems`, GitHub files and maintenance scripts in matching subfolders, and documentation under `docs/`. The list is explicit, not a filesystem glob; keep links up to date when files change. See the [solution items policy](policies/development.md#solution-items).
## Repository validation

`scripts/Validate-Repository.ps1` enforces the required repository documents,
their navigation links, and complete, valid Solution Items. The
`.github/workflows/repository-policy.yml` workflow runs it for pushes and pull
requests. See the [development policy](policies/development.md#repository-validation).

## Formatting

The root `.editorconfig` defines the portable formatting baseline. Existing repositories may add stricter C# or analyzer-specific settings. See the [development policy](policies/development.md#formatting-baseline).

## Testing

Test projects that use RevitThreadExecutor and inherit from RevitApiTest use the .RevitTests suffix because they require a running Revit process.
`Nice3point.TUnit.Revit` supports `RevitAPI` but not `RevitAPIUI`; services should therefore keep meaningful document behavior separate from thin UI adapters so the former remains testable in `*.RevitTests`.

`tests/Revit.Linter.Testing` holds test doubles and helpers shared by several test projects (the in-memory transaction cache, the document query service built over it, repository-root lookup). It is a library, not a test project: it has no test framework reference and its name does not end in `.Tests` or `.RevitTests`, so CI does not try to run it. Put a helper there only when a second test project needs it; fixtures used by one project stay in that project, such as `DiagnosticTestBase` in `Revit.Linter.Diagnostic.RevitTests`.

Generated report formats are pinned with snapshot tests in `Revit.Linter.DiagnosticReportPresenter.Tests`. `Snapshot.Match` from `Revit.Linter.Testing` compares the output with `Snapshots/<Class>.<Test>.verified.<ext>`; on a difference it writes a `*.received.*` file next to it (ignored by Git) and fails. To accept an intended format change, review the received file and rename it over the verified one in the same commit. Snapshot inputs use the invariant culture so the text is identical on .NET Framework and .NET.

## Continuous integration

The CI workflow builds Release_2021.1.9, Release_2023.0.0, and Release_2025.0.0 on push and pull requests. It runs projects ending in .Tests under `tests/` as headless tests. A separate job builds `updater/Revit.Linter.Updater.slnx` in `Release` and runs the headless tests under `updater/tests/`. Projects ending in .RevitTests are compiled with the solution but require a local Revit process to run.

## Versioning and release tags

Revit Linter uses GitVersion in Continuous Delivery mode with patch increments. GitHub releases are created manually from a commit carrying exactly one stable `vMAJOR.MINOR.PATCH` tag; the build uses the corresponding version without the `v` prefix for binaries and MSI installers. This repository publishes a GitHub release with installers rather than a NuGet package. The tagged commit must already contain the release section in `CHANGELOG.md` with an empty `Unreleased` section above it; the publish-release workflow stops otherwise, and the release pipeline publishes that section as the GitHub release description. See the [release changelog policy](policies/development.md#release-changelog).
