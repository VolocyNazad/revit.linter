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
- A failure is logged once at the layer that understands its meaning. Higher layers may translate it into user-visible feedback without logging it again.
- Logs contain useful model, document, diagnostic and operation context, but must not contain secrets or unnecessarily large element payloads.

## Packaging and releases

- The root solution builds the add-in and tests. `installer/`, `build/` and `sandbox/` remain separate solutions with their own responsibilities.
- ILRepack output and localization satellites are validated before MSI creation.
- Stable releases originate from exactly one `vMAJOR.MINOR.PATCH` tag and are published through the repository release pipeline.
