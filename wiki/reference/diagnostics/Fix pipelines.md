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
```

`name` is required and must not be empty. `steps` is a required non-empty sequence. Omitting `fixes` leaves the diagnostic without configuration-driven fixes.

`elementSets` selects the named element groups consumed by a step:

- `Target` — the element that produced the finding;
- `Dependencies` — related elements attached to the finding.

For safety, an omitted or empty `elementSets` list selects only `Target`. Unknown set names stop the fix and roll back its transaction. To delete both sides of a collision-like finding, specify `elementSets: ["Target", "Dependencies"]` explicitly.

## Steps

| `type` | Effect |
| --- | --- |
| `Delete` | Deletes the elements selected through `elementSets` and any elements that Revit considers fully dependent on them. No confirmation is shown by Revit, and pinned elements can also be deleted. |

`Delete` is terminal and must be the final pipeline step. If a step fails, the containing transaction is not committed. The **fix all** action keeps fixes that succeeded for other findings and reports individual failures.

> **Note:** The light-bulb button immediately runs a single available fix. With multiple fixes it opens a choice menu instead. This behavior is the same as the eye button used for visualization pipelines.

> **Note:** **Fix all** processes every finding with the same diagnostic code, not only the rows currently visible after filtering. Successful changes are kept when another finding fails; retry skips elements that are already fixed or no longer valid.
