---
lang: en
---

> Language: **English** · [[Проверки коллизий|Русский]]

The list and behavior of diagnostics is defined through the single [[Configuration file|configuration file]] `diagnostics.config.yaml`. A collision check is a rule with `type: "collision"` and must be located at the [[Diagnostic configuration path convention|path]] specified by the convention.

Ready-made example: [diagnostics.config.yaml](../../examples/configuration/diagnostics.config.yaml).

---

**Example configuration:**
```yml
- type: "collision"
  code: "CLSN001"
  description: "custom"
  message: "Element named '{elementName}' with identifier '{elementId}' intersects {intersection.count} element(s) '{intersection.elementNames}' with identifiers '{intersection.elementIds}'. Execution time '{duration}' ms."
  severity: "Warning"
  isActive: true
  tags: ["MEP", "Coordination"]
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
            halftone: false
            transparency: 15
            projectionLines: { color: "#A63D40", pattern: "Solid", weight: 6 }
            cutLines: { color: "#7C2D30", pattern: "Solid", weight: 8 }
            surfaceForeground: { color: "#A63D40", pattern: "SolidFill", isVisible: true }
            surfaceBackground: { color: "#E3BFB9", pattern: "SolidFill", isVisible: true }
            cutForeground: { color: "#7C2D30", pattern: "SolidFill", isVisible: true }
            cutBackground: { color: "#EFD5D1", pattern: "SolidFill", isVisible: true }
        - type: "OverrideElements"
          elementSets: ["Dependencies"]
          style:
            halftone: false
            transparency: 35
            projectionLines: { color: "#5E8261", pattern: "Solid", weight: 6 }
            cutLines: { color: "#425C43", pattern: "Solid", weight: 8 }
            surfaceForeground: { color: "#5E8261", pattern: "SolidFill", isVisible: true }
            surfaceBackground: { color: "#C6D6C6", pattern: "SolidFill", isVisible: true }
            cutForeground: { color: "#425C43", pattern: "SolidFill", isVisible: true }
            cutBackground: { color: "#DCE6DC", pattern: "SolidFill", isVisible: true }
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
            halftone: false
            transparency: 15
            projectionLines: { color: "#A07C2C", pattern: "Solid", weight: 6 }
            cutLines: { color: "#6E5620", pattern: "Solid", weight: 8 }
            surfaceForeground: { color: "#A07C2C", pattern: "SolidFill", isVisible: true }
            surfaceBackground: { color: "#E6D3AC", pattern: "SolidFill", isVisible: true }
            cutForeground: { color: "#6E5620", pattern: "SolidFill", isVisible: true }
            cutBackground: { color: "#F0E4C8", pattern: "SolidFill", isVisible: true }
        - type: "OverrideFilter"
          elementSets: ["Dependencies"]
          style:
            halftone: true
            transparency: 35
            projectionLines: { color: "#5B7FA6", pattern: "Solid", weight: 6 }
            cutLines: { color: "#3E5A78", pattern: "Solid", weight: 8 }
            surfaceForeground: { color: "#5B7FA6", pattern: "SolidFill", isVisible: true }
            surfaceBackground: { color: "#C3D2E2", pattern: "SolidFill", isVisible: true }
            cutForeground: { color: "#3E5A78", pattern: "SolidFill", isVisible: true }
            cutBackground: { color: "#DDE6F0", pattern: "SolidFill", isVisible: true }
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
| `type`                | string          | `string`            | `-`                              | Rule type; must be `collision` for a collision diagnostic |
| `code`                | string          | `string`            | `-`                              | Unique identifier of the diagnostic                                                                                                                        |
| `description`         | string          | `string`            | `-`                              | Description of the diagnostic                                                                                                                                        |
| `message`             | string          | `string`            | `-`                              | Error message template. Available variables: `{elementName}`, `{elementId}`, `{intersection.elementNames}`, `{intersection.elementIds}`, `{intersection.count}`, `{duration}`. A finding lists every element the target intersects: `{intersection.elementNames}` and `{intersection.elementIds}` are comma-separated lists in the same order, and `{intersection.count}` is their number (default: the localized generic template) (optional) |
| `severity`            | enum    | `string`            | `-`                              | [[Diagnostic severity\|Severity level]] (default: `Message`) (optional)                                                                  |
| `isActive`            | boolean | `bool`              | `-`                              | Whether the diagnostic is active (default: `true`) (optional)                                                                                              |
| `isHidden`            | boolean | `bool`              | `-`                              | Hides the diagnostic from the panes, the run list and the reports and stops it from running (default: `false`) (optional) |
| `isObsolete`          | boolean | `bool`              | `-`                              | [[Obsolete diagnostic\|Whether the diagnostic is obsolete]] (default: `false`) (optional)                                                                   |
| `obsoleteDescription` | string          | `string`            | `-`                              | [[Obsolete diagnostic\|Description of the reason for obsolescence]] (shown when `isObsolete: true`) (optional)                                    |
| `takeDocument`        | string          | `string`            | `bool`                           | [[Formula syntax\|Document filtering formula]]                                                                                                 |
| `take`                | string          | `string`            | `ElementFilter`                  | [[Formula Revit\|Formula selecting the first element group]] for collision checks                                                                   |
| `andTake`             | string          | `string`            | `ElementFilter`                  | [[Formula Revit\|Formula selecting the second element group]] against which intersections are checked                                                      |
| `groupBy`             | string          | `string`            | `object`                         | [[Formula Revit\|Element grouping formula]]. Collision detection runs within groups                                                         |
| `includeLinks`          | boolean         | `bool`              | `-`                              | Whether loaded Revit links contribute read-only candidates (default: `false`) (optional)                                                      |
| `ignoreConnectedElements` | boolean   | `bool`              | `-`                              | Whether elements joined by MEP connectors are excluded from the check (default: `false`) (optional)                                          |
| `tags`                | sequence of strings | `string`         | `-`                              | Tags used to find the diagnostic in the panes and shown in the reports (default: none) (optional)                                                |

---

## Linked models and connected elements

- Links take part only when a rule sets `includeLinks: true` (default `false`), so existing configurations behave as before.
- All loaded links participate; there is no per-link selection. Only direct links are supported, unloaded links are skipped with a warning, and findings stay on host elements: link-versus-link pairs are never checked.
- Linked hits are text-only in the report: they never enter the dependency list, so visualizations highlight the host target alone and fixes stay host-only. Linked element names appear as `Name [Link]` in `{intersection.elementNames}`.
- A link whose transform is not conformal is reported by bounding boxes only, without the solids check.
- `ignoreConnectedElements: true` skips elements joined to the target by MEP connectors: they touch by design while their family geometry overlaps at the joint. A joined pair that genuinely collides elsewhere is skipped as well. The flag applies to host pairs only: connectors cannot join elements across Revit links.

**Notes:**
- If the `message` field is not specified, it defaults to the localized generic template
- If the `severity` field is not specified, it defaults to `Message`
- If the `isActive` field is not specified, it defaults to `true`
- If the `isObsolete` field is not specified, it defaults to `false`
- If the `includeLinks` field is not specified, it defaults to `false`: linked models take part only when explicitly enabled, and their hits stay text-only in the report.
- If the `ignoreConnectedElements` field is not specified, it defaults to `false`. Set it to `true` to skip joined elements: they touch by design while their family geometry overlaps at the joint, so they would otherwise be reported as collisions. A joined pair that genuinely collides elsewhere is skipped as well. The flag applies to host pairs only: connectors cannot join elements across Revit links.
- Tags are free text; the search and the tag matching ignore case. The document-kind tags `Project` (runs in project documents) and `Family` (runs in family documents) are ordinary tags a rule declares in `tags`; see [[Diagnostic tags]] for the product's reserved tags.
