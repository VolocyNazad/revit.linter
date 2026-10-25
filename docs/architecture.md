# Architecture decisions

This document records stable architectural rules. Implementation details belong in code and tests. Read it together with the [development policy](policies/development.md) and [repository guide](repository.md).

## Module boundaries

- Projects are small modules with one primary responsibility. A module owns its contracts, implementation and composition extensions.
- `Revit.Linter.Core.Abstractions` owns the Revit-independent diagnostic and configuration contracts
  (`netstandard2.0`) and must not reference the Revit API, WPF or the Revit SDK. The add-in build consumes
  it, so it is the single definition of a rule, its type, severity, parameters and fix/visualization
  pipeline shape.
- `Revit.Linter.DiagnosticConfiguration` owns the configuration file envelope, YAML reading, rule
  validation and the comment-preserving text editor. It is Revit-independent for the same reason.
  `Revit.Linter.ConfigurationPath` keeps the Revit-version-aware paths behind `IConfigurationPaths`,
  source precedence, change watching and the service surface, and delegates format work to this module.
- The configuration file is hand-documented: comments, `section` markers, key order and quoting are
  part of the user-facing file. Changes are therefore written by patching the affected text spans, not
  by serializing the parsed model; the shared editor guarantees that invariant for both writers.
- Product-wide identity segments (vendor, product, repository, registry policy, updater synchronization) are owned by `ProductIdentity` in `Revit.Linter.Core`; path composition stays with the owning module. Independent build units (`updater/`, `installer/`) compile the same source file via link instead of taking a project reference.
- Cross-project consumers depend on contracts under the owning project's `Abstractions/` namespace, not on its infrastructure implementation.
- Interaction seams are owned explicitly. A contract a module publishes to other modules stays in its `Abstractions/` when consumers may reference the module; when a consumer must not reference the implementation project, the contract lives in a `<Module>.Interactions` project (`Revit.Linter.DiagnosticReportPresenter.Interactions`). Cross-cutting contracts implemented by the host live in `Revit.Linter.Core/Abstractions` (`IRevitIdlingScheduler`, `IRevitEventSource`, `IClipboardService`, `IShellLauncher`, `IDocumentationLauncher`) and their single implementation in `src/Revit.Linter/Infrastructure/Services`; modules receive them through constructor injection instead of starting shell processes, touching the clipboard or subscribing to Revit application events themselves.
- View-model composition between UI modules is limited to the documented points: the four pane views compose `RunDiagnosticViewModel` through `ViewLocator`, and a pane receives the opened-document selector through the `IOpenedDocuments` contract of `Revit.Linter.OpenedDocuments`. A presenter consumes another UI module only through that module's contracts and shared infrastructure; `Revit.Linter.Architecture.Tests` fails when a presenter project references another UI module or uses its view-model type outside those points.
- `Infrastructure/` contains implementation details and external adapters. It must not become a second public contract surface.
- New project references must follow the direction from composition and presentation toward abstractions and domain behavior. Do not introduce reference cycles or use a service locator to bypass them.
- Concrete implementations are created by dependency injection at composition boundaries. Runtime-data objects may use explicit factories.

## Revit integration

- Revit API access stays behind narrow services or command/event boundaries so domain and presentation code can remain testable without a live Revit process.
- The host owns the single subscription to the observed Revit application events (`DocumentCreated`, `DocumentOpened`, `DocumentClosing`, `DocumentClosed`, `DocumentChanged`, `FamilyLoadedIntoDocument`, `ViewActivated`) and publishes them through `IRevitEventSource`; interaction view models only observe that contract, so the same event is subscribed once per Revit session and a view model needs no Revit API context to react.
- Keep services that use `RevitAPIUI` (`UIApplication`, `UIDocument`, `UIView`, `Selection`) as thin adapters over logic that depends only on `RevitAPI` or plain data whenever that logic is substantial enough to test. Do not let a UI-only call make document operations, selection rules, state calculation or restoration logic unavailable to `Nice3point.TUnit.Revit` tests.
- Work that modifies a Revit document runs in an explicit transaction. UI-triggered asynchronous work enters the Revit API through the repository's external-event infrastructure.
- The add-in host is built once per Revit session and does not rebuild its service graph after shutdown; background
  logging probes the live host through `TryGetService` instead of creating one. A failed startup therefore cannot
  resurrect the container from the shutdown path.
- Version differences are isolated in compatibility extensions or adapters. Conditional compilation belongs inside those boundaries rather than at call sites.
- Code that needs live Revit objects is tested in `*.RevitTests` with `Nice3point.TUnit.Revit`. Headless behavior stays in `*.Tests`.

## Presentation

- Views contain layout and view-only mechanics. View models own state and commands; application and diagnostic behavior lives in services.
- Presenter modules publish completed, product-level UI facts through `IUserInterfaceActivityStream`; event types describe the action and result without naming an observer. Optional observers such as onboarding translate those facts in their own module. Observer failures are isolated by the host stream and never change the originating command's result.
- The practical tour owns the starting state of its managed `TOUR001` training diagnostic: tour setup refreshes the managed `tour/practical-tour.config.yaml` file in the current format, where the rule declares `isActive: false`, so the user enables the check as the guided step asks. Completing the tour or opting out removes the managed file. Step advancement itself stays purely observational — only observed UI facts move the tour state machine, and resumed tours keep the current selection. Tour rules carry the reserved `Tour` tag and are sorted above the rest of the diagnostics list.
- Onboarding is realized by two presenter modules with one primary responsibility each:
  `Revit.Linter.WelcomePresenter` owns the modal one-shot welcome wizard and the example configuration installation,
  and `Revit.Linter.PracticalTourPresenter` owns the optional modeless practical tour. The tour publishes its
  contracts from `Revit.Linter.PracticalTourPresenter.Interactions`; the wizard and the host depend only on that
  contract (`IPracticalTour`), so `WelcomePresenter` never references the tour presenter implementation. Both
  presenters share the per-user onboarding state (`welcomeSettings.yml`) through `Revit.Linter.OnboardingState`,
  and each experience has its own host contract (`IWelcomeHost` for the wizard, `IPracticalTourHost` for the tour)
  implemented by a separate host class. `Revit.Linter.Architecture.Tests` records both presenter modules and the
  wizard's reference to the tour contract.
- Reusable WPF mechanics live in `Revit.Linter.Behaviors`. Prefer bindings, converters, commands and behaviors over event handlers.
- `Revit.Linter.Presentation.ViewLocator` resolves embedded views from dependency injection only at module composition boundaries; it is not a general service locator.
- View-model bases shared by presenter modules (`InitializableObservableObject`, `RevitInteractionViewModel`) and `BindingProxy` live in `Revit.Linter.Presentation`; a presenter module does not keep its own copy.
- Shared styles and resources are defined once and consumed through resource references. Theme-dependent values use `DynamicResource`.
- Every view carries design-time data in `DesignData/` next to the view, so the XAML designer shows representative content instead of empty fields. The design class mirrors the binding paths of the view and its item templates, reuses the localized resources through `LocalizationResourceReader` and is attached with `d:DataContext`/`IsDesignTimeCreatable`. `ViewLocatorExtension` resolves the matching design data for a composed sub-view at design time, and a view whose runtime constructor needs services keeps a public parameterless design-time constructor.
- Control styles, fonts, sizes and paddings live in `SharedThemeResources` of `Revit.Linter.ThemeManaging`. Views keep layout and bindings and refer to `Linter*` style keys; they do not reference third-party style keys or repeat sizes that a shared style defines. A style specific to one view is still declared there, based on a shared one.

## Diagnostics and reports

- Diagnostic projects discover or calculate findings. Presenters render and interact with them; providers transport or retain report state. These roles remain separate.
- A run is described by `DiagnosticRunOptions` (full or incremental: duration logging and report batching) and
  measured by `DiagnosticRunMetrics`, which owns timings, garbage-collection counters and query-cache statistics.
  The diagnostic service only decides what to run and how to report findings.
- `Revit.Linter.DiagnosticReportExporting` owns the report export contract and file writing. The diagnostic report presenter supplies plain finding data with cultures and labels through `IDiagnosticReportExportService` and keeps activity publishing; the save-file dialog stays behind `ISaveFileDialogService`, and the module has no Revit API dependency so the formats stay headless-testable.
- `Revit.Linter.ReportMessaging` owns the shared message-template contract and typed message view. Feature modules supply data and element-link adapters rather than duplicating parsing or rendering.
- Configuration is validated when it is loaded, not when a diagnostic runs. A module skips an invalid rule,
  keeps the valid ones, and describes the skipped rules through `IDiagnosticConfigurationErrorSource`;
  file-level problems travel as structured `DiagnosticConfigurationFileProblem` values through
  `IDiagnosticConfigurationProblemSource` and the host localizes them, while rule schema problems are
  formatted through `IDiagnosticConfigurationRuleTextFormatter`; the plain descriptions remain available
  for logs and legacy callers; the application shows all of them once per distinct problem.
  Validation logic stays free of Revit API types so it is covered by headless tests; Revit-version knowledge
  is passed in from the compatibility boundary.
- Every diagnostic module reads the same `diagnostics.config.yaml` through
  `IDiagnosticConfigurationService`; the file is parsed once per catalog revision and each rule is dispatched
  by its `type` (`custom`, `collision`, `parameter`, `builtIn`). Providers do not watch their own files.
- The effective state of a diagnostic (`isActive`, `severity`, `isHidden`) lives in the configuration file.
  The Diagnostics pane writes it back through `IDiagnosticConfigurationWriter`, which patches only the
  affected YAML fields so comments and rule order survive. There is no separate override store.
- Diagnoses whose `isHidden` is `true` are absent from the list, the run selection and the reports, and are
  not executed; `isHidden` is stronger than `isActive`. The reset command removes built-in overrides and
  `isHidden` fields but never rewrites the user's own `isActive`/`severity` values.
- The first rule with a duplicate code and target kind wins; a configured rule never overrides a built-in
  check of the same kind. The conflict is logged, not shown as a configuration error.
- Diagnostics read the transaction cache only through `Revit.Linter.DocumentQueries`. Queries that several
  modules need (all elements, elements of a class, element types, element geometry) are methods of
  `IDocumentQueryService`, so one query has exactly one key. A module-specific derived value uses
  `GetOrCreate` with a `DocumentQueryKey` whose query name starts with the module name. Every key identifies
  the document, and the view when the result depends on it; a key never relies on an element identifier or a
  document title alone.
- Diagnostic codes and serialized configuration contracts are stable user-facing identifiers. Change them only with an explicit migration or compatibility decision.
- A diagnostic identity carries free-text tags used only to find the diagnostic in the panes and to label it in reports; a rule's `tags` list is additive and does not change the file format version. The product also adds reserved system tags the user cannot set: `BuiltIn` marks a check that ships with the product, `Custom`, `Collision` and `ParameterProject` name the rule's declared `type`, and `Example`, `Tour` and `Debug` state where a check came from. The document-kind tags `Project` and `Family` are ordinary tags the product sets on its built-in diagnostics; a configured rule declares them itself, because only the rule knows which document kinds it applies to. System tags are trusted and always English, so a rule tag that reuses a reserved name is dropped.
- Diagnostic configuration files carry a single `version` file format envelope around the rule list. Version 2 is the one-file format with a per-rule `type`; files without it are not read, and a file with a newer version than the running product is refused when it is loaded, so compatibility checks have a reliable signal. The old split files are detected only to show the one-time migration notice and are never modified.

## Element highlighting

- `Revit.Linter.ElementAccentor` owns atomic, reversible Revit view and selection operations.
- `Revit.Linter.ElementVisualization` composes those operations into diagnostic visualization pipelines.
- Every visualization session records enough state to restore what it changed. Composite sessions restore steps in reverse order.

## Element fixing

- `Revit.Linter.ElementFixing` owns configuration-driven element fix pipeline composition.
- Fix pipelines execute ordered destructive steps inside the transaction supplied by the fix presenter.
- Step implementations remain independent of user-diagnostic configuration parsing and presentation.

## Element ignoring

- `Revit.Linter.ElementIgnoring` owns the ignore list behind `IIgnoreElementDetector`,
  `IIgnoreElementProvider` and `IIgnoreListManager`; storage details never leave the module.
- Ignores are stored in one `DataStorage` with Extensible Storage per document, one entry per
  ignored element and diagnostic code or per ignored document and code — never as parameters on
  the elements themselves, so any element can be ignored. A document-level entry suppresses every
  finding of its diagnostic in that document.
- The cached ignore state is dropped (never patched) after a write, so checks inside the writing
  transaction and after a rollback both reload the current storage. Missing entries are dropped on
  every write and on settled pane loads, never inside detector runs or change storms an undo may
  still revert.
- `Revit.Linter.IgnoreListPresenter` shows the ignore list of the active document in its own dockable
  pane and edits it only through `IIgnoreListManager`; it knows nothing about the storage.

## Localization

- English is the neutral resource language. Russian resources use the parent `ru` culture.
- User-facing strings are resolved through `LocalizationResourceReader`; feature-owned strings remain with their owning feature.
- Resource keys and placeholders must remain compatible across cultures. Localization assemblies remain separate from ILRepack and are verified in final published artifacts.

## Logging and failures

- Serilog is configured in the application composition root; modules use the Microsoft logging abstractions.
- A diagnostic that throws is isolated by the diagnostic service: the failure is logged, published as an error
  report under the diagnostic's code, and the remaining diagnostics still run.
- A failure is logged once at the layer that understands its meaning. Higher layers may translate it into user-visible feedback without logging it again.
- Logs contain useful model, document, diagnostic and operation context, but must not contain secrets or unnecessarily large element payloads.

## Packaging and releases

- The root solution builds the add-in and tests. `installer/`, `build/`, `benchmark/`, `updater/` and `sandbox/` remain separate solutions with their own responsibilities.
- The updater is an independent build unit: `updater/` owns its MSBuild settings and central package versions, uses
  plain `Debug`/`Release` configurations and is not referenced by the root solution. The add-in couples to it only by
  launching `Revit.Linter.Updater.exe`, and the release pipeline publishes it directly for the MSI.
- ILRepack output and localization satellites are validated before MSI creation.
- Stable releases originate from exactly one `vMAJOR.MINOR.PATCH` tag and are published through the repository release pipeline.
- One per-user MSI owns every supported Revit build, the shared updater, version-specific manifests,
  and updater startup registration. A stable `UpgradeCode` keeps upgrades within that single product line.
- The updater is an immutable required MSI feature. Revit-version payloads are optional features,
  preselected from installed Revit registry keys and synchronized with their per-user manifests.
- Installer file changes require all Revit processes to be closed; the short-lived updater is stopped
  before replacement begins.
- Update discovery runs in the separate short-lived `Revit.Linter.Updater` process. Its core has no
  Revit API dependency; the add-in may launch it but must not perform release HTTP requests or create
  Windows notifications itself.
- Only one updater process performs work in a Windows user session. Concurrent manual launches signal
  the active process through named synchronization objects; pending requests are coalesced and reuse an
  already-running release check.
- Release notifications use the self-contained Windows App SDK from the updater process. Notification
  activation is handled directly by the unpackaged executable, without a custom URI protocol. Only
  validated HTTPS release links for the official GitHub repository may be opened.
- An explicit **Download** activation may fetch only the exact versioned MSI asset from the official
  GitHub release. The updater checks the declared size and GitHub-provided SHA-256 digest before moving
  the temporary file into the downloads directory, then reveals it in Explorer without launching it.
  Automatic installer launch remains prohibited until expected-publisher Authenticode verification and
  Revit process coordination are implemented.
- Effective updater configuration is resolved in descending precedence from machine policy, current-user
  policy, user settings, and built-in defaults. Policy storage is read-only to the application and installer;
  normal defaults and user preferences must never be written below a Windows `Policies` key.
