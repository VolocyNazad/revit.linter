---
lang: en
---

> Language: **English** · [[Панель результатов диагностики|Русский]]

The panel shows diagnostic findings for the selected document. Search and filter the findings, navigate to
problem elements, run available fixes, and export the currently displayed results to CSV, JSON, YAML, or a
self-contained HTML report. The HTML report includes a severity summary, counts by diagnostic code, and the
detailed findings table.

## Row actions

- Click the light-bulb button to open the menu of the finding: its fixes and its ignore actions.
- In that menu, **Ignore** stops reporting the finding for its element, and **Ignore (all)** stops reporting every finding of the same check in the current report. For a finding of a document check the menu offers **Ignore**, which stops reporting that check in the whole document.
- Click the eye button to run the only available visualization. If several pipelines are available, the same click opens their menu.
- Click the question-mark button to open the documentation of the finding's diagnostic in the default browser: the page of built-in element or document diagnostics, or the page of the module whose configuration file defines the rule.
- Click an element link inside the message to locate that element in the active Revit document.
- A list of element names or identifiers is folded in the message: only its first items are shown, followed by a `+N` counter. Every name in the folded list is quoted, so a name that itself contains a comma stays readable. Click the counter to open the complete list in a popup, which shows one element per line. Only a list value is folded: a long scalar text such as a Revit warning description is never shortened. The search, the sort and the exported report use the complete message.
- Click a tag in the **Tags** column to put it in the search box and narrow the list to the diagnostics that match it. The exported report includes the tags of every finding.
- Select text in the code, message, document, or time columns and copy it normally. The grid is read-only, but text selection and copying remain available.

## Findings heat map

The thermometer button of the tool bar colours the findings of the active document in the persistent
**Revit Linter — Visualization** 3D view. The colour of an element follows its most severe check:

- the hue is the severity: red is an error, orange is a warning, and blue-grey is an informational message;
- the brightness grows with the number of distinct checks of that severity that the element fails. Only
  the most severe checks count, so an element with one error and many warnings uses the error colour with
  a count of one.

The counts use fixed bands — 1, 2, 3–4, 5–8, 9–16 and 17 or more — so the same number of checks always
maps to the same colour. The heat map is built from every finding of the active document in the report; the
search and the filters of the table do not change the map.

Only elements with model geometry that the visualization view displays are coloured. Findings whose target
is not such an element are skipped. The visualization view uses fine detail and a shaded display, and it is
colourless apart from the findings: the surfaces of the other elements become 99% transparent and their
lines are drawn in the background colour, so pipe, duct and other centre lines fade away as well. The colour
steps and the background are applied as reversible per-element overrides, so the coloured findings stay
fully opaque and stand out from the model. While the map is applied, the levels, the scope boxes, the grids
and the linked models are hidden in the visualization view; a linked model stays visible only when a finding
targets one of its instances. Click the button again to remove the colour and restore the previous graphics,
including the visibility of those categories. A new diagnostics run, switching to another document and
closing the document also remove the map; editing the model does not, so the map stays a snapshot until the
button is pressed again.

## Columns

The **Columns** button of the tool bar chooses which data columns the table shows. The row action buttons stay visible, and the choice is remembered for every Revit version. Hiding a column keeps the current sort order, at least one data column always stays visible, and the exported report still carries every column.

> **Note:** An ignored finding disappears from the report immediately and is not reported again. Ignored findings are listed in the [[Ignoring pane|Ignore list]] pane, where an entry can be removed to report the finding again.

> **Note:** A visualization runs only when the report's document is the active Revit document. Clicking the same visualization for the same finding again restores the previous view state instead of applying it twice. Starting another visualization restores the active one first.

> **Note:** Search, document, severity, and status filters affect both the visible rows and exported content. Filtering does not edit report rows or disable text copying.


Hover over a toolbar or row button to see what it does; the question-mark button of the tool bar opens this page. While a tooltip is shown, press **F1** to open this page in the default browser.
