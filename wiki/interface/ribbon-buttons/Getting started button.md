---
lang: en
---

> Language: **English** · [[Кнопка начала работы|Русский]]

# Getting started button

The **Getting started** button opens the welcome window. The same window appears by itself the first time Revit starts after Revit Linter is installed.

The window has three steps:

1. **Welcome** — the main page: what the add-in does, a table of contents, and rows that open the quick start, the documentation and the support page in the browser. Each row names an item and says in one line what it offers; a step row opens that step.
2. **Practical tour** — starts or continues the [[Practical tour]]. If no document is open, this step can prepare a disposable copy of an official Autodesk sample found for the running Revit version, or let you continue with your own document.
3. **Load examples** — pick the disciplines on the left and the **ADSK checks** of your disciplines on the right, then press **Load examples**. The [[Configuration examples|example configurations]] are written to the [[Diagnostic configuration path convention|configuration folder]] of the running Revit version.

> [!note]
> Loading examples always writes to the `examples` subfolder of the configuration folder; your own files are never touched. Copy the rules you need from there into your own configuration files.

The ADSK template check blocks are offered on every supported Revit version. The wizard appends the edition that matches the running Revit: Revit 2021–2023 receives the blocks generated from the Revit 2021 templates, which use the `PG_*` parameter group names, and Revit 2024–2027 receives the same blocks with ForgeTypeId group values such as `autodesk.parameter.group:data-1.0.0`. The two editions carry the same checks and codes. Each block contains the project-parameter rule of its discipline, which requires the template parameters to keep the same binding type, group, category list and vary-between-groups flag, and one fill check per Text or MultilineText parameter bound to element categories, which reports an element whose parameter is empty. The wizard appends both the project-parameter rule and the fill checks of the selected disciplines to the installed example configuration. Because a rule runs in every project document, install only the disciplines whose models you check; the checks of another discipline will report parameters that its template does not have. See [[Configuration examples]] for the list of codes.

The window is shown automatically once per user. The **Load examples** step is offered once for every Revit version, because each version has its own configuration folder. Closing the window in any way counts as shown; the button always opens the complete introduction.

Practical-tour progress is also stored per Revit version. **Reset practical tour** clears only that progress; it does not remove installed examples or reset whether the welcome window was shown.

[[Ribbon buttons|← Ribbon buttons]]
