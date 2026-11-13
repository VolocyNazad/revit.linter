## Summary

<!-- Merge the finished release branch back into dev. Name the version and the tag it produced. -->

## Branch

- [ ] The source branch is `release/<version>`, already merged into `main` and tagged, as the
  [branching policy](../../docs/policies/branching.md) requires.
- [ ] The pull request targets `dev`; this merge carries the release section of `CHANGELOG.md` and
  the fragments deleted from `changelog.d/`.
- [ ] `dev` is not changed by a direct push.

## Merge

- [ ] `## [Unreleased]` is empty on both `main` and `dev`.
- [ ] A fragment written on `dev` after the branch was cut survives the merge: it belongs to the
  next release, as the [changelog policy](../../docs/policies/changelog.md) requires.
- [ ] `dev` stays buildable after the merge.
- [ ] The branch is deleted after the merge.

## Checklist

The [pull request policy](../../docs/policies/pull-requests.md) defines the full checklist.

- [ ] Build and tests for the affected projects pass, with no new compiler or analyzer warnings.
- [ ] `./scripts/Validate-Repository.ps1` passes after changing repository-level
  files, documentation or the solution.
- [ ] User documentation and localized strings are updated when behaviour or text changes.
