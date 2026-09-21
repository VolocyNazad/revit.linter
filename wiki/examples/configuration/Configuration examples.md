---
aliases:
  - configuration examples
lang: en
---

> Language: **English** · [[Примеры конфигурации|Русский]]

The files next to this page are reference templates stored with the documentation. Revit Linter does not read configurations from the repository.

Templates are provided for three configurable modules:

- `config.yaml` — user element diagnostics;
- `collision.config.yaml` — collision diagnostics;
- `parameter-element.config.yaml` — project parameter diagnostics.

## Working configuration

Revit Linter reads the working YAML files that the user creates or edits in the [[Diagnostic configuration path convention|configuration folder]] for the current Revit version. For example, Revit 2025 uses:

`C:\Users\{UserName}\Documents\Revit Linter\2025\`

The easiest way to open this folder is the [[Diagnostic configuration path button|configuration folder button]] on the **Volocy** ribbon tab.

To use a template, copy it to the working configuration folder, keep its required filename, and replace the sample conditions, GUIDs, parameter names, and categories with values from your project. Rules with `isActive: false` are not run by default.
