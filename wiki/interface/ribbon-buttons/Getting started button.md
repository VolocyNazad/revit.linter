---
lang: en
---

> Language: **English** · [[Кнопка начала работы|Русский]]

# Getting started button

The **Getting started** button opens the welcome window. The same window appears by itself the first time Revit starts after Revit Linter is installed.

The window has five steps:

1. **Welcome** — what the add-in does, the name of its ribbon tab, and what to keep in mind: the add-in adds the `Linter_Ignored` and `Linter_IgnoredType` project parameters to every document that is opened or created, and where the configuration and log folders are.
2. **Diagnostics** — compares built-in diagnostics with user-defined YAML rules and shows the main fields of a rule in a read-only example.
3. **Examples** — pick the disciplines you work with (MEP systems, architecture, structure) and press **Load examples**. The [[Configuration examples|example configurations]] are written to the [[Diagnostic configuration path convention|configuration folder]] of the running Revit version.
4. **Practical tour** — starts or continues the [[Practical tour]]. If no document is open, this step can prepare a disposable copy of an official Autodesk sample found for the running Revit version, or let you continue with your own document.
5. **Done** — a short completion summary with links to the quick start, documentation, and support.

> [!note]
> Loading examples never overwrites a configuration file that already contains rules. Such an example is written to the `examples` subfolder of the configuration folder instead; copy the rules you need from there.

The window is shown automatically once per user. The **Examples** step is offered once for every Revit version, because each version has its own configuration folder. Closing the window in any way counts as shown; the button always opens the complete introduction.

Practical-tour progress is also stored per Revit version. **Reset practical tour** clears only that progress; it does not remove installed examples or reset whether the welcome window was shown.

[[Ribbon buttons|← Ribbon buttons]]
