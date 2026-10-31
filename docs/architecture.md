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
- View-model composition between UI modules is limited to the documented points: the four pane views compose `RunDiagnosticViewModel` through `ViewLocatorExtension`, and a pane receives the opened-document selector through the `IOpenedDocuments` contract of `Revit.Linter.OpenedDocuments`. A presenter consumes another UI module only through that module's contracts and shared infrastructure; `Revit.Linter.Architecture.Tests` fails when a presenter project references another UI module or uses its view-model type outside those points.
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

WPF rules — views and view models, design-time data, shared styles and resources, `ViewLocatorExtension` and the
shared view-model bases — are in the [presentation policy](policies/presentation.md). This section
records what the presentation modules own; the module inventory is in the
[repository guide](repository.md).

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

## Diagnostics and reports

- Diagnostic projects discover or calculate findings. Presenters render and interact with them; providers transport or retain report state. These roles remain separate.
- A run is described by `DiagnosticRunOptions` (full or incremental: duration logging and report batching) and
  measured by `DiagnosticRunMetrics`, which owns timings, garbage-collection counters and query-cache statistics.
  The diagnostic service only decides what to run and how to report findings.
- `Revit.Linter.DiagnosticReportExporting` owns the report export contract and file writing. The diagnostic report presenter supplies plain finding data with cultures and labels through `IDiagnosticReportExportService` and keeps activity publishing; the save-file dialog stays behind `ISaveFileDialogService`, and the module has no Revit API dependency so the formats stay headless-testable.
- `Revit.Linter.ReportMessaging` owns the shared message-template contract and typed message view. Feature modules supply data and element-link adapters rather than duplicating parsing or rendering.
- Configuration is validated when it is loaded, not when a diagnostic runs. A module skips an invalid rule,
  keeps the valid ones, and describes the skipped rules through `IDiagnosticConfigurationErrorSource`.
  The formulas of a configured rule are compiled while the module registers it, so a rule whose formula
  cannot be compiled is skipped and reported with its code and rejected formula text instead of silently
  falling back to an always-valid check at run time. Unlike the schema check, formula compilation needs the
  Revit API and stays in the module that owns the formulas.
  File-level problems travel as structured `DiagnosticConfigurationFileProblem` values through
  `IDiagnosticConfigurationProblemSource` and the host localizes them, while rule schema problems are
  formatted through `IDiagnosticConfigurationRuleTextFormatter`; the plain descriptions remain available
  for logs and legacy callers; the application shows all of them once per distinct problem.
  Rule schema validation stays free of Revit API types so it is covered by headless tests; Revit-version
  knowledge is passed in from the compatibility boundary.
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
- Diagnostic configuration files carry a single `version` file format envelope around the rule list. Version 2 is the one-file format with a per-rule `type`; files without it are not read, and a file with a newer version than the running product is refused when it is loaded, so compatibility checks have a reliable signal. Before any configuration is read, a startup migration moves the legacy split files of every Revit version folder — the top-level files and their `examples` and `debug` copies — into a `legacy` subfolder of that version folder, preserving the subfolder layout and adding a numeric suffix when a name is already taken; nothing is deleted, and a file that cannot be moved stays in place and is logged. The product then reads none of those files and shows one warning per product version, with a button to the migration guide; there is no persistent banner in the Diagnostics pane and no notice per Revit version. A newly created `diagnostics.config.yaml` carries only the normal format header. The managed practical-tour file is refreshed automatically when it is not in the current format, and installing examples backs up an existing, different `examples\diagnostics.config.yaml` to `.bak-<timestamp>` before replacing it; an identical file is left untouched. The transition is recorded in [migration MIG-0001](migrations/0001-single-configuration-file.md), which lists the code and documentation that are removed once the migration support ends.

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

Language and culture rules, resource ownership, resolution through `LocalizationResourceReader`, the
resource files and their build, and the localization validation are in the
[localization policy](policies/localization.md).

## Logging and failures

Logging — where Serilog is configured, what each level is for, logging a failure once, structured
messages, privacy and the isolation of a failed diagnostic — is defined by the
[logging policy](policies/logging.md).

## Packaging, releases and updates

Build units and release mechanics — the separate solutions, the independent updater build unit, the
MSI and its identity, release tags and channels — are defined by the
[build and toolchain policy](policies/build.md) and the [release policy](policies/releases.md). This
section records how the updater behaves at runtime.

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
