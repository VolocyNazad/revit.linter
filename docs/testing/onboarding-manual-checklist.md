# Onboarding manual verification

Run this checklist in a debug build for every supported Revit generation that is available on the test machine. Use a disposable model and do not validate fixes on production documents.

## Welcome branches

- With a document open, finish the welcome window with the practical tour enabled. Verify that the panels open and the tour starts by showing where configurations are stored.
- With no document open and an Autodesk sample selected, finish the welcome window. Verify that a new copy opens automatically in the current Revit instance and starts the tour; if opening fails, verify that **Open tutorial** remains available.
- With no document open and no sample selected, finish the welcome window. Verify that the panels open and the tour waits for a project or family.
- Disable the practical tour. Verify that the sample controls are disabled and that the panels can be enabled or disabled independently.
- Reopen **Getting started** and verify that reset clears practical-tour progress, reinstalls the managed `TOUR001` check and restarts the tour at the first step, without removing installed examples.

## Practical tour

- Start or resume the tour and verify that the Diagnostics pane is brought forward, `tour/practical-tour.config.yaml` is installed for the running Revit version and `TOUR001` appears pinned at the top of the Diagnostics pane with the tour badge.
- In the `TOUR001` row, press the document button. Verify that the rule configuration opens in an editor and only this action advances the corresponding task.
- On the diagnostic-selection step, verify that `TOUR001` starts cleared at the top of the list; toggle another check and verify that the tour does not advance; then press Uncheck all, enable only `TOUR001`, and verify that it does.
- On the search step, type text into the Diagnostics search box and verify that **Search and filters** does not complete; toggle a filter with the box empty and verify that it does not complete either; then combine non-empty text with a filter change, in any order, and verify that the combination completes the step. Clear the box and verify that the text half is disarmed.
- Run diagnostics with `TOUR001` and other checks enabled and verify that the tour does not advance; uncheck everything except `TOUR001`, run again, and verify that it does.
- Open the Fix list pane from the ribbon. Verify that showing it completes **Fix list pane**.
- With the Fix list pane already open, select a `TOUR001` fix row and verify that it completes **Fix list pane**; selecting another code does not.
- On the fix-list step, verify that only the first `TOUR001` row pulses; the rest of the pane stays calm.
- Complete diagnostic selection, run, finding inspection, opening the help, visualization, visualization-option selection, fix inspection, the fix list, ignoring a finding, the ignore list, export, and the second export in another format. Verify that each completed action advances exactly one relevant step.
- On the finding-inspection step, verify that only the first `TOUR001` row pulses; other rows stay calm.
- For a finding with multiple visualizations, first use **Show**, then open its arrow menu and choose another option. Verify that the first action completes only **Visualize**, while a successful menu choice completes **Visualize differently**.
- Use the previous and next arrows at the top of the report to step to a neighbouring finding. Verify that stepping to a `TOUR001` finding completes **Step through findings** with the counter between the arrows, and that stepping to another code does not advance the tour.
- On the element-visualization and fix-inspection steps, verify that only the selected row's eye and lightbulb buttons pulse; the same buttons in other rows stay calm.
- Run diagnostics with no findings. Verify that finding-only steps are skipped and the tour can finish.
- Verify that the active task is highlighted, completed tasks receive check marks, and a clean diagnostic run marks finding-specific tasks as unavailable.
- Move through every task and verify that its required Revit Linter pane is brought to the foreground (the ignore-list task brings the Ignore list pane forward), except Export, which stays where the user left it. Verify that the surrounding pane is dimmed, the target remains unobscured inside an accent outline with a soft pulsing halo, and the localized next-step label points to it without blocking mouse or keyboard input. Verify that a highlighted button glows, pulses and is framed in the accent color on the button itself, while a highlighted check box, text box or grid row pulses in scale and halo without glowing the text itself. Disable Windows client-area animations and verify that the pulsing is suppressed.
- On the help step, press the **?** button in the selected report row (or hover any control and press **F1** while its tooltip is shown). Verify that the documentation opens in the browser and that **Open help** completes; opening a page from another pane does not.
- Use **Continue later** and **Do not show again**. Verify resume and opt-out independently for the running Revit version.
- Press **Start over** in the tour pane. Verify that the saved step is discarded, the tour restarts at the first step, and installed examples are kept.
- Complete the tour and verify that `tour/practical-tour.config.yaml` is removed and `TOUR001` disappears from the Diagnostics pane. Opt out and verify the same. Then use **Practical tour** on the ribbon and verify that the tour starts over from the first step instead of staying hidden.
- Complete or dismiss the tour after using search and filters, and verify that the search box is cleared and every filter is enabled again.
- For a finding with fixes, open the lightbulb menu and apply the fix. Verify that applying completes **Apply the fix** and the applied fix appears in the Fix list pane.
- In another `TOUR001` row, open the lightbulb menu and choose **Ignore**. Verify that the row disappears from the report, **Ignore a finding** completes, and the entry appears in the Ignore list pane.
- Show the Ignore list pane with **Show/Hide** on the Diagnostics tab and select the ignored entry. Verify that **Ignore list pane** completes, and that selecting an entry of another code does not advance the tour.
- Export the report twice. Verify that choosing the same format again does not advance the tour, while another format completes **Export in another format**.
- Repeat finding selection, visualization and fix-menu actions on a code other than `TOUR001`; verify that none advances the corresponding task.
- Start every `TOUR001` visualization from a floor plan. Verify that Revit creates and activates one `Revit Linter — Visualization` 3D view, reuses it for later findings, and that crop plus combined visualizations stay in that view without opening additional plans.
- Close the dockable tour pane during every step. Verify that the current step is saved, highlights disappear, and the normal commands keep working. Use **Practical tour** on the ribbon to resume it.

## Autodesk sample lifecycle

- Verify that only `.rvt` files from the running Revit installation's `Samples` directory are offered and that technical source-path and format details are not displayed.
- Open a prepared copy and confirm that its path is below `%TEMP%\Revit Linter\Tutorial\<RevitVersion>\<SessionId>\` while the Autodesk source remains unchanged.
- Close the tutorial document and verify that its session directory is removed only after the document has closed.
- Keep a tutorial open in one Revit process and start another process of the same version. Verify that startup cleanup leaves the active session intact.
- Simulate a missing Samples directory and an unavailable destination. Verify that the welcome flow explains the fallback and still allows opening a document manually.

## Failure containment

- Close the practical tour while diagnostics are running and verify that the run completes normally.
- Start Revit with the practical-tour pane not yet created and review the application log after startup. Verify that hiding it logs no error and the welcome flow still opens the panes.
- Review the application log after a forced onboarding failure. Verify one error at the onboarding boundary and no duplicate fatal startup error.
- Restart Revit and verify that abandoned, unlocked tutorial session directories are removed without touching files outside the exact tutorial root.
