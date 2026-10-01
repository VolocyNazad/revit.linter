# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed

- Ignore the root build-artifact output directory so local packaging does not dirty the Git worktree.
- Convert prerelease GitVersion values to the numeric `major.minor.patch` required by Windows Installer while retaining full semantic versions in application assemblies.
- Run post-publish localization artifact checks through the xUnit v3 executable so Microsoft.Testing.Platform cannot incorrectly fail the build with zero discovered tests.
- Repair malformed Wiki table links, point formula references at their complete pages, reject unmatched brackets or invalid explicit Wiki paths during publication, and support absolute staging paths.
- Wrap long informational and confirmation dialog messages within a stable width and provide vertical scrolling instead of clipping oversized content.
- Harden updater HTTP transport with bounded GitHub API responses, a pinned API version, explicit connection and operation timeouts, and validated HTTPS download redirects.
- Log why a GitHub release installer asset was rejected instead of silently falling back to the release page.
- Propagate caller-requested updater cancellation while continuing to report HTTP timeouts as failed checks.
- Map updater projects to the solution's `x64` platform so Visual Studio resolves every versioned build configuration correctly.

### Added

- Download the exact MSI asset after an explicit notification action, verify its size and GitHub-provided SHA-256 digest, and reveal it in Explorer without launching the installer.
- Resolve read-only machine and user updater policies for checks, notifications, interval, and release API endpoint ahead of user settings and built-in defaults.
- Show a Windows app notification for new releases with download, release-notes, later, and skip actions, validated activation arguments, and a silent fallback when notifications are unavailable.
- Coordinate updater processes through named per-user synchronization objects so concurrent manual checks reuse or activate the running updater.
- Detect installed Revit versions in the unified MSI, preselect their optional features, and let users adjust the component selection.
- Build the updater as a versioned self-contained `win-x64` application before installer creation.
- Add the Revit-independent updater foundation with stable GitHub release discovery, persisted per-user state, 24-hour scheduling, manual checks, skip behavior, and headless tests.
- Add a compact Revit ribbon command that delegates manual update checks to the standalone updater.
- Add ribbon commands for opening Revit Linter support and GitHub Sponsors pages.
- Cover project-parameter diagnostics with live Revit tests for valid definitions, missing parameters, and every checked binding property.
- Add named `elementSets` selection to configured fix steps, with target-only defaults and explicit dependency support.
- Add YAML-configurable user diagnostic fix pipelines with an initial `Delete` step.
- Document stable architecture decisions, current product scope, module abstraction layout, WPF composition, public API documentation, warning hygiene and structured logging policy.
- Add a versioned diagnostic-report export document with Revit/add-in metadata, active filters, result counts, stable severity values, obsolete-result details, and structured target/dependency element IDs.
- Add class-based element diagnostic visualization pipelines with ordered YAML-configurable steps, semantic element sets, restorable view state, full graphic styles, element/filter application strategies, and a compact Show action in diagnostic reports.
- Export the currently displayed diagnostic findings as a self-contained HTML report with severity and diagnostic-code summaries.
- Verify localization key and placeholder parity, source resource registration, satellite assemblies, and culture fallback in final ILRepack outputs before creating installers.
- Validate localized `.resx` tables during regular builds with `VolocyNazad.ResxAnalyzer`.

### Changed

- Replace and migrate separate Revit-version installers with one per-user MSI containing all supported builds and the shared sign-in updater.
- Document the testability boundary between `RevitAPI` document logic and thin `RevitAPIUI` adapters.
- Document build-time localization validation and require nearby remarks for non-obvious API behavior.
- Expand the English and Russian Wiki with report-action semantics, visualization and fix caveats, configuration-failure behavior, application-log locations, and troubleshooting guidance.
- Record the planned unified per-user installer and short-lived Windows update notifier architecture.
- Run the only available diagnostic fix by left-clicking its button and show a choice menu when several fixes are available.
- Keep all three working configuration files synchronized with tested documentation examples and complete their navigation links.
- Add a safe, narrowly targeted `Delete` fix-pipeline test rule to the example user-diagnostic configuration.
- Expand the collision configuration example with individual visualization-step samples and composed filter/crop/isolation pipelines.
- Export the complete versioned report document in JSON and YAML while keeping HTML and CSV rendering on the same shared contract.
- Generate XML documentation for production projects, exempt test projects, and provide an opt-in strict build that treats undocumented public APIs as errors while the existing API baseline is documented.
- Document the public diagnostic catalog, identity, verdict, registration, override, and service contracts in the core module.
- Document the public diagnostic-report contracts and dependency-injection entry point.
- Document the public fix-report contracts and dependency-injection entry point.
- Document the public element-ignore contracts and dependency-injection entry point.
- Document the public element-change monitor lifecycle contract and dependency-injection entry point.
- Document the base element-dependency definer extension-point contract.
- Document the public element-dependency composition and filtering definers.
- Document the public element type, family, and instance dependency definers.
- Document the public group membership and nested-component dependency definers.
- Document the public host, insert, and MEP insulation dependency definers.
- Document the public room, space, scope-box, connection, and identity dependency definers.
- Document discovery of built-in element dependency definer types.
- Document the shared localization lookup API and keep its assembly marker internal.
- Document the diagnostic execution, registration, and duplicate-code error contracts.
- Document the informational and confirmation dialog contracts, WPF views, and dependency-injection entry point.
- Document the diagnostic report presenter interaction contract.
- Document the public localization source-generator entry point.
- Document the public WPF theme-management contract and dependency-injection entry point.
- Document the project-parameter management contract and keep its collection helper internal.
- Document the version-specific configuration path, YAML loading, and file-change notification APIs.
- Document generated localization properties, opened-document selector models, the Revit event-aware view-model base, and its dependency-injection entry point.
- Keep localized resource enumeration warning-free while preserving specific-culture precedence.
- Document the diagnostic-run settings, WPF presentation models, Revit event-aware base, and dependency-injection entry point.
- Document the diagnostic-list WPF composition API and keep its initialization base internal.
- Document the dependency-injection-backed WPF view-locator markup extension.
- Remove the obsolete diagnostic-progress TODO that produced an analyzer warning without representing an active contract.
- Document the fix-report WPF composition API and keep its initialization base internal.
- Document the reusable WPF behavior that forwards nested report scrolling to its parent.
- Document the shared report-message parsing, link, text-part, and WPF rendering contracts.
- Document the element and document diagnostic composition entry points.
- Document the diagnostic-report WPF composition API, keep its initialization base internal, and remove an unused debug converter.
- Remove obsolete diagnostic-report presenter warnings without changing filtering, fixing, or transaction behavior.
- Use the existing category-ID compatibility helper in curve diagnostics across Revit 2021–2027.
- Limit the formula-language public API to documented grammar profile factories.
- Limit collision diagnostic composition to its documented dependency-injection entry point.
- Limit user-defined diagnostic composition to its documented dependency-injection entry point.
- Limit parameter element diagnostic composition to its documented dependency-injection entry point.
- Document the Revit application and command entry points and keep application infrastructure internal.
- Handle shutdown failures without duplicate exception propagation and document path-based add-in dependency loading.
- Avoid generating unused `System.Index` and `System.Range` compatibility types so ILRepack can merge .NET Framework builds without duplicate-type warnings.
- Document intentional path-based assembly loading and no-op command events in artifact and presentation tests.
- Use an explicit path-separator overload in localization integrity tests to keep analyzer output unambiguous.
- Document the public element-change notification contracts and dependency-injection entry point.
- Reduce the element accent module's public surface to its contracts and documented dependency-injection entry point.
- Separate atomic element accent operations from visualization pipeline orchestration, graphics overrides, and rollback state.
- Document the planned branching diagnostic report history and file-based storage design.
- Document the planned class-based diagnostic visualizations, internal reusable pipelines, safe highlighting session, custom-rule registration, and compact Show action.
- Declare English as the neutral resource language, use a parent-culture `ru` satellite for all Russian regional cultures, and resolve global, view-model, and feature strings through the same localization reader.

### Fixed

- Stop the updater before installer file changes and refuse installation, modification or removal while Revit is running.
- Remove Revit manifests only during a real uninstall, not during installer repair or upgrade.
- Use the explicit `ViewLocatorExtension` type name so Visual Studio's XAML language service resolves the shared markup extension.
- Keep the diagnostic report grid out of WPF edit mode so changing filters can safely refresh its collection view.
- Include the rejected formula in user-diagnostic compilation warnings.
- Parse project-parameter groups as `PG_*` names or numeric IDs through Revit 2023 and as full `ForgeTypeId` values in Revit 2024 and newer.

- Keep the safe `FIXTEST001` sample narrow by evaluating its parameter condition in `check`, where parameter access is supported.
- Notify users about invalid user-diagnostic YAML and load it as an empty configuration instead of retaining stale rules or failing add-in startup.
- Calculate 3D visualization section boxes in model coordinates, use a stable millimetre offset, and avoid temporary model elements while cropping views.
- Keep diagnostic visualizations active when their own Revit transactions raise `DocumentChanged`, log the event that requests restoration, and use the correct temporary-visibility capability check for isolation.
- Support both 32-bit and 64-bit `BuiltInCategory` enum representations and make ignore-feedback tests independent of the Revit UI language.
- Supply the visualization-pipeline dependency in the built-in diagnostic registration integration test and verify every registration exposes a pipeline.
- Read UTF-8 repository documents and solution files consistently in the repository validator, including under Windows PowerShell 5.1.
- Apply visualization pipelines through a transaction group, retain failed restoration sessions for retry, and defer restoration triggered by Revit events to a modifiable API context.
- Restore the application project's theme-service import and remove empty global-using files.
- Store add-in logs under `%LOCALAPPDATA%` without the machine-name property, bound them to the newest 14 daily or 20 MB files, keep Console/Debug sinks development-only, record startup and shutdown lifecycle events, capture fatal lifecycle failures, and dispose the logging host during Revit shutdown so buffered events are flushed.
- Localize built-in diagnostics, report filters, selected severity and target labels, document filters, fixes, transactions, ignore-parameter feedback, and parameter diagnostic details in English and Russian.

## [1.7.0] - 2026-09-23

### Added

- Export diagnostic reports for further analysis and sharing.
- Filter the diagnostics list by target type using interactive filter chips.
- Detect Revit warnings, unused materials, and unused profile family types.
- Notify users when a diagnostic formula cannot be compiled.
- Enrich application logs with Revit application, document, model, and add-in context.
- Publish task-oriented user documentation in English and Russian for both Obsidian and GitHub Wiki.
- Provide a local Revit sandbox launcher and benchmarks for collision detection and spatial indexing.

### Changed

- Improve collision diagnostics with spatial indexing and support configuration that allows values to vary between groups.
- Present Revit warnings as document diagnostics alongside the other linting results.
- Replace diagnostic grouping with target-type filters and use theme-aware colors throughout report panels.
- Render diagnostic and fix messages through a shared parser with typed element links and cached message content.
- Compose shared views through dependency injection and a centralized view locator.
- Support the configured Revit 2021, 2023, and 2025 build targets in local and CI builds.
- Separate headless tests from tests that require a running Revit process, and add repository validation to CI.

### Fixed

- Correct connector connection checks and avoid reporting connected connectors as disconnected.
- Delay Revit external-event work until the application is ready to process it.
- Preserve unknown message placeholders, support escaped braces, and handle element identifiers correctly on Revit 2024 and later.
- Use the active Material Design palette for panel backgrounds in both light and dark themes.
- Publish Wiki links with the correct targets when source pages use Obsidian aliases.
