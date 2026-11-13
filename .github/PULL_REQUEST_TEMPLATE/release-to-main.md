## Summary

<!-- The version that ships, what is in it, and a link to the release section of CHANGELOG.md. -->

## Branch

- [ ] The source branch is `release/<version>`, cut from `dev`, as the
  [branching policy](../../docs/policies/branching.md) requires.
- [ ] The pull request targets `main` and carries only fixes; a new feature waits for the next
  release.
- [ ] `main` is not changed by a direct push.

## Release

- [ ] The full test matrix was executed and its result is recorded in this pull request, as the
  [testing policy](../../docs/policies/testing.md) requires.
- [ ] `./scripts/Update-Changelog.ps1 -Compose` collected the fragments and the composed entries
  were edited as one narrative.
- [ ] `./scripts/Update-Changelog.ps1 -Finalize <version>` closed the section and deleted the
  fragments of `changelog.d/`.
- [ ] `## [Unreleased]` is empty and `changelog.d/` holds no fragment.
- [ ] The tag is created on the merge commit after this pull request is merged and the release is
  published, as the [release policy](../../docs/policies/releases.md) requires.
- [ ] The release branch is merged back into `dev` and deleted after the merge.

## Checklist

The [pull request policy](../../docs/policies/pull-requests.md) defines the full checklist.

- [ ] Build and tests for the affected projects pass, with no new compiler or analyzer warnings.
- [ ] `./scripts/Validate-Repository.ps1` passes after changing repository-level
  files, documentation or the solution.
- [ ] User documentation and localized strings are updated when behaviour or text changes.
- [ ] Saved settings, configuration files and upgrade paths are reviewed when affected.
- [ ] Logging was reviewed for significant operations, refusals and failures.
