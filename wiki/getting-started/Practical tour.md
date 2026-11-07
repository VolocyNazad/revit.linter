---
lang: en
---

> Language: **English** · [[Практическое обучение|Русский]]

# Practical tour

The practical tour guides you through the real workflow without taking control of it:

`Model → Diagnostics → Findings → Show or fix → Export report`

It runs in a dockable pane on the right and presents the workflow as a compact checklist. The active task is highlighted, completed tasks have check marks, and the required panel is brought forward. A spotlight dims the surrounding panel and outlines and labels the next control without blocking it. When that control is a button, the button itself also gets an accent border, so a flat icon button stays visible on the dimmed panel. Closing the tour never runs a diagnostic, modifies a model, or applies a fix.

When the tour starts, the Diagnostics pane is brought forward and Revit Linter installs and refreshes the managed `TOUR001` training collision check in the `tour` configuration subfolder. The check is pinned at the top of the Diagnostics pane and carries the `Collision`, `Tour` and `Project` tags in the **Tags** column; its selection is cleared. Starting the tour also restores the pane view — the search box is cleared, every filter is re-enabled and the default sort order returns — so the training row is never hidden by earlier work. Type TOUR001 into the search box, narrow the list with the filters and clear the Document checkbox. When the tour then asks you to enable the training check, the pane lists only TOUR001: the other checks are hidden for the rest of the tour and come back with the states you set when it ends, because the tour never changes them. Enable TOUR001 and use only its findings for the guided actions. Diagnostics run only with this single check enabled. It provides the visualizations and fix menu required by every task, independently of the user's own rules. Applying its fix completes the task and shows the result in the Fix list pane.

## If a document is already open

Finish the welcome window with **Start or continue the practical tour** selected. The panels open and the tour first points to the document button in the TOUR001 row, showing that every check is a YAML file you can copy and adapt.

## If no document is open

Choose one of two paths:

1. Select an official Autodesk sample found in the Samples folder of the running Revit version. Revit Linter creates a new disposable copy for every tour and opens it in the current Revit instance. **Open tutorial** on the **Diagnostics** tab remains available if automatic opening fails.
2. Start the tour without a sample. It waits at **Open a project or family** and continues after you open a document yourself.

Only Autodesk samples for the running Revit version are offered. Your original sample is never opened as the working tutorial file.

## Progress and control

Progress is kept separately for each Revit version.

- **Continue later** hides the tour pane, ends the session and remembers the current step. The training check disappears from the Diagnostics pane and your own checks return with the states you set; the training findings are removed from the Warnings pane, so only your own results remain.
- Starting the tour again continues from the **Run checks** step: any leftover training findings are removed first, then the training check runs again and finds fresh findings for the remaining tasks, while the steps before the run stay completed. The tour says so on that step instead of asking you to run the checks for the first time.
- **Do not show again** opts out for the running Revit version.
- **Start over** in the pane discards the saved step and restarts the tour from the first task.
- Opening or switching documents ends the tour, except while it waits for you to open a project or family. The pane hides and the step is remembered: use **Practical tour** on the **Diagnostics** tab to return.
- Use **Practical tour** on the **Diagnostics** tab to return to a saved step, or to start over after completion or opt-out. Open [[Getting started button|Getting started]] to configure the tour or reset its progress.

After running `TOUR001`, select one of its findings. The tour asks you to open the **Show** menu and choose “Isolate and inspect”, then open it again and choose “Cut and inspect”, then step to neighbouring findings with the previous and next arrows at the top of the report. Hover any control and press **F1** while its tooltip is shown, or press the question-mark button in the selected report row, to open the documentation, then open the lightbulb menu and apply the fix. Then open the Fix list pane to see the applied fix, and select its row. Then ignore another finding from the same menu and open the Ignore list pane to see the ignored entry. Finally export the report and then export it once more, choosing a different format. Actions performed on findings from other checks do not advance these tasks.

[[Quick start|← Quick start]]
