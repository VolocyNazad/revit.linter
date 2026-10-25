---
lang: en
---

> Language: **English** · [[Миграция конфигурации|Русский]]

# Migrating configuration

Revit Linter used to read three separate files — `config.yaml`, `collision.config.yaml` and `parameter-element.config.yaml` — and kept the per-user switches in separate settings files. Configuration format 2 reads one `diagnostics.config.yaml` per Revit version and stores the effective settings there. **The old files are not read at all.**

On the first start after the update, before anything reads configuration, the product moves the old files of every Revit version folder into a `legacy` subfolder of that folder. It then shows **one warning for the installed product** — not one per Revit version — when it found old files, and the warning opens this page. There is no banner in the **Diagnostic list** pane and no notice per Revit version. The product never deletes the old files.

## Where the old files are now

The product scans every version folder under `Documents\Revit Linter\<year>\`, not only the running Revit version, and preserves the subfolder layout. Only folders named after a four-digit release year are scanned, so a backup copy placed next to them (for example `2025-backup`) is left alone:

| Old location | New location |
| --- | --- |
| `<year>\config.yaml` | `<year>\legacy\config.yaml` |
| `<year>\collision.config.yaml` | `<year>\legacy\collision.config.yaml` |
| `<year>\parameter-element.config.yaml` | `<year>\legacy\parameter-element.config.yaml` |
| `<year>\examples\` copies | `<year>\legacy\examples\` |
| `<year>\debug\` copies | `<year>\legacy\debug\` |

A moved file that would overwrite an existing file in the `legacy` folder gets a numeric suffix, for example `config-2.yaml`. The product writes `<year>\legacy\README.txt`, which explains what happened and links to this page. A file that cannot be moved, for example because another program holds it open, stays where it is and is logged; the warning still appears because old files were found.

The product reads none of these files again. The rules, switches and ignores they contain do not apply. The old per-user settings files `elementDiagnosticSettings.yml` and `documentDiagnosticSettings.yml` under `%LOCALAPPDATA%\Volocy\Revit.Linter\settings\` are not moved and are not read either, and the old in-model ignore parameters `Linter_Ignored` and `Linter_IgnoredType` are not read.

## What to do

Migrate by hand, in this order. The product moves no rule, switch or ignore for you: everything after the warning is your responsibility. Before you start, copy the whole configuration folder, for example to `2025-backup`. For Revit 2025 the folder is `Documents\Revit Linter\2025\`. Keep the copy until you are sure the new configuration works.

1. Open the configuration folder: in the **Diagnostic list** pane, press [[Diagnostic configuration path button|Open configuration folder]] on the toolbar at the right edge. Every Revit version has its own folder and its own `diagnostics.config.yaml`.
2. Create or open `diagnostics.config.yaml` in that folder. If the file does not exist yet, start Revit Linter once: the product creates it with the normal format header only, and there is no migration comment in it. You can also create the file by hand.
3. Move the rules into the `rules:` list of `diagnostics.config.yaml` and give every rule the `type` of the file it came from:
   - rules from `config.yaml` get `type: custom`;
   - rules from `collision.config.yaml` get `type: collision`;
   - rules from `parameter-element.config.yaml` get `type: parameter`.

   Keep the rest of each rule unchanged:

   ```yaml
   version: 2
   rules:
     - type: custom
       code: "CSTM001"
       description: "Generic model instances"
       takeDocument: "!property('IsFamilyDocument')"
       take: "instance and builtincategory('OST_GenericModel')"
       check: "isnull(property('Id'))"

     - type: collision
       code: "CLSN101"
       ...
   ```

4. Reproduce the switches that lived in the old settings files:
   - for your own rules, set `isActive` and `severity` on the rule itself;
   - for built-in checks, add entries at the end of the rule list, under `# Built-in overrides`:

   ```yaml
   # Built-in overrides
   - type: builtIn
     code: "SHRD003"
     isActive: false
     severity: "Warning"
   ```

   The old `elementDiagnosticSettings.yml` and `documentDiagnosticSettings.yml` files under `%LOCALAPPDATA%\Volocy\Revit.Linter\settings\` are not migrated automatically. They stay on disk for a downgrade; copy the values you still need into the entries above by hand.
5. Re-ignore the findings you want to keep ignoring. The old ignore list from 2.0.0, including its in-model ignore parameters, is not migrated, so findings you ignored before reappear. Use the lightbulb menu or the [[Ignoring pane|Ignore list]] pane to add them again.
6. If you installed the examples, reinstall them from **Getting started**. The wizard offers the examples step again and writes the new `examples\diagnostics.config.yaml`; an existing example file that differs from the shipped one is backed up to `.bak-<timestamp>` first, and an identical file is left untouched. Obsolete example copies are no longer handled by the installer, because the startup migration already moved them to `<year>\legacy\examples\`.
7. Save the file and let Revit Linter reload it. The **Diagnostic list** pane shows the new list. Verify that the expected rules are there and that any skipped rule is reported, then fix the rule and save the file again.
8. Only after the new configuration works, clean up. The product keeps the `legacy` folders and never deletes them; deleting them is your decision and cannot be undone. Keep them until you are sure, because they are the only remaining copy of the old rules and the only way back to an older product version. The old settings files under `%LOCALAPPDATA%\Volocy\Revit.Linter\settings\` may be deleted at the same time.

## One file, one failure domain

All rules now live in one file, so a single YAML error or a wrong `version` disables every rule in `diagnostics.config.yaml` at once. Keep the folder copy from before the steps so you can restore the last working file quickly.

## Visualization default

A migrated rule that omits `visualizations` now gets the current default pipeline, **Show and select**. The earlier format default was **Show**. If you want the old behavior, state the visualization explicitly in the rule.

## The practical tour

The managed `tour\practical-tour.config.yaml` is installed by the product. A stale copy in an old format is refreshed automatically at startup: there is no user action and no error dialog. Starting the tour installs the current format again. You do not migrate this file.

## If something is skipped

The notification starts with the file name and the rule code, for example:

`diagnostics.config.yaml: rule 'CLSN101' was skipped: the andTake field is missing`

Fix the rule and save the file again. A file with `version` other than `2` is reported as a whole and ignored. A file that is not valid YAML is treated as empty.

## Rolling back

The old files are moved, not deleted, but they no longer sit at the version folder root. To go back to an older product version, first move them back from `<year>\legacy\` to their original locations: the top-level files to `<year>\`, the copies from `legacy\examples\` to `examples\`, and the copies from `legacy\debug\` to `debug\`. If a moved file received a numeric suffix, rename it to the original name. Only then reinstall the older product version; until the files are moved back, it does not see its configuration.

Nothing was deleted, so the data is recoverable. The old settings files were not touched and are read again by the older version, and the old in-model ignore parameters stay in the model. The managed tour file was refreshed to the current format, so an older version may replace it again.
