# Product scope

The concise contract for the product's current scope. Detailed user instructions live in the bilingual [Wiki](../wiki/Home.md); implementation constraints live in [Architecture decisions](architecture.md).

## Included

- Run built-in and YAML-configured diagnostics against Revit projects and families.
- Present findings by severity, diagnostic code, target type and tags, with typed element links and localized messages; clicking a tag searches for it.
- Apply configured fixes through Revit-safe transactions and report their results: a fix pipeline can delete its elements or write a parameter value computed by the formula language.
- Ignore a finding for its element or for the whole document, review and remove entries in the built-in **Ignore list** pane, and keep the list inside the model.
- Choose which data columns each of the four tables shows from a tool bar button, with the choice remembered per user across Revit versions; the row action buttons always stay visible, at least one data column stays visible, the current sort survives hiding its column, and a guided step shows a hidden column only for the duration of that step.
- Highlight diagnostic targets through reversible, composable visualization pipelines; a built-in finding whose target is a view, a sheet included, opens that view instead of the shared 3D visualization view.
- Colour the findings of the active document in the 3D visualization view as a heat map: the hue shows the most severe check of each element and the brightness the number of distinct failed checks of that severity; the view uses fine detail and a shaded display, the rest of the model is 99% transparent with its lines fading into the background, and levels, scope boxes, grids and untargeted linked models are hidden, so the findings stand out.
- Export the displayed findings as a self-contained HTML report or as CSV, JSON and YAML data, including each finding's tags.
- Configure every rule in one `diagnostics.config.yaml` per Revit version: `type` selects the module, `builtIn` entries override the activity, severity and visibility of product checks, and `isHidden` removes a diagnostic from the interface, execution and reports.
- Introduce the add-in on first start with a welcome wizard that can install one example configuration by discipline, together with optional project-parameter checks and per-parameter element fill checks generated from the ADSK Revit 2021 project templates; the wizard offers them on every supported Revit version and installs the parameter group spelling of the running version (Revit 2021-2027).
- Offer an optional observational practical tour in a dockable pane along the real workflow (model, diagnostics, findings, show, fix or ignore, export report, then export it again in another format); the tour never runs diagnostics, modifies the model, or applies fixes by itself.
- Back the tour with a managed TOUR001 training check that is visible only while a tour session is active: from the selection step on the diagnostics pane shows that check alone, pausing hides it, returns the user's own checks with their states untouched and clears the training findings, and starting a tour restores the pane search, filters and sort order. Progress and opt-out are stored separately for each Revit version; a resumed tour continues at its run checkpoint so it always explains fresh findings.
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
