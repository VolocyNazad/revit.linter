# Repository guide

Paths in this document are relative to the repository root.
Read when: the solution layout, the technology stack, a build unit, a module or the release mechanics change.
For the area you touch, open its section or its [module inventory](repository/modules.md) entry; follow the [development policy](policies/development.md) alongside this guide.

## About the project

Revit.Linter is an extension for Autodesk Revit that lets users keep projects and families clean.

## Solution and structure

- `Revit.Linter.slnx` — solution
- `src/` - projects
- `tests/` - tests
- `docs/` - contributor documentation and repository policies
- `docs/testing/` - manual verification checklists for workflows that require a live Revit UI
- `installer/` - MSI installer
- `updater/` - the standalone updater (`src/`, `tests/`) with its own
  `Directory.Build.props`, `Directory.Packages.props` and `Revit.Linter.Updater.slnx`; it is not part of the root solution
- `build/` - solution building, compilation, package
- `scripts/` - repository validation, release verification and CI helper scripts
- `output/` - artifacts after building
- `benchmark/` - benchmarking
- `sandbox/` - a separate solution (`Revit.Linter.Sandbox.slnx`) for
  local experiments, not part of the main build/tests
- `wiki/` - user documentation published to GitHub Wiki; open this folder as an Obsidian vault (`Home.md` is the entry point)

A partial type split into companion files keeps the `Type.Suffix.cs` file name and is nested under
`Type.cs` in **Solution Explorer** automatically. Localization resources follow the same rule:
`Type.resx` nests under `Type.cs`, and the satellite `Type.ru.resx` nests under `Type.resx`.
The rules live once in the solution-root `.filenesting.json`, so a new companion or language file
needs no project-file change:

```json
{
  "dependentFileProviders": {
    "add": {
      "pathSegment": { "add": { ".*": [ ".cs", ".resx" ] } },
      "extensionToExtension": { "add": { ".resx": [ ".cs" ] } }
    }
  }
}
```

Project files do not list `.resx` files at all. The root `Directory.Build.targets` removes them from
`EmbeddedResource` and adds them as `None` for every project except `Revit.Linter.Localization`, the
only project that embeds them (with a `LogicalName`). A project that generates typed localized
properties only sets `<UsesLocalizationGenerator>true</UsesLocalizationGenerator>`; the same targets
file passes its base `*.resx` files to the generator as `AdditionalFiles`, excluding the `*.ru.resx`
satellites. Adding a new resource file therefore needs no project-file or nesting change.

File nesting is a Visual Studio display setting, not a build rule: enable **File Nesting** (the
**Default** preset is enough) in **Solution Explorer**. Use `<DependentUpon>` only when a file's parent
cannot be inferred, for example a generated code-behind file. Split a type into companion files only
when the parts are large enough to read separately; otherwise keep the members in the file named after
the type.

## Module inventory

The projects, their responsibilities and the updater and installer mechanics are in the
[module inventory](repository/modules.md); the stable boundaries and the runtime rules are in
[architecture decisions](architecture.md).

## Technology stack

- `VolocyNazad.Revit.Sdk` (a custom MSBuild SDK, source in a separate
  repository `toolkit.revit.sdk`) + `Nice3point.Revit.Api.RevitAPI` /
  `Nice3point.Revit.Api.RevitAPIUI` (floating `$(MajorRevitVersion).*`)
- WPF with the add-in's own control styles, CommunityToolkit.Mvvm, Microsoft.Xaml.Behaviors.Wpf
- `VolocyNazad.Revit.Async`, `VolocyNazad.Revit.Context`,
  `VolocyNazad.Revit.Events`, `VolocyNazad.Revit.TransactionMemoryCache`,
  `VolocyNazad.Revit.Sugar`, `VolocyNazad.MVVM.DependencyInjection`,
  `VolocyNazad.AssemblyResolver` — packages from the `toolkit.revit.*` family of repositories
- `VolocyNazad.ValueStore` — the per-user settings value store
- `Nice3point.TUnit.Revit` — testing inside the Revit environment
- Microsoft.Extensions.* (DependencyInjection, Logging, Localization, Options, Hosting) and System.Text.Json
- English is the declared neutral resource language; Russian UI resources use parent-culture `ru`
  satellite assemblies. Global, view-model and feature-owned user-facing strings are resolved by
  `LocalizationResourceReader` from the localization assembly excluded from ILRepack
- Microsoft.CodeAnalysis.CSharp (Roslyn) — the analyzer API of the in-repo source generator
  `Revit.Linter.Localization.Generator`, which generates the typed localized properties of view models
- YamlDotNet, StringToExpression, Humanizer.Core(.ru)
- Serilog + Serilog.Sinks.* (Console and Debug in development, File in all builds); logs are stored
  under `%LOCALAPPDATA%\Volocy\Revit.Linter\logs\`, roll daily and at 20 MB, retain the newest 14
  files, record startup and shutdown lifecycle events, capture fatal failures, and are flushed when
  the add-in host shuts down
- ILRepack (assembly merging during publishing)
- Tests: **xunit.v3** through Microsoft.Testing.Platform (see the [testing policy](policies/testing.md))
- Localization tests validate resource key/placeholder parity, require every source `.resx` to be
  declared by the localization project, and load every declared resource from the excluded
  localization assembly plus its Russian satellite in each final ILRepack output before MSI creation
  (rules in the [localization policy](policies/localization.md))
- `VolocyNazad.ResxAnalyzer` runs during regular builds of `Revit.Linter.Localization` and reports
  missing, extra or duplicate keys, mismatched composite-format placeholders, empty translations,
  missing culture files, and satellites without a neutral resource (`RESX001`-`RESX007`) — see the
  [localization policy](policies/localization.md)
- Central package management via `Directory.Packages.props` (`installer/`, `build/`, `benchmark/`,
  `sandbox/` and `updater/` have their own); AutoConstructor, PolySharp and SonarAnalyzer.CSharp are
  wired in globally via `GlobalPackageReference`. Package version rules: [build and toolchain
  policy](policies/build.md).
- The SDK defines the `BEFORE<year>` and `AFTER<year>` conditional-compilation constants; their
  inclusive semantics and the boundary rule are in the [build and toolchain policy](policies/build.md).

## Documentation layout

- `AGENTS.md` links to the required repository guidance.
- `TODO.md` records agreed future work that is not yet implemented.
- `GLOSSARY.md` records the product vocabulary shared by the code, the documentation and the tests.
- `docs/policies/` contains the repository policies — the rules a change must follow. The
  [policy index](policies/README.md) says what each policy covers.
- `docs/diagnostics/adding.md` describes the end-to-end workflow for adding a built-in diagnostic.
- `docs/repository.md` describes the project, repository structure and technology stack;
  the [module inventory](repository/modules.md) lists the projects and their responsibilities.
- The [changelog archive](changelog/archive.md) holds releases older than the current major version;
  `CHANGELOG.md` keeps `[Unreleased]` and the current major release line.
- `docs/architecture.md` records stable module boundaries and design decisions.
- `docs/FEATURES.md` summarizes the current product scope and compatibility contract.
- `docs/migrations/README.md` defines the migration registry: lifecycle, version window, index and entry template.
  Every user-visible breaking migration gets one numbered entry, for example
  `docs/migrations/0001-single-configuration-file.md` for the split-to-single configuration migration (`MIG-0001`).
  An entry records the versions on both sides, why the change is breaking, the user action, the data-safety
  guarantees and the cleanup checklist with the release that removes the transition.
- `wiki/Home.md` is the entry point for user documentation. Wiki pages use Obsidian-compatible links and are published to GitHub Wiki by `.github/workflows/publish-wiki.yml`.
- `wiki/getting-started/` contains installation, quick-start, and configuration setup.
- `wiki/diagnostics/` contains diagnostic modules and their configuration.
- `wiki/reference/` contains diagnostic and formula reference material.
- `wiki/interface/` contains the UI, panes, and ribbon-button documentation.
- `wiki/examples/` contains configuration files users can copy and edit; the welcome wizard installs the same single `diagnostics.config.yaml`. Keep the discipline `section` comment markers in pairs, and keep the English file and its `ru/` counterpart in step.
- `wiki/assets/` contains static images shared by Obsidian and GitHub Wiki.
- English pages live in the task-oriented folders directly under `wiki/`; Russian pages mirror that structure under `wiki/ru/`.
- Wiki page rules — unique page names, language front matter, link syntax and the publishing conversion — are
  in the [wiki policy](policies/wiki.md).

Local Obsidian settings under `wiki/.obsidian/` are ignored; the contributor vault under
`docs/.obsidian/` is committed. Keep shared content and navigation in Markdown.

The root solution exposes contributor documentation under `docs` and user documentation under `wiki`,
preserving their subfolder structure. When adding documentation files, also add them as solution
items, as the [repository maintenance policy](policies/repository-maintenance.md) requires.

XML documentation rules — what a public API must document and how the strict build switch works — are
in the [documentation policy](policies/documentation.md).

## Localization validation

`Revit.Linter.Localization` is the assembly and build-time validation boundary for localized
resources: it explicitly embeds the neutral and `ru` resources owned by presenter and feature
projects, so the RESX analyzer is referenced only by this aggregating project. The assembly layout,
resource file conventions and validation rules are in the [localization policy](policies/localization.md).

> **Note:** `RESX008` is intentionally disabled. Existing resource keys use both `PascalCase` and
> `snake_case`; enable a naming convention only as a separately planned migration that updates existing keys and their consumers together.

The root `global.json` selects stable .NET SDK 10.0 (minimum `10.0.103`, `rollForward: latestFeature`).
CI and publishing install the SDK from this file. The SDK selection rules are in the [build and
toolchain policy](policies/build.md).

## Solution items

The root solution exposes repository-level documents and configuration under `solutionItems`, GitHub
files and maintenance scripts in matching subfolders, and documentation under `docs/`. The list is
explicit, not a filesystem glob; keep links up to date when files change. The
[repository maintenance policy](policies/repository-maintenance.md) defines the contract.

## Repository validation

`scripts/Validate-Repository.ps1` enforces the required repository documents, their navigation links,
and complete, valid Solution Items. The `.github/workflows/repository-policy.yml` workflow runs it for
pushes and pull requests. See the
[repository maintenance policy](policies/repository-maintenance.md#repository-validation).

## Formatting

The root `.editorconfig` defines the portable formatting baseline. Repository-specific sections may
add stricter C# or analyzer-specific settings. See the
[repository maintenance policy](policies/repository-maintenance.md#formatting-baseline).

## Testing

`tests/Revit.Linter.Testing` holds test doubles and helpers shared by several test projects (the
in-memory transaction cache, the document query service built over it, repository-root lookup). It is
a library, not a test project: it has no test framework reference and its name does not end in
`.Tests` or `.RevitTests`, so CI does not try to run it. Put a helper there only when a second test
project needs it; fixtures used by one project stay in that project, such as `DiagnosticTestBase` in
`Revit.Linter.Diagnostic.RevitTests`.

Generated report formats are pinned with snapshot tests in
`Revit.Linter.DiagnosticReportExporting.Tests`. `Snapshot.Match` from `Revit.Linter.Testing` compares
the output with `Snapshots/<Class>.<Test>.verified.<ext>`; on a difference it writes a
`*.received.*` file next to it (ignored by Git) and fails. To accept an intended format change, review
the received file and rename it over the verified one in the same commit. Snapshot inputs use the
invariant culture.

`Revit.Linter.Architecture.Tests` guards the documented coupling between UI presenter projects: it
fails when a presenter project references another UI module or uses that module's view model outside
the composition points recorded in the [architecture decisions](architecture.md).

What must be tested, on which Revit versions and how the suites are run is defined by the
[testing policy](policies/testing.md).

## Continuous integration

The CI workflow resolves its build matrix from the `Release_*` configurations of the root
`Directory.Build.props` through `scripts/Get-RevitConfigurations.ps1` and builds every configuration
on push to `main` and `dev` and on pull requests, with `-p:TreatWarningsAsErrors=true` on
`windows-2025`. It runs projects ending in .Tests under `tests/` through
`scripts/Invoke-HeadlessTests.ps1`, which writes a TRX report per project and fails when a run
discovers no tests.

A separate job builds `build/Revit.Linter.Build.slnx` and the installer project; another builds
`updater/Revit.Linter.Updater.slnx` in `Release` and runs the headless tests under `updater/tests/`.
The `CI gate` job aggregates those jobs and is the required check; a documentation-only pull request,
whose changed files are all under `docs/` or `wiki/` or are Markdown, skips the Windows jobs and the
gate stays green. The workflow does not publish the MSI: the release publish creates it. Projects
ending in .RevitTests are compiled with the solution but require a local Revit process to run.

The `repository-policy` workflow runs `./scripts/Validate-Repository.ps1`, which also enforces the
action pins and the Revit version-matrix consistency, and `./scripts/Verify-Release.ps1`, which checks
the `CHANGELOG.md` structure on every push and pull request. Both workflows cancel a superseded run
for pull requests, set job timeouts and cache the NuGet packages keyed by the central package files;
every action reference in `.github/workflows/` is pinned to a full release tag such as
`actions/checkout@v7.0.1`.

Security automation lives in `.github/workflows/codeql.yml` (CodeQL for C# in build-mode none on push
to `main` and `dev`, and weekly), `.github/workflows/dependency-review.yml` (dependency review on every
pull request) and `.github/dependabot.yml` (weekly grouped updates for GitHub Actions and NuGet). Those
workflows report findings; they are not required checks.

What CI must prove, how the Revit version matrix is covered and how a change is verified before a
release tag is defined by the [testing policy](policies/testing.md).

## Versioning and release tags

Versioning follows GitVersion in Continuous Delivery mode with patch increments: a release carries
exactly one stable `vMAJOR.MINOR.PATCH` tag, binaries and the MSI use the version without the `v`, and
the repository publishes an installer release rather than a NuGet package. The release channels, tag
immutability and metadata rules are in the [release policy](policies/releases.md); the release section
of `CHANGELOG.md` and its composition are in the [changelog policy](policies/changelog.md).
