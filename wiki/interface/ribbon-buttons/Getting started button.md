---
lang: en
---

> Language: **English** · [[Кнопка начала работы|Русский]]

# Getting started button

The **Getting started** button opens the welcome window. The same window appears by itself the first time Revit starts after Revit Linter is installed.

The window has three steps:

1. **Welcome** — what the add-in does, the name of its ribbon tab, and what to keep in mind: the add-in adds the `Linter_Ignored` and `Linter_IgnoredType` project parameters to every document that is opened or created, and where the configuration and log folders are.
2. **Examples** — pick the disciplines you work with (MEP systems, architecture, structure) and press **Load examples**. The [[Configuration examples|example configurations]] are written to the [[Diagnostic configuration path convention|configuration folder]] of the running Revit version.
3. **Done** — optionally opens the panels when the window closes and links to the quick start, the documentation and support.

> [!note]
> Loading examples never overwrites a configuration file that already contains rules. Such an example is written to the `examples` subfolder of the configuration folder instead; copy the rules you need from there.

The window is shown automatically once per user. The **Examples** step is offered once for every Revit version, because each version has its own configuration folder. Closing the window in any way counts as shown; the button always opens all three steps.

[[Ribbon buttons|← Ribbon buttons]]
