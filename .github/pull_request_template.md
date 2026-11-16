<!-- Default template: a feature, fix or chore branch into dev. A release or hotfix pull request uses
     the matching template from .github/PULL_REQUEST_TEMPLATE/; the pull request policy lists them. -->

## Summary

<!-- What changed and why. Link the issue when there is one. -->

## Branch

- [ ] The source branch follows the [branching policy](../docs/policies/branching.md):
  `feature/<ticket>-<slug>`, `fix/<ticket>-<slug>` or `chore/<slug>`, cut from `dev`.
- [ ] The pull request targets `dev`. A `release/<version>` or `hotfix/<version>` pull request is
  opened with its own template instead of this one.
- [ ] `main` and `dev` are not changed by a direct push.

## Checklist

The [pull request policy](../docs/policies/pull-requests.md) defines the full checklist.

- [ ] Tests are added or updated when the change needs them; the test approval rule was followed.
- [ ] Build and tests for the affected projects pass, with no new compiler or analyzer warnings.
- [ ] `./scripts/Validate-Repository.ps1` passes after changing repository-level
  files, documentation or the solution.
- [ ] The changelog fragment is in `changelog.d/` in the state the
  [changelog policy](../docs/policies/changelog.md) requires.
- [ ] User documentation and localized strings are updated when behaviour or text changes.
- [ ] Saved settings, configuration files and upgrade paths are reviewed when affected.
- [ ] Logging was reviewed for significant operations, refusals and failures.
