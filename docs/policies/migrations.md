# Migration policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Read when: adding, changing or removing a user-visible migration.

The [migration registry](../migrations/README.md) is the single source of truth for which migrations
exist, the entry template and the registry instructions; this policy states the norms the registry
and every change must satisfy.

## Scope

This policy applies to every change that alters what a user can observe in configuration, saved
settings or user data: the file format or layout, the configuration paths, the persistence locations,
serialized identifiers and placeholders, and how the effective settings are stored. It applies from
design through support to removal of the transitional behaviour.

The rules also cover an update to an existing migration entry, not only a new one.

This policy does not apply to internal refactors and implementation changes a user cannot observe.
Migrations that shipped before the registry existed are not backfilled: only migrations that still
have in-product support are recorded.

The registry is contributor documentation; user instructions belong to the Wiki guide, not to the
registry or a policy.

## Principles

1. **One entry per migration, append-only.** Exactly one numbered entry under `docs/migrations/`; entries are never deleted or renumbered, and a `Retired` entry stays as history.
2. **A migration is a contract.** The entry names both product versions, the exact user action and the data-safety guarantees before the release ships.
3. **No silent breaking change.** Release notes plus an in-product signal — a warning, a notice, or both — announce every breaking migration; the Wiki guide still explains it to a user who dismissed the warning.
4. **User data is never destroyed.** Never delete or rewrite user-authored files; a file that must leave a product-read place is moved (for example into a `legacy` folder) or backed up, and the entry records where it went and how to restore it.
5. **Migration is manual unless the entry says otherwise.** Automatic rewriting of user rules or settings is a separate, explicitly approved decision recorded in the entry.
6. **Rollback stays possible while support exists.** The entry states what a downgrade still sees and what the change already altered beyond it.
7. **Transitional code is disposable by design.** The entry lists every migration-only artifact — code, tests, documentation, resources and solution items.
8. **Support is time-boxed.** A migration is supported for every release of the introducing major line, for at least 12 months and at least two stable releases.
9. **Removal is decided from evidence.** The in-product migration log plus support reports; support ends only when neither shows unmigrated data for two stable releases.
10. **Documentation outlives the code.** After support ends the Wiki guide stays as historical documentation; only product-side mentions of the transition are removed.

## Lifecycle and statuses

The status is recorded in the entry and repeated in the registry index. The allowed statuses are
`Planned`, `Active` and `Retired`.

- A migration is `Planned` while it is implemented or being implemented and the introducing release is
  not tagged. The entry, the Wiki guide and the removal task in `TODO.md` exist.
- A migration becomes `Active` with the release that ships it, which records the release tag in the
  entry.
- Status changes and recorded release facts are made in the commit that prepares the release or the
  removal, not in a separate follow-up.
- While `Planned`, the entry may be edited freely. After the migrating release is tagged, only the
  status, dates and removal notes change. Never rewrite the recorded history of a shipped migration.
- `Retired` is set only after the removal gate is met and every migration-only artifact is deleted.
  The entry records the release that removed support, the Wiki guide is kept and marked historical,
  and the `TODO.md` task is closed.

## Version window

The product follows SemVer for user-visible breaking changes: a break ships in a major release, and
the entry names the release that ships it.

- Removal happens in the first major release after the introducing line, never earlier than 12 months
  and two stable releases after the introduction, and never without an announcement in the previous
  stable release.
- If the planned release number changes or the release slips, update the entry in the same commit that
  prepares the release; a tagged entry is historical.

## Obligations per change

- The entry is created when the migration is designed, not when it ships. A change that introduces,
  revises or ends a migration updates its entry in the same change.
- Every entry states the product versions on both sides, why the change is breaking, the exact user
  action, what the product does automatically, the data-safety guarantees, the removal gate and the
  exact cleanup checklist.
- Every breaking migration has a user-facing Wiki guide in both languages before the release, and the
  entry links it.
- The release that ships the migration sets the status to `Active` with the actual tag and describes
  the migration and its removal plan in the release changelog section.
- Transitional behaviour is removed only through the entry's removal gate. That removal change also
  updates the entry, the architecture decision, the compatibility contract, the manual checklist in
  `docs/testing/`, the registry index and the `TODO.md` task.
- The removal task in [TODO.md](../../TODO.md) names the entry id and the target release, and it is
  closed only together with the retirement.
- New or changed user-visible behaviour around the migration updates the affected user documentation
  in the same change.

## Data safety

- No migration path deletes user-authored files. A file that must leave a place the product reads
  from is moved or backed up, and the entry records the new location and the restore path.
- A migration never silently overwrites a user-edited file. When a product-managed file replaces an
  existing difference, the previous file is backed up and the entry names the backup naming scheme.
- The product does not import, rewrite or invent user rules, switches or settings unless the entry
  records an explicitly approved automatic decision.
- Data safety is verified in the change that ships the migration, with headless tests for detection
  and data safety and a manual verification section when the workflow needs a live Revit. The
  evidence is recorded in the entry.

## Removal gate

- The entry defines the objective conditions under which support may be removed. Supporting behaviour
  is removed only when every condition is met.
- The gate always covers the minimum support window (one full major line, 12 months, two stable
  releases), the announcement in the stable release immediately before the removal, the absence of
  unmigrated data in the two most recent stable releases, and a complete cleanup checklist.
- The cleanup checklist is exact and exhaustive: it names the code, tests, documentation, resources
  and solution items that exist only for the migration. An item that turns out to be permanent is
  removed from the checklist and explained, not silently kept.
- Before retiring a migration, the cleanup change must leave the solution warning-free, pass the
  headless tests, and pass `scripts/Validate-Repository.ps1`.
- Retiring a migration keeps the new steady-state behaviour untouched and removes only the
  transition.

## Registry and template

- [The migration registry](../migrations/README.md) is the registry. It defines the index, the entry
  template and the step-by-step instructions for adding, shipping and retiring an entry. Follow it;
  do not invent a local format for a new migration.
- Every migration is recorded once, with a `MIG-NNNN` id that stays stable for the life of the entry.
  References from `TODO.md`, code comments, architecture decisions and the compatibility contract use
  that id.
- The registry index row mirrors the entry status and links the entry and the user guide. Adding an
  entry includes updating the index.
- The registry and every entry are managed repository files: they are listed as solution items under
  their folder hierarchy.
- The registry is not published to the Wiki and does not replace the release changelog.

## Documentation

- When in-product support is removed, the Wiki guide is never deleted. It stays as historical
  documentation, marked historical, and only in-product references (warnings, notices, comments) are
  removed.
- The release section of `CHANGELOG.md` describes the migration and its removal plan; the changelog
  follows the [changelog policy](changelog.md).
- [Product scope](../FEATURES.md) records the compatibility contract of the current release, and
  [architecture decisions](../architecture.md) record the steady state after the migration. Neither
  may describe a transition that no longer exists.
- The manual checklist in `docs/testing/` verifies the migration in a live Revit and is cleaned up
  when support ends.
- Changes to these documents follow the [documentation policy](documentation.md).

The rest of the contributor workflow is defined by the [development policy](development.md).
