---
lang: en
---

> Language: **English** · [[Кнопка проверки обновлений|Русский]]

# Check for updates button

The **Updates** button starts the standalone Revit Linter updater in manual mode. Manual checks ignore the 24-hour automatic-check interval and report both an up-to-date result and a failure.

When a newer release is available, Windows shows a local notification with **Download**, **What's new**,
**Later**, and **Skip this version** actions. **Download** fetches the exact MSI from the matching GitHub
release, verifies its expected size and SHA-256 digest, and shows the verified file in Explorer. It does
not start the installer; installation remains an explicit user action. **What's new** opens the release
page. Notifications are skipped when Windows does not support them, they are disabled, or the updater is
running elevated.

> [!note]
> The current download check proves that the file matches the asset published by GitHub. Automatic
> installation is intentionally unavailable until the MSI can also be verified by an expected-publisher
> Authenticode signature. Failed or mismatched downloads are deleted and recorded in the updater log.

**Later** allows the same release to be offered after the next scheduled check. **Skip this version**
suppresses that release until a newer stable version is published.

> [!note]
> Revit itself does not contact GitHub. The separate updater performs the request and stores its state under `%LOCALAPPDATA%\Volocy\Revit.Linter\updater\`. The command does not depend on the active document and does not modify the model.

If the updater executable is missing, Revit Linter asks you to repair or reinstall the application.

[[Ribbon buttons|← Ribbon buttons]]
