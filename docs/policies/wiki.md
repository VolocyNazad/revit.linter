# Wiki policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

The GitHub wiki is a published mirror of documentation that lives in this repository, not a
second source. It is never edited on GitHub: the next publish replaces the pages.

## Source of truth

A repository uses one of two conventions:

- **Single page**: `docs/WIKI.md` is published as `Home`.
- **Page per file**: `wiki/**/*.md` is published one page per file, named without the extension.

This repository uses **page per file**: every `wiki/**/*.md` becomes a wiki page named after the file; `scripts/Prepare-Wiki.ps1` validates the `[[Page]]` and `[[Label|Page]]` links before publishing.

## Rules

- Only `main` publishes. The workflow triggers on a push to `main` and on manual dispatch, and
  refuses any other ref; `dev` and feature branches never write to the wiki.
- A wiki change starts in the source file on a feature branch, is reviewed and merged to `dev`,
  and reaches the wiki only when it lands in `main` - through a release or a back-merge. The
  wiki follows `main`, not `dev`.
- Do not edit the wiki in the browser. A manual page is overwritten at the next publish, and the
  change is lost without a trace.
- The publish workflow is the only writer and commits as `github-actions[bot]`.
- Links to repository files are absolute `blob/main` URLs, because a wiki page has no relative
  path back to the repository.
- The first page is created manually once, because GitHub does not create the wiki repository
  until then; after that the workflow maintains it.
- When the wiki or its source is removed, the publish workflow is removed in the same change.

## Enabling the wiki in a new repository

1. Enable Wiki in the repository settings.
2. Create the first page manually.
3. Add the source file or folder.
4. Add `publish-wiki.yml` that publishes from `main` only.
5. Link this policy from `AGENTS.md` and `README.md`.
The rest of the workflow is defined by the [development policy](development.md).
