---
lang: en
---

> Language: **English** · [[Пайплайны исправлений|Русский]]

# Fix pipelines

A user element diagnostic can declare named fix pipelines. Each pipeline appears as an available fix in the diagnostic report. Its steps run from top to bottom inside the report presenter's Revit transaction.

```yaml
fixes:
  - name: "Delete"
    steps:
      - type: "Delete"
        elementSets: ["Target"]
  - name: "Mark as checked"
    steps:
      - type: "SetParameter"
        parameter: "ALL_MODEL_INSTANCE_COMMENTS"
        value: "'checked ' + now('dd.MM.yyyy')"
```

`name` is required and must not be empty. `steps` is a required non-empty sequence. Omitting `fixes` leaves the diagnostic without configuration-driven fixes.

`elementSets` selects the named element groups consumed by a step:

- `Target` — the element that produced the finding;
- `Dependencies` — related elements attached to the finding.

For safety, an omitted or empty `elementSets` list selects only `Target`. Unknown set names stop the fix and roll back its transaction. To delete both sides of a collision-like finding, specify `elementSets: ["Target", "Dependencies"]` explicitly.

## Steps

| `type` | Effect |
| --- | --- |
| `SetParameter` | Writes one parameter value for every element selected through `elementSets`. |
| `Delete` | Deletes the elements selected through `elementSets` and any elements that Revit considers fully dependent on them. No confirmation is shown by Revit, and pinned elements can also be deleted. |

`Delete` is terminal and must be the final pipeline step. A `SetParameter` step is allowed before it, and a pipeline may contain several of them. If a step fails, the containing transaction is not committed. The **fix all** action keeps fixes that succeeded for other findings and reports individual failures.

## SetParameter

`parameter` identifies the parameter of the element itself:

1. a `BuiltInParameter` name, for example `ALL_MODEL_INSTANCE_COMMENTS`;
2. a shared parameter GUID;
3. the parameter display name. The display name is localized, so prefer the first two forms.

`value` is a formula in the element grammar — the same grammar `check` uses: `parameter(me, '…')`, `property('…')`, `if(…)`, string functions and arithmetic.

The formula result is converted to the storage type of the parameter:

| Result | Storage type | Written as |
| --- | --- | --- |
| string | `String` | as is |
| number | `Double` | in the project unit of the parameter |
| number | `Integer` | rounded away from zero |
| number | Yes/No `Integer` | `0` or `1` |
| `true` / `false` | Yes/No `Integer` | `0` or `1` |
| number | `String` | invariant culture, exactly like `str(…)` |
| `ElementId` | `ElementId` | only an element that exists in the same document |

A missing parameter, a read-only parameter, an empty value and a value type that does not fit the storage type leave the parameter unchanged: the step reports failure, the log names the element and the reason, and the containing transaction is not committed. A formula that cannot be compiled rejects the fix when the configuration is loaded; the diagnostic itself stays active and the configuration error names the fix and the step.

> [!warning]
> Numbers are written in the project units of the parameter: `value: "3000"` means 3000 millimetres in a metric project and 3000 metres in a metres project. A `property(…)` result is a raw Revit API value in internal units — feet for lengths and radians for angles — so do not mix it with `parameter(…)` in one expression.

> [!note]
> When the finding is an element type, the step writes the type parameter, so every instance of that type changes; the log records a warning.

> [!note]
> The light-bulb button immediately runs a single available fix. With multiple fixes it opens a choice menu instead. This behavior is the same as the eye button used for visualization pipelines.

> [!note]
> **Fix all** processes every finding with the same diagnostic code, not only the rows currently visible after filtering. Successful changes are kept when another finding fails; retry skips elements that are already fixed or no longer valid.
