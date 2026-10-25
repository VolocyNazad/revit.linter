---
lang: en
---

> Language: **English** · [[Проверки параметров проекта|Русский]]

The list and behavior of diagnostics is defined through the single [[Configuration file|configuration file]] `diagnostics.config.yaml`. A project-parameter check is a rule with `type: "parameter"` and must be located at the [[Diagnostic configuration path convention|path]] specified by the convention.

Ready-made example: [diagnostics.config.yaml](../../examples/configuration/diagnostics.config.yaml).

---

**Example configuration:**`
```yml
- type: "parameter"
  code: "PRMTR001"
  description: "custom"
  message: "Document named '{documentTitle}' contains invalid project parameters. Details: \n\n{details} \n\nExecution time '{duration}' ms."
  severity: "Message"
  isActive: false
  tags: ["Parameters"]
  take: "!property('IsFamilyDocument')"
  parameters:
    - guid: "dd002437-b54b-41b5-9142-71704a513ef3"
      name: "CUBE_ID"
      # Revit 2024 and newer. For Revit 2021-2023 use a BuiltInParameterGroup name such as "PG_TEXT".
      group: "autodesk.parameter.group:text-1.0.0"
      isInstance: true
      categories: ["OST_DuctCurves"]
      allowVaryBetweenGroups: true
    - guid: "2f60ce79-f71b-4b1e-9b74-7ce425cf3cec"
      name: "CUBE_Зависимости"
      group: "autodesk.parameter.group:text-1.0.0"
      isInstance: true
      categories: ["-2008000"]
      allowVaryBetweenGroups: true
    - name: "Какой-то параметр"
      group: "autodesk.parameter.group:text-1.0.0"
      isInstance: true
      categories: ["-2008000"]
      allowVaryBetweenGroups: true
- code: "PRMTR002"
  ...
```

---

**Parameters:**

| Field                  | Type              | Input data type | Formula return type | Purpose                                                                                                            |
| --------------------- | ---------------- | ------------------- | -------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `type`                | string           | `string`            | `-`                              | Rule type; must be `parameter` for a project-parameter diagnostic |
| `code`                | string           | `string`            | `-`                              | Unique identifier of the diagnostic                                                                                     |
| `description`         | string           | `string`            | `-`                              | Description of the diagnostic                                                                                                     |
| `message`             | string           | `string`            | `-`                              | Error message template. Available variables: `{documentTitle}`, `{details}`, `{duration}` (default: the localized generic template) (optional) |
| `severity`            | enum     | `string`            | `-`                              | [[Diagnostic severity\|Severity level]] <br>(default: `Message`) (optional)                           |
| `isObsolete`          | boolean | `bool`              | `-`                              | [[Obsolete diagnostic\|Whether the diagnostic is obsolete]] (default: `false`) (optional)                                |
| `obsoleteDescription` | string           | `string`            | `-`                              | [[Obsolete diagnostic\|Description of the reason for obsolescence]] (shown when `isObsolete: true`) (optional) |
| `isActive`            | boolean | `bool`              | `-`                              | Whether the diagnostic is active (default: `true`) (optional)                                                           |
| `isHidden`            | boolean | `bool`              | `-`                              | Hides the diagnostic from the panes, the run list and the reports and stops it from running (default: `false`) (optional) |
| `take`                | string           | `string`            | `bool`                           | [[Formula syntax\|Document filtering formula]]                                                              |
| `parameters`          | list           | `array`             | `-`                              | The list of project parameters to check                                                                                |
| `tags`                | sequence of strings | `string`         | `-`                              | Tags used to find the diagnostic in the panes and shown in the reports (default: none) (optional)                       |

---

**Parameters (fields inside `parameters`):**

|Field|Type|Input data type|Formula return type|Purpose|
|---|---|---|---|---|
|`guid`|string|`string`|`-`|Parameter GUID|
|`name`|string|`string`|`-`|Parameter name|
|`group`|enum|`string`|`-`|Expected parameter grouping|
|`isInstance`|boolean|`bool`|`-`|Whether the parameter should be an instance parameter (true) or a type parameter (false)|
|`categories`|list|`array`|`-`|Expected list of categories the parameter applies to (by ID or built-in names)|
|`allowVaryBetweenGroups`|boolean|`bool`|`-`|Whether the parameter is allowed to vary between groups|

---

**`group` format by Revit version:**

- Revit 2021–2023: a `BuiltInParameterGroup` name such as `PG_TEXT`, or its numeric identifier.
- Revit 2024 and newer: the complete `ForgeTypeId`, for example `autodesk.parameter.group:text-1.0.0`.

The formats are intentionally version-specific. Store each configuration in the folder for its Revit version.

**Validation on load:** each rule is checked when the file is loaded. A rule with an unknown category, a `group` that is not valid for the running Revit version, an invalid `guid`, or a missing `code`, `take`, `parameters`, `name` or `categories` is skipped, and a message names the rule, the parameter and the rejected value. The other rules stay in use. Fix the file and save it to reload the rules.

---

**Notes:**
- If the `message` field is not specified, it defaults to the localized generic template
- If the `severity` field is not specified, it defaults to `Message`
- If the `isActive` field is not specified, it defaults to `true`
- If the `isObsolete` field is not specified, it defaults to `false`
- Tags are free text; the search and the tag matching ignore case. The document-kind tags `Project` (runs in project documents) and `Family` (runs in family documents) are ordinary tags a rule declares in `tags`; see [[Diagnostic tags]] for the product's reserved tags
