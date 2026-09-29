# TODO

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

