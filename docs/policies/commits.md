# Commit message policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Every commit message follows [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/):
`<type>(<scope>): <description>`. The message is read by people and by tooling: the type
selects the changelog category, and `feat` and `fix` drive the version bump.

## Types

| Type | Use for |
| --- | --- |
| `feat` | New behaviour visible to a consumer of the library or add-in. |
| `fix` | A defect fix. |
| `docs` | Documentation only. |
| `style` | Formatting, whitespace, unused usings - no change in behaviour. |
| `refactor` | Code reshaped without new behaviour and without a defect fix. |
| `perf` | A change that makes the code faster or lighter. |
| `test` | Adding or updating tests. |
| `build` | Build system, dependencies, package versions. |
| `ci` | Continuous integration and delivery configuration. |
| `chore` | Routine work outside source and tests: configs, ignore files, release scripts. |
| `revert` | Rolling back an earlier commit. |

## Rules

- `<scope>` is optional but preferred when it clarifies what changed; use lowercase
  kebab-case (`feat(manifest): ...`, `fix(list): ...`).
- Write the description in the imperative mood, lowercase, without a trailing period:
  `fix(list): keep the selected row after a refresh`.
- Keep the subject within 72 characters and put the reasoning in the body.
- Mark a breaking change with `!` after the type or the scope and describe it in a
  `BREAKING CHANGE:` footer.
- Write messages in English.
- A revert names the commit it reverts, with the hash in the body:
  `revert: keep the selected row after a refresh`.
- Merge commits created by GitHub are not validated; do not rewrite them.
- Keep one logical change per commit; do not mix a refactor with a behaviour change.

## Examples

```
feat(manifest): read the vendor from the .addin file
fix(list): keep the selected row after a refresh
docs(policy): describe the release changelog
refactor(launcher): move the splitter handler into the observable block
feat(sdk)!: drop the Revit 2021 target

BREAKING CHANGE: the SDK no longer builds for Revit 2021.
```

The rest of the workflow - changelog, tests and the build - is defined by the
[development policy](development.md). Branches and merge rules are in the
[branching policy](branching.md).
