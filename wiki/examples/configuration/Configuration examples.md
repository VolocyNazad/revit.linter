---
aliases:
  - configuration examples
lang: en
---

> Language: **English** · [[Примеры конфигурации|Русский]]

The files next to this page are working example configurations stored with the documentation. Revit Linter does not read configurations from the repository: the [[Getting started button|welcome window]] installs these same files, or you can copy them by hand.

| File | Module | Contents |
| --- | --- | --- |
| [`config.yaml`](config.yaml) | [[User diagnostics\|User element diagnostics]] | Six checks: generic model instances, ducts and pipes without a system, doors and windows without a mark, mirrored doors, rooms without a meaningful name, and unplaced rooms with a **Delete** fix. |
| [`collision.config.yaml`](collision.config.yaml) | [[Collision diagnostics]] | Nine rules, three per discipline: MEP systems (`CLSN1xx`), architecture (`CLSN2xx`) and structure (`CLSN3xx`). |
| [`parameter-element.config.yaml`](parameter-element.config.yaml) | [[Project parameter diagnostics]] | One rule per discipline built from the ADSK shared parameter file: `PRMTR101`, `PRMTR201` and `PRMTR301`. |

The same files with Russian message texts are in the [`ru`](ru/config.yaml) subfolder. Formulas reference parameters by `BuiltInParameter` name, so the rules work in any Revit language.

## Discipline blocks

In the collision and project parameter files every discipline is enclosed in a pair of comment lines:

```yaml
# >>> section: mep — MEP systems
- code: "CLSN101"
  ...
# <<< section: mep
```

The welcome window keeps only the blocks of the selected disciplines (`mep`, `architecture`, `structure`). When copying by hand, delete the blocks you do not need or leave them all.

## What to keep in mind

- Collisions are searched inside one document; linked models do not take part. A rule selects elements by category and class, so it cannot tell a structural wall from an architectural one. Run a block in the model of its discipline, or narrow it with `takeDocument`.
- `groupBy` is a constant in the examples, so every selected element is compared with every other one. Replace it with a formula to compare elements only inside a group.
- The project parameter rules use names and GUIDs from the ADSK shared parameter file. The binding type, categories and group are an example of a company standard; adjust them to your template.
- The project parameter file uses the `group` format of Revit 2024 and newer. For Revit 2021–2023 replace it with a `BuiltInParameterGroup` name such as `PG_DATA`; a rule whose `group` does not fit the running Revit version is skipped when the file is loaded. The welcome window converts the group automatically. See [[Project parameter diagnostics]].

## Working configuration

Revit Linter reads the working YAML files in the [[Diagnostic configuration path convention|configuration folder]] for the current Revit version. For example, Revit 2025 uses:

`C:\Users\{UserName}\Documents\Revit Linter\2025\`

The easiest way to open this folder is the [[Diagnostic configuration path button|configuration folder button]] on the **Diagnostics** ribbon tab. To use an example by hand, copy it to that folder and keep its required filename.
