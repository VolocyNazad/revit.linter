---
lang: en
---

> Language: **English** · [[Встроенные проверки и пользовательские правила|Русский]]

# Built-in diagnostics and user rules

Revit Linter can check a model in two complementary ways.

| | Built-in diagnostics | User rules |
|---|---|---|
| Best for | Common model-quality problems | Project and company requirements |
| Setup | None | YAML files in the configuration folder |
| Maintenance | Updated with Revit Linter | Owned and edited by your team |
| First step | Enable a diagnostic and run it | Copy an example and change one condition |

## Start with built-in diagnostics

Built-in diagnostics are ready immediately. Open the [[Diagnostics pane]], select the checks that fit the current model, then run them from [[Diagnostic reports pane|Diagnostic results]]. This is the quickest path to a useful report and does not require YAML knowledge.

## Add a user rule when the requirement is yours

A user rule describes:

- `code` — a stable unique identifier;
- `description` — the name shown in the interface;
- `severity` — message, warning, or error;
- `take` — which Revit elements to inspect;
- `check` — what a valid element must satisfy.

The `check` expression describes the valid state. Every element selected by `take` for which `check` returns `false` becomes a finding.

Follow [[Create your first rule]] for a small editable example. Use [[User diagnostics]] when you need the complete field reference, formulas, visualizations, or fixes.

[[Diagnostics overview|← Diagnostics overview]]
