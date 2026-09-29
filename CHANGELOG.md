# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Document stable architecture decisions, current product scope, module abstraction layout, WPF composition, public API documentation, warning hygiene and structured logging policy.
- Add class-based element diagnostic visualization pipelines with ordered YAML-configurable steps, semantic element sets, restorable view state, full graphic styles, element/filter application strategies, and a compact Show action in diagnostic reports.
- Export the currently displayed diagnostic findings as a self-contained HTML report with severity and diagnostic-code summaries.
- Verify localization key and placeholder parity, source resource registration, satellite assemblies, and culture fallback in final ILRepack outputs before creating installers.

### Changed

- Generate XML documentation for production projects, exempt test projects, and provide an opt-in strict build that treats undocumented public APIs as errors while the existing API baseline is documented.
- Separate atomic element accent operations from visualization pipeline orchestration, graphics overrides, and rollback state.
- Document the planned class-based diagnostic visualizations, internal reusable pipelines, safe highlighting session, custom-rule registration, and compact Show action.
- Declare English as the neutral resource language, use a parent-culture `ru` satellite for all Russian regional cultures, and resolve global, view-model, and feature strings through the same localization reader.

### Fixed

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
