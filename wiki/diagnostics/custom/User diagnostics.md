---
lang: en
---

> Language: **English** · [[Пользовательские проверки|Русский]]

The list and behavior of diagnostics is defined through a configuration file with a `.yaml` extension, which in turn must be located at the [[Diagnostic configuration path convention|path]] specified by the convention. The file must be named `config.yaml`.

Ready-made file: [config.yaml](../../examples/configuration/config.yaml).

---

**Example configuration:**
```yml
- code: "CSTM001"
  description: "custom"
  message: "For the element named '{elementName}' with identifier '{elementId}', the 'Марка' and/or 'Комментарии' parameters are incorrectly set. Execution time '{duration}' ms."
  isObsolete: true
  takeDocument: "!property('IsFamilyDocument')"
  take: "instance and builtincategory('OST_Walls')"
  check: "parameter(me, 'Комментарии') == 'Ура!' & parameter(me, 'Марка') == 'Ура!'"
- code: "CSTM002"
  description: "custom"
  message: "For the element named '{elementName}' with identifier '{elementId}', the name is incorrectly set"
  severity: "Message"
  isActive: false
  isObsolete: true
  obsoleteDescription: "Some text describing the reason the check is obsolete"
  takeDocument: "true"
  take: "instance and builtincategory('OST_Walls')"
  check: "property('Name') == '1'"
- code: "CSTM003"
  ...
```

---

**Parameters:**

| Field                  | Type              | Input data type | Formula return type | Purpose                                                                                                            |
| --------------------- | ---------------- | ------------------- | -------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `code`                | string           | `string`            | `-`                              | Unique identifier of the diagnostic                                                                                     |
| `description`         | string           | `string`            | `-`                              | Description of the diagnostic                                                                                                     |
| `message`             | string           | `string`            | `-`                              | Error message template. Available variables: `{elementName}`, `{elementId}`, `{duration}`                        |
| `severity`            | enum     | `string`            | `-`                              | [[Diagnostic severity\|Severity level]] <br>(default: `Message`) (optional)                           |
| `isActive`            | boolean | `bool`              | `-`                              | Whether the diagnostic is active (default: `true`) (optional)                                                           |
| `isObsolete`          | boolean | `bool`              | `-`                              | [[Obsolete diagnostic\|Whether the diagnostic is obsolete]] (default: `false`) (optional)                                |
| `obsoleteDescription` | string           | `string`            | `-`                              | [[Obsolete diagnostic\|Description of the reason for obsolescence]] (shown when `isObsolete: true`) (optional) |
| `takeDocument`        | string           | `string`            | `bool`                           | Document filtering [[syntax/Formula syntax\|formula]]                                                              |
| `take`                | string           | `string`            | `ElementFilter`                  | [[syntax/Formula Revit\|Element filtering formula]]                                                                |
| `check`               | string           | `string`            | `bool`                           | [[syntax/Formula syntax\|Element check formula]]; if the result is `true`, the element is considered valid               |


---

**Notes:**
- If the `severity` field is not specified, it defaults to `Message`
- If the `isActive` field is not specified, it defaults to `true`
- If the `isObsolete` field is not specified, it defaults to `false`
