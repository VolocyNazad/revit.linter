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
  message: "Element named '{elementName}' with identifier '{elementId}' intersects {intersection.count} element(s) '{intersection.elementNames}' with identifiers '{intersection.elementIds}'. Execution time '{duration}' ms."
  severity: "Warning"
  isActive: true
  takeDocument: "property('Title') != '' & !property('IsFamilyDocument')"
  take: "instance and (class('Pipe') or class('Duct') or class('CableTray') or class('Conduit') or builtincategory('OST_DuctFitting') or builtincategory('OST_PipeFitting') or builtincategory('OST_CableTrayFitting') or builtincategory('OST_ConduitFitting'))"
  andTake: "instance and (class('Pipe') or class('Duct') or class('CableTray') or class('Conduit') or builtincategory('OST_DuctFitting') or builtincategory('OST_PipeFitting') or builtincategory('OST_CableTrayFitting') or builtincategory('OST_ConduitFitting'))"
  groupBy: "parameter(me, 'Комментарии')"
  visualizations:
    - name: "Locate collision"
      steps:
        - type: "OverrideElements"
          elementSets: ["Target"]
          style:
            projectionLines: { color: "#E53935", weight: 6 }
            surfaceForeground: { color: "#E53935", pattern: "SolidFill", isVisible: true }
        - type: "OverrideElements"
          elementSets: ["Dependencies"]
          style:
            projectionLines: { color: "#43A047", weight: 6 }
            surfaceForeground: { color: "#43A047", pattern: "SolidFill", isVisible: true }
        - type: "Show"
          elementSets: ["Target", "Dependencies"]
        - type: "Cut"
          elementSets: ["Target", "Dependencies"]
    - name: "Select collision"
      steps:
        - type: "Select"
          elementSets: ["Target", "Dependencies"]
    - name: "Isolate collision"
      steps:
        - type: "Isolate"
          elementSets: ["Target", "Dependencies"]
        - type: "Show"
          elementSets: ["Target", "Dependencies"]
    - name: "Show with filters"
      steps:
        - type: "OverrideFilter"
          elementSets: ["Target"]
          style:
            surfaceForeground: { color: "#FF9800", pattern: "SolidFill", isVisible: true }
        - type: "OverrideFilter"
          elementSets: ["Dependencies"]
          style:
            halftone: true
            surfaceForeground: { color: "#1E88E5", pattern: "SolidFill", isVisible: true }
    - name: "Test all graphics"
      steps:
        - type: "OverrideElements"
          elementSets: ["Target"]
          style:
            halftone: true
            transparency: 25
            detailLevel: "Fine"
            projectionLines: { color: "#D81B60", weight: 7 }
            cutLines: { color: "#8E24AA", weight: 9 }
            surfaceForeground: { color: "#D81B60", pattern: "SolidFill", isVisible: true }
            surfaceBackground: { color: "#F8BBD0", pattern: "SolidFill", isVisible: true }
            cutForeground: { color: "#8E24AA", pattern: "SolidFill", isVisible: true }
            cutBackground: { color: "#E1BEE7", pattern: "SolidFill", isVisible: true }
- code: "CLSN002"
...
```

---

**Parameters:**

| Field                  | Type             | Input data type | Formula return type | Purpose                                                                                                                                               |
| --------------------- | --------------- | ------------------- | -------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `code`                | string          | `string`            | `-`                              | Unique identifier of the diagnostic                                                                                                                        |
| `description`         | string          | `string`            | `-`                              | Description of the diagnostic                                                                                                                                        |
| `message`             | string          | `string`            | `-`                              | Error message template. Available variables: `{elementName}`, `{elementId}`, `{intersection.elementNames}`, `{intersection.elementIds}`, `{intersection.count}`, `{duration}`. A finding lists every element the target intersects: `{intersection.elementNames}` and `{intersection.elementIds}` are comma-separated lists in the same order, and `{intersection.count}` is their number |
| `severity`            | enum    | `string`            | `-`                              | [[Diagnostic severity\|Severity level]] (default: `Message`) (optional)                                                                  |
| `isActive`            | boolean | `bool`              | `-`                              | Whether the diagnostic is active (default: `true`) (optional)                                                                                              |
| `isObsolete`          | boolean | `bool`              | `-`                              | [[Obsolete diagnostic\|Whether the diagnostic is obsolete]] (default: `false`) (optional)                                                                   |
| `obsoleteDescription` | string          | `string`            | `-`                              | [[Obsolete diagnostic\|Description of the reason for obsolescence]] (shown when `isObsolete: true`) (optional)                                    |
| `takeDocument`        | string          | `string`            | `bool`                           | [[Formula syntax\|Document filtering formula]]                                                                                                 |
| `take`                | string          | `string`            | `ElementFilter`                  | [[Formula Revit\|Formula selecting the first element group]] for collision checks                                                                   |
| `andTake`             | string          | `string`            | `ElementFilter`                  | [[Formula Revit\|Formula selecting the second element group]] against which intersections are checked                                                      |
| `groupBy`             | string          | `string`            | `object`                         | [[Formula Revit\|Element grouping formula]]. Collision detection runs within groups                                                         |

---

**Notes:**
- If the `severity` field is not specified, it defaults to `Message`
- If the `isActive` field is not specified, it defaults to `true`
- If the `isObsolete` field is not specified, it defaults to `false`
