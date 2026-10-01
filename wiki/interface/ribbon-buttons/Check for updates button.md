---
lang: en
---

> Language: **English** · [[Кнопка проверки обновлений|Русский]]

# Check for updates button

The **Updates** button starts the standalone Revit Linter updater in manual mode. Manual checks ignore the 24-hour automatic-check interval and report both an up-to-date result and a failure.

When a newer release is available, Windows shows a local notification with **Download**, **What's new**,
**Later**, and **Skip this version** actions. The first two actions open the matching GitHub release;
the updater never downloads or installs it without an explicit user action. Notifications are skipped
when Windows does not support them, they are disabled, or the updater is running elevated.

**Later** allows the same release to be offered after the next scheduled check. **Skip this version**
suppresses that release until a newer stable version is published.

> [!note]
> Revit itself does not contact GitHub. The separate updater performs the request and stores its state under `%LOCALAPPDATA%\Volocy\Revit.Linter\updater\`. The command does not depend on the active document and does not modify the model.

If the updater executable is missing, Revit Linter asks you to repair or reinstall the application.

[[Ribbon buttons|← Ribbon buttons]]
