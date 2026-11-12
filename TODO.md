# TODO

## Unified per-user installer and updates

- [ ] Before allowing the updater to launch an MSI, require a valid Authenticode signature from the expected publisher and wait until all affected Revit processes have closed.
- [ ] Cover release parsing, version comparison, interval and skip behavior, single-instance handling (covered), Revit-running refusal, notification decisions and activation validation (covered), manual/background error behavior, asset metadata and downloaded-content validation (covered), and updater shutdown with headless tests.

## Configuration migration (MIG-0001)

- [ ] Remove the split-configuration migration support once the removal gate in `docs/migrations/0001-single-configuration-file.md` is met. The target is the first major release after the release that ships configuration format 2 (planned 4.0.0), at least 12 months and two stable releases after it. Update the entry, `docs/architecture.md`, `docs/FEATURES.md`, the manual checklist and the Wiki notes in the same change; keep the Wiki guide as historical documentation.

## Collision diagnostics

- [ ] Show the user a visible warning when a collision rule skips selected elements without geometry (today this is only a `Warning` in the log from `ElementDiagnostic.BuildGroups`): after the run, report the skipped count per rule and advise narrowing the `take`/`andTake` filters, since skipped elements cannot collide but still cost a geometry read each.
- [ ] Cover the `ignoreNestedFamilies` pair exclusion with a checked-in family asset: only nested families marked as shared become separate project elements, and the Revit API cannot set that flag, so a programmatic fixture cannot produce the parent/nested pair. Add a `.rfa` asset with one parent and two overlapping shared nested components, and build the `RevitTests` case on it.

## Diagnostic tags (idea, needs a consumer first)

- [ ] Add an optional `tags` field to diagnostic rules only together with a concrete consumer (filter chips in the Diagnostics pane, tour selection by tag, or discipline packs replacing `section` comments). Without a consumer, a curated dictionary, validation, UI, and localization, freeform tags rot and duplicate the existing substring search.

## Asset-based RevitTests for built-in diagnostics

- [ ] Cover the LargeArray (SHRD0032) family size boundary (49 versus 50 members) with a checked-in binary test asset. Live runs proved it cannot be built deterministically through the API: `LinearArray.Create` on an extrusion in a model family template silently drops the association with no warnings (counts 3/5/10 persist as `BaseArray`, counts 20/50 leave plain copies), and the `NumMembers` setter silently keeps the old value. Project boundaries (99 versus 100) and the family label branch are covered by code-built `ArrayDiagnosticsTests`.
- [ ] Cover SYST001 with the code-built `ConnectorDiagnosticsTests` (cube families carrying pipe/duct/cable-tray/conduit/electrical connectors, the full connector-type matrix and the project-only filter — so far only compiled, needs a live Revit run) and SHRD0016 (imported CAD file) with a checked-in binary test asset. The `MEPCurve` branch still needs a project asset or a way to build MEP types in code. SHRD0020 title blocks are built in code (`FamilyTestDocuments.LoadTitleblockSymbol`: an annotation template reassigned to `OST_TitleBlocks`).
- [ ] SHRD003 has no fix: `FamilyInstance.Mirrored` is get-only, and no in-place API mirror clears the flag (verified live: re-mirroring a mirrored instance about the same plane and about an orthogonal plane through its origin leaves `Mirrored` set); any odd-mirror alternative displaces the element, so there is no safe automatic unmirror.
- [ ] Cover ARCH003 (zero-area room with boundaries) and SHRD0022 (materials cannot be created through the Revit API and empty documents have none to duplicate): no deterministic API construction is known; revisit if one is found.
- [ ] Cover SHRD0009 when floor types become available in empty test documents.

## Material performance diagnostics

- [ ] Verify on a live Revit 2023 or later session what `ExternalResourceReference.ServerId` and `InSessionPath` return for material textures stored in ACC/Forma and resolved through Desktop Connector. Add the document-level check for cloud-hosted textures only when cloud storage is distinguishable from an ordinary local file; otherwise record the negative result here and close the idea.
- [ ] Compare appearance assets by content, not only by `AppearanceAssetId`, in SHRD0038: a material whose appearance asset was duplicated along with it has identical settings but a different asset id today and stays unreported.
- [ ] Decide whether the material checks should run in the active-view scope. The element collector is view-scoped and material and appearance-asset elements are not visible in a view, so SHRD0022, SHRD0038, SHRD0039 and SHRD0040 produce nothing in an active-view run. This is an inherited limitation and a product decision, not a defect of one check.

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
