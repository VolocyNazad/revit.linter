---
lang: en
---

> Language: **English** · [[Устранение неполадок|Русский]]

# Troubleshooting

Application logs are stored in `%LOCALAPPDATA%\Volocy\Revit.Linter\logs\`. Start with the newest file and reproduce the problem once. Logs roll daily and at 20 MB; the newest 14 files are retained.

Useful records include add-in startup and shutdown, configuration loads, diagnostic runs, requested visualizations, applied fixes, exports, and failures with their operation context. Formula-compilation warnings include the rejected formula. YAML parsing failures include the configuration path and are also surfaced in the UI.

## Common checks

1. Confirm that the report's document is open and active before using an element link or visualization.
2. Check that the configuration file uses the required name and the folder for the running Revit version.
3. After editing YAML, look for a configuration error in the UI and then inspect the newest log entry.
4. For a visualization that appears to do nothing, click once more only if it is already active: the second click intentionally restores the previous view state.
5. If findings that were ignored by an older installation are reported again, check the [[Ignoring pane|Ignore list]] pane. Revit Linter 2.0.0 stored ignores in the `Linter_Ignored` and `Linter_IgnoredType` project parameters, while the current version reads only the in-document ignore list, so those entries are not carried over. Ignore the findings again; the unused parameters may stay in the model.

> **Note:** A malformed user-diagnostic configuration is loaded as an empty list. This prevents stale rules from continuing to run, but it can make the diagnostics list appear empty until the file is corrected.

> **Note:** Routine cancellations and documented benign fallbacks may not produce an error entry. Capture the operation, document, diagnostic code, element id, and relevant formula or YAML fragment when reporting a problem.

[[Documentation|Documentation index]]
