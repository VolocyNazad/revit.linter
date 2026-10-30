# Changelog policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

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

## Release

Before creating a release tag:

1. Compose the `## [VERSION] - YYYY-MM-DD` section directly below `## [Unreleased]`,
   where `VERSION` is the tag without a leading `v`.
2. Leave `## [Unreleased]` empty.
3. Commit that change and create the tag on that commit, so the changelog at the tagged
   commit names the released version and carries no unreleased entries.

The publish workflow enforces this before building: it refuses to publish when
`## [Unreleased]` contains an entry or when the changelog has no section for the tagged
version. When the publish workflow also creates a GitHub release, the version section is
used verbatim as the release description.

The rest of the workflow - tests, build and the message format - is defined by the
[development policy](development.md) and the [commit message policy](commits.md).
Release branches and tags are in the [branching policy](branching.md).
