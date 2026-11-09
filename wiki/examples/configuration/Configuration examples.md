---
aliases:
  - configuration examples
lang: en
---

> Language: **English** · [[Примеры конфигурации|Русский]]

The files next to this page are working example configurations stored with the documentation. Revit Linter does not read configurations from the repository: the [[Getting started button|welcome window]] installs this same file, or you can copy it by hand.

| File | Contents |
| --- | --- |
| [`diagnostics.config.yaml`](diagnostics.config.yaml) | The single configuration file: six element checks (`type: custom`), eleven collision rules (`type: collision`) and three project-parameter rules (`type: parameter`). The first element check, `CSTM001`, carries a configured fix that fills the empty Comments parameter. MEP (`CLSN1xx`) has host-only, linked-model (`CLSN104`) and connected-run (`CLSN105`) variants; architecture (`CLSN2xx`) and structure (`CLSN3xx`) follow. Every collision rule sets `ignoreNestedFamilies: true`, so pairs inside one placed family are not reported. |
| [`practical-tour.config.yaml`](practical-tour.config.yaml) | The managed `TOUR001` collision rule with the visualizations and fix menu required by the guided workflow. It is installed automatically when the tour starts. |

The same file with Russian message texts is in the [`ru`](ru/diagnostics.config.yaml) subfolder. Formulas reference parameters by `BuiltInParameter` name, so the rules work in any Revit language.

## Checks generated from the ADSK templates

The [`template-checks`](../template-checks) folder holds two editions of the same check blocks, generated from the ADSK Revit 2021 project templates. The blocks in the root `en/` and `ru/` folders use the `PG_*` parameter group spelling of Revit 2021–2023; the blocks in `2024/en/` and `2024/ru/` are the same rules with the group values of Revit 2024 and newer, written as ForgeTypeId strings such as `autodesk.parameter.group:data-1.0.0`. Each block contains one project-parameter rule and one element fill check for every Text or MultilineText parameter bound to element categories.

The `type: parameter` rule requires the project parameters of its discipline template to keep the same name, shared-parameter GUID, binding type, parameter group, category list and vary-between-groups flag.

The fill checks are `type: custom` rules for the Text and MultilineText parameters of the same template that are bound to element categories. A rule selects the elements of the bound categories and checks `!isnullorempty(parameter(me, '...'))`: a finding means that the parameter is empty on that element. Instance and type parameters are checked separately — an instance parameter is selected with `take: instance and (builtincategory(...) or ...)` and a type parameter with `take: type and (builtincategory(...) or ...)` — so a filled instance value does not hide an empty type value, and vice versa. The fill checks use the severity **Message**, unlike the **Warning** of the project-parameter rule.

A fill check has the code `CSTM` followed by the block number and a three-digit index, for example `CSTM401001` for the first architecture fill check. The codes are unique across all blocks.

| Code | Discipline | Parameters | Fill checks |
| --- | --- | --- | --- |
| `PRMTR401` | Architecture (АР) | 104 | 81 |
| `PRMTR402` | Water supply and sewerage (ВК) | 65 | 58 |
| `PRMTR403` | Reinforced concrete structures (КЖ) | 93 | 72 |
| `PRMTR404` | Steel structures (КМ) | 93 | 69 |
| `PRMTR405` | Heating and ventilation (ОВ) | 88 | 68 |
| `PRMTR406` | Low-current systems (СС) | 119 | 97 |
| `PRMTR407` | Technology (ТХ) | 61 | 46 |
| `PRMTR408` | Electrical systems (ЭОМ) | 108 | 93 |
| | **Total** | **731** | **584** |

The English blocks are in `template-checks/en/` and `template-checks/2024/en/`, the Russian ones in `template-checks/ru/` and `template-checks/2024/ru/`. The [[Getting started button|welcome window]] appends the selected blocks to the installed `examples\diagnostics.config.yaml`, so the checks stay in the examples group and appear in the diagnostics list like the example rules. The codes and counts above are the same in both editions, and the fill checks are identical. To install a block by hand, add it under the `rules:` key of your own [[Configuration file|configuration file]] and take the edition that matches your Revit: the root blocks for Revit 2021–2023 and the `2024` blocks for Revit 2024–2027.

The welcome window offers the check blocks on every supported Revit version and selects the edition automatically: Revit 2021–2023 receives the `PG_*` blocks and Revit 2024–2027 the ForgeTypeId blocks. A parameter whose group was `INVALID` in the 2021 templates is matched with an empty `group: ""` in the 2024 edition. A rule runs in every project document, so install only your disciplines — a document created from another discipline template does not have those parameters and would report them as missing. One parameter of the architecture block, `ADSK_Класс конструктивной пожарной опасности здания`, is matched by name, because its template definition carries an all-zero shared parameter GUID.

A fill check reports any empty parameter, including one that a company standard leaves optional on purpose: for example `ADSK_Примечание` or the rows of the sheet stamp (`ADSK_Штамп*`) are often empty by design. Such a check keeps reporting until the value is filled, so deactivate or remove it from the installed `examples\diagnostics.config.yaml` when your company standard does not require the value.

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

- Collisions are searched inside one document by default. Linked models take part only when a rule sets `includeLinks: true` (the shipped CLSN104 rule does), and linked hits stay text-only in the report. A rule selects elements by category and class, so it cannot tell a structural wall from an architectural one. Run a block in the model of its discipline, or narrow it with `takeDocument`.
- `groupBy` is a constant in the examples, so every selected element is compared with every other one. Replace it with a formula to compare elements only inside a group.
- The project parameter rules use names and GUIDs from the ADSK shared parameter file. The binding type, categories and group are an example of a company standard; adjust them to your template.
- The example's project parameter rules use the `group` format of Revit 2024 and newer. For Revit 2021–2023 replace it with a `BuiltInParameterGroup` name such as `PG_DATA`; a rule whose `group` does not fit the running Revit version is skipped when the file is loaded. The welcome window converts the group automatically. See [[Project parameter diagnostics]].
- The example rules carry a `tags` list. Tags are free text: they let you find a rule on the panes and appear in exported reports. Every rule starts with its discipline tag, spelled in the language of the file: `Shared`/`Общие`, `Systems`/`Системы`, `Architecture`/`Архитектура` and `Structure`/`Конструкции`. Adjust them to your own conventions; a rule without `tags` has none. The example rules declare `Project`, because their `takeDocument` restricts them to project documents.

## Working configuration

Revit Linter reads the working `diagnostics.config.yaml` in the [[Diagnostic configuration path convention|configuration folder]] for the current Revit version. For example, Revit 2025 uses:

`C:\Users\{UserName}\Documents\Revit Linter\2025\`

The easiest way to open this folder is the [[Diagnostic configuration path button|configuration folder button]] on the Diagnostics pane. To use the example by hand, copy it to that folder as `diagnostics.config.yaml`.

`examples\diagnostics.config.yaml` and `debug\diagnostics.config.yaml` are read too, in that order after the user file. A file must declare `version: 2`; older split files are moved into a `legacy` folder at startup and are no longer read, and a file with a newer version than the running product is ignored. See [[Configuration file]] and [[Migrating configuration]].
