# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

Older releases are in the [changelog archive](docs/changelog/archive.md).

## [Unreleased]

## [3.0.5] - 2026-11-04

### Added

- Check that a Revit link is pinned: the new `SHRD0037` element diagnostic reports an unpinned link instance in a project, offers the `Pin Revit link` fix and a visualization that shows and selects the link. An unloaded link is not reported: its instances stay in the model, but there is no content to move until the link is reloaded, and the run after reloading reports the pin state again.
- Check that walls are single-layer: the new `ARCH005` element diagnostic reports a wall whose wall type carries a compound structure with more than one layer, and offers a visualization that shows and selects the wall. A wall type whose compound structure cannot be read is not reported. The check does not run in family documents.

## [3.0.4] - 2026-11-03

### Added

- Check that levels and grids are pinned: the built-in `SHRD0035` project diagnostic reports an unpinned level and `SHRD0036` reports an unpinned grid. Each has a fix that pins the element, the level finding offers a select visualization and the grid finding shows and selects the grid, and a multi-segment grid is reported once for the whole chain instead of once per segment. The checks do not run in family documents.
- Write parameter values from a configured fix: the new `SetParameter` fix pipeline step evaluates a value formula in the element grammar for every element selected through the step's `elementSets` and writes the result to the named parameter. The parameter is identified by a `BuiltInParameter` name, a shared parameter GUID or the display name; the result is converted to the parameter storage type — a number is written in the project unit of the parameter, an integer is rounded away from zero, a yes/no parameter accepts a boolean, and an `ElementId` must exist in the same document. A pipeline may contain several `SetParameter` steps, `Delete` remains the terminal final step, a value that cannot be converted leaves the parameter unchanged and fails the fix, and the value formula is compiled once when the rule is registered; writing a type parameter logs a warning because every instance of that type changes.
- The shipped English and Russian example configurations installed by the welcome wizard demonstrate the parameter fix: the `CSTM001` element check carries a configured fix that fills the empty Comments parameter, so its finding disappears on the next run.

### Fixed

- A configured rule is no longer dropped when one of its fix or visualization pipelines is invalid: the invalid pipeline is skipped and the configuration error names the rule, the pipeline and the reason, while the diagnostic stays registered with its other pipelines. Previously one invalid pipeline removed the whole rule.

## [3.0.3] - 2026-11-01

### Fixed

- Touching elements are no longer reported as collisions: Revit rejects the Boolean intersection of solids whose faces or edges coincide — a curtain wall abutting a wall, two walls meeting end to end — and the rejected pair is now left unverified instead of being counted as a clash. A target whose only overlaps are unverified is reported with its own message that lists the pairs to review, the rule message applies to verified collisions only, linked candidates are classified the same way, and the rejection is logged once per pair.
- Undoing the visualization view no longer breaks the feature with `Failed to restore the previous visualization state`. A restore session whose Revit objects were deleted or undone now counts as restored and is dropped with one warning instead of being retried forever, a closed view only skips the zoom, showing elements works from the captured view id, and an explicit visualization request is no longer blocked by a previous one that cannot be restored.
- The date and time column of the diagnostic and fix reports keeps the row age current while the pane stays open instead of freezing the text from the moment the cell was first bound.

## [3.0.2] - 2026-10-27

### Added

- Show the installed product version in the header of the welcome wizard, next to the step counter, in English and Russian.

### Changed

- Compile the formulas of every configured diagnostic when the configuration is loaded. A rule whose `takeDocument`, `take`, `andTake`, `groupBy` or `check` formula cannot be compiled is skipped, and the configuration notification names the rule code and the rejected formula; the other rules stay in use. Previously only a warning reached the application log and the affected stage silently used a fallback.

### Fixed

- `if(...)` expressions and boolean parameter values are accepted in `check`, `takeDocument` and the project-parameter `take`; a conditional no longer falls back to an always-valid check and silently reports no findings.
- The search box height leaves room for descenders, so characters such as `y` are no longer clipped.

## [3.0.0] - 2026-10-25

### Added

- Check the model with Revit's own health rules: the built-in Revit Performance Adviser rules run as the `RVT002` document diagnostic for projects and families.
- Add built-in checks for families and model hygiene: reference planes and lines without locked dimensions (`SHRD0024`, `SHRD0029`), unfilled family parameters (`SHRD0025`), duplicate family types and symbols (`SHRD0026`, `SHRD0028`), group presence (`SHRD0027`) and purgeable elements (`RVT003`), with select and show visualizations and safe delete fixes where possible.
- Add array checks: associated arrays (`SHRD0031`), excessive member counts in linear (`SHRD0032`) and radial (`SHRD0034`) arrays, and arrays driven by a family parameter (`SHRD0033`), with visualizations, a delete fix and an unbind fix.
- Add document checks for unused family size tables (`DOC002`) and a missing preview view (`DOC003`), including a fix that sets the active view as the preview. The applicable built-in checks now also run in family documents.
- Give every diagnostic tags: the diagnostics and report panes show a clickable **Tags** column, search and report exports use it, and a rule accepts an optional `tags` list. The product adds trusted system tags (`BuiltIn`, `Example`, `Tour`, `Debug` and the rule type) that a rule cannot set.
- Store ignores inside the model: element and document ignores live in one Extensible Storage data storage and are skipped before a run visits elements. Ignore from a report row, in bulk or for the whole document; the new **Ignore list** pane lists, removes and clears the entries, and a document-level entry suppresses every finding of that diagnostic in that document.
- Check loaded Revit links in collision rules: with `includeLinks: true` linked elements take part in the collision with their own spatial indexes and conformal transforms, and a linked hit names its element as `Name [Link]`. New rule flags exclude MEP-connected elements (`ignoreConnectedElements`) and parent/nested pairs of one family tree (`ignoreNestedFamilies`).
- Support negated filter leaves in formulas, for example `!category(...)`, through inverted native filter overloads.
- Add visualization pipeline steps for detail level, display style and category visibility.
- Open the rule configuration from the diagnostics pane with **Open With**, reveal it in Explorer or open it in Notepad, reset persisted check settings behind a confirmation, open a module's documentation from its rows and mark debug configurations with a debug badge.
- Offer element, bulk and document ignore actions on report rows with applied notifications, and mark the active visualization on report rows and menus.
- Restart the practical tour after completion or opt-out, keep a restored tour pane hidden without an active session, install, refresh and remove the managed `TOUR001` configuration with the session, and polish the spotlight pulse, button glow and halo.
- Report the actual example installation outcome in the welcome wizard — created, left untouched, or replaced with a backup — and offer the examples step again once per Revit version (wizard content version 4).

### Changed

- **Breaking:** configuration moved to one `diagnostics.config.yaml` per Revit version in file format version 2, with a per-rule `type` (`custom`, `collision`, `parameter`, `builtIn`), sparse built-in overrides and `isHidden` in the same file. The split `config.yaml`, `collision.config.yaml` and `parameter-element.config.yaml` files and the per-user diagnostic settings are no longer read. On startup the product moves those legacy files, their `examples` and `debug` copies, into a `legacy` folder of every Revit version folder without deleting anything, refreshes the managed practical-tour file, and shows one warning per product version with a link to the migration guide. Rules, switches and ignores are migrated by hand; the migration support is kept through the 3.x line and removed in 4.0.0 once the removal gate in the migration registry is met.
- **Breaking:** ignores are no longer stored in the `Linter_Ignored` and `Linter_IgnoredType` project parameters but in the model itself, so findings ignored by an older version appear again until they are ignored in the **Ignore list** pane.
- **Breaking:** the Revit-warning check code `RVT` is now `RVT001`; update `builtIn` overrides and configured rules that use the old code.
- One per-user MSI ships builds for Revit 2021, 2022, 2023, 2024, 2025, 2026 and 2027 and preselects the versions installed on the computer.
- Report configuration problems once per distinct problem with localized messages, including a configuration file written by a newer product version and rules skipped by validation.
- Refresh the English and Russian example configurations for the one-file format: 20 rules (6 custom, 11 collision and 3 parameter), with collision link, connected-element and nested-family flags, the array checks and the tags field; the welcome wizard installs the same files.
- Speed up diagnostic runs further: share collectors and lookup maps through the document queries, cache family-symbol signatures, level elevations and dimension-reference maps per run, and reduce per-element and cold-start allocations. Bulk check actions write the configuration file in one pass instead of once per rule.
- Document the one-file configuration format and its migration in both languages, add the built-in check and visualization pipeline references, and refresh the manual checklists.

### Fixed

- The practical tour no longer completes the selection step when other diagnostics were enabled before it, and an identical example configuration is no longer rewritten with another backup.
- Stacked dialogs are owned by the active window, and their default and close buttons pass real boolean results.
- Report rows whose target element was deleted or undone stay visible as stale rows without fixes or visualizations instead of failing.
- `Double` parameters are now converted with the parameter's own unit instead of the length unit.

