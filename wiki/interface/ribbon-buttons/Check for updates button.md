---
lang: en
---

> Language: **English** · [[Кнопка проверки обновлений|Русский]]

# Check for updates button

The **Updates** button starts the standalone Revit Linter updater in manual mode. Manual checks ignore the 24-hour automatic-check interval and report both an up-to-date result and a failure.

> [!note]
> Revit itself does not contact GitHub. The separate updater performs the request and stores its state under `%LOCALAPPDATA%\Volocy\Revit.Linter\updater\`. The command does not depend on the active document and does not modify the model.

If the updater executable is missing, Revit Linter asks you to repair or reinstall the application.

[[Ribbon buttons|← Ribbon buttons]]
