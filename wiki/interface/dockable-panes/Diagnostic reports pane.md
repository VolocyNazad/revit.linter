---
lang: en
---

> Language: **English** · [[Панель результатов диагностики|Русский]]

The panel shows diagnostic findings for the selected document. Search and filter the findings, navigate to
problem elements, run available fixes, and export the currently displayed results to CSV, JSON, YAML, or a
self-contained HTML report. The HTML report includes a severity summary, counts by diagnostic code, and the
detailed findings table.

## Row actions

- Click the light-bulb button to run the only available fix. If the finding has several fixes, the same click opens their menu.
- Click the eye button to run the only available visualization. If several pipelines are available, the same click opens their menu.
- Click the question-mark button to open the documentation of the finding's diagnostic in the default browser: the page of built-in element or document diagnostics, or the page of the module whose configuration file defines the rule.
- Click an element link inside the message to locate that element in the active Revit document.
- Select text in the code, message, document, or time columns and copy it normally. The grid is read-only, but text selection and copying remain available.

> **Note:** A visualization runs only when the report's document is the active Revit document. Clicking the same visualization for the same finding again restores the previous view state instead of applying it twice. Starting another visualization restores the active one first.

> **Note:** Search, document, severity, and status filters affect both the visible rows and exported content. Filtering does not edit report rows or disable text copying.


Hover over a toolbar or row button to see what it does. While the tooltip is shown, press **F1** to open this page in the default browser.
