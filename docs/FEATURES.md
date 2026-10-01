# Product scope

This document is the concise contract for the product's current scope. Detailed user instructions live in the bilingual [Wiki](../wiki/Home.md); implementation constraints live in [Architecture decisions](architecture.md).

## Included

- Run built-in and YAML-configured diagnostics against Revit projects and families.
- Present findings by severity, diagnostic code and target type, with typed element links and localized messages.
- Apply supported fixes through Revit-safe transactions and report their results.
- Ignore supported findings and react to relevant element and document changes.
- Highlight diagnostic targets through reversible, composable visualization pipelines.
- Export the displayed findings as a self-contained HTML report.
- Configure paths, diagnostic rules, formulas, filters and visualization behavior.
- Provide English and Russian UI resources, theme-aware WPF presentation and structured diagnostic logs.
- Package the supported Revit 2021, 2023 and 2025 variants and the shared updater as one per-user MSI release.

## Compatibility contract

- Serialized diagnostic codes, configuration keys and message placeholders are user-facing contracts.
- Revit-version-specific API differences stay behind compatibility boundaries.
- Headless logic must remain usable without a running Revit process; operations on live Revit objects use the dedicated Revit test infrastructure.

## Outside the current scope

- Running hosted Revit integration tests in GitHub Actions.
- Merging localization satellite assemblies into the main add-in assembly.
- Treating the sandbox and benchmarks as part of the production add-in or its release payload.

When product behavior changes, update this summary and the affected Wiki pages in the same change.
