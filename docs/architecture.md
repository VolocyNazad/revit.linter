# Architecture decisions

This document records stable architectural rules. Implementation details belong in code and tests. Read it together with the [development policy](policies/development.md) and [repository guide](repository.md).

## Module boundaries

- Projects are small modules with one primary responsibility. A module owns its contracts, implementation and composition extensions.
- Cross-project consumers depend on contracts under the owning project's `Abstractions/` namespace, not on its infrastructure implementation.
- `Infrastructure/` contains implementation details and external adapters. It must not become a second public contract surface.
- New project references must follow the direction from composition and presentation toward abstractions and domain behavior. Do not introduce reference cycles or use a service locator to bypass them.
- Concrete implementations are created by dependency injection at composition boundaries. Runtime-data objects may use explicit factories.

## Revit integration

- Revit API access stays behind narrow services or command/event boundaries so domain and presentation code can remain testable without a live Revit process.
- Keep services that use `RevitAPIUI` (`UIApplication`, `UIDocument`, `UIView`, `Selection`) as thin adapters over logic that depends only on `RevitAPI` or plain data whenever that logic is substantial enough to test. Do not let a UI-only call make document operations, selection rules, state calculation or restoration logic unavailable to `Nice3point.TUnit.Revit` tests.
- Work that modifies a Revit document runs in an explicit transaction. UI-triggered asynchronous work enters the Revit API through the repository's external-event infrastructure.
- Version differences are isolated in compatibility extensions or adapters. Conditional compilation belongs inside those boundaries rather than at call sites.
- Code that needs live Revit objects is tested in `*.RevitTests` with `Nice3point.TUnit.Revit`. Headless behavior stays in `*.Tests`.

## Presentation

- Views contain layout and view-only mechanics. View models own state and commands; application and diagnostic behavior lives in services.
- Reusable WPF mechanics live in `Revit.Linter.Behaviors`. Prefer bindings, converters, commands and behaviors over event handlers.
- `Revit.Linter.Presentation.ViewLocator` resolves embedded views from dependency injection only at module composition boundaries; it is not a general service locator.
- Shared styles and resources are defined once and consumed through resource references. Theme-dependent values use `DynamicResource`.

## Diagnostics and reports

- Diagnostic projects discover or calculate findings. Presenters render and interact with them; providers transport or retain report state. These roles remain separate.
- `Revit.Linter.ReportMessaging` owns the shared message-template contract and typed message view. Feature modules supply data and element-link adapters rather than duplicating parsing or rendering.
- Configuration is validated when it is loaded, not when a diagnostic runs. A module skips an invalid rule,
  keeps the valid ones, and describes the skipped rules through `IDiagnosticConfigurationErrorSource`;
  the application shows them once per distinct description. Validation logic stays free of Revit API types
  so it is covered by headless tests; Revit-version knowledge is passed in from the compatibility boundary.
- Diagnostics read the transaction cache only through `Revit.Linter.DocumentQueries`. Queries that several
  modules need (all elements, elements of a class, element types, element geometry) are methods of
  `IDocumentQueryService`, so one query has exactly one key. A module-specific derived value uses
  `GetOrCreate` with a `DocumentQueryKey` whose query name starts with the module name. Every key identifies
  the document, and the view when the result depends on it; a key never relies on an element identifier or a
  document title alone.
- Diagnostic codes and serialized configuration contracts are stable user-facing identifiers. Change them only with an explicit migration or compatibility decision.

## Element highlighting

- `Revit.Linter.ElementAccentor` owns atomic, reversible Revit view and selection operations.
- `Revit.Linter.ElementVisualization` composes those operations into diagnostic visualization pipelines.
- Every visualization session records enough state to restore what it changed. Composite sessions restore steps in reverse order.

## Element fixing

- `Revit.Linter.ElementFixing` owns configuration-driven element fix pipeline composition.
- Fix pipelines execute ordered destructive steps inside the transaction supplied by the fix presenter.
- Step implementations remain independent of user-diagnostic configuration parsing and presentation.

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

- The root solution builds the add-in and tests. `installer/`, `build/`, `updater/` and `sandbox/` remain separate solutions with their own responsibilities.
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
