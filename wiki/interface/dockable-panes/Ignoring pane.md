---
lang: en
---

> Language: **English** · [[Панель игнорирования|Русский]]

# Ignoring pane

The **Ignore list** pane shows which diagnostics are suppressed in the active document and lets you take an ignore back. Revit opens it as a tab beside the results panes. The list is stored inside the model, so it travels with the file and needs no configuration.

The pane follows the active document: switching documents shows the ignore list of the other document.

## Columns

| Column | Meaning |
| --- | --- |
| **Code** | The diagnostic code the entry applies to. |
| **Scope** | **Element** — only that element is suppressed; **Document** — the whole diagnostic is suppressed in this document. |
| **Id** | The Revit element id, empty for an entry of the whole document. |
| **Target** | The element name, the document title, or **Deleted element** when the stored element no longer exists. |

The **Code** column sorts by the text of the code and by its numbers, so `CSTM2` comes before `CSTM10`.

## Actions

- **Show** (eye in the row) selects the target of the entry in the model. It is unavailable for an entry of the whole document, for a deleted element, and in families.
- **Remove** (trash can in the row) stops ignoring that entry. The results pane brings its finding back without a manual run.
- **Clear all** (toolbar) removes every entry of the active document after a confirmation. The results pane brings their findings back too.
- **Refresh** (toolbar) reloads the list.
- **Columns** (toolbar) chooses which data columns the table shows; the row action buttons stay visible, and the choice is remembered for every Revit version.
- The search text, the search switches and the sort order are remembered for every Revit version and restored when Revit starts.
- The tool bar also carries the shared diagnostic controls (run, pause, on active view) and shows the title of the active document.
- The search box filters the list by code and target. The **Aa** button next to the box matches case, and the **ab|** button matches whole words; both are off by default, so the search ignores case and finds the text inside a longer word. The list is updated when you leave the field.

Entries whose elements no longer exist are dropped the next time the pane loads or reloads the list, and whenever the list is written. An entry of the whole document stays until it is removed explicitly.

> **Note:** An ignore written by Revit Linter 2.0.0 is stored in the `Linter_Ignored` and `Linter_IgnoredType` project parameters and is not read by the current version. See [[Troubleshooting|Troubleshooting and logs]].

Hover over a toolbar button to see what it does; the question-mark button of the tool bar opens this page. While the tooltip is shown, press **F1** to open this page in the default browser.

[[Dockable panes|← Dockable panes]]
