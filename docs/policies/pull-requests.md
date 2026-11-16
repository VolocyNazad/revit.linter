# Pull request policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

Read when: preparing, reviewing or merging a pull request.

Every change reaches `main` and `dev` through a short-lived branch and a pull request. The
[branching policy](branching.md) owns branch kinds, base branches, merge strategy and protection
settings. The templates are under `.github/` and the default is `.github/pull_request_template.md`;
the required checks are `CI gate` from the `build-and-test` workflow and the `repository-policy`
workflow.

## Source branch and target

| Source branch | Cut from | Pull request targets |
| --- | --- | --- |
| `feature/<ticket>-<slug>` | `dev` | `dev` |
| `fix/<ticket>-<slug>` | `dev` | `dev` |
| `chore/<slug>` | `dev` | `dev` |
| `release/<version>` | `dev` | `main` |
| `hotfix/<version>` | `main` | `main` |

- A documentation-only change is a `chore/<slug>` branch that targets `dev`, as the
  [branching policy](branching.md#branches) defines; there is no separate `docs/` branch kind.
- Branch names, base branches and lifetimes are defined in the [branching policy](branching.md); the
  table is the pull request view of that contract.
- A hotfix is also merged back into `dev`, or into the open release branch when one exists.
- Never push directly to `main` or `dev`. Every change, including a release or hotfix preparation,
  arrives through a pull request that passes review and CI.

## Required checks

- `CI gate` must pass. It aggregates the `build-and-test` matrix, the `build-units` job and the
  updater job: every supported `Release_*` configuration is built with warnings as errors and tested
  through `scripts/Invoke-HeadlessTests.ps1`, `build/Revit.Linter.Build.slnx` and the installer
  project are compiled, and the updater solution is built and tested. A documentation-only pull
  request whose files are all under `docs/` or `wiki/` or are Markdown skips the Windows jobs; the
  gate stays green.
- `repository-policy` must pass. It runs `./scripts/Validate-Repository.ps1` for the required files,
  navigation links and Solution Items, action pins and Revit version matrix consistency,
  `./scripts/Update-Changelog.ps1 -Verify` for the pending changelog fragments, and
  `./scripts/Verify-Release.ps1` for the `CHANGELOG.md` structure and release inputs on every push
  and pull request.
- Merge only after both required workflows pass and the pull request has the required approval.
- The security workflows (`CodeQL` and `Dependency review`) report findings but are not required
  checks.
- Release and hotfix pull requests keep the same required checks; only the reviewer count may differ,
  as described under [Review](#review).

## Review

- A pull request needs at least one approval.
- Only maintainers merge into `main`; a release or hotfix pull request may use a single reviewer.
- Merge with a merge commit (`--no-ff`, **Create a merge commit**), not with squash or rebase;
  `git revert -m 1 <merge>` reverts the whole change.
- Update a branch by merging its target branch into it; do not rebase a branch that has been pushed
  and reviewed, and never force-push `main` or `dev`.

## Author checklist

Before opening a pull request, check every item that applies to the change:

- Assess whether tests or a manual Revit check are needed; add or update tests, follow the test
  approval rule in the [development policy](development.md), and use the manual smoke check from
  the [testing policy](testing.md) for user-interface or add-in lifecycle changes. Built-in
  diagnostics follow [adding a built-in diagnostic](../diagnostics/adding.md).
- Build the affected projects and configurations; resolve new compiler and analyzer warnings in
  touched code. The release publish treats warnings as errors.
- Review compatibility with saved settings, configuration files and upgrade paths when the change
  affects them.
- Update user documentation and localized strings when user-visible behavior or text changes, as the
  [documentation policy](documentation.md) requires.
- Review logging for significant operations, refusals and failures when behavior changes.
- Add the changelog fragment in `changelog.d/` and update the repository documentation when the
  development policy requires it, following the [changelog policy](changelog.md).
- Run `./scripts/Validate-Repository.ps1` after changing repository-level files, documentation or the
  solution.
- Write commit messages as the [commit message policy](commits.md) requires.

## Release and hotfix pull requests

- A `release/<version>` pull request targets `main` and carries only fixes; new features wait for the
  next release. Compose, edit and finalize the release section with `./scripts/Update-Changelog.ps1`
  in the release branch before the tag, as the [changelog policy](changelog.md) requires.
- A `hotfix/<version>` pull request targets `main`. Merge the same branch back into `dev`, or into
  the open release branch when there is one.
- The tag is created on the merge commit on `main` after the pull request is merged; the branch and
  tag steps are in the [branching policy](branching.md) and the publishing steps in the [release
  policy](releases.md).
- Before publishing, confirm CI succeeds and inspect the release build, the installer and the
  published artifacts when the release pipeline completes.

## Template

Every pull request starts from a template under `.github/`. GitHub fills the body from the default
file `.github/pull_request_template.md`; the four files in `.github/PULL_REQUEST_TEMPLATE/` cover the
release and hotfix pull requests:

| Pull request | Template file |
| --- | --- |
| `feature/`, `fix/` or `chore/` into `dev` | `pull_request_template.md`, applied automatically |
| `release/<version>` into `main` | `PULL_REQUEST_TEMPLATE/release-to-main.md` |
| `release/<version>` back into `dev` | `PULL_REQUEST_TEMPLATE/release-to-dev.md` |
| `hotfix/<version>` into `main` | `PULL_REQUEST_TEMPLATE/hotfix-to-main.md` |
| `hotfix/<version>` back into `dev` | `PULL_REQUEST_TEMPLATE/hotfix-to-dev.md` |

GitHub offers no picker for the others: select one with the `template` query parameter on the
comparison URL, which replaces the body GitHub filled in.

```text
https://github.com/VolocyNazad/revit.linter/compare/<target>...<source-branch>?quick_pull=1&template=<template-file>
```

The parameter takes the file name alone, without the directory: a `hotfix/1.2.4` branch opened
against `main` uses `.../compare/main...hotfix/1.2.4?quick_pull=1&template=hotfix-to-main.md`. GitHub
fills the body from the template that exists on the branch the pull request targets: `dev` has these
files once the template set is merged, `main` after the next release or hotfix merge. Until `main`
has them, paste the matching template into the body by hand.

Every template keeps the same three sections; a release or hotfix template adds one section of its
own before the checklist:

- **Summary** — what changed and why; link the issue when there is one.
- **Branch** — the source branch, its base and target, and confirmation that `main` and `dev` are
  not changed by a direct push.
- **Release** or **Merge** — the one extra section of a release or hotfix template. `Release`, in
  the template into `main`, carries the changelog composition and finalize, the test matrix before
  the tag, the tag on the merge commit and the follow-up merges. `Merge`, in the template back into
  `dev`, carries what that merge takes over.
- **Checklist** — the build and test result for the affected projects,
  `./scripts/Validate-Repository.ps1`, and the changelog fragment for a change that carries one. A
  merge back into `dev` carries the deleted fragments instead and has none of its own.

The Branch and Checklist items restate rules owned by the [branching policy](branching.md) and the
[changelog policy](changelog.md); complete them.

The rest of the contributor workflow is defined by the [development policy](development.md).
