---
lang: en
---

> Language: **English** · [[Панель диагностики|Русский]]

Panel with the list of all available diagnostics.

---
Actions:
- View the code and description of a diagnostic
- Enable/disable a diagnostic (diagnostics)
- Change the severity level of a diagnostic (diagnostics)
- Open the YAML rule configuration of a row with the document button of that row; the button opens a menu with **Open**, **Open with…** (the Windows application chooser), **Open in Notepad** and **Show in File Explorer**. The change is saved to `diagnostics.config.yaml` when you toggle a diagnostic or change its severity
- Open the documentation of a row with the question-mark button of that row
- Open the configuration folder with the folder button at the right edge of the tool bar
- Reset the saved settings with the refresh button of the tool bar, after confirmation: this removes built-in overrides and `isHidden` fields but keeps your own rules' `isActive` and `severity`
- A diagnostic with `isHidden: true` is not shown in the list, does not run and does not appear in reports; remove the field or use reset to show it again
---
Features:
- Search, sort, and filter the diagnostics list; the **Filters** button chooses which rows the table shows
- Find diagnostics by tag: the **Tags** column shows the tags of each diagnostic, and clicking a tag searches for it. A rule states the document kinds it applies to with the ordinary tags `Project` and `Family`. Alongside the tags written in a rule, the product adds reserved system tags; see [[Diagnostic tags]] for the full list and how the tag list is built
- Copy text
- Batch enable/disable diagnostics: check or uncheck all or only the visible rows, and invert the selection

Hover over a toolbar or row button to see what it does; the question-mark button of the tool bar opens this page. While the tooltip is shown, press **F1** to open this page in the default browser.
