# Wiki policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Read when: editing, adding or publishing user documentation.

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
  and reaches the wiki only when it lands in `main` - through a release or a back-merge.
- Do not edit the wiki in the browser. A manual page is overwritten at the next publish.
- The publish workflow is the only writer and commits as `github-actions[bot]`.
- Links to repository files are absolute `blob/main` URLs.
- The first page is created manually once; after that the workflow maintains it.
- When the wiki or its source is removed, the publish workflow is removed in the same change.

## Page conventions

- English pages live in the task-oriented folders directly under `wiki/`; Russian pages mirror that
  structure under `wiki/ru/`. Keep a page and its counterpart in step in the same change.
- `wiki/Home.md` is the language selector. Every localized page links to its counterpart and
  declares `lang: en` or `lang: ru` in YAML front matter.
- Page names must remain unique across both languages.
- Sources use Obsidian's `[[Target|Label]]` alias order. Inside Markdown tables, escape the alias
  separator as `\|`. `scripts/Prepare-Wiki.ps1` rejects malformed brackets, missing pages and
  incorrect explicit paths, and converts aliased links to GitHub Wiki's `[[Label|Target]]` order in
  a staging directory without changing the Obsidian sources.
- Keep shared content and navigation in Markdown.

## Enabling the wiki in a new repository

1. Enable Wiki in the repository settings.
2. Create the first page manually.
3. Add the source file or folder.
4. Add `publish-wiki.yml` that publishes from `main` only.
5. Link this policy from `AGENTS.md` and `README.md`.

The rest of the workflow is defined by the [development policy](development.md).
