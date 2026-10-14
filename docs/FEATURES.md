# Product scope

This document is the concise contract for the product's current scope. Detailed user instructions live in the bilingual [Wiki](../wiki/Home.md); implementation constraints live in [Architecture decisions](architecture.md).

## Included

- Run built-in and YAML-configured diagnostics against Revit projects and families.
- Present findings by severity, diagnostic code and target type, with typed element links and localized messages.
- Apply supported fixes through Revit-safe transactions and report their results.
- Ignore a finding for the element it affects or for the whole document, review and remove the entries in the built-in **Ignore list** pane, and keep the list inside the model itself.
- Highlight diagnostic targets through reversible, composable visualization pipelines.
- Export the displayed findings as a self-contained HTML report or as CSV, JSON and YAML data.
- Configure paths, diagnostic rules, formulas, filters and visualization behavior.
- Introduce the add-in on first start with a welcome wizard that can install example configurations by discipline.
- Offer an optional observational practical tour in a dockable pane along the real workflow (model, diagnostics, findings, show, fix or ignore, export report, then export it again in another format); the tour never runs diagnostics, modifies the model, or applies fixes by itself.
- Back the tour with a managed TOUR001 training check that is installed and refreshed on start and removed on completion or opt-out; progress and opt-out are stored separately for each Revit version and the tour resumes from the ribbon.
- Show the tour pane only while a tour session is active; after opt-out, completion, or refusal it stays hidden instead of appearing empty.
- Offer disposable copies of the official Autodesk samples of the running Revit version as tour starting points, with a ribbon fallback when automatic opening fails.
- Check for new stable releases through the standalone updater at sign-in or on demand, and announce them with a Windows notification offering a verified download of the release MSI.
- Provide English and Russian UI resources, theme-aware WPF presentation and structured diagnostic logs.
- Package the supported Revit 2021, 2023 and 2025 variants and the shared updater as one per-user MSI release.

## Compatibility contract

- Serialized diagnostic codes, configuration keys and message placeholders are user-facing contracts.
- The in-document ignore list is persisted user data with its own schema version; a list written by a newer product version is read as empty instead of being reinterpreted.
- Revit-version-specific API differences stay behind compatibility boundaries.
- Headless logic must remain usable without a running Revit process; operations on live Revit objects use the dedicated Revit test infrastructure.

## Outside the current scope

- Running hosted Revit integration tests in GitHub Actions.
- Merging localization satellite assemblies into the main add-in assembly.
- Treating the sandbox and benchmarks as part of the production add-in or its release payload.

When product behavior changes, update this summary and the affected Wiki pages in the same change.
