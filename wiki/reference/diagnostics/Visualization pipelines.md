---
lang: en
---

> Language: **English** · [[Пайплайны визуализации|Русский]]

# Visualization pipelines

An element diagnostic can declare several named visualization pipelines. The report shows the only pipeline directly or lets the user choose one. Steps run from top to bottom. When the visualization ends, every applied step is restored in reverse order.

```yaml
visualizations:
  - name: "Show and select"
    steps:
      - type: "Show"
        elementSets: ["Target"]
      - type: "Select"
        elementSets: ["Target"]
```

`name` is required and must not be empty. `steps` is a required non-empty sequence. A diagnostic without `visualizations` receives the default `Show and select` pipeline.

## Element sets

`elementSets` selects named groups supplied by the diagnostic. An omitted or empty list combines every available group. Unknown names stop the pipeline with a configuration error.

| Key | Meaning |
| --- | --- |
| `Target` | Elements that directly produced the finding. |
| `Dependencies` | Related elements that explain the finding, including the other side of a collision. |

## Steps

| `type` | Required fields | Effect and rollback |
| --- | --- | --- |
| `Show` | none | Zooms to the elements in the current view without opening another view; restores the previous zoom rectangle. |
| `Select` | none | Selects the elements; restores the previous selection. |
| `Isolate` | none | Temporarily isolates the elements; disables the isolation on rollback. It does not replace an isolation already active before the pipeline. |
| `Cut` | none | Fits a 3D section box to the elements; restores the previous box and its active state. |
| `OverrideElements` | `style` | Applies per-element view overrides; restores each element's previous overrides. |
| `OverrideFilter` | `style` | Creates a temporary `SelectionFilterElement`; removes only that filter on rollback. |
| `DetailLevel` | `detailLevel` | Sets the view detail level; restores the previous level. |
| `DisplayStyle` | `displayStyle` | Sets the view display style; restores the previous style. |
| `SetCategoryVisibility` | `categories` | Hides every hideable category except the listed ones through the persistent view visibility; unhides exactly the hidden ones on rollback. It composes with an isolation already active before the pipeline. |

`style` is invalid on `Show`, `Select`, `Isolate`, and `Cut`. It is required on both override steps. `style` and `elementSets` are invalid on `DetailLevel` and `DisplayStyle`: view steps address the whole view. `style` and `elementSets` are invalid on `SetCategoryVisibility`: it takes `categories`, a list of `BuiltInCategory` names such as `OST_Walls`, matched case-insensitively. Categories absent from the document are ignored.

`detailLevel` accepts `Coarse`, `Medium` or `Fine`. `displayStyle` accepts `Wireframe`, `HLR`, `Shading`, `ShadingWithEdges`, `Rendering`, `Realistic`, `FlatColors`, `RealisticWithEdges` and `Textures` (`Textures` needs Revit 2024 or later). Names are matched case-insensitively. A view without the requested setting fails the step; completed steps roll back as usual.

```yaml
- type: "DetailLevel"
  detailLevel: "Fine"
- type: "DisplayStyle"
  displayStyle: "Shading"
- type: "SetCategoryVisibility"
  categories: ["OST_Walls", "OST_Floors"]
```

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
    color: "#A63D40"
    pattern: "Solid"
    weight: 6
  cutLines:
    color: "#7C2D30"
    weight: 8
  surfaceForeground:
    color: "#A63D40"
    pattern: "SolidFill"
    isVisible: true
  surfaceBackground:
    color: "#E3BFB9"
    pattern: "SolidFill"
    isVisible: true
  cutForeground:
    color: "#7C2D30"
    pattern: "SolidFill"
    isVisible: true
  cutBackground:
    color: "#EFD5D1"
    pattern: "SolidFill"
    isVisible: true
```

## Complete collision example

```yaml
visualizations:
  - name: "Collision: both sides"
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

  - name: "Collision through filters"
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
      - type: "Isolate"
        elementSets: ["Target", "Dependencies"]
      - type: "Select"
        elementSets: ["Target"]
```

Only one pipeline is active at a time. Starting another pipeline first restores the active one. Closing or clearing the report and changing the active Revit context also restore the visualization.

> **Note:** The eye button is a toggle for the active pipeline and finding. Clicking it again restores the recorded state. `Show` changes the view framing; it does not promise keyboard focus for the report panel or Revit canvas.

> **Note:** A pipeline is skipped when its report document is closed, invalid, or not currently active. If a later step fails, completed steps are restored in reverse order; restoration failures are retained for a later retry and written to the application log.
