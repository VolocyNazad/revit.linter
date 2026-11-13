## Summary

<!-- The production defect, its impact on users and the version that fixes it. Link the issue. -->

## Branch

- [ ] The source branch is `hotfix/<version>`, cut from `main`, as the
  [branching policy](../../docs/policies/branching.md) requires.
- [ ] The pull request targets `main` and contains only the fix for this defect.
- [ ] `main` is not changed by a direct push.

## Release

- [ ] A regression test fails without the fix and passes with it, as the
  [testing policy](../../docs/policies/testing.md) requires; the versions it was run on are
  recorded here.
- [ ] The full test matrix was executed before the tag, as the
  [testing policy](../../docs/policies/testing.md) and the
  [release policy](../../docs/policies/releases.md) require.
- [ ] `./scripts/Update-Changelog.ps1 -Compose` collected the fragments and the composed entries
  were edited as one narrative.
- [ ] `./scripts/Update-Changelog.ps1 -Finalize <version>` closed the section and deleted the
  fragments of `changelog.d/`.
- [ ] `## [Unreleased]` is empty and `changelog.d/` holds no fragment.
- [ ] The tag is created on the merge commit after this pull request is merged and the release is
  published, as the [release policy](../../docs/policies/releases.md) requires.
- [ ] The fix reaches `dev`, or the open release branch when there is one, through its own pull
  request; the branch is deleted after the merges.

## Checklist

The [pull request policy](../../docs/policies/pull-requests.md) defines the full checklist.

- [ ] Build and tests for the affected projects pass, with no new compiler or analyzer warnings.
- [ ] `./scripts/Validate-Repository.ps1` passes after changing repository-level
  files, documentation or the solution.
- [ ] User documentation and localized strings are updated when behaviour or text changes.
- [ ] Saved settings, configuration files and upgrade paths are reviewed when affected.
- [ ] Logging was reviewed for significant operations, refusals and failures.
