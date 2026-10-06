# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed

- Keep localized table headers, diagnostic codes, severities, document names and timestamps from being clipped by undersized columns.
- Focus the practical-tour activation step on the `TOUR001` checkbox instead of highlighting the entire diagnostics table.
- Explain and automatically skip the visualization-choice tour task when the selected finding offers only one visualization.
- Clarify where every practical-tour action is located in the Revit ribbon and diagnostic panes.
- Restore the startup welcome wizard by keeping practical-tour orchestration out of the welcome host dependency graph.
- Practical-tour prompts now size to their content and leave enough room for localized button labels.
- Turning a visualization off no longer fails with "Failed to restore the previous visualization state" when Revit cannot return the view to its previous zoom.
- A visualization started from a view that does not show the elements, such as a sheet, is now applied to the view Revit opens to show them instead of the view the user has left.
- Running diagnostics while a visualization is active no longer fails with "Failed to restore the previous visualization state": the run now executes in a Revit API context and restores the visualization before it starts.
- Report a project parameter that exists in the document but is not bound to categories instead of aborting the whole diagnostic run.

### Added

- Show a localized success notification after the practical tour is completed, and write the completion to the log.
- Install a dedicated `TOUR001` diagnostic for the practical tour, with the visualizations and fix menu required by every guided task.
- Extend the practical tour with tasks for opening the custom-configuration folder, selecting a visualization from a finding row's menu, stepping through findings, using search and filters, opening the Fix list pane, and applying the fix.
- Expand the installed element-rule examples with every visualization step and practical combined pipelines for close inspection, distraction-free review, and coordination.
- Host the practical tour in a right-side Revit dockable pane and provide a ribbon command for resuming it from the saved step.
- Make the welcome step labels clickable, hide technical sample metadata, and automatically open the prepared Autodesk copy in the current Revit instance.
- Guide an optional modeless practical tour through the real diagnostics workflow. The tour observes document opening, diagnostic selection and execution, finding inspection, element visualization, available fixes and successful report export without taking ownership of those actions; it highlights the relevant controls inside Revit Linter panels, skips finding-only steps after a clean run, and keeps resumable progress separately for each Revit version. Users can continue later, opt out, or reset the tour from **Getting started**.
- Prepare every Autodesk tutorial model in a uniquely owned temporary session, delete its copy only after the document closes, and safely clean abandoned sessions while leaving copies used by another Revit process alone.
- Explain built-in diagnostics and user-defined YAML rules in a new welcome step, including a localized read-only example that identifies the purpose of the main rule fields.
- Show read-only, syntax-highlighted examples for element, collision and project-parameter YAML configuration files in the welcome wizard.
- Load rules placed in the `examples` configuration subfolder, mark them as examples in the diagnostics list, and show an animated in-window confirmation after installation.
- Step through the findings of the diagnostic report: the new previous and next buttons of the tool bar select the neighbouring row and show it in the active view, with the position among the shown rows between them, such as "3 / 47". In the table, **Up** and **Down** select a row and **Enter** shows it. Stepping repeats the visualization that was used last and skips rows that cannot be shown.
- Control the add-in dialogs from the keyboard: **Enter** presses the default button (Close, the confirming button, or Next and Finish in the welcome window), **Esc** closes the dialog, and **Alt+Left**/**Alt+Right** or **Page Up**/**Page Down** move between the steps of the welcome window. The default button has the keyboard focus when a dialog opens.
- Show a welcome window the first time Revit starts with the add-in: what the add-in does and changes in a document, the available diagnostics, an offer to load example configurations for the selected disciplines (MEP systems, architecture, structure), the optional practical tour, and links to the quick start, the documentation and support. The new **Getting started** ribbon command opens it again. Loading examples never overwrites a configuration file that already contains rules: such an example is written to the `examples` subfolder.
- Open the documentation from the interface: **F1** over a ribbon button opens its Wiki page, and the new question-mark button in a diagnostic report row opens the page that describes the finding's diagnostic. Pages open in English or Russian following the Revit language.
- Show ribbon-like tooltips on the pane buttons (run, pause, active-view option, export, more filters, fix, show): a title, a description and, where it applies, the duration of the last run. Disabled buttons explain why they are unavailable, and **F1** over a tooltip opens the Wiki page of the pane. The document list, the search boxes, the **Active** and severity column headers and the bulk selection menu of the diagnostics pane are explained the same way: what the search covers, that your changes override the rule and are saved, and how "all" differs from "visible".

### Changed

- Keep the managed practical-tour configuration in the `tour` configuration subfolder instead of `examples/`, mark its rules with a tour badge, and pin them at the top of the Diagnostics pane. A superseded `examples/practical-tour.config.yaml` is removed when the tour starts, and the managed file is removed when the tour is completed or dismissed.
- Pulse only the relevant row during the practical tour instead of whole tables: the selected row's **Show** (eye) and fix (lightbulb) buttons, and the first `TOUR001` finding and fix rows.
- Breathe highlighted tour controls slightly: the glow pulse is joined by a subtle scale pulse, suppressed together when Windows client-area animations are off. Large surfaces such as the findings table keep the glow only.
- Align the practical-tour visualization instructions with the real report controls: **Show** opens a menu, and the second option is named in full.
- Add practical-tour steps for stepping through neighbouring findings, narrowing the Diagnostics pane with search and filters, and opening the Fix list pane.
- Apply the `TOUR001` fix as the practical-tour fix step instead of merely viewing the menu, so the Fix list pane has a result to show.
- Reset the Diagnostics search and filters when the practical tour is completed or dismissed.
- Open the rule configuration from its diagnostics row: the configuration step advances only on the `TOUR001` file, showing that every check is an editable YAML rule.
- Move the configuration folder button from the ribbon to the Diagnostics pane toolbar.
- Complete the Fix list tour step by selecting an applied fix; opening the pane still counts.
- Bring the Fix list pane forward on its practical-tour step.
- Keep the current pane on the practical-tour export step instead of switching to Warnings.
- Observe diagnostic selection changes made through bulk check actions, so the practical tour no longer misses them.
- Name the practical-tour visualization steps “Visualize” and “Visualize differently” (“Визуализировать” / “Визуализировать иначе”).
- List the user rule kinds (element, collision and project parameter checks) on the welcome diagnostics step.
- Switch the `TOUR001` check off when the practical tour starts fresh, so the user enables it as the guided step asks. Resumed tours keep the current selection.
- Remove the redundant welcome messages: the skipped-examples note on the practical-tour step, the closing summary on the finish step, and the configuration-folder and log-folder notes in "Good to know".
- Configure the project ignore-list parameters silently when a document is opened: no success dialog, and no transaction when the bindings are already correct. A failed setup is logged and still reported once; an empty transaction is rolled back instead of committed.
- Select the target element in the `Focus on the element` example visualizations (`config.yaml` and `practical-tour.config.yaml`, English and Russian).
- Run diagnostic visualizations in a reusable `Revit Linter — Visualization` 3D view so crop and combined pipelines behave consistently instead of being redirected to a plan view.
- Advance finding-specific practical-tour tasks only for `TOUR001`, so unrelated rules and warnings cannot desynchronize the walkthrough.
- Bring the pane needed by each practical-tour task to the foreground and keep the target control gently pulsing until the action is completed.
- Focus each practical-tour step with a high-contrast non-blocking spotlight, clearly dimmed surroundings and a localized next-step label.
- Explain the collision-specific and project-parameter-specific keys below their YAML examples in the welcome wizard.
- Activate the **Warnings** pane as soon as a diagnostic run starts, so the previous report is cleared and new findings appear in front of the user.
- Present the practical tour as a lightweight checklist with a highlighted active task and check marks for completed tasks, remove manual step skipping, and make applying the fix an explicit task.
- Simplify the practical-tour welcome step to one primary remark, with the selected next action and feedback shown as compact inline status text.
- Wrap the practical-tour ribbon label onto two lines so the button uses less horizontal space.
- Move Autodesk sample selection and all practical-tour controls into a dedicated welcome step, leaving the final step as a short completion summary with documentation links.
- Highlight YAML keys, strings and comments in the read-only rule example using colors that follow the active Revit theme.
- Populate the welcome and practical-tour windows with representative design-time data for accurate XAML previews without starting Revit.
- Improve the welcome layout by centering the workflow, expanding the YAML preview without wrapping, and grouping ribbon-tab information under the important notes.
- Present each important welcome note as a separate marked item instead of an undifferentiated text block.
- Reshape the English and Russian getting-started Wiki around user tasks: choosing built-in diagnostics or YAML rules, creating a first rule, following the practical tour, and starting with or without an open document or Autodesk sample.
- Isolate the practical tour behind neutral typed UI activity events and a disposable highlight session, so observer failures cannot affect diagnostic commands and the report presenter no longer exposes an onboarding-specific result query.
- Contain and log failures at the onboarding boundary so a broken coach window, highlight, progress store, or tutorial cleanup cannot interrupt the main diagnostics workflow.
- Clarify the welcome flow with a model-to-report overview and an adaptive final instruction that distinguishes between an already open document and a Revit session without one.
- Make the final welcome action reflect every selected option immediately, and keep the panels enabled whenever the practical tour needs them.
- After preparing an Autodesk sample, keep a modeless instruction visible until the user opens it from the dedicated ribbon command; the copy then opens in the current Revit instance, shows the panels and starts the tour.
- Draw every control with the add-in's own flat templates instead of Material Design: square buttons without ripple animations, outlined dialog buttons and input fields, plain check boxes, radio buttons, menus and tool tips. The Material Design libraries are no longer installed with the add-in, which removes a source of version conflicts with other add-ins.
- Color the panes and windows like the Revit interface in both the light and the dark theme: background, text, lines, selection and scroll bars use the colors of the Revit palettes, and the accent color is the Autodesk blue instead of cyan. Warning codes use a darker orange in the light theme so that they stay readable.
- Make the panes and windows denser so they sit closer to the Revit interface: 12 px Segoe UI text, 16 px icons, smaller tool bar buttons, tighter table rows, menus and dialog buttons. The search box of a pane now shows its hint inside the field instead of above it.
- Fit the tables of the panes to the pane width: the message column of the report and fix panes and the description column of the diagnostics pane take the room left by the other columns and wrap their text, so the table no longer needs horizontal scrolling.
- Make the row buttons of the diagnostic report quieter: their icons use the text color and are dimmed until the row is pointed at or selected, the fix button is shown only for findings that have a fix, and the documentation button moved to the last column.
- Remove the severity column from the diagnostic report: the code of a finding is colored by its severity instead (orange for a warning, red for an error) and names the severity in its tool tip. The severity filters remain available.
- Move the filters of the diagnostics and diagnostic report panes from chips on the tool bar into a list of check boxes opened by the filter button, which is no longer a disabled placeholder. The document choice of the diagnostic report and fix panes moved into the same list, and the tool bar names the document whose rows are shown. The export button of the diagnostic report moved to the right edge of the tool bar, after the search box and the filter button. The **On active view** option next to the run button is now an icon button that stays pressed while the option is on.
- Replace the scroll bars of the panes and windows with ones drawn like those of the Revit palettes: a light track, chevron arrows and a narrow rounded thumb that follow the light and the dark theme.
- Replace the Wiki configuration examples with rules that are usable as shipped: nine collision rules (three per discipline), six element checks, and project parameter rules built from the ADSK shared parameter file. The examples are available with English and Russian message texts and reference parameters by `BuiltInParameter` name.
- Correct the ribbon tab name in the README and the Wiki: the commands are on the **Diagnostics** tab.
- Speed up diagnostic runs: built-in, parameter and collision diagnostics, the diagnostic service and the dependency functions used in formulas now share one cached result for the same document query (all elements, elements of a class, element types, element geometry) instead of collecting it separately. Results are keyed by document, view and rule filter, so runs never mix each other's data.
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
