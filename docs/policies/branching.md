# Branching policy

Paths mentioned in this policy are relative to the repository root unless a Markdown link specifies otherwise.

The repository follows [Git Flow](https://nvie.com/posts/a-successful-git-branching-model/).
`main` always reflects a released state, `dev` integrates the changes for the next
release, and every other branch is short-lived. Branch names, base branches and merge
targets are a contract: the publish workflows, the tag-based versioning and the
changelog policy depend on them.

Where a repository still names its main branch `master`, read `main` as that branch.
The integration branch is always `dev`.

## Branches

| Branch | Created from | Merged into | Lifetime | Purpose |
| --- | --- | --- | --- | --- |
| `main` | - | - | permanent | Released state; every commit is a release and carries a version tag. |
| `dev` | `main` | - | permanent | Integration branch: the state of the next release. |
| `feature/<ticket>-<slug>` | `dev` | `dev` | days | New behaviour. |
| `fix/<ticket>-<slug>` | `dev` | `dev` | days | Defect that is not urgent in production. |
| `release/<version>` | `dev` | `main` and `dev` | hours | Release preparation: changelog, final fixes. |
| `hotfix/<version>` | `main` | `main` and `dev` | hours | Urgent production defect. |

`<ticket>` is the issue number when the repository has an issue tracker and is omitted
otherwise. `<slug>` is a short lowercase kebab-case description, for example
`feature/123-parameter-dependency-creator` or `fix/207-null-selection`. No other
prefixes (`bugfix/`, `topic/`, `wip/`): review and tooling expect the six branch kinds
above.

`main` and `dev` are permanent; every other branch is deleted after it is merged,
locally and on the remote.

## Rules

- Never commit to `main` or `dev` directly. Both branches are protected: a change
  arrives only through a pull request that passes review and CI.
- Merge with a merge commit (`--no-ff`), not with squash or rebase. The merge commit
  keeps the branch as one unit, so `git revert -m 1 <merge>` reverts the whole change.
  In the GitHub interface choose **Create a merge commit**.
- Keep one topic per branch. A branch that grows into a second unrelated change is
  split before review.
- Update a branch by merging its target branch into it. Do not rebase a branch that has
  been pushed and reviewed, and never force-push `main` or `dev`.
- Commit messages follow Conventional Commits, as described in the
  [commit message policy](commits.md).
- `dev` stays buildable. A change that breaks the build or the tests is fixed or
  reverted immediately, not at the end of the release.
- Update [CHANGELOG.md](../../CHANGELOG.md) as the [development policy](development.md)
  requires, in the same branch as the change.

## Feature and fix branches

```powershell
git switch dev
git pull --ff-only
git switch -c feature/123-parameter-dependency-creator
# work and commit
git push -u origin feature/123-parameter-dependency-creator
```

Open a pull request into `dev` and merge it with a merge commit. Then delete the branch:

```powershell
git branch -d feature/123-parameter-dependency-creator
git push origin --delete feature/123-parameter-dependency-creator
```

`fix/` branches work the same way and differ only in intent.

## Release branches

A release branch is cut when `dev` contains everything that should ship.

1. Create `release/<version>` from `dev`.
2. Prepare the release in it: compose the changelog section and commit it, as described
   in the [release changelog policy](development.md#release-changelog). Only fixes are
   allowed here; new features wait for the next release.
3. Open a pull request into `main` and merge it with a merge commit.
4. Tag the merge commit on `main` with the release version and push the tag. The publish
   workflow requires exactly one stable tag on the selected commit and refuses to run
   without it; the tag format is the one the repository already uses.
5. Merge `release/<version>` back into `dev`, so the release fixes are not lost, then
   delete the branch.

## Hotfix branches

A hotfix is the only way to change `main` outside a release. The branch lives minutes,
but it keeps the fix traceable, revertable and present in the next release.

1. Create `hotfix/<version>` from `main`.
2. Fix the defect and compose the changelog entry.
3. Open a pull request into `main`, merge it, tag the merge commit, publish.
4. Merge the branch into `dev` as well. Without this step the next release silently
   drops the fix or reintroduces the defect when `dev` is merged into `main`.
5. Delete the branch.

If a release branch is still open, merge the hotfix into it instead of `dev`; the fix
reaches `dev` when the release is finished.

## Protected branches

`main` and `dev` are protected in the repository settings:

- no direct pushes, no force-push, no deletion;
- a pull request with at least one approval is required;
- the `build-and-test` and `repository-policy` workflows must pass;
- only maintainers merge into `main`.

Release and hotfix pull requests keep the same checks but may use a single reviewer, so
the urgent path stays short.

## Adopting the policy

A repository that has only `main` gets its integration branch once:

```powershell
git switch main
git pull --ff-only
git switch -c dev
git push -u origin dev
```

Then protect both branches as described above.
