---
lang: en
---

> Language: **English** · [[Пайплайны визуализации|Русский]]

# Visualization pipelines

An element diagnostic can declare several named visualization pipelines. The report shows the only pipeline directly or lets the user choose one. Steps run from top to bottom. When the visualization ends, every applied step is restored in reverse order.

```yaml
visualizations:
  - name: "Locate"
    steps:
      - type: "Show"
        elementSets: ["Target"]
```

`name` is required and must not be empty. `steps` is a required non-empty sequence. A diagnostic without `visualizations` receives the default `Show` pipeline.

## Element sets

`elementSets` selects named groups supplied by the diagnostic. An omitted or empty list combines every available group. Unknown names stop the pipeline with a configuration error.

| Key | Meaning |
| --- | --- |
| `Target` | Elements that directly produced the finding. |
| `Dependencies` | Related elements that explain the finding, including the other side of a collision. |

## Steps

| `type` | Required fields | Effect and rollback |
| --- | --- | --- |
| `Show` | none | Zooms to the elements; restores the previous zoom rectangle. |
| `Select` | none | Selects the elements; restores the previous selection. |
| `Isolate` | none | Temporarily isolates the elements; disables the isolation on rollback. It does not replace an isolation already active before the pipeline. |
| `Cut` | none | Fits a 3D section box to the elements; restores the previous box and its active state. |
| `OverrideElements` | `style` | Applies per-element view overrides; restores each element's previous overrides. |
| `OverrideFilter` | `style` | Creates a temporary `SelectionFilterElement`; removes only that filter on rollback. |

`style` is invalid on `Show`, `Select`, `Isolate`, and `Cut`. It is required on both override steps.

## Graphics style

Every property is optional. An omitted property preserves the corresponding Revit setting.

| Property | Type and range |
| --- | --- |
| `halftone` | `true` or `false` |
| `transparency` | integer from `0` (opaque) to `100` (transparent) |
| `detailLevel` | `Coarse`, `Medium`, or `Fine` |
| `projectionLines`, `cutLines` | line style |
| `surfaceForeground`, `surfaceBackground`, `cutForeground`, `cutBackground` | pattern style |

A line style supports `color` in `#RRGGBB` format, `pattern` by Revit line-pattern name (`Solid` selects a solid line), and `weight` from `1` to `16`.

A pattern style supports `color` in `#RRGGBB` format, `pattern` by Revit fill-pattern name (`SolidFill` selects the document's solid fill), and `isVisible`. Pattern names are matched case-insensitively in the current document; a missing pattern stops the visualization and rolls back completed steps.

```yaml
style:
  halftone: false
  transparency: 25
  detailLevel: "Fine"
  projectionLines:
    color: "#E53935"
    pattern: "Solid"
    weight: 6
  cutLines:
    color: "#B71C1C"
    weight: 8
  surfaceForeground:
    color: "#E53935"
    pattern: "SolidFill"
    isVisible: true
  surfaceBackground:
    color: "#FFCDD2"
    pattern: "SolidFill"
    isVisible: true
  cutForeground:
    color: "#B71C1C"
    pattern: "SolidFill"
    isVisible: true
  cutBackground:
    color: "#FF8A80"
    pattern: "SolidFill"
    isVisible: true
```

## Complete collision example

```yaml
visualizations:
  - name: "Collision: red and green"
    steps:
      - type: "OverrideElements"
        elementSets: ["Target"]
        style:
          transparency: 15
          projectionLines: { color: "#E53935", weight: 6 }
          surfaceForeground: { color: "#E53935", pattern: "SolidFill", isVisible: true }
      - type: "OverrideElements"
        elementSets: ["Dependencies"]
        style:
          transparency: 35
          projectionLines: { color: "#43A047", weight: 6 }
          surfaceForeground: { color: "#43A047", pattern: "SolidFill", isVisible: true }
      - type: "Show"
        elementSets: ["Target", "Dependencies"]
      - type: "Cut"
        elementSets: ["Target", "Dependencies"]

  - name: "Collision through filters"
    steps:
      - type: "OverrideFilter"
        elementSets: ["Target"]
        style:
          halftone: false
          surfaceForeground: { color: "#E53935", pattern: "SolidFill", isVisible: true }
      - type: "OverrideFilter"
        elementSets: ["Dependencies"]
        style:
          halftone: true
          surfaceForeground: { color: "#43A047", pattern: "SolidFill", isVisible: true }
      - type: "Isolate"
        elementSets: ["Target", "Dependencies"]
      - type: "Select"
        elementSets: ["Target"]
```

Only one pipeline is active at a time. Starting another pipeline first restores the active one. Closing or clearing the report and changing the active Revit context also restore the visualization.
