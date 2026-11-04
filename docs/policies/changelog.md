# Changelog policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Read when: adding a changelog entry or composing a release section.

`CHANGELOG.md` follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): the newest
release comes first, `## [Unreleased]` stays on top, and every release has one
`## [VERSION] - YYYY-MM-DD` section. Empty category headings under `Unreleased` are
tolerated.

## Entries

A change is described in `CHANGELOG.md` as part of the same change, not in a separate
follow-up. The repository uses one of two modes consistently:

- **Entry per change**: add the entry under `## [Unreleased]` in the matching category
  (`Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`).
- **Compose at release**: keep `## [Unreleased]` empty and compose the section when the
  release is prepared, from the changes since the previous stable tag.

[CONTRIBUTING.md](../../CONTRIBUTING.md) states which mode applies to the repository.

Write for the consumer of the release: keep changes a consumer can observe, merge related
entries, and drop fixes for behaviour that was never released.

## Release history window

`CHANGELOG.md` keeps `## [Unreleased]` and every release of the current major version. Older
releases move to the [changelog archive](../changelog/archive.md): the archive preserves history
and is not context — read it only when a historical detail is needed.

When a new major version is released, move the previous major version's sections to the archive
in the same change and update the archive link near the top of `CHANGELOG.md`. The archive keeps
releases newest first: insert the moved sections above the entries already there. It is add-only:
existing entries are never rewritten or reordered, and a factually wrong entry is corrected in
place.

A release always belongs to the current major line: a defect found in an older major line is fixed
in a release of the current one, not in a patch of the archived line. Supporting a maintenance
line is a separate decision that changes this section, the versioning configuration and the
release channels together.

## Release

Before creating a release tag:

1. Compose the `## [VERSION] - YYYY-MM-DD` section directly below `## [Unreleased]`,
   where `VERSION` is the tag without a leading `v`.
2. Leave `## [Unreleased]` empty.
3. Commit that change and create the tag on that commit, so the changelog at the tagged
   commit names the released version and carries no unreleased entries.

`./scripts/Verify-Release.ps1` enforces this: `repository-policy` runs its structural checks on
every push and pull request, and the publish workflow runs its tag checks before building. Both
refuse to continue when `## [Unreleased]` contains an entry, when a release heading has no valid
date, when versions are duplicated or out of order, or when the changelog has no section for the
tagged version. When the publish workflow also creates a GitHub release, the version section is
used verbatim as the release description.

The same script checks the release history window: every release section belongs to the current
major line, the archive holds only older releases in newest-first order, and the two files do not
repeat a version.

The rest of the workflow - tests, build and the message format - is defined by the
[testing policy](testing.md), [build and toolchain policy](build.md) and the
[commit message policy](commits.md). Release branches and tags are in the
[branching policy](branching.md), and the release workflow in the [release policy](releases.md).

The rest of the contributor workflow is defined by the [development policy](development.md).
