# Pull request policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Every change reaches `main` and `dev` through a short-lived branch and a pull request; direct pushes to either branch are blocked. The [branching policy](branching.md) owns the branch kinds, base branches, merge strategy and protection settings, and this policy covers what a pull request must target, which checks gate it, how it is reviewed and what its author verifies. The repository template is `.github/pull_request_template.md`; the `build-and-test` and `repository-policy` workflows provide the required checks.

## Source branch and target

| Source branch | Cut from | Pull request targets |
| --- | --- | --- |
| `feature/<ticket>-<slug>` | `dev` | `dev` |
| `fix/<ticket>-<slug>` | `dev` | `dev` |
| `release/<version>` | `dev` | `main` |
| `hotfix/<version>` | `main` | `main` |

- The branch names, their base branches and their lifetimes are defined in the [branching policy](branching.md); the table above is the pull request view of that contract.
- A hotfix is also merged back into `dev`, or into the open release branch when one exists; do this as part of the same hotfix work.
- Never push directly to `main` or `dev`. Every change, including a release or hotfix preparation, arrives through a pull request that passes review and CI.

## Required checks

- `build-and-test` must pass. It builds `Revit.Linter.slnx` for every supported `Release_*` configuration and runs the headless `*.Tests` executables; a separate job builds `updater/Revit.Linter.Updater.slnx` in `Release` and runs the headless tests under `updater/tests/`.
- `repository-policy` must pass. It runs `./scripts/Validate-Repository.ps1`, which checks the required files, navigation links and Solution Items.
- Merge only after both required workflows pass and the pull request has the required approval. A pending or failing required check blocks the merge.
- `build/Revit.Linter.Build.slnx` and `installer/Revit.Linter.Installer.slnx` are not built by pull request CI; the release publish compiles them, so build them locally when a change touches them.
- Release and hotfix pull requests keep the same required checks; only the reviewer count may differ, as described under [Review](#review).

## Review

- A pull request needs at least one approval.
- Only maintainers merge into `main`; a release or hotfix pull request may use a single reviewer, so the urgent path stays short.
- Merge with a merge commit (`--no-ff`, **Create a merge commit**), not with squash or rebase, so `git revert -m 1 <merge>` reverts the whole change.
- Update a branch by merging its target branch into it; do not rebase a branch that has been pushed and reviewed, and never force-push `main` or `dev`.

## Author checklist

Before opening a pull request, check every item that applies to the change:

- Assess whether automated tests or a manual Revit check are needed; add or update tests, follow the test approval rule in the [development policy](development.md), and use the manual smoke check from the [testing policy](testing.md) when the change affects the user interface or the add-in lifecycle. Built-in diagnostics follow [adding a built-in diagnostic](../diagnostics/adding.md).
- Build the affected projects and configurations; resolve new compiler and analyzer warnings in touched code. The release publish treats warnings as errors, so do not rely on pull request CI alone.
- Confirm the required CI checks pass before merging.
- Review compatibility with saved settings, configuration files and upgrade paths when the change affects them.
- Update user documentation and localized strings when user-visible behavior or text changes, as the [documentation policy](documentation.md) requires.
- Review logging for significant operations, refusals and failures when behavior changes.
- Update the changelog and repository documentation when the development policy requires it, following the [changelog policy](changelog.md).
- Run `./scripts/Validate-Repository.ps1` after changing repository-level files, documentation or the solution.
- Write commit messages as the [commit message policy](commits.md) requires.

## Release and hotfix pull requests

- A `release/<version>` pull request targets `main` and carries only fixes; new features wait for the next release. Compose and commit the release section of `CHANGELOG.md` in the release branch before the tag, as the [changelog policy](changelog.md) requires.
- A `hotfix/<version>` pull request targets `main`. Merge the same branch back into `dev`, or into the open release branch when there is one; without the back-merge the next release drops the fix.
- Release and hotfix pull requests keep the same required checks but may use a single reviewer.
- The tag is created on the merge commit on `main` after the pull request is merged; the branch and tag steps are in the [branching policy](branching.md) and the publishing steps in the [release policy](releases.md).
- Before publishing, confirm CI succeeds and inspect the release build, the installer and the published artifacts when the release pipeline completes.

## Template

A pull request uses `.github/pull_request_template.md` and keeps its three sections:

- **Summary** — what changed and why; link the issue when there is one.
- **Branch** — the source branch, its base and target, and confirmation that `main` and `dev` are not changed by a direct push.
- **Checklist** — the build and test result for the affected projects, `./scripts/Validate-Repository.ps1`, and the changelog update when the development policy requires it.

The Branch and Checklist items restate rules owned by the [branching policy](branching.md) and the [changelog policy](changelog.md); complete them for the change.

The branch contract is in the [branching policy](branching.md), the release section in the [changelog policy](changelog.md), tests and the manual smoke check in the [testing policy](testing.md), user-facing documentation in the [documentation policy](documentation.md), and the release mechanics in the [release policy](releases.md).

The rest of the contributor workflow is defined by the [development policy](development.md).
