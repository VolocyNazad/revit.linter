# Contributing

Before making a change, read the [development policy](docs/policies/development.md)
and the [repository guide](docs/repository.md). They define the repository-specific
stack, testing expectations and maintenance rules.

Keep changes focused and update [CHANGELOG.md](CHANGELOG.md) under `Unreleased`
when the development policy requires it. Before submitting a pull request, check
the items that apply to the change:

- Assess whether automated tests or a manual Revit check are needed; add or update
  tests when appropriate. Follow the test approval rule in the development policy.
- Build the affected projects and configurations. Resolve new compiler and
  analyzer warnings in touched code.
- Confirm relevant CI checks pass before merging.
- Review compatibility with saved settings, configuration files and upgrade paths
  when those are affected.
- Update user documentation and localized strings when user-visible behavior or
  text changes.
- Review logging for significant operations, refusals and failures when behavior
  changes; follow the [logging policy](docs/policies/development.md#logging).
- Update the changelog and repository documentation when required by the
  development policy.
- Run the repository policy validator after changing repository-level files,
  documentation or the solution:

```powershell
./scripts/Validate-Repository.ps1
```

## Before publishing a release

- Verify the release tag and version against the release commit.
- Move the user-facing entries from `Unreleased` into the versioned changelog
  section and leave `Unreleased` empty, as described in the
  [release changelog policy](docs/policies/development.md#release-changelog).
- Confirm CI succeeds and inspect the release build, installer and published
  artifacts when the release pipeline completes.
