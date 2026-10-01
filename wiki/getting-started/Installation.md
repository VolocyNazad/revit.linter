---
lang: en
---

> Language: **English** · [[Установка|Русский]]

## Windows

1. Close Revit.
2. Open the [Revit Linter releases](https://github.com/VolocyNazad/revit.linter/releases) page.
3. Download the Revit Linter MSI installer.
4. Run the installer, review the detected Revit versions, and adjust the selected components if needed.
5. Start Revit and find the commands on the **Volocy** ribbon tab.

The single per-user installer includes Revit 2021, 2023, and 2025 builds and the shared updater. It
does not require elevation. Application files are installed under the current user's local application
data, while Revit manifests are registered separately for each supported year.

Setup must replace add-in files while Revit is closed. If any Revit process is running, setup stops
without changing the files and asks you to close Revit before running it again. Upgrades preserve the
previous component selection where Windows Installer can migrate it.

## Troubleshooting

- If the **Volocy** tab is missing, close every Revit process and run the installer again.
- Make sure the Revit version you opened is one of the supported versions listed above.
- If a panel is hidden, use the [[Ribbon buttons|ribbon buttons]] to show it again.

[[Quick start|Continue to Quick start →]]
