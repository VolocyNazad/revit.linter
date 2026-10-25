---
lang: en
---

> Language: **English** · [[Миграция конфигурации|Русский]]

# Migrating configuration

Revit Linter used to read three separate files — `config.yaml`, `collision.config.yaml` and `parameter-element.config.yaml` — plus per-user override settings. Version 2 reads one `diagnostics.config.yaml` per Revit version and stores the effective settings there. **The old files are not read at all.** They are left untouched, so you can return to an older product version if needed.

The product shows a migration notice once per Revit version when it finds an old file. The notice has a button that opens this page.

## What to do

1. Open the configuration folder: **Diagnostics** tab → **Open configuration folder**. For Revit 2025 it is `Documents\Revit Linter\2025\`.
2. If `diagnostics.config.yaml` does not exist yet, start Revit Linter once: the file is created with a header. You can also create it by hand.
3. Move the rules from the old files into `diagnostics.config.yaml`:
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

4. Reproduce the switches you had in the **Diagnostics** pane:
   - for your own rules, set `isActive` and `severity` on the rule itself;
   - for built-in checks, add entries at the end of the rule list, under `# Built-in overrides`:

   ```yaml
   # Built-in overrides
   - type: builtIn
     code: "SHRD003"
     isActive: false
     severity: "Warning"
   ```

   The old `elementDiagnosticSettings.yml` and `documentDiagnosticSettings.yml` files under `%LOCALAPPDATA%\Volocy\Revit.Linter\settings\` are no longer read. They are kept for a downgrade; you can copy the values from them into the entries above.
5. Save the file and let Revit Linter reload it. The **Diagnostics** pane shows the new list; the configuration error notification describes every rule that was skipped.
6. Optionally delete the old files. Leaving them is harmless: the product never reads them.

## Files in `examples` and `debug`

The same change applies to the optional folders:

- the welcome window now installs `examples\diagnostics.config.yaml` and removes the obsolete example files it used to install;
- if you keep your own debug rules, move `debug\config.yaml`, `debug\collision.config.yaml` and `debug\parameter-element.config.yaml` into `debug\diagnostics.config.yaml` with the same `type` fields.

## The practical tour

The managed `tour\practical-tour.config.yaml` is installed by the tour itself and needs no migration. Starting the tour reinstalls the current format.

## If something is skipped

The notification starts with the file name and the rule code, for example:

`diagnostics.config.yaml: rule 'CLSN101' was skipped: the andTake field is missing`

Fix the rule and save the file again. A file with `version` other than `2` is reported as a whole; a file that is not valid YAML is treated as empty.

## Rolling back

The old files and settings are untouched. To return to an older product version, reinstall it; it reads its old files and settings again.
