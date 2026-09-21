---
lang: en
---

> Language: **English** · [[Проверки коллизий|Русский]]

The list and behavior of diagnostics is defined through a configuration file with a `.yaml` extension, which in turn must be located at the [[Diagnostic configuration path convention|path]] specified by the convention. The file must be named `collision.config.yaml`.

Ready-made file: [collision.config.yaml](../../examples/configuration/collision.config.yaml).

---

**Example configuration:**
```yml
- code: "CLSN001"
  description: "custom"
  message: "Element named '{elementName}' with identifier '{elementId}' intersects element '{intersection.elementName}' with identifier '{intersection.elementId}'. Execution time '{duration}' ms."
  severity: "Warning"
  isActive: true
  takeDocument: "property('Title') != '' & !property('IsFamilyDocument')"
  take: "instance and (class('Pipe') or class('Duct') or class('CableTray') or class('Conduit') or builtincategory('OST_DuctFitting') or builtincategory('OST_PipeFitting') or builtincategory('OST_CableTrayFitting') or builtincategory('OST_ConduitFitting'))"
  andTake: "instance and (class('Pipe') or class('Duct') or class('CableTray') or class('Conduit') or builtincategory('OST_DuctFitting') or builtincategory('OST_PipeFitting') or builtincategory('OST_CableTrayFitting') or builtincategory('OST_ConduitFitting'))"
  groupBy: "parameter(me, 'Комментарии')"
- code: "CLSN002"
...
```

---

**Parameters:**

| Field                  | Type             | Input data type | Formula return type | Purpose                                                                                                                                               |
| --------------------- | --------------- | ------------------- | -------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `code`                | string          | `string`            | `-`                              | Unique identifier of the diagnostic                                                                                                                        |
| `description`         | string          | `string`            | `-`                              | Description of the diagnostic                                                                                                                                        |
| `message`             | string          | `string`            | `-`                              | Error message template. Available variables: `{elementName}`, `{elementId}`, `{intersection.elementName}`, `{intersection.elementId}`, `{duration}` |
| `severity`            | enum    | `string`            | `-`                              | [[Diagnostic severity\|Severity level]] (default: `Message`) (optional)                                                                  |
| `isActive`            | boolean | `bool`              | `-`                              | Whether the diagnostic is active (default: `true`) (optional)                                                                                              |
| `isObsolete`          | boolean | `bool`              | `-`                              | [[Obsolete diagnostic\|Whether the diagnostic is obsolete]] (default: `false`) (optional)                                                                   |
| `obsoleteDescription` | string          | `string`            | `-`                              | [[Obsolete diagnostic\|Description of the reason for obsolescence]] (shown when `isObsolete: true`) (optional)                                    |
| `takeDocument`        | string          | `string`            | `bool`                           | Document filtering [[syntax/Formula syntax\|formula]]                                                                                                 |
| `take`                | string          | `string`            | `ElementFilter`                  | [[syntax/Formula Revit\|Formula]] selecting the first group of elements to check for collisions                                                                   |
| `andTake`             | string          | `string`            | `ElementFilter`                  | [[syntax/Formula Revit\|Formula]] selecting the second group of elements against which intersections are checked                                                      |
| `groupBy`             | string          | `string`            | `object`                         | Element [[syntax/Formula Revit\|grouping formula]]. Collision detection runs within groups                                                         |

---

**Notes:**
- If the `severity` field is not specified, it defaults to `Message`
- If the `isActive` field is not specified, it defaults to `true`
- If the `isObsolete` field is not specified, it defaults to `false`
