# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Changed

- Replace the Wiki configuration examples with rules that are usable as shipped: nine collision rules (three per discipline), six element checks, and project parameter rules built from the ADSK shared parameter file. The examples are available with English and Russian message texts and reference parameters by `BuiltInParameter` name.
- Correct the ribbon tab name in the README and the Wiki: the commands are on the **Diagnostics** tab.

- Speed up diagnostic runs: built-in, parameter and collision diagnostics, the diagnostic service and the dependency functions used in formulas now share one cached result for the same document query (all elements, elements of a class, element types, element geometry) instead of collecting it separately.
- **Breaking:** a collision finding now lists every element the target intersects instead of the first one found. The message variables `{intersection.elementName}` and `{intersection.elementId}` are replaced by the comma-separated lists `{intersection.elementNames}` and `{intersection.elementIds}`, with the new `{intersection.count}` giving their number; update the `message` of existing rules in `collision.config.yaml`. The `Dependencies` element set of visualizations contains all intersecting elements.
- Speed up runs with many active diagnostics: the ignore list is consulted only for the elements a rule applies to, the ignore parameter of an element is read once per run instead of once per diagnostic, it is not read at all in a document that has no ignore parameters, and the ignore information of the document is resolved once per run.
- Speed up the unused parameter diagnostic: the parameters present in the document are collected once per run from one representative element per category, type and class, instead of asking every element about every parameter.
- Speed up collision diagnostics: candidates from the spatial index are pre-checked against bounds stored in the index, so elements that are candidates of every query (long pipes, ducts) no longer cost a cached bounding-box lookup each, and the `groupBy` formula is evaluated once per element instead of twice.
- Update the Wiki configuration examples: the project parameter template uses the Revit 2024+ `group` identifier with a note for Revit 2021–2023, and the collision template uses the new `intersection` message variables.
- Hand the findings of each element diagnostic to the report as one batch during a full run instead of one notification per finding; findings produced before a diagnostic fails are still delivered. The fixes and visualizations of a report row are now prepared when the row is first shown or used instead of for every finding up front.
- Clear the previous findings of a document from the diagnostic report in one operation instead of one by one, so a repeated run on a large report is not delayed by the cleanup.
- Write the start and the result of each full diagnostic run to the log (duration, memory, garbage collections), with a timing breakdown for every diagnostic that takes 100 ms or longer: elements visited, ignored and checked, findings, and the time spent in the rule filter, the ignore list, checking and publishing findings. Faster diagnostics, cache hits and misses per document query, and report clearing and refreshing are written at the `Debug` level.
- A diagnostic that fails no longer stops the others: the failure appears in the report as an error under the diagnostic's code, and the remaining diagnostics still produce their results.
- Validate `parameter-element.config.yaml` when it is loaded: a rule with an unknown category, a group that is not valid for the running Revit version, an invalid GUID or a missing field is skipped with a message naming the rule, the parameter and the value, while the other rules stay in use.

### Fixed

- Turning a visualization off no longer fails with "Failed to restore the previous visualization state" when Revit cannot return the view to its previous zoom.
- A visualization started from a view that does not show the elements, such as a sheet, is now applied to the view Revit opens to show them instead of the view the user has left.
- Running diagnostics while a visualization is active no longer fails with "Failed to restore the previous visualization state": the run now executes in a Revit API context and restores the visualization before it starts.
- Collision rules with different `andTake` formulas no longer check each other's elements; rules with the same formulas share the collected elements.
- Cached diagnostic data is no longer mixed between open documents with the same title or the same element IDs, or between a run on the active view and a run on the whole document.
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
