# Module inventory

Read when: you need the project that owns a responsibility, or you add, move or delete a project.

Paths in this document are relative to the repository root.

Each project in the solution owns one primary responsibility, its contracts, its implementation and its
composition extensions. `src/` holds the modules; most are named
`Revit.Linter.<Area>`, and `VolocyNazad.Revit.Sugar` holds Revit API extensions outside the
`Revit.Linter.*` name space. The dependency direction, the abstraction seams and the interaction
contracts are in [module boundaries](../architecture.md#module-boundaries).

## Core and shared contracts

`Revit.Linter.Core.Abstractions` owns the shared diagnostic surface that compiles without Revit: rule
models, rule types, severity, parameters and fix/visualization pipeline definitions, the diagnostic
identity, state, tags, verdict and feedback models, documentation pages, UI activity facts, product
identity, the configuration-reading contracts and the host seams that carry no Revit type. It targets
`netstandard2.0` and must not reference the Revit API, WPF or the Revit SDK.

`Revit.Linter.Core` owns the Revit-facing contracts of the same surface: element and document
diagnostics, their filters and fixes, visualization pipelines and contexts, catalog and registration
types, and the session catalog scope that narrows the published checks for a guided session and hides
its checks outside one. A Revit-free type stays there only when it belongs to one of those clusters.

The host seams live in `Revit.Linter.Core.Abstractions`: `IRevitEventSource`, `IClipboardService`,
`IShellLauncher`, `IDocumentationLauncher`, `IUserInterfaceActivityStream` and
`IFormulaCompilationNotifier`. `IRevitIdlingScheduler`, which takes `UIApplication`, and
`IVisualizationViewProvider`, which returns `View3D`, stay with the Revit-facing contracts in
`Revit.Linter.Core/Abstractions`. Their implementations live in
`src/Revit.Linter/Infrastructure/Services` and the host registers them; modules depend only on the
contracts.

`Revit.Linter.DiagnosticReportPresenter.Interactions` holds the contracts other modules and the host
use for that presenter (`IDiagnosticReportPresenter`), so no consumer
references the presenter project for them. A presenter that needs such an outward contract gets a
matching `<Module>.Interactions` project instead of exposing it from its implementation assembly.

## Configuration

`Revit.Linter.DiagnosticConfiguration` owns the configuration file envelope, YAML reading,
type-specific rule validation and the comment-preserving text editor used to write configuration
changes. It is Revit-independent; `Revit.Linter.ConfigurationPath` keeps the Revit-version-aware paths
behind `IConfigurationPaths`, file discovery and change watching, and delegates format work to it.
The startup migration of the legacy split files and the validation and dispatch rules are in
[configuration and diagnostics](../architecture.md#diagnostics-and-reports).

## Document data

`Revit.Linter.DocumentQueries` owns the document queries shared by diagnostics (all elements, elements
of a class, element types, element geometry) and the typed `DocumentQueryKey` under which their results
are kept in the transaction cache. Diagnostic modules, the diagnostic service and the dependency
definers reach `IRevitTransactionMemoryCache` only through its `IDocumentQueryService`; the query keys
and their caching rules are in [diagnostics and reports](../architecture.md#diagnostics-and-reports).

`Revit.Linter.ParameterValues` owns the parameter value model used by configuration-driven logic. It
resolves a parameter identifier (a `BuiltInParameter` name, a shared parameter GUID or the display
name) and converts between the Revit storage value and the formula value. Reading and writing are
explicit operations with a stable failure code instead of a written default value; the conversion
guarantees are in [parameter values](../architecture.md#parameter-values).

`Revit.Linter.ReportMessaging` holds the shared report message template parser (`Template` + `Args` to
cached plain text and text parts with element links) and the typed WPF message view used by the
diagnostic and fix report presenters. The project has no compile-time dependency on the Revit API;
presenters adapt `ElementId` values through the generic link factory.

`Revit.Linter.Languages` owns the formula grammar and, in `Factories`, the factories that compile
configuration formulas into document predicates, element functions and native element filters. The
diagnostic modules share them through `AddFormulaFactories` and compile the formulas of every
configured rule while they register it. The runtime factories keep their fallback for any remaining
compile failure, logged and reported through `IFormulaCompilationNotifier`.

## Presentation

`Revit.Linter.Presentation` holds shared WPF composition infrastructure. Its `ViewLocatorExtension`
resolves an embedded view from DI by the corresponding view-model type; it is used only at module
composition boundaries, not as a general service locator.

The project also holds the view-model bases in `ViewModels` (`InitializableObservableObject` with the
initialize and deinitialize commands, and `RevitInteractionViewModel`, which observes the
host-published `IRevitEventSource` events and reports them as `RevitEventType`), `ReportRowViewModel`
for the diagnostic and fix row state, `FilteredRowCollection` for the collection-view, sort and filter
plumbing shared by the list panes, and `BindingProxy` for bindings outside the visual tree.

The project also remembers which data columns of the four tables the user hides: `Abstractions` and
`Services` hold `ITableColumnsService`, which stores the `columns.yml` choice and the temporary reveal
of a guided step, `ViewModels` holds the popup of one table, `Models` holds the table and column keys
and the stored settings, and the `TableColumns` attached properties at the project root apply the
choice to a `DataGrid`. Only data columns carry a key, so the action icon columns always stay visible.

It references `Revit.Linter.Core` and the Revit API for the row type's element identifiers,
`Revit.Linter.ReportMessaging` for the shared message parser, and `VolocyNazad.ValueStore` for the
per-user column choice.

The dependency properties of this project, of `Revit.Linter.Behaviors` and of
`Revit.Linter.ReportMessaging` are declared with the `VolocyNazad.Wpf.DependencyPropertyGenerator`
package: the registration, the CLR property or the `Get`/`Set` accessors, the XML documentation and
the callback wiring are generated from `[GenerateDependencyProperty<T>]` and
`[GenerateAttachedDependencyProperty<T>]`. The projects that XAML compiles keep their registrations
hand-written, as the [presentation policy](../policies/presentation.md) explains.

`Revit.Linter.OpenedDocuments` owns the shared opened-document selector of the report and fix panes. It
exposes the selector through `IOpenedDocuments` and the stable `OpenedDocument` item model, so the panes
bind to the contract and never reference the selector view model; the view model keeps the Revit-aware
refresh of the open document list.

Each presenter project keeps the design-time data of its views next to them in `DesignData/`
(`<ViewName>DesignData` classes wired through `d:DataContext` with `IsDesignTimeCreatable`); they exist
for the XAML designer only, mirroring the binding paths and localized resources of their view.

`ViewLocatorExtension` resolves the design-time view and its data for a composed sub-view when the
designer has no service provider, and views, windows and dialogs whose runtime constructor needs
services keep a public parameterless design-time constructor. The view, view-model and design-time
rules are in the [presentation policy](../policies/presentation.md).

`Revit.Linter.ThemeManaging` owns the look shared by all views and depends on no control library.
`SharedThemeResources` is the single resource dictionary with the palette and the style and template of
every control the add-in uses; each root view merges it instead of declaring colors or restyling
controls itself. Views refer only to its `Linter*` keys and take the font and the colors from the
`LinterViewRoot` style set on the root element.

Colors live in `LightPalette` and `DarkPalette` next to it: both declare the same `Linter*Brush` keys,
and styles reference them only through `DynamicResource`.

`IThemeService` calls `SharedThemeResources.ApplyTheme` for registered element trees when the Revit
theme changes. `Controls` holds what the styles need beyond WPF: `PathIcon` with its `IconKind` outlines
(from Material Design Icons, Apache License 2.0) and the `Hint.Text` attached property for the empty
text box hint.

`DocumentationPage` in `Revit.Linter.Core.Abstractions` names a Wiki page by its English and Russian
page names and builds its address for the current UI culture. Diagnostic registrations carry the page of
their module, ribbon buttons use it for F1 help, and the report and diagnostics rows open it; a renamed
Wiki page must be renamed in the code that refers to it.

`HelpToolTip` in `Revit.Linter.Behaviors` gives a pane control a ribbon-like tooltip through attached
properties (title, description, note). F1 while a tooltip is open executes its inherited `HelpCommand`,
set once on the root of a view. View models open pages through `IDocumentationLauncher`, implemented by
the add-in host.

## Diagnostics and reports

`Revit.Linter.DiagnosticReportExporting` owns the diagnostic report export: the shared export document
contract, the CSV/JSON/YAML/HTML exporters behind `IDiagnosticReportExporter`, and the
`IDiagnosticReportExportService` orchestration (suggested file name, export document, exporter choice).

The presenter supplies plain finding data with cultures and labels and keeps the
`DiagnosticReportExportedActivity` publishing; the modal save-file dialog stays behind
`ISaveFileDialogService`. The project has no Revit API dependency. The ownership rules are in
[diagnostics and reports](../architecture.md#diagnostics-and-reports).

`tests/Revit.Linter.CollisionDiagnostics.RevitTests` owns the collision fixture builder: it creates a
host document with two shared-coordinates links whose walls, floors and system runs overlap the host
content, covering volumetric and touch-only hits and no-collision controls. The builder lives in the
test project instead of a product module, so the add-in ships no test code and the diagnostics tab has
no test-tooling panel.

## Element operations

`Revit.Linter.ElementAccentor` owns atomic, reversible element interaction and graphics operations such
as show, select, isolate, crop, per-element overrides and temporary-filter overrides.
`Revit.Linter.ElementVisualization` composes those operations into diagnostic visualization pipelines and
restores their sessions in reverse order; the session and reversal rules are in
[element highlighting](../architecture.md#element-highlighting).

The diagnostic report presenter obtains the persistent `Revit Linter — Visualization` isometric view
and passes it to the pipeline. The host owns creation, preparation and activation behind
`IVisualizationViewProvider`, keeping `RevitAPIUI` calls out of the presenter. When a built-in finding's
target is itself a view, the presenter instead passes that view and applies the target-view pipeline in
it, so the visualization opens the found view and creates no isometric view. The pipeline activates
the view only after the steps declared before the first step that needs it on screen are committed,
because Revit rejects an active-view change inside a transaction.

`Revit.Linter.ElementFixing` composes configuration-driven destructive fix steps into fixes consumed by
the diagnostic report presenter. A configured step deletes its element sets or writes one parameter
value per element, and the value formula is compiled once when the rule is registered; the execution
rules are in [element fixing](../architecture.md#element-fixing).

`Revit.Linter.ElementIgnoring` owns the ignore list behind `IIgnoreElementDetector`,
`IIgnoreElementProvider` and `IIgnoreListManager`; storage details never leave the module. The entries
live inside the document as Extensible Storage on a single `DataStorage`, with one entry per element and
diagnostic code or per document and code.

`Revit.Linter.IgnoreListPresenter` shows the ignore list of the active document in its own dockable pane,
where entries can be selected or removed, and reaches the list only through `IIgnoreListManager`. The
storage and cache rules are in [element ignoring](../architecture.md#element-ignoring).

## Onboarding

`Revit.Linter.WelcomePresenter` owns the first-run welcome wizard, its example configuration
installation and the tutorial sample handling. `Revit.Linter.PracticalTourPresenter` owns the optional
practical tour, its managed `TOUR001` configuration and its dockable pane; its contracts live in
`Revit.Linter.PracticalTourPresenter.Interactions`, so the wizard and the host depend on `IPracticalTour`
without referencing the presenter implementation.

Both presenters share the per-user onboarding state (`welcomeSettings.yml` in the settings value store)
owned by `Revit.Linter.OnboardingState`: it keeps the completed wizard version, the example offers per
Revit version and the resumable practical-tour progress per Revit version.

The installed example is the `diagnostics.config.yaml` file under `wiki/examples/configuration/`
(English) and its `ru/` subfolder, embedded as resources. The **Examples** step also installs the
check blocks generated from the ADSK Revit 2021 project templates, stored under
`wiki/examples/template-checks/`: each block holds one project-parameter rule and one element fill
check for every text parameter of the template. The blocks ship in two editions: the root `en/` and
`ru/` folders use the Revit 2021-2023 `PG_*` parameter group spelling, and `2024/en/` and `2024/ru/`
hold the same rules with the ForgeTypeId group values of Revit 2024 and newer. The step offers the
blocks on every supported Revit version and installs the edition matching the running version. The
selected blocks are appended to the same installed example file, so the element checks stay in place
and the installed checks are marked as examples.
The host supplies the ribbon tab name, pane
and link actions through `IWelcomeHost`, and the tour's pane, notification and diagnostics-filter
actions through `IPracticalTourHost`.

The wizard's headless rules (example composition, installation into `examples\` with a different
existing file backed up to a `.bak-<timestamp>` copy, first-run state) are covered by
`Revit.Linter.WelcomePresenter.Tests`, and the tour's state machine and managed configuration by
`Revit.Linter.PracticalTourPresenter.Tests`; the WPF views themselves are not.

The wizard content version is raised so an upgraded user is offered the examples step again once per
Revit version. Debug builds show the whole wizard on every Revit start; release builds show only the
steps the user has not seen. The practical tour observes neutral completed UI facts published through
`IUserInterfaceActivityStream`; diagnostic presenters do not reference welcome or practical-tour presenter
types.

Its WPF emphasis is owned by a disposable highlight session, so closing or restarting the tour restores
every affected control. A non-blocking spotlight dims the surrounding pane, outlines the active target
and labels it as the next step; a button target additionally glows through its own element effect and
takes an accent frame.

The host also brings the required pane to the foreground; the tour is hosted in a right-side Revit
dockable pane and its registration and visibility remain host actions behind `IPracticalTourHost`.

The tour's managed state, its removal and the `TOUR001` ordering are in
[presentation](../architecture.md#presentation).

## Updater and installer

The updater lives in `updater/src/` and its tests in `updater/tests/`. `updater/Directory.Packages.props`
replaces the root central package versions for everything under `updater/`, so updater dependencies can
be versioned independently of the Revit add-in. `updater/Directory.Build.props` likewise replaces the
root one: the updater projects use `Microsoft.NET.Sdk` instead of `VolocyNazad.Revit.Sdk` and target
`net8.0-windows` explicitly, except the notification executable, which adds the Windows 10 version to
that target framework; all of them have plain `Debug`/`Release` configurations.

Nothing in the root solution references the updater projects; the add-in only starts
`Revit.Linter.Updater.exe` by path, and the release build publishes the updater project directly.

`Revit.Linter.Updater.Core` contains Revit-independent release discovery, stable-version comparison,
check scheduling, per-user state persistence, and single-instance coordination. `Revit.Linter.Updater`
is the short-lived executable entry point; it must remain free of Revit API dependencies. The build-unit
rules for `updater/` are in the [build and toolchain policy](../policies/build.md), and the runtime
update rules are in
[packaging, releases and updates](../architecture.md#packaging-releases-and-updates).

Its product icon is embedded in the executable and reused by the MSI registration shown in Windows
Installed Apps. Manual update checks run without a console window and always attempt to show a Windows
notification with the result. The updater derives comparison data from the numeric assembly version while
retaining the full GitVersion semantic version in logs and assembly metadata.

> **Note:** Windows App SDK activation registration can fail in a self-contained unpackaged process when
> its runtime resource DLL is unavailable. Notification display remains supported in that case, but action
> buttons are omitted because their callbacks cannot be delivered safely.

Updater policy is read from `Software\Policies\Volocy\Revit.Linter\Updater` in `HKLM` and then `HKCU`.
Machine values take precedence over current-user policy, user JSON settings, and built-in defaults. The
registry adapter is read-only; neither the updater nor the per-user installer provisions policy values.

The release pipeline publishes the updater and Windows App SDK notification runtime for `win-x64` as a
self-contained unpackaged application before MSI creation. The publish directory is an explicit
build-module result; installer generation must consume that result instead of relying on
configuration-specific paths under `bin`.

Update discovery runs through GitHub's `/releases/latest` endpoint (`ProductIdentity.DefaultReleaseApiUrl`),
which returns only the latest published release that is neither a draft nor a pre-release; the client
also rejects draft and pre-release payloads. Only a newer **stable** version can produce an update.

The updater runs at Windows sign-in (at most once every 24 hours by default, subject to policy and user
settings) and on demand from the ribbon command, and shows one Windows notification per new stable
version with **Download**, **What's new**, **Later** and **Skip this version**. A version that was
already notified, skipped or postponed is remembered in the updater state. The add-in itself never
contacts GitHub and the command does not modify the model. What the download may do is in
[packaging, releases and updates](../architecture.md#packaging-releases-and-updates).

Release packaging produces one per-user MSI containing every supported Revit build and the shared
updater. It installs under `%LOCALAPPDATA%\Programs\VolocyNazad\Revit.Linter\`, creates version-specific
manifests under `%APPDATA%\Autodesk\Revit\Addins\<year>\`, and registers the updater under the current
user's `Run` key. The MSI has one stable `UpgradeCode`; its product code changes deterministically with
the stable product version. The feature-tree installer UI keeps the per-user installation directory
fixed.

Manifest assembly paths still use the effective MSI `INSTALLDIR`. The first unified installation also
detects and removes older per-Revit-version products by their legacy upgrade codes.

The MSI exposes one required `Core` feature and one optional feature per supported Revit year. A
`ProductName` lookup under `HKLM\SOFTWARE\Autodesk\Revit\Autodesk Revit <year>\Components` selects
installed versions by default; the feature-tree UI lets the user override that selection. Manifest
synchronization follows the requested feature states on install, repair and modify. Before any file
change, setup stops the short-lived updater and refuses to continue while a Revit process is running.

Product-wide identity (vendor, product and repository names, GitHub destinations, registry policy
location, updater synchronization name) is owned by `ProductIdentity` in
`Revit.Linter.Core.Abstractions` (`src/Revit.Linter.Core.Abstractions/ProductIdentity.cs`). The updater
and the installer compile the same file as a source link, so they stay independent build units without a
project reference to the Revit API.

Path composition stays with the owning module: `IConfigurationPaths` for user configurations
(`Documents\Revit Linter\<year>\`, `diagnostics.config.yaml`, `examples\`, `debug\`, `tour\`, `legacy\`, and
the `VersionsDirectory` that the startup migration enumerates), `UpdaterPaths` in
`Revit.Linter.Updater.Core` for `%LOCALAPPDATA%\Volocy\Revit.Linter\updater\` (`logs/`, `state.json`,
`downloads/`), `InstallerProduct` for MSI identity and manifest paths, and the composition root for add-in
logs (`...\logs\`) and the ValueStore settings directory (`%LOCALAPPDATA%\Volocy\Revit.Linter\settings\`).

Renaming the vendor, product or repository requires editing `ProductIdentity` only; the `build/` asset
name `RevitLinter-<version>.msi` mirrors `ProductIdentity.GetMsiAssetName`.
