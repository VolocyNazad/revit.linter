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
`Revit.Linter.ReportMessaging` holds the shared report message
template parser (`Template` + `Args` to cached plain text and text parts with element links)
and the typed WPF message view used by the diagnostic and fix report
presenters. The project has no compile-time dependency on Revit API;
presenters adapt `ElementId` values through the generic link factory.
`Revit.Linter.Presentation` holds shared WPF composition infrastructure. Its
`ViewLocator` resolves an embedded view from DI by the corresponding view-model type;
use it only at module composition boundaries, not as a general service locator.

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
- Microsoft.CodeAnalysis.CSharp (Roslyn — presumably for code analysis/parsing)
- YamlDotNet, StringToExpression, Humanizer.Core(.ru)
- Serilog + Serilog.Sinks.* (Console, Debug, File)
- ILRepack (assembly merging during publishing)
- Tests: **xunit.v3** + xunit.runner.visualstudio + Microsoft.NET.Test.Sdk
- Central package management via `Directory.Packages.props`;
  AutoConstructor, PolySharp, SonarAnalyzer.CSharp are wired in globally
  via `GlobalPackageReference` for all projects

## Documentation layout

- `AGENTS.md` links to the required repository guidance.
- `docs/policies/development.md` contains the development policy.
- `docs/repository.md` describes the project, repository structure, and technology stack.
- `wiki/Home.md` is the entry point for user documentation. Wiki pages use Obsidian-compatible links and are published to GitHub Wiki by `.github/workflows/publish-wiki.yml`.
- `wiki/getting-started/` contains installation, quick-start, and configuration setup.
- `wiki/diagnostics/` contains diagnostic modules and their configuration.
- `wiki/reference/` contains diagnostic and formula reference material.
- `wiki/interface/` contains the UI, panes, and ribbon-button documentation.
- `wiki/examples/` contains configuration files users can copy and edit.
- `wiki/assets/` contains static images shared by Obsidian and GitHub Wiki.
- English pages live in the task-oriented folders directly under `wiki/`; Russian pages mirror that structure under `wiki/ru/`.
- `wiki/Home.md` is the language selector. Every localized page links to its counterpart and declares `lang: en` or `lang: ru` in YAML front matter.
- Page names must remain unique across both languages because GitHub Wiki addresses pages by name rather than by language folder.

Local Obsidian settings under `wiki/.obsidian/` are ignored. Keep shared content and navigation in Markdown so the same files work in Obsidian and GitHub Wiki.

The root solution exposes contributor documentation under `docs` and user documentation under `wiki`, preserving their subfolder structure. When adding documentation files, also add them as solution items; solution folders do not automatically include new files.

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

## Continuous integration

The CI workflow builds Release_2021.1.9, Release_2023.0.0, and Release_2025.0.0 on push and pull requests. It runs projects ending in .Tests as headless tests. Projects ending in .RevitTests are compiled with the solution but require a local Revit process to run.

## Versioning and release tags

Revit Linter uses GitVersion in Continuous Delivery mode with patch increments. GitHub releases are created manually from a commit carrying exactly one stable `vMAJOR.MINOR.PATCH` tag; the build uses the corresponding version without the `v` prefix for binaries and MSI installers. This repository publishes a GitHub release with installers rather than a NuGet package.
