## Summary

<!-- Merge the finished hotfix back into dev. Name the version and the tag it produced. -->

## Branch

- [ ] The source branch is `hotfix/<version>`, already merged into `main` and tagged, as the
  [branching policy](../../docs/policies/branching.md) requires.
- [ ] The pull request targets `dev`. When a release branch was open, the hotfix went into it
  instead and this pull request is not needed: the fix reaches `dev` with the release.
- [ ] `dev` is not changed by a direct push.

## Merge

- [ ] `## [Unreleased]` is empty on both `main` and `dev`.
- [ ] The fragments deleted on the hotfix branch are deleted on `dev` too, as the
  [changelog policy](../../docs/policies/changelog.md) requires.
- [ ] `dev` stays buildable after the merge.
- [ ] The branch is deleted after the merge.

## Checklist

The [pull request policy](../../docs/policies/pull-requests.md) defines the full checklist.

- [ ] Build and tests for the affected projects pass, with no new compiler or analyzer warnings.
- [ ] `./scripts/Validate-Repository.ps1` passes after changing repository-level
  files, documentation or the solution.
