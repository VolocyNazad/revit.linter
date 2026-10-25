# 0001 — Split configuration files → single `diagnostics.config.yaml`

| Field | Value |
|---|---|
| Id | `MIG-0001` |
| Status | `Planned` |
| Kind | Breaking configuration format (user data and rule state) |
| Migrates from | Any product version up to and including the last released split-file version, `2.0.0` (`config.yaml`, `collision.config.yaml`, `parameter-element.config.yaml` plus the per-user override settings) |
| Migrates to | The release that ships configuration format 2 (planned as `3.0.0`, the first major release after the breaking change); see [Version rules](README.md#version-rules) |
| Introduced | Planned `3.0.0`; confirm the actual tag when the release is prepared and record it here |
| User action | `manual` for rules, switches and ignores; `automatic` for the startup move of the legacy files and for product-managed files (see below) |
| User guide | [Migrating configuration](../../wiki/Migrating%20configuration.md) (EN) · [Миграция конфигурации](../../wiki/ru/Миграция%20конфигурации.md) (RU) |
| Removal target | First major release after the introducing line, planned `4.0.0`, only after the [removal gate](#removal-gate) is met |
| Tracking | Removal task in `TODO.md`; this entry; the Wiki guide stays after retirement |

## Why

The old design read three independent YAML files and kept the effective per-diagnostic state (active flag and
severity) in two separate per-user settings stores, so the same rule had to be traced through four places to
know what would run. The format also forced three parsers, three validators and three change watchers, made the
source order between the files implicit, and allowed a rule code to mean different things in different files.

Configuration format 2 replaces that with one versioned file per Revit version. The envelope carries the file
format version and a rule list; every rule declares its module with `type` (`custom`, `collision`, `parameter`,
`builtIn`), so source order, validation and the effective state have exactly one representation. This also lets
the product own its example and practical-tour files in the same format.

The change is breaking because the old files have no `type` and no version envelope: a file can only be read
after it is wrapped and each rule is classified. The switches that used to live outside the files now live in
them, so a migrated rule carries its own state.

Rejected alternatives:

* **Read both formats indefinitely** — two parsers and two precedence rules forever, for a small number of
  folders that only shrink.
* **Import the old files automatically at startup** — would rewrite user-authored state without the user asking.
  The product owner explicitly chose a manual migration; the code must never invent user rules.
* **Keep the per-user override stores alongside the new file** — two sources of truth again, with an unclear
  winner when both exist.

## Who is affected

* Users updating from `2.0.0` or older whose configuration folders still contain one of the legacy files.
  The startup migration handles every version folder at once when any installed Revit version starts after
  the update.
* Multi-version installations: all version folders are migrated on the first start, and the user meets one
  warning for the installed product instead of one per Revit version.
* Users whose folders are already migrated: unaffected. No warning appears and no legacy file is moved.
* Clean installations: unaffected. No warning appears and no legacy file or `legacy` folder is created.

## What the product does automatically

| Automatic action | Guarantee |
|---|---|
| On startup, before any configuration is read, scans every version folder under `Documents\Revit Linter\<year>\` and moves the legacy top-level files, their `examples` copies and their `debug` copies into the `legacy` subfolder of the same version folder | Files are **moved**, never deleted; the subfolder layout is preserved (`legacy\examples\`, `legacy\debug\`), and a name collision gets a numeric suffix |
| Writes `<year>\legacy\README.txt` when it moved files from that version folder | Explains what happened and links to the user guide |
| Leaves a file that cannot be moved (locked, no permission) where it is and logs it | The product does not read it either way; the warning still appears because legacy files were found |
| Shows one warning dialog per product version when legacy files were found, whether they were moved or left in place, with a button to the Wiki guide | Not one per Revit version; the warning is recorded as shown only **after** it was actually displayed, so a failure retries on the next start |
| Reads no legacy file after the migration and shows no persistent banner in the **Diagnostic list** pane and no per-Revit-version notice | The old rules, switches and ignores are inert; the warning is the only in-product signal |
| Creates a new `diagnostics.config.yaml` with the normal format header only | There is no migration comment in the file |
| Refreshes the managed `tour\practical-tour.config.yaml` when it is not in the current format, before the catalog reads it | The file is product-managed and reproducible; it is excluded from configuration-error reporting, so a stale copy never raises a false error or shows a dialog |
| Re-offers the examples step: the wizard content version is raised to 4, so an upgraded user sees the step again once per Revit version | Installing examples stays the user's decision |
| On example installation, writes `examples\diagnostics.config.yaml` and backs up an existing one to `.bak-<timestamp>` | User-edited files are preserved instead of overwritten; obsolete example copies are not moved by the installer because the startup migration already moved them |

Nothing else is automatic. In particular the product does **not** import rules, switches or ignore entries.

## What the user must do

1. Open the configuration folder and create or open `diagnostics.config.yaml`.
2. Move the rules into the `rules:` list of `diagnostics.config.yaml`, giving each rule the `type` of the file
   it came from (`custom`, `collision`, `parameter`).
3. Reproduce the switches that lived in `elementDiagnosticSettings.yml` and `documentDiagnosticSettings.yml`:
   set `isActive`/`severity` on the rule itself for user rules, and add `type: builtIn` entries for built-in
   checks. The old stores were per user, the new file is per Revit version, so this is repeated in every
   version folder.
4. Re-ignore the findings that the old `Linter_Ignored`/`Linter_IgnoredType` project parameters suppressed. The
   old parameters are not migrated, are no longer read, and old ignored findings reappear.
5. Reinstall the examples from **Getting started** if they were used.
6. Verify the new list and the skipped-rule notification.
7. Delete the `legacy` folders only after the new configuration works; the product never deletes them, and they
   are the only way back to an older product version.

The step-by-step guide, including the YAML shape and the skipped-rule message format, is the Wiki page from the
table above. This entry does not duplicate it.

## Data safety and rollback

* The legacy files are **moved**, never deleted. They now live in `<year>\legacy\`, `<year>\legacy\examples\`
  and `<year>\legacy\debug\`; a collision gets a numeric suffix and nothing is dropped.
* **Rollback:** before reinstalling an older product version, move the files back from `legacy\` to their
  original locations — top-level files to `<year>\`, the copies from `legacy\examples\` to `examples\`, and the
  copies from `legacy\debug\` to `debug\`; rename a suffixed file to its original name. Until they are moved
  back, the older version does not see its configuration, because the files are no longer at the version
  folder root. As long as the `legacy` folder exists, the data is recoverable.
* The old per-user settings files under `%LOCALAPPDATA%\Volocy\Revit.Linter\settings\` are not moved and not
  read; they stay on disk and are read again by an older product version. The old in-model ignore parameters
  stay in the model in the same way.
* The managed practical-tour file is the only configuration file that is regenerated. It is product-owned and
  reproducible; starting the tour writes the same current-format content.
* An existing `examples\diagnostics.config.yaml` is backed up to `.bak-<timestamp>` before the shipped example
  replaces it.
* The ignore list is not migrated: findings ignored under the old parameters reappear, and the current
  in-document ignore list is read as empty by older product versions.

## Verification

Headless coverage (all in the release that ships the migration):

* `tests/Revit.Linter.ConfigurationPath.Tests/ConfigurationMigrationServiceTests.cs` — moves the legacy files of
  every version folder, including the `examples` and `debug` copies; preserves the subfolder layout, gives a
  collision a numeric suffix, leaves an unreadable file in place and reports it as remaining, writes
  `legacy\README.txt`, and is a no-op on a second run and on a clean folder. The result model reports moved and
  remaining files.
* `ConfigurationPathUtilsTests` — the legacy paths are computed from a version folder rather than static state,
  and a newly created configuration file carries only the format header.
* The migration warning runs in the add-in host, which has no headless test project; its behaviour (one warning
  per product version, only when the result has findings, recorded as shown only after it was displayed) is
  covered by the manual checklist.
* `tests/Revit.Linter.WelcomePresenter.Tests/ExampleConfigurationInstallerTests.cs` — an existing example file
  is backed up, and the shipped example is installed.
* `tests/Revit.Linter.PracticalTourPresenter.Tests/PracticalTourConfigurationInstallerTests.cs` — an old-format
  tour file is replaced; a current-format file is left byte-identical.

Manual verification: the **Upgrade from 2.0.0** section of
[the onboarding checklist](../testing/onboarding-manual-checklist.md). It covers the single warning, the
absence of a banner, the moved files in several version folders with subfolder preservation, `legacy\README.txt`,
the file that could not be moved, the re-offered examples step with the backup, the stale tour file, and the
skipped-rule reporting.

After a correct migration the Diagnostic list shows the expected user and built-in overrides, skips and reports
only genuinely invalid rules, shows no banner, and produces no configuration error from the managed tour file.

## Removal gate

Support may be removed only when **all** conditions hold:

- [ ] At least 12 months have passed since the introducing release.
- [ ] At least two stable releases have shipped since the introducing release.
- [ ] The removal is described in the release notes of the stable release immediately before the removal, with
      the last supported version and a link to the historical Wiki guide.
- [ ] Neither the in-product signal nor support reports indicated unmigrated folders for the two most recent
      stable releases. The signal is the `Information` log line emitted at startup when legacy files were
      found (`Moved {MovedCount} legacy configuration files ...`), plus the warning for files that could not be
      moved; the `configurationMigrationWarning.yml` value-store file records the product version that showed
      the warning.
- [ ] The cleanup checklist below is complete, the solution builds without warnings, all headless tests pass,
      and `scripts/Validate-Repository.ps1` passes.

The removal itself is a normal breaking-cleanup change: update this entry, `docs/architecture.md`,
`docs/FEATURES.md`, the release notes and the `TODO.md` task in the same change.

## Cleanup checklist

Delete when the removal gate is met. Each item exists only for this migration.

**ConfigurationPath module**

- [ ] `ConfigurationPathUtils`: the three `Legacy*ConfigFileName` constants and `GetLegacyConfigurationPaths`.
- [ ] `IConfigurationPaths.VersionsDirectory` and its implementation in `ConfigurationPaths`.
- [ ] `Abstractions/Services/IConfigurationMigrationService.cs`, `Services/ConfigurationMigrationService.cs`,
      `Abstractions/Models/ConfigurationMigrationResult.cs` and their registrations in
      `DI/ServiceCollectionExtensions.cs`.
- [ ] The `legacy` move logic in `ConfigurationMigrationService`: `LegacyFolderName`, destination under `legacy`
      with preserved `examples` and `debug` subfolders, numeric-suffix collision handling, the `legacy\README.txt`
      writer, moved/remaining logging and the result model.
- [ ] `ConfigurationPath.Tests`: `ConfigurationMigrationServiceTests` (whole file) and the
      `Legacy_configuration_paths_cover_the_top_level_examples_and_debug_folders` test in
      `ConfigurationPathUtilsTests`.

**Add-in host**

- [ ] `Infrastructure/Services/ConfigurationMigrationWarning.cs`,
      `Infrastructure/Models/ConfigurationMigrationWarningSettings.cs` (and its
      `configurationMigrationWarning.yml` store file), the host DI registration in
      `HostServiceCollectionExtensions` and `InitExternalApplication.Startup.ScheduleConfigurationMigrationWarning`
      with its call in `InitExternalApplication`.
- [ ] `GlobalLocalizations.resx` / `.ru.resx`: `configurationMigration_warning_message`,
      `configurationMigration_warning_remaining_message` and `configurationMigration_warning_openGuide`.
- [ ] The startup log line that reports the moved files and the warning for the files that could not be moved.

**Diagnostic list presenter**

- [ ] No migration-only state remains: there is no banner and no migration command in the presenter.

**Welcome presenter**

- [ ] Keep the `.bak-<timestamp>` backup: it protects user edits in the examples folder independently of the
      legacy layout.
- [ ] Keep the wizard content version as it is; it is onboarding state, not a migration-only artifact.

**Practical tour**

- [ ] `IPracticalTour.RefreshOutdatedConfiguration`, its implementation in `PracticalTour`, the startup call in
      `InitExternalApplication`, `PracticalTourConfigurationInstaller.RefreshOutdated` and `TourDiagnosticCode`.
- [ ] `DiagnosticConfigurationService`: the two `descriptor.IsTour` skip clauses.
- [ ] The two `Refresh_outdated_*` tests in `PracticalTourConfigurationInstallerTests`.

**Documentation and solution**

- [ ] `docs/architecture.md`: rewrite the split-file sentence to the steady state; keep the format-2 rules.
- [ ] `docs/FEATURES.md`: remove the startup-migration clause from the compatibility contract.
- [ ] `docs/repository.md`: remove the migration details from the configuration, wizard and tour descriptions.
- [ ] `docs/testing/onboarding-manual-checklist.md`: remove the **Upgrade from 2.0.0** section.
- [ ] `TODO.md`: close the removal task.
- [ ] This entry: status `Retired`, the release that removed support, and a short note that the Wiki guide is now
      historical.
- [ ] `docs/migrations/README.md`: update the index row.
- [ ] Keep `docs/migrations/0001-single-configuration-file.md` forever; do not delete or renumber it.

## What stays after removal

- Configuration format 2 and everything that describes it: the format header, the single-file reader and writer,
  `docs/architecture.md` and `docs/FEATURES.md`.
- The Wiki guide [Migrating configuration](../../wiki/Migrating%20configuration.md) and its Russian counterpart,
  marked historical. Users who skip several versions still need the conversion steps.
- The `.bak-<timestamp>` backup of an existing example file, which is general data safety, not migration-only.
- The `legacy` folders already created on user machines: they are user data, the product does not delete them,
  and the Wiki guide explains how to move the files back for a downgrade.
- The `Information` log line is removed together with the migration service that produces it.

## References

- [Migration registry and lifecycle policy](README.md)
- [Architecture decisions](../architecture.md) — single-file configuration and no separate override store
- [Compatibility contract](../FEATURES.md)
- [Repository guide](../repository.md) — configuration module boundaries and versioning
- [User guide, English](../../wiki/Migrating%20configuration.md) ·
  [Russian](../../wiki/ru/Миграция%20конфигурации.md)
- [Manual upgrade checklist](../testing/onboarding-manual-checklist.md)
