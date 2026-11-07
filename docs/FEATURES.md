# Product scope

The concise contract for the product's current scope. Detailed user instructions live in the bilingual [Wiki](../wiki/Home.md); implementation constraints live in [Architecture decisions](architecture.md).

## Included

- Run built-in and YAML-configured diagnostics against Revit projects and families.
- Present findings by severity, diagnostic code, target type and tags, with typed element links and localized messages; clicking a tag searches for it.
- Apply configured fixes through Revit-safe transactions and report their results: a fix pipeline can delete its elements or write a parameter value computed by the formula language.
- Ignore a finding for its element or for the whole document, review and remove entries in the built-in **Ignore list** pane, and keep the list inside the model.
- Highlight diagnostic targets through reversible, composable visualization pipelines.
- Export the displayed findings as a self-contained HTML report or as CSV, JSON and YAML data, including each finding's tags.
- Configure every rule in one `diagnostics.config.yaml` per Revit version: `type` selects the module, `builtIn` entries override the activity, severity and visibility of product checks, and `isHidden` removes a diagnostic from the interface, execution and reports.
- Introduce the add-in on first start with a welcome wizard that can install one example configuration by discipline.
- Offer an optional observational practical tour in a dockable pane along the real workflow (model, diagnostics, findings, show, fix or ignore, export report, then export it again in another format); the tour never runs diagnostics, modifies the model, or applies fixes by itself.
- Back the tour with a managed TOUR001 training check installed and refreshed on start and removed on completion or opt-out; from the selection step on the diagnostics pane shows that check alone while the user's own checks and their states stay untouched. Progress and opt-out are stored separately for each Revit version and the tour resumes from the ribbon.
- Show the tour pane only while a tour session is active; after opt-out, completion or refusal it stays hidden.
- Offer disposable copies of the official Autodesk samples of the running Revit version as tour starting points, with a ribbon fallback when automatic opening fails.
- Check for new stable releases through the standalone updater at sign-in or on demand, and announce them with a Windows notification offering a verified download of the release MSI.
- Provide English and Russian UI resources, theme-aware WPF presentation and structured diagnostic logs.
- Package the supported Revit 2021–2027 variants and the shared updater as one per-user MSI release.

## Compatibility contract

- Serialized diagnostic codes, configuration keys and message placeholders are user-facing contracts.
- Diagnostic configuration uses one `diagnostics.config.yaml` per Revit version with a `type` discriminator; at startup the legacy split files of every Revit version are moved into a `legacy` folder and never read again, one warning per product version links to the migration guide, there is no persistent banner or per-version notice, and nothing is deleted.
- The managed practical-tour file is refreshed automatically when it is not in the current format, and installing examples backs up an existing, different `examples\diagnostics.config.yaml` to `.bak-<timestamp>` before replacing it; an identical file is left untouched.
- The in-document ignore list is persisted user data with its own schema version; a list written by a newer product version is read as empty instead of being reinterpreted.
- Revit-version-specific API differences stay behind compatibility boundaries.
- Headless logic must remain usable without a running Revit process; operations on live Revit objects use the dedicated Revit test infrastructure.

## Outside the current scope

- Running hosted Revit integration tests in GitHub Actions.
- Merging localization satellite assemblies into the main add-in assembly.
- Treating the sandbox and benchmarks as part of the production add-in or its release payload.

When product behavior changes, update this summary and the affected Wiki pages in the same change.
