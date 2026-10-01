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

When an update notification's **Download** action is used, the updater stores the MSI under
`%LOCALAPPDATA%\Volocy\Revit.Linter\updater\downloads\` only after its size and GitHub-provided SHA-256
digest match the release metadata. Explorer opens with that file selected, but setup is not started
automatically. Close Revit and run the verified MSI explicitly.

## Administrator update policies

Revit Linter reads optional policy from `Software\Policies\Volocy\Revit.Linter\Updater` under both
`HKEY_LOCAL_MACHINE` and `HKEY_CURRENT_USER`. Machine policy has the highest priority,
followed by user policy, user settings, and built-in defaults. The installer does not create these values.

| Value | Registry type | Meaning |
|---|---|---|
| `ChecksEnabled` | `DWORD` (`0` or `1`) | Disables or allows all automatic and manual release checks. |
| `NotificationsEnabled` | `DWORD` (`0` or `1`) | Disables or allows Windows update notifications without disabling checks. |
| `CheckIntervalHours` | `DWORD` (`1`–`720`) | Sets the successful automatic-check interval; the default is 24 hours. |
| `ReleaseApiUrl` | `REG_SZ` | Replaces the latest-release API endpoint; only an absolute HTTPS URL is accepted. |

Invalid values are ignored and logged. Policies are read-only to Revit Linter and are never stored in
the per-user updater state.

## Troubleshooting

- If the **Volocy** tab is missing, close every Revit process and run the installer again.
- Make sure the Revit version you opened is one of the supported versions listed above.
- If a panel is hidden, use the [[Ribbon buttons|ribbon buttons]] to show it again.

[[Quick start|Continue to Quick start →]]
