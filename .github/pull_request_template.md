## Summary

<!-- What changed and why. Link the issue when there is one. -->

## Branch

- [ ] The source branch follows the [branching policy](../docs/policies/branching.md):
  `feature/<ticket>-<slug>` or `fix/<ticket>-<slug>` from `dev`, `release/<version>`
  from `dev`, or `hotfix/<version>` from `main`.
- [ ] The pull request targets `dev` for a feature or a fix, and `main` for a release
  or a hotfix. A hotfix is also merged back into `dev` or into the open release branch.
- [ ] `main` and `dev` are not changed by a direct push.

## Checklist

- [ ] Build and tests for the affected projects pass.
- [ ] `./scripts/Validate-Repository.ps1` passes after changing repository-level
  files, documentation or the solution.
- [ ] `CHANGELOG.md` is updated when the development policy requires it.
