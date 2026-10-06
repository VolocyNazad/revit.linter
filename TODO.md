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

## First-run welcome wizard and meaningful configuration examples

- [x] Show a modal welcome wizard on the first Idling event after the add-in starts: **Welcome**, **Diagnostics**, **Examples**, and **Done** (optionally open the panes; links to the quick start, the documentation and support).
- [x] Keep the wizard state in the user settings store: the completed wizard version (per user) and the Revit years for which the examples step was already offered. Closing the window in any way counts as shown; a ribbon command reopens the full wizard.
- [x] Give every step one concise caption in English and Russian. Error messages and navigation buttons stay plain.
- [x] Let the user pick disciplines (MEP, architecture, structure) on the **Examples** step and install the matching blocks into the configuration folder for the running Revit version. Never overwrite a non-empty user file: place the example in an `examples` subfolder instead.
- [x] Rewrite the Wiki configuration examples so that they are usable as shipped: nine collision rules (three per discipline), six user diagnostics, and project parameter rules built from the ADSK shared parameter file. Provide English and Russian message texts and reference parameters in formulas by `BuiltInParameter` name.
- [x] Build the installed examples from the same Wiki files (embedded as resources) and convert the Revit 2024+ parameter `group` identifiers to `BuiltInParameterGroup` names for Revit 2021-2023.
- [x] Replace the outdated **Volocy** ribbon tab name in `README.md` and the Wiki with the actual tab name (**Diagnostics** / **Диагностика**).
- [x] Cover example assembly by discipline, parameter group conversion, non-overwriting installation, the embedded example resources and the wizard state rules with headless tests in `Revit.Linter.WelcomePresenter.Tests`.

### Welcome experience and isolated practical onboarding

#### Product flow

- [x] Restructure the common welcome flow around four short steps: what Revit Linter does; built-in diagnostics versus user-defined YAML rules; optional example configuration installation; and the next action appropriate to the current Revit state.
- [x] Present the product workflow as **Revit model → Diagnostics → Findings → Show or fix → Export report** and keep paths, logs and the `Linter_Ignored` parameters in a secondary **Good to know** disclosure instead of competing with the primary explanation.
- [x] Add a read-only rule visualizer that pairs a small real YAML example with plain-language explanations of `code`, `description`, `severity`, `take` and `check`. Link detailed syntax to the Wiki rather than turning the welcome window into an editor or reference manual.
- [x] Branch only at the practical part of the experience. If a document is already open, offer the practical tour immediately. If no document is open, finish the short introduction and offer either an Autodesk sample or continuation after the user opens a document.
- [x] Keep example configuration installation optional. The practical tour remains usable with built-in diagnostics when the user does not install the YAML examples.
- [x] Replace the final promise to run diagnostics with an accurate next action that follows the selected state: open panels and start the tour, prepare a selected Autodesk sample, wait for a document, open panels without the tour, or simply finish.

#### Practical tour

- [x] Run the practical tour as a separate modeless experience after the modal welcome window has closed, so Revit document and pane interaction is never blocked by `ShowDialog`.
- [x] Guide the user through opening a document, selecting an applicable diagnostic, running it, inspecting findings, showing an element, applying a fix and exporting a report. Do not run diagnostics automatically; fixes stay explicit user actions.
- [x] Make the tour event-driven: advance when the expected user action occurs, allow every step to be skipped, and provide a persistent **Stop tour** action. A missing finding is a valid successful diagnostic result and must not trap the user.
- [x] Highlight controls only inside Revit Linter-owned WPF views using a reusable onboarding visual state or behavior. Use short, non-blocking pulses followed by a static emphasis, respect reduced-motion settings, and avoid unsupported traversal or modification of Revit's native ribbon visual tree.
- [x] Keep native-ribbon guidance textual or illustrative. The official Revit API may be used to show registered dockable panes, but the tour must not depend on animating native ribbon controls.
- [x] Store welcome completion, practical-tour progress, completion/opt-out state and the installed-example offer per Revit version as separate values. Reopening **Getting started** offers the short introduction, the practical tour and a reset of practical-tour progress independently.

#### Autodesk sample discovery and disposable copies

- [x] Search only the official Autodesk `Samples` location beside the currently running Revit executable. Do not scan user documents, recent files, other drives, network locations or samples from another Revit version.
- [x] Inspect candidate `.rvt` files with `BasicFileInfo.Extract` before presenting them. Exclude unreadable files, files from a later or different Revit version, and workshared models; the `.rvt` filter excludes templates and families.
- [x] Rank compatible samples by the disciplines selected on the examples step (MEP, architecture, structure), show the full source path and Revit file format, and retain choosing another found sample or opening a document yourself as explicit alternatives.
- [x] Create a fresh disposable copy for every tour under a Revit Linter-owned session directory in `%TEMP%\Revit Linter\Tutorial\<RevitVersion>\<SessionId>\`. Never open an Autodesk sample source for modification and never overwrite or reuse a previous tour copy.
- [x] Remove a session copy only after its Revit document has closed. On a later startup, best-effort cleanup removes abandoned session directories only below the exact Revit Linter tutorial root and skips sessions locked by another Revit process.
- [x] If no compatible Autodesk sample exists or copying/opening fails, explain the fallback without treating it as an add-in failure and let the user continue with a document they open themselves or skip the tour.

#### Isolation and failure containment

- [x] Keep the onboarding state machine, sample candidate ranking, step completion rules and copy-path planning free of Revit API and WPF types. Put Revit document observation, sample metadata reading/opening and pane control behind narrow host-owned adapters.
- [x] Keep onboarding composition in `Revit.Linter.WelcomePresenter` (or a separately approved onboarding module) and expose only the minimum host contracts required to observe document availability, show panes and request a sample opening. Do not add onboarding branches to diagnostic discovery, execution, report presentation, visualization, fixing or export services.
- [x] Treat onboarding as an optional observer of existing application events and commands. It may react to successful actions but must not own, wrap or change the semantics of those actions.
- [x] Never keep live Revit `Document`, `UIDocument`, pane or control references in persisted onboarding state. Subscribe only while a tour session is active and deterministically detach every event handler, timer and animation when the tour stops, completes, the window closes or the add-in shuts down.
- [x] Enter Revit API work through the repository's established Revit-context boundary and only where the selected API permits it. Do not open documents, show panes or touch Revit UI from a WPF timer, background thread or unsupported event callback.
- [x] Catch and log onboarding failures at the onboarding boundary. A failure closes or disables the tour while leaving add-in startup, document opening, diagnostics, panes and the working model unaffected.
- [x] Do not modify a user document as part of onboarding. A fix still requires the same explicit confirmation and transaction path as normal operation, even for a disposable sample copy.
- [x] Keep the existing first-run wizard and practical tour behind independently removable registrations so disabling onboarding requires no changes to the core diagnostic composition.

#### Documentation and verification

- [x] Add paired English and Russian Wiki guidance for **Built-in diagnostics and user rules**, **Create your first rule** and **Practical tour**, and reshape the diagnostics overview into a task-oriented entry point. Keep detailed YAML fields and formula syntax in reference pages.
- [x] Update the quick start and **Getting started** button pages for the document-present/document-absent branches, Autodesk sample selection, disposable copies, resuming and resetting the tour.
- [ ] Before adding or changing tests, agree the coverage scope as required by the development policy. At minimum, plan headless coverage for the onboarding state machine, branching, sample filtering/ranking, unique copy-path planning, progress persistence and failure fallbacks; keep thin Revit UI adapters outside headless tests.
- [ ] Deliver in stages: content and information architecture; pure onboarding state machine; host adapters and Autodesk sample discovery; modeless coach marks; then optional animation polish. Each stage must leave onboarding safely disableable and the existing diagnostics workflow unchanged.

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

