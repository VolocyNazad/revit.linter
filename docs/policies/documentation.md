# Documentation policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

The repository documents what the product does, why the repository is arranged the way it is, and how to change it. Every document has one genre and one subject, so each fact has one home and the rest of the repository links to it instead of restating it. This policy defines which document owns which fact, when documentation changes, and how it is written.

## Documents and genres

| Document | Genre | Answers |
|---|---|---|
| `docs/policies/*.md` (index: [repository policies](README.md)) | Binding rules | What a change must do in one area |
| [Repository guide](../repository.md) | Facts about the repository | How the solution, projects, tooling and documents are arranged |
| [Architecture decisions](../architecture.md) | Decisions | Which stable module boundaries and design rules the product keeps |
| [Migration registry](../migrations/README.md) and its entries | Registry | Which user-visible migrations exist, their versions, data safety and removal plan |
| [Adding a built-in diagnostic](../diagnostics/adding.md) and `docs/testing/*.md` | Procedures and checklists | How to add a diagnostic; how to verify a workflow by hand in a live Revit |
| [Product scope](../FEATURES.md) | Product contract | Which scope and compatibility contract the current release promises |
| [TODO.md](../../TODO.md) | Plan | Agreed future work that is not implemented, including tracked removal tasks |
| `wiki/` | User documentation | What a user does, step by step, and what the product offers |
| [GLOSSARY.md](../../GLOSSARY.md) | Vocabulary | Which terms the code, documents and tests share |
| [AGENTS.md](../../AGENTS.md) | Entry point | Which documents a contributor must read before changing the repository |
| [CONTRIBUTING.md](../../CONTRIBUTING.md) | Contributor workflow | How a change is prepared, checked and submitted |
| [CHANGELOG.md](../../CHANGELOG.md) | Release history | What one release changed (see the [changelog policy](changelog.md)) |

The genre decides the content. A policy states a rule and its boundary; the repository guide records facts about the structure and the stack; the architecture document records decisions and their consequences; a registry entry records a contract with versions, safety guarantees and a removal plan; a guide records a procedure step by step. Do not move a rule into a guide, and do not record a procedure as a policy rule.

## Single source of truth

- Each fact, rule or procedure has exactly one owning document. Another document that needs it links to it.
- Do not copy a section into a second document. If the same fact must be visible in two places, keep the full text in the owning document and link to it from the other one.
- When a section is renamed, moved or split, update every document that references it in the same change. Never leave a link pointing at a heading that no longer exists.
- A guide may summarize a policy or an entry, but the summary links to the owning document and does not become a second rule.

## Updating documentation in the same change

- A change that affects the folder structure, the technology stack, a dependency, a recorded version or a new convention updates the relevant policy and the [repository guide](../repository.md) in the same change, never as a later follow-up.
- A change that alters product behaviour updates the summary in [Product scope](../FEATURES.md) and the affected Wiki pages in the same change.
- A change that creates, revises or ends a migration updates its registry entry in the same change; the [migration policy](migrations.md) defines the entry's obligations.
- A change that introduces or revises a fundamental architectural decision updates [Architecture decisions](../architecture.md) in the same change.
- A change that adds or changes a built-in diagnostic updates its descriptions and messages in both languages and the documentation tables named by the diagnostic procedure, together with the diagnostic itself.
- A change that adds or removes a user-visible string updates the localized resources and the affected Wiki page in both languages.
- A change covered by the [changelog policy](changelog.md) updates `CHANGELOG.md` in the way that policy requires.
- A new documentation file is added as a solution item together with the file; the [repository-maintenance policy](repository-maintenance.md) defines the solution items contract.
- Documentation is not deferred. "Document later" leaves the repository describing a state that no longer exists.

## XML documentation

- XML documentation is mandatory for public APIs in production projects; test projects are exempt.
- Documentation explains the observable contract rather than restating the identifier.
- Use `<remarks>` for behaviour that is not evident from the signature: implicit fallback, inferred defaults, side effects, ordering, caching, thread or Revit-context requirements, transaction ownership and intentionally ignored failures.
- Keep the note next to the affected API or configuration setting. Do not rely on a distant architecture document as the only explanation.
- If a type is public only accidentally and has no external or cross-project consumer, reduce its visibility instead of documenting it as an API.
- Run the strict audit with `dotnet build Revit.Linter.slnx --configuration <configuration> -p:EnforcePublicApiDocumentation=true` and, for the updater, `dotnet build updater/Revit.Linter.Updater.slnx --configuration Release -p:EnforcePublicApiDocumentation=true`. The switch promotes `CS1591` to an error.
- Keep the switch opt-in until the existing public API baseline is documented, then make strict enforcement the default.
- The shared documentation settings live in the root `Directory.Build.props` and `Directory.Build.targets`; the updater build unit repeats them in `updater/Directory.Build.props`.

## Documentation language

- Policies and guides are written in English. This covers every file in `docs/policies/` and every contributor guide in `docs/`.
- User documentation is bilingual: English pages live under `wiki/` and their Russian counterparts under `wiki/ru/`, and both copies change together.
- The two exceptions are `docs/diagnostics/adding.md` and `docs/testing/diagnostics-manual-checklist.md`, which are still written in Russian. They are translated when their section is next edited substantially; no other document is exempt and no additional exception is introduced.
- Identifiers follow the naming rules of the [development policy](development.md); documentation uses the terms recorded in [GLOSSARY.md](../../GLOSSARY.md).

## Links and anchors

- Link between repository documents with relative Markdown links. Do not use absolute repository URLs in repository documents; the Wiki policy defines how published pages link back to repository files.
- An anchor must point at a heading that exists. When a heading is renamed or moved, update every referencing link and anchor in the same change.
- Do not leave a broken or ambiguous link. If a target no longer exists, update the text instead of deleting only the link.
- `scripts/Validate-Repository.ps1` checks the required repository files, the navigation links and their anchors, and the completeness of the solution items. The [repository-maintenance policy](repository-maintenance.md) defines when it must run.

## Glossary

- [GLOSSARY.md](../../GLOSSARY.md) is the single source of the shared product vocabulary. It names product concepts, not the code that implements them.
- A new term, or a change to an existing term, is made in the same change that introduces or renames the concept, and every usage across code, documentation and messages is updated with it.
- Documentation, messages and tests use the glossary term and never the variants listed under `_Avoid_:` in the glossary entry.
- The `_Avoid_` list is part of the term's definition: adding a synonym there forbids it in new text, so a term is not left with an obsolete `_Avoid_` entry after a rename.

## Wiki

User documentation is written in the `wiki/` sources and published according to the [wiki policy](wiki.md). That policy owns the page conventions — the English and Russian mirror, language front matter, unique page names, the Obsidian link order, the committed contributor vault and the publication conversion — and the repository guide records the folder layout. This policy adds one rule to it: a change that alters user-visible behaviour or text updates the affected Wiki sources in both languages in the same change.

## Adding a policy document

A new policy follows the procedure in the [policy index](README.md): one file per subject in `docs/policies/`, a row in the index, a solution item, and a link from the place that needs the rule rather than a second copy of the list. Write it in the same form as the other policies — the paths sentence, the rules, and the closing pointer to the [development policy](development.md) — and run `./scripts/Validate-Repository.ps1`; the [repository-maintenance policy](repository-maintenance.md) owns the solution items and validator contract.

The Wiki publication rules are in the [wiki policy](wiki.md), the release history rules in the [changelog policy](changelog.md), and the solution items, validator and formatting rules in the [repository-maintenance policy](repository-maintenance.md).

The rest of the contributor workflow is defined by the [development policy](development.md).
