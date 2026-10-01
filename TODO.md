# TODO

## Unified per-user installer and updates

- [x] Replace the separate user-facing Revit-version installers with one per-user Revit Linter MSI, migrate legacy per-version products, and leave one Programs and Features entry.
- [x] Install application files under `%LOCALAPPDATA%\Programs\Volocy\Revit.Linter\` and version-specific `.addin` manifests under `%APPDATA%\Autodesk\Revit\Addins\<year>\` without requiring elevation.
- [x] Detect installed supported Revit versions, preselect them, and let the user change the selected components before installation.
- [x] Package every selectable Revit build and one shared updater in the same MSI so installation and removal cannot orphan shared infrastructure.
- [x] Keep one stable MSI `UpgradeCode`, upgrade the updater and selected plugin components in one transaction, and migrate feature selection on unified upgrades.
- [x] Stop the updater before replacing it. Refuse plugin replacement while any Revit process is running, and provide a clear retry path after Revit is closed.
- [x] Implement the updater as a separate executable with no Revit API dependency. The Revit add-in must not perform release HTTP requests or create Windows notifications itself.
- [x] Prefer a short-lived updater over a permanently running worker: start it for the current user at sign-in, check persisted state, perform any due work, and exit. Do not add a tray icon.
- [x] Register updater startup per user under `HKCU`; a manual **Check for updates** action in Revit launches the same executable with `--check-now`.
- [x] Guarantee a single updater instance with a named semaphore and make concurrent `--check-now` requests activate or reuse the existing check instead of starting duplicate work.
- [x] Query the public GitHub `releases/latest` endpoint with an explicit `User-Agent`, a short HTTP timeout, and no embedded access token. Use only stable, non-draft releases initially.
- [x] Read the installed product version from installer/build metadata, compare stable `MAJOR.MINOR.PATCH` values, and tolerate an optional leading `v` and assembly build metadata.
- [x] Persist `AutomaticChecksEnabled`, `LastCheckedAt`, `LastNotifiedVersion`, and `SkippedVersion` under `%LOCALAPPDATA%\Volocy\Revit.Linter\updater\`.
- [x] Check automatically no more than once per 24 hours. Manual checks bypass the interval, report both the up-to-date and failure states, and never change the automatic-check timestamp after an unsuccessful request.
- [x] Keep automatic network failures silent to the user and log them once. A background failure must not affect Revit startup or normal add-in operation.
- [x] Show a local Windows app notification only for a newer, non-skipped version. Provide **Download**, **What's new**, **Later**, and **Skip this version** actions without stealing focus from Revit.
- [x] Publish the updater self-contained with the Windows App SDK notification support it needs, initialize the Windows App Runtime through its self-contained unpackaged auto-initializers, and degrade to logging/manual checks when notifications are unavailable or the process is elevated.
- [x] Avoid a custom URI protocol because unpackaged Windows App SDK activation registers the executable directly; validate every activation argument before opening the release page.
- [x] Download the unified MSI only after an explicit notification action, validate its official HTTPS origin, exact versioned asset name, size, and GitHub-provided SHA-256 digest, then reveal it in Explorer without launching it.
- [ ] Before allowing the updater to launch an MSI, require a valid Authenticode signature from the expected publisher and wait until all affected Revit processes have closed.
- [x] Keep administrative policy separate from user state. Read machine policy, then user policy, then user settings, then built-in defaults; do not write ordinary defaults under a `Policies` registry key.
- [x] Support policies for disabling checks or notifications, fixing the release API URL, and controlling the check interval. Add download or installation policies only with those later capabilities.
- [x] On uninstall, remove all installed plugin components, manifests and updater startup registration. Preserve logs and user settings.
- [ ] Cover release parsing, version comparison, interval and skip behavior, single-instance handling (covered), Revit-running refusal, notification decisions and activation validation (covered), manual/background error behavior, asset metadata and downloaded-content validation (covered), and updater shutdown with headless tests.

## Branching diagnostic report history

- [ ] Persist published full-document diagnostic runs as immutable report snapshots.
- [ ] Use one fixed Extensible Storage schema. Store only `SchemaVersion`, immutable `LineageId`, and current `HeadReportId` in the Revit document.
- [ ] Assign every report a new `ReportId` and its source head as `ParentReportId`. Build branches when multiple reports share the same parent.
- [ ] Store reports as compressed JSON files under `.revit-linter/<LineageId>/reports/<ReportId>.json.gz` next to the RVT file, or next to the central model for workshared documents.
- [ ] Resolve the report location relative to the model; do not persist absolute report paths.
- [ ] Keep a recoverable `index.json` with branch heads and metadata. Treat immutable report files as the source of truth for rebuilding the index and tree.
- [ ] Publish under a short-lived file lock with an expected-head check, temporary-file write, and atomic rename. On a head conflict, require the user to rerun from the current head or create a branch.
- [ ] Do not publish unsaved, partial-view, selected-element, incremental, or failed runs as shared history checkpoints.
- [ ] Detect a missing `.revit-linter` directory and ask the user to locate the history or start a new one; never create an empty replacement silently.
- [ ] Initially support ordinary file-based and network workshared models whose central path is accessible. Defer ACC/BIM 360 and Revit Server storage.

