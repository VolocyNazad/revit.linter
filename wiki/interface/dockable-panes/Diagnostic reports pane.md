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
- Select text in the code, message, document, or time columns and copy it normally. The grid is read-only, but text selection and copying remain available.

> **Note:** An ignored finding disappears from the report immediately and is not reported again. Ignored findings are listed in the [[Ignoring pane|Ignore list]] pane, where an entry can be removed to report the finding again.

> **Note:** A visualization runs only when the report's document is the active Revit document. Clicking the same visualization for the same finding again restores the previous view state instead of applying it twice. Starting another visualization restores the active one first.

> **Note:** Search, document, severity, and status filters affect both the visible rows and exported content. Filtering does not edit report rows or disable text copying.


Hover over a toolbar or row button to see what it does; the question-mark button of the tool bar opens this page. While a tooltip is shown, press **F1** to open this page in the default browser.
