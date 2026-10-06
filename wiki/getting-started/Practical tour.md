---
lang: en
---

> Language: **English** · [[Практическое обучение|Русский]]

# Practical tour

The practical tour guides you through the real workflow without taking control of it:

`Model → Diagnostics → Findings → Show or fix → Export report`

It runs in a dockable pane on the right and presents the workflow as a compact checklist. The active task is highlighted, completed tasks have check marks, and the required panel is brought forward. A spotlight dims the surrounding panel and outlines and labels the next control without blocking it. Closing the tour never runs a diagnostic, modifies a model, or applies a fix.

When the tour starts, Revit Linter installs and refreshes the managed `TOUR001` training check in the `tour` configuration subfolder. The check is pinned at the top of the Diagnostics pane with a tour badge and its selection cleared. Narrow the list with the search box or filters and clear the Document checkbox, then enable this exact check and use only its findings for the guided actions. It provides the visualizations and fix menu required by every task, independently of the user's own rules. Applying its fix completes the task and shows the result in the Fix list pane.

## If a document is already open

Finish the welcome window with **Start or continue the practical tour** selected. The panels open and the tour first points to **Diagnostics → Open configuration folder**, where the YAML files for your own rules are stored.

## If no document is open

Choose one of two paths:

1. Select an official Autodesk sample found in the Samples folder of the running Revit version. Revit Linter creates a new disposable copy for every tour and opens it in the current Revit instance. **Open tutorial** on the **Diagnostics** tab remains available if automatic opening fails.
2. Start the tour without a sample. It waits at **Open a project or family** and continues after you open a document yourself.

Only Autodesk samples for the running Revit version are offered. Your original sample is never opened as the working tutorial file.

## Progress and control

Progress is kept separately for each Revit version.

- **Continue later** hides the tour pane and remembers the current step.
- **Do not show again** opts out for the running Revit version.
- Use **Practical tour** on the **Diagnostics** tab to return to a saved step. Open [[Getting started button|Getting started]] to configure the tour or reset its progress.

After running `TOUR001`, select one of its findings. The tour asks you to open the **Show** menu and choose a visualization, then open it again and choose “Inspect closely: color, crop, focus”, then step to neighbouring findings with the previous and next arrows at the top of the report, and then open the lightbulb menu and apply the fix. Then open the Fix list pane to see the applied fix, and select its row. Actions performed on findings from other checks do not advance these tasks.

[[Quick start|← Quick start]]
