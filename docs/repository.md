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
- `docs/testing/` - manual verification checklists for workflows that require a live Revit UI
- `installer/` -msi installer
- `updater/` - the standalone updater (`src/`, `tests/`) with its own
  `Directory.Build.props`, `Directory.Packages.props` and `Revit.Linter.Updater.slnx`; it is not part of the root solution
- `build/` - solution building, compilation, package
- `output/` - artifacts after building
- `benchmark/` - benchmarking
- `sandbox/` - a separate solution (`Revit.Linter.Sandbox.slnx`) for
  local experiments, not part of the main build/tests
- `wiki/` - user documentation published to GitHub Wiki; open this folder as an Obsidian vault (`Home.md` is the entry point)

A partial type that is split into companion files keeps the `Type.Suffix.cs` file name and is nested
under `Type.cs` in **Solution Explorer** automatically. Localization resources follow the same rule:
`Type.resx` is nested under `Type.cs`, and the satellite `Type.ru.resx` is nested under `Type.resx`.
The rules live once in the solution-root `.filenesting.json`, so a new companion file or a new
language file needs no project-file change:

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
`EmbeddedResource` and adds them as `None` for every project except `Revit.Linter.Localization`, which
is the only project that embeds them (with a `LogicalName`). A project that generates typed localized
properties only sets `<UsesLocalizationGenerator>true</UsesLocalizationGenerator>`; the same targets
file passes its base `*.resx` files to the generator as `AdditionalFiles`, excluding the `*.ru.resx`
satellites. Adding a new resource file therefore needs neither a project-file nor a nesting change.

File nesting is a Visual Studio display setting, not a build rule: enable **File Nesting** (the
**Default** preset is enough) in **Solution Explorer** and the solution-level file applies to the whole
team. Use `<DependentUpon>` in a project only when a file must belong to a parent that the solution
rules cannot infer, for example a generated code-behind file.

Split a type into companion files only when the parts are large enough to read separately; otherwise
keep the members in the file named after the type.

`src/` contains dozens of small, single-responsibility-per-project
projects (diagnostics, presenters, state managers, etc.) — most are
 named `Revit.Linter.<Area>`. `VolocyNazad.Revit.Sugar` is Revit API
 extensions, separate from `Revit.Linter.*`.
`Revit.Linter.Core.Abstractions` owns the Revit-independent diagnostic and configuration
contracts: rule models, rule types, severity, parameters and fix/visualization pipeline
definitions. It targets `netstandard2.0` and is referenced by the add-in build, so it must not reference the Revit API, WPF or the Revit SDK.
`Revit.Linter.DiagnosticConfiguration` owns the configuration file envelope, YAML reading,
type-specific rule validation and the comment-preserving text editor used to write configuration
changes. It is Revit-independent for the same reason; `Revit.Linter.ConfigurationPath` keeps the
Revit-version-aware paths behind `IConfigurationPaths`, file discovery and change watching, and delegates
the format work to it. Before any configuration is read, its startup migration service moves the legacy
split files of every Revit version folder into a `legacy` subfolder; the files are never deleted, and the
host shows one warning per product version that opens the migration guide. There is no persistent banner
in the Diagnostics pane and no notice per Revit version.
`Revit.Linter.DocumentQueries` owns the document queries shared by diagnostics (all elements, elements of a
class, element types, element geometry) and the typed `DocumentQueryKey` under which their results are kept
in the transaction cache. Diagnostic modules, the diagnostic service and the dependency definers reach
`IRevitTransactionMemoryCache` only through its `IDocumentQueryService`.
`Revit.Linter.ReportMessaging` holds the shared report message
template parser (`Template` + `Args` to cached plain text and text parts with element links)
and the typed WPF message view used by the diagnostic and fix report
presenters. The project has no compile-time dependency on Revit API;
presenters adapt `ElementId` values through the generic link factory.
`Revit.Linter.Languages` owns the formula grammar and, in `Factories`, the factories that compile configuration
formulas into document predicates, element functions and native element filters. The diagnostic modules share
them through `AddFormulaFactories` and compile the formulas of every configured rule while they register it:
a rule whose formula is rejected is skipped and reported through the configuration error source. The runtime
factories keep their fallback for any remaining compile failure, which is logged and reported through
`IFormulaCompilationNotifier`.
`Revit.Linter.Presentation` holds shared WPF composition infrastructure. Its
`ViewLocator` resolves an embedded view from DI by the corresponding view-model type;
use it only at module composition boundaries, not as a general service locator.
The project also holds what the presenter modules would otherwise each repeat: the view-model bases in
`ViewModels` (`InitializableObservableObject` with the initialize and deinitialize commands, and
`RevitInteractionViewModel`, which observes the host-published `IRevitEventSource` events and reports them as
`RevitEventType`), `ReportRowViewModel` for the row state shared by the diagnostic and fix reports,
`FilteredRowCollection` for the collection-view, sort and filter plumbing shared by the list panes, and
`BindingProxy` for bindings outside the visual tree. Its row type works with Revit element identifiers, so it
references `Revit.Linter.Core` and the Revit API, and it references `Revit.Linter.ReportMessaging` for the shared
report message parser.
`Revit.Linter.Core/Abstractions` owns the cross-project interaction contracts that the add-in host implements for
the modules: `IRevitIdlingScheduler` and `IRevitEventSource` schedule work and publish Revit events,
`IClipboardService`, `IShellLauncher` and `IDocumentationLauncher` reach the Windows desktop, and
`IUserInterfaceActivityStream` publishes completed UI workflow facts. Implementations live in
`src/Revit.Linter/Infrastructure/Services` and are registered by the host; modules depend only on the contracts.
`Revit.Linter.DiagnosticReportPresenter.Interactions` is the interaction project of the diagnostic report
presenter: it holds the contracts other modules and the host use for that presenter
(`IDiagnosticReportPresenter`, `IVisualizationViewActivator`), so no consumer has to reference the presenter
project for them. A presenter that needs such an outward contract gets a matching `<Module>.Interactions` project
instead of exposing it from its implementation assembly.
`Revit.Linter.OpenedDocuments` owns the shared opened-document selector of the report and fix panes. It exposes
the selector through `IOpenedDocuments` and the stable `OpenedDocument` item model, so those panes bind to the
contract and never reference the selector view model; the view model keeps the Revit-aware refresh of the open
document list.
Each presenter project keeps the design-time data of its views next to them in `DesignData/`
(`<ViewName>DesignData` classes wired through `d:DataContext` with `IsDesignTimeCreatable`); they exist for the XAML designer only,
mirror the binding paths of their view and read the same localized resources, so the preview never
shows empty fields. `ViewLocatorExtension` resolves the design-time view and its data for a composed
sub-view when the designer has no service provider, and views, windows and dialogs whose runtime
constructor needs services keep a public parameterless design-time constructor.
`Revit.Linter.ThemeManaging` owns the look shared by all views and depends on no control library.
`SharedThemeResources` is the single resource dictionary with the palette and the style and template of every
control the add-in uses, drawn flat and dense like the Revit palettes; each root view merges it instead of
declaring colors or restyling controls itself. Views refer only to its `Linter*` keys and take the font and the
colors from the `LinterViewRoot` style set on the root element. Colors live in `LightPalette` and `DarkPalette`
next to it: both declare the same `Linter*Brush` keys, with values taken from the Revit interface, and styles
reference them only through `DynamicResource`; `IThemeService` calls `SharedThemeResources.ApplyTheme` for
registered element trees when the Revit theme changes. `Controls` holds what the styles need beyond WPF:
`PathIcon` with its `IconKind` outlines (from Material Design Icons, Apache License 2.0) and the `Hint.Text`
attached property for the text shown in an empty text box. The dictionaries are XAML-compiled classes that
views reference by type rather than by a pack URI, which survives ILRepack.
`DocumentationPage` in `Revit.Linter.Core` names a Wiki page by its English and Russian page names and builds its
address for the current UI culture. Diagnostic registrations carry the page of their module, ribbon buttons use
it for F1 help, and the report and diagnostics rows open it; a renamed Wiki page must be renamed in the code that refers to it.
`HelpToolTip` in `Revit.Linter.Behaviors` gives a pane control a ribbon-like tooltip through attached properties
(title, description, note). Its inherited `HelpCommand`, set once on the root of a view, is executed when F1 is
pressed while a tooltip is open; the key is caught by a thread keyboard hook that exists only while the tooltip is
shown, because a dockable pane does not have the keyboard focus on hover. View models open pages through
`IDocumentationLauncher`, implemented by the add-in host.
`Revit.Linter.WelcomePresenter` owns the first-run welcome wizard, its example configuration installation and the
tutorial sample handling. `Revit.Linter.PracticalTourPresenter` owns the optional practical tour, its managed
`TOUR001` configuration and its dockable pane; its contracts live in
`Revit.Linter.PracticalTourPresenter.Interactions`, so the wizard and the host depend on `IPracticalTour` without
referencing the presenter implementation. Both presenters share the per-user onboarding state
(`welcomeSettings.yml` in the settings value store) owned by `Revit.Linter.OnboardingState`; it keeps the completed
wizard version, the example offers per Revit version and the resumable practical-tour progress per Revit version. The
installed example is the `diagnostics.config.yaml` file under `wiki/examples/configuration/` (English) and its
`ru/` subfolder, embedded as resources, so the Wiki and the product share one source. The host supplies the
ribbon tab name, pane and link actions through `IWelcomeHost`, and the tour's pane, notification and
diagnostics-filter actions through `IPracticalTourHost`. The wizard's headless rules
(example composition, installation into `examples\` with a different existing file backed up to a `.bak-<timestamp>`
copy, first-run state) are covered by
`Revit.Linter.WelcomePresenter.Tests`, and the tour's state machine and managed configuration by
`Revit.Linter.PracticalTourPresenter.Tests`; the WPF views themselves are not. The wizard content version
is raised so an upgraded user is offered the examples step again once per Revit version. Debug builds show
the whole wizard on every Revit start; release builds show only the steps the user has not seen.
The optional practical tour observes neutral completed UI facts published through `IUserInterfaceActivityStream`;
diagnostic presenters do not reference welcome or practical-tour presenter types. Its WPF emphasis is owned by a disposable
highlight session, so closing or restarting the tour restores every affected control. A non-blocking spotlight dims
the surrounding pane, outlines the active target with a soft pulsing halo and labels it as the next step; both
pulse while Windows client-area animations are enabled. A target that is a button
(not a check box, a radio button or another control) additionally glows through its own element effect and
takes an accent frame on the button itself, while rows, text and other surfaces take no element effect,
so their inner text never casts accent shadows. The host also brings the required pane to the foreground.
The tour is hosted in a right-side Revit dockable pane; its registration and visibility remain host actions
behind `IPracticalTourHost`.
Starting the tour refreshes the managed `tour/practical-tour.config.yaml` file in the current format, and a
stale old-format copy is refreshed automatically at startup without a user action or an error dialog, where
`TOUR001` declares `isActive: false`, so the user enables the check as the guided step asks. Completing the tour or
opting out removes the managed file, so no training configuration is left behind; starting the tour
again reinstalls it. Its `TOUR001` diagnostic owns
the visualizations and fix menu required by the walkthrough, carries the reserved `Tour` tag, and stays pinned
at the top of the diagnostics list; typed UI activities carry diagnostic codes so
unrelated findings cannot advance the tour.
`Revit.Linter.ElementAccentor` contains atomic, reversible element interaction and graphics
operations such as show, select, isolate, crop, per-element overrides, and temporary-filter
overrides. `Revit.Linter.ElementVisualization` composes those operations into diagnostic
visualization pipelines and restores their sessions in reverse order.
The diagnostic report presenter activates a persistent `Revit Linter — Visualization` isometric view before
applying any pipeline. The host owns creation and activation behind `IVisualizationViewActivator`, keeping
`RevitAPIUI` calls out of the presenter while allowing crop and combined visualizations to use a predictable 3D view.
`Revit.Linter.ElementFixing` composes configuration-driven destructive fix steps into fixes
consumed by the diagnostic report presenter; the initial supported step deletes the target element.
`Revit.Linter.ElementIgnoring` owns the ignore list. `IIgnoreElementDetector` answers whether a
finding is ignored and `IIgnoreElementProvider` writes ignores, while `IIgnoreListManager` lists,
removes and clears the entries; storage details never leave the module. The entries live inside the
document as one Extensible Storage entity on a single `DataStorage`, with one entry per element and
diagnostic code or per document and code, so any element can be ignored without adding parameters to
it. `Revit.Linter.IgnoreListPresenter` shows the ignore list of the active document in its own
dockable pane, where entries can be selected or removed, and reaches the list only through
`IIgnoreListManager`.
`Revit.Linter.DiagnosticReportExporting` owns the diagnostic report export: the shared export
document contract, the CSV/JSON/YAML/HTML exporters behind `IDiagnosticReportExporter`, and the
`IDiagnosticReportExportService` orchestration (suggested file name, export document, exporter
choice). The presenter supplies plain finding data with cultures and labels and keeps the
`DiagnosticReportExportedActivity` publishing; the modal save-file dialog stays behind
`ISaveFileDialogService`, keeping `Microsoft.Win32` calls out of the presenter. The project has
no Revit API dependency, so its snapshot-covered formats stay headless-testable.
`tests/Revit.Linter.CollisionDiagnostics.RevitTests` owns the collision fixture builder: it creates a host
document with two shared-coordinates links whose walls, floors and system runs overlap the host content,
covering volumetric hits as well as touch-only and isolated no-collision controls, so collision rules with
link checks enabled can be verified. The builder lives in the test project instead of a product module, so the
add-in ships no test code and the diagnostics tab has no test-tooling panel.
The updater lives in `updater/src/` and its tests in `updater/tests/`. `updater/Directory.Packages.props`
replaces the root central package versions for everything under `updater/` (MSBuild uses the nearest file and it
does not import the root one), so updater dependencies can be versioned independently of the Revit add-in.
`updater/Directory.Build.props` likewise replaces the root one: the updater projects use `Microsoft.NET.Sdk` instead of
`VolocyNazad.Revit.Sdk` and target `net8.0-windows` explicitly, except the notification executable,
which adds the Windows 10 version to that target framework; all of them have plain `Debug`/`Release`
configurations because the updater runs outside Revit. Nothing in the root solution references the updater projects; the add-in only starts
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

Release packaging produces one per-user MSI containing every supported Revit build and the shared updater. It installs under
`%LOCALAPPDATA%\Programs\VolocyNazad\Revit.Linter\`, creates version-specific manifests under
`%APPDATA%\Autodesk\Revit\Addins\<year>\`, and registers the updater under the current user's `Run`
key. The MSI has one stable `UpgradeCode`; its product
code changes deterministically with the stable product version.
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

Product-wide identity (vendor, product and repository names, GitHub destinations, registry policy
location, updater synchronization name) is owned by `ProductIdentity` in `Revit.Linter.Core`
(`src/Revit.Linter.Core/Abstractions/ProductIdentity.cs`). The updater and the installer compile the
same file as a source link, so they stay independent build units without a project reference to the
Revit API. Path composition stays with the owning module: `IConfigurationPaths` for user
configurations (`Documents\Revit Linter\<year>\`, `diagnostics.config.yaml`, `examples\`,
`debug\`, `tour\`, `legacy\`, and the `VersionsDirectory` that the startup migration enumerates), `UpdaterPaths` in `Revit.Linter.Updater.Core` for `%LOCALAPPDATA%\Volocy\Revit.Linter\updater\`
(`logs/`, `state.json`, `downloads/`), `InstallerProduct` for MSI identity and manifest paths, and the
composition root for add-in logs (`...\\logs\\`) and the ValueStore settings directory
(`%LOCALAPPDATA%\Volocy\Revit.Linter\settings\`). Renaming the vendor, product or repository requires editing
`ProductIdentity` only; the `build/` asset name `RevitLinter-<version>.msi` mirrors
`ProductIdentity.GetMsiAssetName`.

## Technology stack

- `VolocyNazad.Revit.Sdk` (a custom MSBuild SDK, source in a separate
  repository `toolkit.revit.sdk`) + `Nice3point.Revit.Api.RevitAPI` /
  `Nice3point.Revit.Api.RevitAPIUI` (floating `$(MajorRevitVersion).*`)
- WPF with the add-in's own control styles, CommunityToolkit.Mvvm, Microsoft.Xaml.Behaviors.Wpf
- `VolocyNazad.Revit.Async`, `VolocyNazad.Revit.Context`,
  `VolocyNazad.Revit.Events`, `VolocyNazad.Revit.TransactionMemoryCache`,
  `VolocyNazad.Revit.Sugar`, `VolocyNazad.MVVM.DependencyInjection`,
  `VolocyNazad.AssemblyResolver` —
  packages from the `toolkit.revit.*` family of repositories
- `VolocyNazad.ValueStore` — the per-user settings value store
- `Nice3point.TUnit.Revit` — testing inside the Revit environment
- Microsoft.Extensions.* (DependencyInjection, Logging, Localization,
  Options, Hosting) and System.Text.Json
- English is the explicitly declared neutral resource language; Russian UI resources use
  parent-culture `ru` satellite assemblies. Global, view-model, and feature-owned user-facing
  strings are resolved by `LocalizationResourceReader` from the localization assembly excluded
  from ILRepack
- Microsoft.CodeAnalysis.CSharp (Roslyn) — the analyzer API of the in-repo source generator
  `Revit.Linter.Localization.Generator`, which generates the typed localized properties of view models
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
- Central package management via `Directory.Packages.props` (`installer/`, `build/`, `benchmark/`, `sandbox/` and `updater/` have their own);
  AutoConstructor, PolySharp, SonarAnalyzer.CSharp are wired in globally
  via `GlobalPackageReference` for all projects. Revit-versioned package versions use
  `$(MajorRevitVersion)` and are conditional on that property being defined, because the
  Revit-independent `netstandard2.0` projects are evaluated without a Revit configuration and
  an empty version would fail the whole restore.
- The SDK defines `BEFORE<year>` for `MajorRevitVersion <= year` and `AFTER<year>` for
  `MajorRevitVersion >= year`. Both constants are inclusive, so a boundary that starts at the year
  itself must be written with `AFTER<year>` (`#if AFTER2024` / `#if !AFTER2024`) instead of
  `BEFORE<year>`; otherwise Revit 2024 would compile the 2021-2023 compatibility branch and hit the
  obsolete-API warnings that the release build treats as errors.

## Documentation layout

- `AGENTS.md` links to the required repository guidance.
- `TODO.md` records agreed future work that is not yet implemented.
- `GLOSSARY.md` records the product vocabulary shared by the code, the documentation and the tests.
- `docs/policies/development.md` contains the development policy.
- `docs/diagnostics/adding.md` describes the end-to-end workflow for adding a built-in diagnostic.
- `docs/repository.md` describes the project, repository structure, and technology stack.
- `docs/architecture.md` records stable module boundaries and design decisions.
- `docs/FEATURES.md` summarizes the current product scope and compatibility contract.
- `docs/migrations/README.md` defines the migration registry: lifecycle, version window, index and entry template.
  Every user-visible breaking migration gets one numbered entry, for example
  `docs/migrations/0001-single-configuration-file.md` for the split-to-single configuration migration (`MIG-0001`).
  An entry records the versions on both sides, why the change is breaking, the user action, the data-safety
  guarantees and the exact cleanup checklist with the release that removes the transition.
- `wiki/Home.md` is the entry point for user documentation. Wiki pages use Obsidian-compatible links and are published to GitHub Wiki by `.github/workflows/publish-wiki.yml`.
- `wiki/getting-started/` contains installation, quick-start, and configuration setup.
- `wiki/diagnostics/` contains diagnostic modules and their configuration.
- `wiki/reference/` contains diagnostic and formula reference material.
- `wiki/interface/` contains the UI, panes, and ribbon-button documentation.
- `wiki/examples/` contains configuration files users can copy and edit; the welcome wizard installs the same single `diagnostics.config.yaml`. Keep the discipline `section` comment markers in pairs, and keep the English file and its `ru/` counterpart in step.
- `wiki/assets/` contains static images shared by Obsidian and GitHub Wiki.
- English pages live in the task-oriented folders directly under `wiki/`; Russian pages mirror that structure under `wiki/ru/`.
- `wiki/Home.md` is the language selector. Every localized page links to its counterpart and declares `lang: en` or `lang: ru` in YAML front matter.
- Page names must remain unique across both languages because GitHub Wiki addresses pages by name rather than by language folder.
- Wiki sources use Obsidian's `[[Target|Label]]` alias order. Inside Markdown tables, escape the alias separator as `\|`. The publication workflow runs `scripts/Prepare-Wiki.ps1` to reject malformed brackets, missing pages and incorrect explicit paths, then convert aliased links to GitHub Wiki's `[[Label|Target]]` order in a staging directory without changing the Obsidian sources.

Local Obsidian settings under `wiki/.obsidian/` are ignored; the contributor vault under
`docs/.obsidian/` is committed, so its shared settings travel with the repository. Keep shared content
and navigation in Markdown so the same files work in Obsidian and GitHub Wiki.

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

Generated report formats are pinned with snapshot tests in `Revit.Linter.DiagnosticReportExporting.Tests`. `Snapshot.Match` from `Revit.Linter.Testing` compares the output with `Snapshots/<Class>.<Test>.verified.<ext>`; on a difference it writes a `*.received.*` file next to it (ignored by Git) and fails. To accept an intended format change, review the received file and rename it over the verified one in the same commit. Snapshot inputs use the invariant culture so the text is identical on .NET Framework and .NET.

`Revit.Linter.Architecture.Tests` guards the documented coupling between UI presenter projects: it fails when a presenter project references another UI module or uses that module's view model outside the composition points recorded in the [architecture decisions](architecture.md).

## Continuous integration

The CI workflow builds Release_2021.1.100, Release_2022.1.80, Release_2023.1.90, Release_2024.3.60, Release_2025.4.60, Release_2026.4.10, and Release_2027.3.0 on push and pull requests. It runs projects ending in .Tests under `tests/` as headless tests. A separate job builds `updater/Revit.Linter.Updater.slnx` in `Release` and runs the headless tests under `updater/tests/`. The workflow does not build `build/Revit.Linter.Build.slnx` or `installer/Revit.Linter.Installer.slnx` and does not pass `-warnaserror`; those solutions are compiled by the release publish, so their compile errors surface during release preparation rather than on a pull request. Projects ending in .RevitTests are compiled with the solution but require a local Revit process to run.

## Versioning and release tags

Revit Linter uses GitVersion in Continuous Delivery mode with patch increments. GitHub releases are created manually from a commit carrying exactly one stable `vMAJOR.MINOR.PATCH` tag; the build uses the corresponding version without the `v` prefix for binaries and MSI installers. This repository publishes a GitHub release with installers rather than a NuGet package. The tagged commit must already contain the release section in `CHANGELOG.md` with an empty `Unreleased` section above it; the publish-release workflow stops otherwise, and the release pipeline publishes that section as the GitHub release description. The release section is composed at release time from the changes since the previous stable tag; regular changes do not add changelog entries. See the [release changelog policy](policies/development.md#release-changelog) and [release channels policy](policies/development.md#release-channels).

### Update discovery and user notification

The updater discovers releases through GitHub's `/releases/latest` endpoint
(`ProductIdentity.DefaultReleaseApiUrl`). That endpoint returns only the latest published release that is
neither a draft nor a pre-release, and the client additionally rejects draft and pre-release payloads.
Only a newer **stable** version can produce an update, so a stable release carrying the pre-release flag is
invisible to the updater even though its tag and MSI exist.

Update discovery and the notification live in the separate updater process, not in Revit. The updater runs
at Windows sign-in (at most once every 24 hours by default, subject to policy and user settings) and on
demand from the ribbon command. It compares the installed product version with the release tag and shows
one Windows notification per new stable version. The notification offers **Download**, **What's new**,
**Later** and **Skip this version**; **Download** verifies the exact MSI asset name, URL, size and SHA-256
digest before making the file available in Explorer, and never starts the installer. A version that was
already notified, skipped or postponed is remembered in the updater state. The add-in itself never contacts
GitHub and the command does not modify the model.
