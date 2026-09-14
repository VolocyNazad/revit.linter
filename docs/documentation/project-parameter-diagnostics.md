## 📋 Project Parameter Diagnostics

The list and behavior of diagnostics is defined through a configuration file with a `.yaml` extension, which in turn must be located at the [[diagnostic-configuration-path-convension|path]] specified by the convention. The file must be named `parameter-element.config.yaml`.

Ready-made file: [parameter-element.config.yaml](../examples/configuration/parameter-element.config.yaml).

---

**Example configuration:**`
```yml
- code: "PRMTR001"
  description: "custom"
  message: "Document named '{documentTitle}' contains invalid project parameters. Details: \n\n{details} \n\nExecution time '{duration}' ms."
  severity: "Message"
  isActive: false
  take: "!property('IsFamilyDocument')"
  parameters:
    - guid: "dd002437-b54b-41b5-9142-71704a513ef3"
      name: "CUBE_ID"
      group: "PG_TEXT"
      isInstance: true
      categories: ["OST_DuctCurves"]
      allowVaryBetweenGroups: true
    - guid: "2f60ce79-f71b-4b1e-9b74-7ce425cf3cec"
      name: "CUBE_Зависимости"
      group: "PG_TEXT"
      isInstance: true
      categories: ["-2008000"]
      allowVaryBetweenGroups: true
    - name: "Какой-то параметр"
      group: "PG_TEXT"
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
| `code`                | string           | `string`            | `-`                              | Unique identifier of the diagnostic                                                                                     |
| `description`         | string           | `string`            | `-`                              | Description of the diagnostic                                                                                                     |
| `message`             | string           | `string`            | `-`                              | Error message template. Available variables: `{documentTitle}`, `{details}`, `{duration}`                        |
| `severity`            | enum     | `string`            | `-`                              | [[diagnostic-severity\|Severity level]] <br>(default: `Message`) (optional)                           |
| `isObsolete`          | boolean | `bool`              | `-`                              | [[obsolete-diagnostic\|Whether the diagnostic is obsolete]] (default: `false`) (optional)                                |
| `obsoleteDescription` | string           | `string`            | `-`                              | [[obsolete-diagnostic\|Description of the reason for obsolescence]] (shown when `isObsolete: true`) (optional) |
| `isActive`            | boolean | `bool`              | `-`                              | Whether the diagnostic is active (default: `true`) (optional)                                                           |
| `take`                | string           | `string`            | `bool`                           | Document filtering [[syntax/formula-syntax\|formula]]                                                              |
| `parameters`          | list           | `array`             | `-`                              | The list of project parameters to check                                                                                |

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

**Notes:**
- If the `severity` field is not specified, it defaults to `Message`
- If the `isActive` field is not specified, it defaults to `true`
- If the `isObsolete` field is not specified, it defaults to `false`
