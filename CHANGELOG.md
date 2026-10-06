# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

## [2.0.0] - 2026-10-06

### Added

- Show a first-run welcome wizard: what the add-in does and changes in a document, built-in diagnostics versus user-defined YAML rules with a read-only syntax-highlighted example of the main rule fields, optional example configurations per discipline (MEP systems, architecture, structure), the optional practical tour, and links to the quick start, the documentation and support. The new **Getting started** ribbon command opens it again. Installing examples never overwrites a configuration file that already contains rules: such an example is written to the `examples` subfolder, whose rules are marked as examples in the diagnostics list.
- Guide an optional modeless practical tour through the real diagnostics workflow: open a document (or a disposable copy of an Autodesk sample), select and run a diagnostic, inspect findings, visualize elements, apply a fix and export the report. The tour runs in a right-side dockable pane with a ribbon resume command, highlights the relevant controls, advances only on observed user actions without taking ownership of them, skips finding-only steps after a clean run, and keeps resumable progress separately for each Revit version, with restart, opt-out and reset from **Getting started**. A dedicated `TOUR001` training diagnostic with the visualizations and fix menu required by the walkthrough is installed for the tour, marked with a tour badge and pinned at the top of the diagnostics list.
- Step through the findings of the diagnostic report: the new previous and next buttons of the tool bar select the neighbouring row and show it in the active view, with the position among the shown rows between them, such as "3 / 47". In the table, **Up** and **Down** select a row and **Enter** shows it. Stepping repeats the visualization that was used last and skips rows that cannot be shown.
- Control the add-in dialogs from the keyboard: **Enter** presses the default button (Close, the confirming button, or Next and Finish in the welcome window), **Esc** closes the dialog, and **Alt+Left**/**Alt+Right** or **Page Up**/**Page Down** move between the steps of the welcome window. The default button has the keyboard focus when a dialog opens.
- Open the documentation from the interface: **F1** over a ribbon button opens its Wiki page, and the new question-mark button in a diagnostic report row opens the page that describes the finding's diagnostic. Pages open in English or Russian following the Revit language.
- Show ribbon-like tooltips on the pane buttons (run, pause, active-view option, export, more filters, fix, show): a title, a description and, where it applies, the duration of the last run. Disabled buttons explain why they are unavailable, and **F1** over a tooltip opens the Wiki page of the pane. The document list, the search boxes, the **Active** and severity column headers and the bulk selection menu of the diagnostics pane are explained the same way: what the search covers, that your changes override the rule and are saved, and how "all" differs from "visible".
- Open the rule configuration from its diagnostics row.
- Validate `parameter-element.config.yaml` when it is loaded: a rule with an unknown category, a group that is not valid for the running Revit version, an invalid GUID or a missing field is skipped with a message naming the rule, the parameter and the value, while the other rules stay in use.
- Isolate failing diagnostics: a diagnostic that throws is logged and shown in the report as an error under the diagnostic's code, and the remaining diagnostics still produce their results.

### Changed

- **Breaking:** a collision finding now lists every element the target intersects instead of the first one found. The message variables `{intersection.elementName}` and `{intersection.elementId}` are replaced by the comma-separated lists `{intersection.elementNames}` and `{intersection.elementIds}`, with the new `{intersection.count}` giving their number; update the `message` of existing rules in `collision.config.yaml`. Finding visualizations now cover all intersecting elements.
- Speed up diagnostic runs: built-in, parameter and collision diagnostics, the diagnostic service and the dependency functions used in formulas now share one cached result for the same document query (all elements, elements of a class, element types, element geometry) instead of collecting it separately. The ignore list is consulted only for the elements a rule applies to and not at all in a document that has no ignore parameters; findings are handed to the report as one batch per diagnostic, and the fixes and visualizations of a report row are prepared when the row is first shown or used instead of for every finding up front. On the Snowdon Towers sample model (19 412 elements, 35 active diagnostics) a full run dropped from about 22 s to under 4 s.
- Write the start and the result of each full diagnostic run to the log (duration, memory, garbage collections), with a timing breakdown for every diagnostic that takes 100 ms or longer.
- Draw every control with the add-in's own flat templates instead of Material Design: square buttons without ripple animations, outlined dialog buttons and input fields, plain check boxes, radio buttons, menus and tool tips. The Material Design libraries are no longer installed with the add-in, which removes a source of version conflicts with other add-ins.
- Color the panes and windows like the Revit interface in both the light and the dark theme: background, text, lines, selection and scroll bars use the colors of the Revit palettes, and the accent color is the Autodesk blue instead of cyan. Warning codes use a darker orange in the light theme so that they stay readable. The panes are denser and sit closer to the Revit interface: 12 px Segoe UI text, 16 px icons, smaller tool bar buttons and tighter table rows, with the search hint inside the field.
- Fit the tables of the panes to the pane width: the message column of the report and fix panes and the description column of the diagnostics pane take the room left by the other columns and wrap their text, so the table no longer needs horizontal scrolling. Row buttons use the text color and stay dimmed until the row is pointed at or selected, the fix button is shown only for findings that have a fix, and a finding's code is colored by its severity (orange for a warning, red for an error) instead of a separate severity column. Pane filters, the report document choice and the export button moved into the filter-button list, and the **On active view** option is now an icon button that stays pressed while on.
- Run diagnostic visualizations in a reusable `Revit Linter — Visualization` 3D view so crop and combined pipelines behave consistently instead of being redirected to a plan view.
- Activate the **Warnings** pane as soon as a diagnostic run starts, so the previous report is cleared and new findings appear in front of the user.
- Configure the project ignore-list parameters silently when a document is opened: no success dialog, and no transaction when the bindings are already correct. A failed setup is logged and still reported once; an empty transaction is rolled back instead of committed.
- Move the configuration folder button from the ribbon to the Diagnostics pane toolbar.
- Replace the Wiki configuration examples with rules that are usable as shipped: nine collision rules (three per discipline), six element checks, and project parameter rules built from the ADSK shared parameter file. The examples are available with English and Russian message texts and reference parameters by `BuiltInParameter` name; the welcome wizard installs these same files. The project parameter template uses the Revit 2024+ `group` identifier with a note for Revit 2021–2023, and the collision template uses the new `intersection` message variables.
- Reshape the English and Russian getting-started Wiki around user tasks: choosing built-in diagnostics or YAML rules, creating a first rule, following the practical tour, and starting with or without an open document or Autodesk sample.
- Store per-user settings in `%LOCALAPPDATA%\Volocy\Revit.Linter\settings\`.

### Fixed

- Turning a visualization off no longer fails with "Failed to restore the previous visualization state" when Revit cannot return the view to its previous zoom.
- A visualization started from a view that does not show the elements, such as a sheet, is now applied to the view Revit opens to show them instead of the view the user has left.
- Running diagnostics while a visualization is active no longer fails with "Failed to restore the previous visualization state": the run now executes in a Revit API context and restores the visualization before it starts.
- Report a project parameter that exists in the document but is not bound to categories instead of aborting the whole diagnostic run.

## [1.8.0] - 2026-10-01

### Added

- Check for new stable releases with a standalone updater that runs at Windows sign-in (by default at most once every 24 hours) and on demand from the new **Check for updates** ribbon command. New releases are announced with a Windows notification offering **Download**, **Release notes**, **Later** and **Skip**. **Download** fetches the exact release MSI, verifies its size and SHA-256 digest, and reveals it in Explorer without starting the installer.
- Let administrators control update checks, notifications, the check interval and the release API endpoint through machine or user policies under `Software\Policies\Volocy\Revit.Linter\Updater`.
- Add configurable visualization pipelines for element diagnostics: ordered YAML steps, named element sets and full graphic styles, applied from a compact **Show** action in the diagnostic report and restored afterwards.
- Add YAML-configurable fix pipelines for user diagnostics, starting with a `Delete` step and named `elementSets` that select the target and, explicitly, its dependencies.
- Export the displayed diagnostic findings as a self-contained HTML report with severity and diagnostic-code summaries.
- Add ribbon commands that open the Revit Linter support page and GitHub Sponsors.

### Changed

- Replace the separate per-Revit-version installers with one per-user MSI for Revit 2021, 2023 and 2025. It installs under `%LOCALAPPDATA%\Programs\VolocyNazad\Revit.Linter\`, preselects the Revit versions installed on the computer while letting the user change the selection, refuses to install, modify or remove the add-in while Revit is running, and removes older per-version installations on first install.
- Write JSON and YAML report exports as a versioned document with Revit and add-in metadata, active filters, result counts, stable severity values, obsolete-result details and structured target and dependency element IDs; CSV and HTML exports use the same data.
- Run a diagnostic fix by left-clicking its button in the report; when several fixes are available, the button opens a menu to choose one.
- Complete the English and Russian localization of built-in diagnostics, report filters, document filters, fixes, transactions and parameter diagnostic details, and use Russian for every Russian regional culture.
- Show the Revit ribbon tab as **Diagnostics** / **Диагностика** instead of the vendor name.
- Store add-in logs under `%LOCALAPPDATA%\Volocy\Revit.Linter\logs\` instead of a single unbounded `logs.txt` next to the add-in. Logs roll daily and at 20 MB, keep the newest 14 files, no longer record the machine name, include add-in startup, shutdown and fatal startup failures, and are flushed when Revit closes.
- Expand the English and Russian Wiki with report actions, visualizations, fixes, configuration errors, log locations and troubleshooting, and update the example configurations to match.

### Fixed

- Repairing or modifying the installation no longer removes the Revit add-in manifest.
- Invalid user-diagnostic YAML no longer breaks diagnostics loading: the user is notified and the file is treated as empty.
- Check project-parameter groups correctly in Revit 2023 and earlier, where numeric IDs and `PG_*` names were parsed the wrong way round.
- Resolve the insulation, lining and scope-box dependency operands in formulas in Revit 2023 and earlier instead of failing on the category check.
- Changing report filters no longer fails after a report row has entered edit mode.
- Wrap long messages in informational and confirmation dialogs and scroll them instead of clipping.
- Fix broken formula-reference links in the Wiki.

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
