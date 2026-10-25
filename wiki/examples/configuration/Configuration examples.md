---
aliases:
  - configuration examples
lang: en
---

> Language: **English** · [[Примеры конфигурации|Русский]]

The files next to this page are working example configurations stored with the documentation. Revit Linter does not read configurations from the repository: the [[Getting started button|welcome window]] installs this same file, or you can copy it by hand.

| File | Contents |
| --- | --- |
| [`diagnostics.config.yaml`](diagnostics.config.yaml) | The single configuration file: six element checks (`type: custom`), eleven collision rules (`type: collision`) and three project-parameter rules (`type: parameter`). MEP (`CLSN1xx`) has host-only, linked-model (`CLSN104`) and connected-run (`CLSN105`) variants; architecture (`CLSN2xx`) and structure (`CLSN3xx`) follow. |
| [`practical-tour.config.yaml`](practical-tour.config.yaml) | The managed `TOUR001` collision rule with the visualizations and fix menu required by the guided workflow. It is installed automatically when the tour starts. |

The same file with Russian message texts is in the [`ru`](ru/diagnostics.config.yaml) subfolder. Formulas reference parameters by `BuiltInParameter` name, so the rules work in any Revit language.

## File structure

Every rule declares its module through `type`:

```yaml
version: 2
rules:
  - type: custom
    code: "CSTM001"
    ...
  - type: collision
    code: "CLSN101"
    ...
  - type: parameter
    code: "PRMTR101"
    ...
```

`custom` and `collision` rules are element diagnostics; `parameter` rules are document diagnostics. A `builtIn` entry overrides the activity, severity or visibility of a check that ships with the product and contains only `code`, `isActive`, `severity` and `isHidden`.

## Discipline blocks

In the example, every discipline is enclosed in a pair of comment lines:

```yaml
# >>> section: mep — MEP systems
- type: "collision"
  code: "CLSN101"
  ...
# <<< section: mep
```

The welcome window keeps only the blocks of the selected disciplines (`mep`, `architecture`, `structure`). When copying by hand, delete the blocks you do not need or leave them all.

## What to keep in mind

- Collisions are searched inside one document; linked models do not take part. A rule selects elements by category and class, so it cannot tell a structural wall from an architectural one. Run a block in the model of its discipline, or narrow it with `takeDocument`.
- `groupBy` is a constant in the examples, so every selected element is compared with every other one. Replace it with a formula to compare elements only inside a group.
- The project parameter rules use names and GUIDs from the ADSK shared parameter file. The binding type, categories and group are an example of a company standard; adjust them to your template.
- The project parameter rules use the `group` format of Revit 2024 and newer. For Revit 2021–2023 replace it with a `BuiltInParameterGroup` name such as `PG_DATA`; a rule whose `group` does not fit the running Revit version is skipped when the file is loaded. The welcome window converts the group automatically. See [[Project parameter diagnostics]].
- The example rules carry a `tags` list. Tags are free text: they let you find a rule on the panes and appear in exported reports. Adjust them to your own conventions; a rule without `tags` has none. The example rules declare `Project`, because their `takeDocument` restricts them to project documents.

## Working configuration

Revit Linter reads the working `diagnostics.config.yaml` in the [[Diagnostic configuration path convention|configuration folder]] for the current Revit version. For example, Revit 2025 uses:

`C:\Users\{UserName}\Documents\Revit Linter\2025\`

The easiest way to open this folder is the [[Diagnostic configuration path button|configuration folder button]] on the Diagnostics pane. To use the example by hand, copy it to that folder as `diagnostics.config.yaml`.

`examples\diagnostics.config.yaml` and `debug\diagnostics.config.yaml` are read too, in that order after the user file. A file must declare `version: 2`; older split files are not read, and a file with a newer version than the running product is ignored. See [[Configuration file]] and [[Migrating configuration]].
