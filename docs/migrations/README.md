# Migration registry

This folder is the single source of truth about **user-visible migrations**: which product version introduced
a breaking change, from and to which versions the migration applies, why it exists, what the product does
automatically and what the user must do by hand, and when the transitional behaviour is removed.

The registry complements the other documents instead of repeating them:

| Document | Answers |
|---|---|
| `docs/migrations/README.md` (this page) | Which migrations exist, how long they are supported, how they end |
| `docs/migrations/NNNN-*.md` | One migration: versions, reason, user action, safety guarantees, cleanup plan |
| `wiki/Migrating configuration.md` (EN) and `wiki/ru/Миграция конфигурации.md` (RU) | What the user actually does, step by step |
| `docs/architecture.md` | The steady state the product has after the migration |
| `docs/FEATURES.md` | The compatibility contract of the current release |
| `CHANGELOG.md` | What one release changed; announced to users in GitHub releases |
| `TODO.md` | Scheduled work, including the tracked removal task |
| `docs/testing/*-manual-checklist.md` | The manual checks that verify the migration in a live Revit |

The registry does not replace the release changelog and is not published to the Wiki: users read the Wiki
page and the release notes, contributors read this folder.

## Principles

1. **One entry per migration, append-only.** Entries are numbered (`0001`, `0002`, ...) and are never
   deleted or renumbered. A finished migration stays as history and gets the status `Retired`.
2. **A migration is a contract, not a code detail.** The entry names the product versions on both sides, the
   user action and the data-safety guarantees *before* the release ships them.
3. **No silent breaking change.** Every breaking migration reaches the user through the release notes plus an
   in-product signal (a warning, a notice, or both). A user who dismissed the warning must still find the
   migration explained in the release notes and in the Wiki guide.
4. **User data is never destroyed.** The product never deletes user-authored files and never rewrites their
   content. When a user-owned file must leave the place the product reads from, it is moved (for example into a
   `legacy` folder) or backed up, and the entry states where it went and how to restore it.
5. **Migration is manual unless the entry says otherwise.** Automatically rewriting user rules or settings is
   a separate, explicitly approved decision recorded in the entry.
6. **Rollback stays possible while support exists.** The entry states what remains on disk for a downgrade and
   what is already changed beyond it.
7. **Transitional code is disposable by design.** The entry lists every artifact that exists only for the
   migration, so removal is a checklist instead of archaeology.
8. **Support is time-boxed.** A migration is supported for the whole major line that introduced it, for at
   least 12 months and at least two stable releases. It is removed only in a later major release, and the
   removal is announced one stable release in advance.
9. **Removal is decided from evidence.** The signal is the in-product migration log plus support reports;
   support ends only when neither has indicated unmigrated data for two stable releases (see the entry's
   removal gate).
10. **Documentation outlives the code.** When the in-product support is removed, the Wiki guide stays as
    historical documentation for users who skip several versions; only the product-side mentions are removed.

## Status lifecycle

| Status | Meaning | Required updates |
|---|---|---|
| `Planned` | The migration is implemented or being implemented, the release that introduces it is not tagged yet | Entry and Wiki guide exist; the TODO removal task exists |
| `Active` | The introducing release is tagged and users can meet the migration | Entry records the actual release tag; release notes describe the migration and the removal plan |
| `Retired` | The removal gate was met and all migration-only artifacts are deleted | Entry records the release that removed support; the Wiki guide is kept and marked historical; the TODO task is closed |

Entries are edited while `Planned`, then only their status, dates and removal notes change. Never rewrite the
recorded history of a shipped migration.

## Version rules

* The product follows SemVer for user-visible breaking changes, as recorded in
  [the repository guide](../repository.md#versioning-and-release-tags). A migration is introduced by the
  release that ships the break; the entry names that release.
* Support stays for **every release of the major line** that introduced the migration.
* Removal happens in the **first major release after that line**, never earlier than 12 months and two stable
  releases after the introduction, and never without a release-note announcement in the previous stable
  release.
* If the planned release number changes or the release slips, update the entry in the same commit that prepares
  the release; a tagged entry is historical.
* Migrations that shipped before this registry existed are not backfilled. Only migrations that still have
  in-product support are recorded.

## Index

| Id | Status | Migrates from → to | Introduced | Support removed | User guide |
|---|---|---|---|---|---|
| [MIG-0001](0001-single-configuration-file.md) | `Active` | product ≤ 2.0.0 (split YAML files) → configuration format 2 (`3.0.0`); the product moves the legacy files into `legacy` folders automatically | `3.0.0` | planned 4.0.0, gated | [Migrating configuration](../../wiki/Migrating%20configuration.md) |

Statuses in this table are repeated in every entry; the table is the quick overview, the entry is the detail.

## Adding an entry

1. Copy the template below into `docs/migrations/<next number>-<kebab-case-title>.md` and fill every field.
   Mark the status `Planned`.
2. Decide and write down the four things reviewers will challenge: source version, target version, the exact
   user action, and the data-safety guarantees.
3. Write or update the user-facing Wiki guide (both languages) and link it from the entry.
4. Add the entry to the [index](#index) and to the solution items
   (`docs/migrations/` folder in `Revit.Linter.slnx`); `scripts/Validate-Repository.ps1` fails otherwise.
5. Add the removal task to `TODO.md`, naming the entry id and the target release.
6. At release: set the status to `Active`, record the actual tag, mention the migration in the release section
   of `CHANGELOG.md`, and confirm the Wiki guide matches what shipped.
7. Add the manual verification of the migration to `docs/testing/` when the workflow needs a live Revit; add
   headless tests for detection and data safety.

## Entry template

```markdown
# NNNN — <human title>

| Field | Value |
|---|---|
| Id | `MIG-NNNN` |
| Status | `Planned` / `Active` / `Retired` |
| Kind | Example: breaking configuration format |
| Migrates from | The last product version(s) that used the old behaviour |
| Migrates to | The first product version(s) that provide the new behaviour |
| Introduced | Planned or actual release tag |
| User action | `manual` or `automatic` (with the reason) |
| User guide | Link to the Wiki page in both languages |
| Removal target | Planned release plus the removal gate summary |
| Tracking | The `TODO.md` removal task and this entry |

## Why

The problem, the alternatives that were rejected, and why a breaking change was accepted.

## Who is affected

Which users meet the migration, and which users are unaffected. Name clean installs and multi-version
installations explicitly.

## What the product does automatically

Each automatic action with its data-safety guarantee.

## What the user must do

The manual steps, summarized; the Wiki guide holds the details.

## Data safety and rollback

What is never touched, what is backed up, what a downgrade still sees, and what a downgrade loses.

## Verification

What must be true after a correct migration, including headless tests and the manual checklist section.

## Removal gate

The objective conditions under which support may be removed.

## Cleanup checklist

Every artifact that exists only for the migration, grouped by code, tests, documentation and solution items.

## What stays after removal

The new behaviour, the documentation that remains valid, and the historical Wiki guide.

## References

Links to the architecture decision, compatibility contract, user guide and manual checklist.
```

## Retiring an entry

1. Check the removal gate in the entry. If any condition is unmet, keep the status `Active`.
2. Delete the artifacts listed in the cleanup checklist; keep the new behaviour untouched.
3. Keep the Wiki guide, mark it historical if the current format documentation makes it confusing, and remove
   only in-product references (warnings, notices, comments).
4. Update the compatibility contract in `docs/FEATURES.md` and the architecture decision in
   `docs/architecture.md` so neither mentions a transition that no longer exists.
5. Set the status to `Retired`, record the release that removed support and close the `TODO.md` task.
6. Describe the removal in the release section of `CHANGELOG.md`; users of the oldest supported version must
   still be able to follow the historical Wiki guide.
