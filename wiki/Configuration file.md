---
lang: en
---

> Language: **English** · [[Файл конфигурации|Русский]]

# Configuration file

Revit Linter reads every diagnostic configuration from one file per Revit version:

`Documents\Revit Linter\<year>\diagnostics.config.yaml`

The product creates the file with a comment header on first start. Two more copies are read after it, in this order:

| Location | Purpose |
| --- | --- |
| `diagnostics.config.yaml` | Your rules and settings. |
| `debug\diagnostics.config.yaml` | Optional debug rules. |
| `examples\diagnostics.config.yaml` | The example configuration installed by the welcome window. |

The managed [[Practical tour|tour]] configuration stays in `tour\practical-tour.config.yaml`; the tour installs and removes it itself.

## File structure

```yaml
# File format version for compatibility checks.
version: 2
rules:
  - type: custom
    code: "CSTM001"
    ...
```

`version` must be `2`. A file with a newer version than the running product is ignored; a file without a supported version is ignored and reported. Old `config.yaml`, `collision.config.yaml` and `parameter-element.config.yaml` files are moved into a `legacy` folder at startup and are no longer read — see [[Migrating configuration]].

## Rule types

| `type` | Target | Purpose |
| --- | --- | --- |
| `custom` | element | Your own element check. |
| `collision` | element | Compares elements with each other. |
| `parameter` | document | Checks project parameters. |
| `builtIn` | element or document | Overrides a check that ships with the product. |

The fields of each type are described in [[User diagnostics]], [[Collision diagnostics]] and [[Project parameter diagnostics]]. A `builtIn` entry contains only `code`, `isActive`, `severity` and `isHidden`; everything else is rejected.

## Common fields

| Field | Meaning |
| --- | --- |
| `code` | Stable unique identifier of the diagnostic. |
| `isActive` | Whether the diagnostic runs. Defaults to `true`. |
| `severity` | `Message`, `Warning` or `Error`. Defaults to `Message`. |
| `isHidden` | Hides the diagnostic from the panes, the run list and the reports, and stops it from running. Defaults to `false`. |
| `tags` | Free-text tags used to find the diagnostic and shown in reports. |

`isHidden` is stronger than `isActive`: a hidden diagnostic never runs, even when `isActive: true`. A built-in entry that is missing uses the defaults declared in code.

## Precedence and conflicts

Rules are read in source order: the user file, `debug`, `examples`, then the managed tour file; inside one file from top to bottom. When two rules of the same target kind share a code, the first one wins. A configured rule never overrides a built-in check with the same code and target; the built-in check wins.

## Editing and reloading

The **Diagnostics** pane writes changes made with the checkboxes and the severity list back into `diagnostics.config.yaml`. Only the affected fields change, so comments and the order of the rules are preserved. A new built-in override is appended under the `# Built-in overrides` marker at the end of the rule list.

Revit Linter watches the files and reloads them after an external edit. An invalid rule is skipped and described in a notification; the other rules stay in use. A file that is not valid YAML is treated as empty and reported.

The **Open configuration** button on a diagnostic row opens the file that declared it. For built-in checks this is the top-level `diagnostics.config.yaml`.

**Reset settings** removes every built-in override and every `isHidden` field. It does not change `isActive` or `severity` of your own rules.

## Related pages

- [[Diagnostic parameters]]
- [[Diagnostic tags]]
- [[Diagnostic severity]]
- [[Migrating configuration]]
