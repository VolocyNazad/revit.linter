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

## Lifecycle rules

The principles, the version window, the data-safety guarantees and the removal gate are defined by
the [migration policy](../policies/migrations.md). This registry records the entries, their status and
the operational steps for adding and retiring one.

## Status lifecycle

| Status | Meaning | Required updates |
|---|---|---|
| `Planned` | The migration is implemented or being implemented, the release that introduces it is not tagged yet | Entry and Wiki guide exist; the TODO removal task exists |
| `Active` | The introducing release is tagged and users can meet the migration | Entry records the actual release tag; release notes describe the migration and the removal plan |
| `Retired` | The removal gate was met and all migration-only artifacts are deleted | Entry records the release that removed support; the Wiki guide is kept and marked historical; the TODO task is closed |

Entries are edited while `Planned`, then only their status, dates and removal notes change. Never rewrite the
recorded history of a shipped migration.

## Index

| Id | Status | Migrates from → to | Introduced | Support removed | User guide |
|---|---|---|---|---|---|
| [MIG-0001](0001-single-configuration-file.md) | `Active` | product ≤ 2.0.0 (split YAML files) → configuration format 2 (`3.0.0`); the product moves the legacy files into `legacy` folders automatically | `3.0.0` | planned 4.0.0, gated | [Migrating configuration](../../wiki/Migrating%20configuration.md) (EN) · [Миграция конфигурации](../../wiki/ru/Миграция%20конфигурации.md) (RU) |

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
