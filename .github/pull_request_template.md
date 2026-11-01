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

The [pull request policy](../docs/policies/pull-requests.md) defines the full checklist.

- [ ] Tests are added or updated when the change needs them; the test approval rule was followed.
- [ ] Build and tests for the affected projects pass, with no new compiler or analyzer warnings.
- [ ] `./scripts/Validate-Repository.ps1` passes after changing repository-level
  files, documentation or the solution.
- [ ] `CHANGELOG.md` is in the state the [changelog policy](../docs/policies/changelog.md) requires.
- [ ] User documentation and localized strings are updated when behaviour or text changes.
- [ ] Saved settings, configuration files and upgrade paths are reviewed when affected.
- [ ] Logging was reviewed for significant operations, refusals and failures.
