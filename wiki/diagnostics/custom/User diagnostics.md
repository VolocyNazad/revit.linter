---
lang: en
---

> Language: **English** · [[Пользовательские проверки|Русский]]

The list and behavior of diagnostics is defined through the single [[Configuration file|configuration file]] `diagnostics.config.yaml`. A user element check is a rule with `type: "custom"` and must be located at the [[Diagnostic configuration path convention|path]] specified by the convention.

Ready-made example: [diagnostics.config.yaml](../../examples/configuration/diagnostics.config.yaml).

---

**Example configuration:**
```yml
- type: "custom"
  code: "CSTM001"
  description: "custom"
  message: "For the element named '{elementName}' with identifier '{elementId}', the 'Марка' and/or 'Комментарии' parameters are incorrectly set."
  isObsolete: true
  takeDocument: "!property('IsFamilyDocument')"
  take: "instance and builtincategory('OST_Walls')"
  check: "parameter(me, 'Комментарии') == 'Ура!' & parameter(me, 'Марка') == 'Ура!'"
- type: "custom"
  code: "CSTM002"
  description: "custom"
  message: "For the element named '{elementName}' with identifier '{elementId}', the name is incorrectly set"
  severity: "Message"
  isActive: false
  isObsolete: true
  obsoleteDescription: "Some text describing the reason the check is obsolete"
  takeDocument: "true"
  take: "instance and builtincategory('OST_Walls')"
  check: "property('Name') == '1'"
  tags: ["Walls", "Naming"]
- type: "custom"
  code: "CSTM003"
  ...
```

---

**Parameters:**

| Field                  | Type              | Input data type | Formula return type | Purpose                                                                                                            |
| --------------------- | ---------------- | ------------------- | -------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `type`                | string           | `string`            | `-`                              | Rule type; must be `custom` for a user element diagnostic |
| `code`                | string           | `string`            | `-`                              | Unique identifier of the diagnostic                                                                                     |
| `description`         | string           | `string`            | `-`                              | Description of the diagnostic                                                                                                     |
| `message`             | string           | `string`            | `-`                              | Error message template. Available variables: `{elementName}`, `{elementId}`, `{duration}` (default: the localized generic template) (optional) |
| `severity`            | enum     | `string`            | `-`                              | [[Diagnostic severity\|Severity level]] <br>(default: `Message`) (optional)                           |
| `isActive`            | boolean | `bool`              | `-`                              | Whether the diagnostic is active (default: `true`) (optional)                                                           |
| `isHidden`            | boolean | `bool`              | `-`                              | Hides the diagnostic from the panes, the run list and the reports and stops it from running (default: `false`) (optional) |
| `isObsolete`          | boolean | `bool`              | `-`                              | [[Obsolete diagnostic\|Whether the diagnostic is obsolete]] (default: `false`) (optional)                                |
| `obsoleteDescription` | string           | `string`            | `-`                              | [[Obsolete diagnostic\|Description of the reason for obsolescence]] (shown when `isObsolete: true`) (optional) |
| `takeDocument`        | string           | `string`            | `bool`                           | [[Formula syntax\|Document filtering formula]]                                                              |
| `take`                | string           | `string`            | `ElementFilter`                  | [[Formula Revit\|Element filtering formula]]                                                                |
| `check`               | string           | `string`            | `bool`                           | [[Formula syntax\|Element check formula]]; if the result is `true`, the element is considered valid               |
| `fixes`               | sequence         | `-`                 | `-`                              | Optional [[Fix pipelines]] offered for invalid elements. |
| `tags`                | sequence of strings | `string`         | `-`                              | Tags used to find the diagnostic in the panes and shown in the reports (default: none) (optional) |


---

Defaults for `message`, `severity`, `isActive` and `isObsolete`, and the tag rules, are in [[Diagnostic parameters]] and [[Diagnostic tags]].

> **Note:** If `diagnostics.config.yaml` cannot be parsed, the whole file is treated as empty instead of keeping previously loaded rules. Revit Linter reports the configuration error in the UI and writes the exception and configuration path to the application log. A structurally invalid rule is skipped on its own and the other rules stay in use.

> **Note:** If `takeDocument`, `take`, or `check` cannot be compiled, the rule is skipped and the configuration notification names the rule code and the rejected formula; the application log contains the compilation exception. The other rules stay in use.
